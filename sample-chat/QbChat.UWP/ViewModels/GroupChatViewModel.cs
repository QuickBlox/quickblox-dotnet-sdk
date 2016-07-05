using QbChat.Pcl.Helpers;
using QbChat.Pcl.Repository;
using QbChat.UWP.Views;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.Modules.UsersModule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;
using Xmpp.Im;

namespace QbChat.UWP.ViewModels
{
    public class GroupChatViewModel : BaseChatViewModel
    {
        private GroupChatManager groupChatManager;
        private AsyncLock lockAddingObject = new AsyncLock();

        public GroupChatViewModel(string dialogId) : base (dialogId)
        {
            this.OpenChatInfoCommand = new RelayCommand(this.OpenChatInfoCommandExecute);
            base.SendMessageCommand = new RelayCommand(this.SendMessageCommandExecute);

            if (string.IsNullOrEmpty(dialogId))
                return;

            var dialog = Database.Instance().GetDialog(dialogId);
            if (dialog == null)
                return;

            DialogName = dialog.Name;

            groupChatManager = App.QbProvider.GetXmppClient().GetGroupChatManager(dialog.XmppRoomJid, dialogId);
            groupChatManager.MessageReceived += OnMessageReceived;
        }

        public RelayCommand OpenChatInfoCommand { get; private set; }

        public override async void OnAppearing()
        {
            base.OnAppearing();

            if (string.IsNullOrEmpty(dialogId))
                return;

            var dialog = Database.Instance().GetDialog(dialogId);
            if (dialog == null)
                return;
            
            this.IsBusy = true;

            ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/groupholder.png"));

            var users = await App.QbProvider.GetUsersByIdsAsync(dialog.OccupantIds);
            this.opponentUsers = new List<User>(users.Where(u => u.Id != App.QbProvider.UserId));
            
            await this.LoadMessages();

            this.IsBusy = false;
        }

        public async Task LoadMessages()
        {
            List<Message> messages;
            List<MessageTable> chatMessages = new List<MessageTable>();
            try
            {
                messages = await App.QbProvider.GetMessagesAsync(dialogId);
            }
            catch (Exception ex)
            {
                new MessageDialog(ex.ToString(), "Error").ShowAsync();
                return;
            }

            using (await lockAddingObject.LockAsync())
            {
                if (messages != null)
                {
                    messages = messages.OrderBy(message => message.DateSent).ToList();
                    foreach (var message in messages)
                    {
                        var chatMessage = new MessageTable();
                        chatMessage.DateSent = message.DateSent;
                        chatMessage.SenderId = message.SenderId;
                        chatMessage.MessageId = message.Id;
                        if (message.RecipientId.HasValue)
                            chatMessage.RecepientId = message.RecipientId.Value;
                        chatMessage.DialogId = message.ChatDialogId;
                        chatMessage.IsRead = message.Read == 1;

                        await this.SetRecepientName(chatMessage);

                        if (message.NotificationType == NotificationTypes.GroupCreate ||
                            message.NotificationType == NotificationTypes.GroupUpdate)
                        {
                            if (message.AddedOccupantsIds.Any())
                            {
                                var userIds = new List<int>(message.AddedOccupantsIds);
                                userIds.Add(message.SenderId);

                                var users = await App.QbProvider.GetUsersByIdsAsync(string.Join(",", userIds));

                                var addedUsers = users.Where(u => u.Id != message.SenderId);
                                var senderUser = users.First(u => u.Id == message.SenderId);
                                chatMessage.Text = senderUser.FullName + " added users: " +
                                                   string.Join(",", addedUsers.Select(u => u.FullName));
                            }
                            else if (message.DeletedOccupantsIds.Any())
                            {
                                var userIds = new List<int>(message.DeletedOccupantsIds);
                                var users = await App.QbProvider.GetUsersByIdsAsync(string.Join(",", userIds));
                                chatMessage.Text = string.Join(",", users.Select(u => u.FullName)) + " left this room";
                            }
                        }
                        else
                        {
                            chatMessage.Text = System.Net.WebUtility.UrlDecode(message.MessageText);
                        }

                        chatMessages.Add(chatMessage);
                    }
                }

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                           () =>
                           {
                               this.Messages.Clear();
                               foreach (var item in chatMessages)
                               {
                                   this.Messages.Add(item);
                               }

                               var page = App.NavigationFrame.Content as GroupChatPage;
                               if (page != null)
                                   page.ScrollList();
                           });
            }
        }

        private async void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            if (messageEventArgs.MessageType == MessageType.Chat ||
                messageEventArgs.MessageType == MessageType.Groupchat)
            {
                var message = this.Messages.FirstOrDefault(m => m.MessageId == messageEventArgs.Message.Id);
                if (message != null)
                    return;

                using (await lockAddingObject.LockAsync())
                {
                    string decodedMessage = System.Net.WebUtility.UrlDecode(messageEventArgs.Message.MessageText);

                    var messageTable = new MessageTable();
                    messageTable.SenderId = messageEventArgs.Message.SenderId;
                    messageTable.DialogId = messageEventArgs.Message.ChatDialogId;
                    messageTable.DateSent = messageEventArgs.Message.DateSent;
					messageTable.MessageId = messageEventArgs.Message.Id;

                    if (messageEventArgs.Message.NotificationType != 0)
                    {
                        if (messageEventArgs.Message.NotificationType == NotificationTypes.GroupUpdate)
                        {
                            if (messageEventArgs.Message.AddedOccupantsIds.Any())
                            {
                                var userIds = new List<int>(messageEventArgs.Message.AddedOccupantsIds);
                                userIds.Add(messageEventArgs.Message.SenderId);
                                var users = await App.QbProvider.GetUsersByIdsAsync(string.Join(",", userIds));
                                var addedUsers = users.Where(u => u.Id != messageEventArgs.Message.SenderId);
                                var senderUser = users.First(u => u.Id == messageEventArgs.Message.SenderId);
                                messageTable.Text = senderUser.FullName + " added users: " +
                                                    string.Join(",", addedUsers.Select(u => u.FullName));
                            }
                            else if (messageEventArgs.Message.DeletedOccupantsIds.Any())
                            {
                                var userIds = new List<int>(messageEventArgs.Message.DeletedOccupantsIds);
                                var users = await App.QbProvider.GetUsersByIdsAsync(string.Join(",", userIds));
                                messageTable.Text = string.Join(",", users.Select(u => u.FullName)) + " left this room";
                            }
                        }
                    }
                    else
                    {
                        messageTable.Text = decodedMessage;
                    }

                    await SetRecepientName(messageTable);

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        this.Messages.Add(messageTable);
                        var page = App.NavigationFrame.Content as GroupChatPage;
                        if (page != null)
                            page.ScrollList();
                    });
                }
            }
        }

        private void SendMessageCommandExecute()
        {
            var message = this.MessageText != null ? this.MessageText.Trim() : string.Empty;
            if (!string.IsNullOrEmpty(message))
            {
                var m = new MessageTable();
                m.SenderId = (int)App.QbProvider.UserId;
                m.Text = message;
                m.DialogId = dialogId;
                m.RecepientFullName = "Me";

                long unixTimestamp = DateTime.UtcNow.Ticks - new DateTime(1970, 1, 1).Ticks;
                unixTimestamp /= TimeSpan.TicksPerSecond;
                m.DateSent = unixTimestamp;
                m.ID = Database.Instance().SaveMessage(m);

                var dialog = Database.Instance().GetDialog(this.dialogId);
                dialog.LastMessage = m.Text;
                dialog.LastMessageSent = DateTime.UtcNow;
                Database.Instance().SaveDialog(dialog);

                try
                {
                    var encodedMessage = System.Net.WebUtility.UrlEncode(message);
                    groupChatManager.SendMessage(encodedMessage);
                    MessageText = "";
                }
                catch (Exception ex)
                {
                    new MessageDialog("Internet connection is lost. Please check it and restart the Application", "Error").ShowAsync();
                }
            }
        }

        private void OpenChatInfoCommandExecute()
        {
            App.NavigationFrame.Navigate(typeof(ChatInfoPage), this.dialogId);
        }
    }
}
