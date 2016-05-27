using QbChat.Pcl.Repository;
using QbChat.UWP.Views;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.Modules.UsersModule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;
using Xmpp.Im;

namespace QbChat.UWP.ViewModels
{
    public class PrivateChatViewModel : BaseChatViewModel
    {
        private PrivateChatManager privateChatManager;
        
        public PrivateChatViewModel(string dialogId) : base (dialogId)
        {
            base.SendMessageCommand = new RelayCommand(this.SendMessageCommandExecute);
        }

        public override async void OnAppearing()
        {
            base.OnAppearing();

            this.IsBusy = true;

            var dialog = Database.Instance().GetDialog(dialogId);
            var opponentId =
                dialog.OccupantIds.Split(',').Select(Int32.Parse).First(id => id != App.QbProvider.UserId);
            privateChatManager = App.QbProvider.GetXmppClient().GetPrivateChatManager(opponentId, dialogId);
            privateChatManager.MessageReceived += OnMessageReceived;

            DialogName = dialog.Name;

            ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/privateholder.png"));
                        
            var users = await App.QbProvider.GetUsersByIdsAsync(dialog.OccupantIds);
            var opponentUser = users.FirstOrDefault(u => u.Id != App.QbProvider.UserId);
            if (opponentUser != null && opponentUser.BlobId.HasValue)
            {
                var bytes = await App.QbProvider.GetImageAsync(opponentUser.BlobId.Value);
                BitmapImage image = new BitmapImage();
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    await stream.WriteAsync(bytes.AsBuffer());
                    stream.Seek(0);
                    await image.SetSourceAsync(stream);
                }
            }

            this.opponentUsers = new List<User>() { opponentUser };
            await this.LoadMessages();

            this.IsBusy = false;
        }

        public async Task LoadMessages()
        {
            List<Message> messages;
            try
            {
                messages = await App.QbProvider.GetMessagesAsync(dialogId);
            }
            catch (Exception ex)
            {
                new MessageDialog(ex.ToString(), "Error").ShowAsync();
                return;
            }

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

                    chatMessage.Text = System.Net.WebUtility.UrlDecode(message.MessageText);

                    Messages.Add(chatMessage);
                }

                var page = App.NavigationFrame.Content as PrivateChatPage;
                if (page != null)
                    page.ScrollList();
            }
        }

        private async void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            if (messageEventArgs.MessageType == MessageType.Chat ||
                messageEventArgs.MessageType == MessageType.Groupchat)
            {
                string decodedMessage = System.Net.WebUtility.UrlDecode(messageEventArgs.Message.MessageText);

                var messageTable = new MessageTable();
                messageTable.SenderId = messageEventArgs.Message.SenderId;
                messageTable.DialogId = messageEventArgs.Message.ChatDialogId;
                messageTable.DateSent = messageEventArgs.Message.DateSent;

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

                        //var dialogInfo = await App.QbProvider.GetDialogAsync(messageEventArgs.Message.ChatDialogId);
                        //if (dialogInfo == null)
                        //{
                        //    return;
                        //}
                        //var dialog = new DialogTable(dialogInfo);
                        //Database.Instance().SaveDialog(dialog);
                    }
                }
                else
                {
                    messageTable.Text = decodedMessage;
                }

                await SetRecepientName(messageTable);

                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                  () =>
                  {
                      this.Messages.Add(messageTable);
                      var page = App.NavigationFrame.Content as PrivateChatPage;
                      if (page != null)
                          page.ScrollList();
                  });
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
                    privateChatManager.SendMessage(encodedMessage);
                }
                catch (Exception ex)
                {
                    this.IsBusy = true;
                    try
                    {
                        App.QbProvider.GetXmppClient().Connect(App.UserId, App.UserPassword);
                    }
                    catch (Exception ex2)
                    {
                        new MessageDialog("Please, check your internet connection", "Error").ShowAsync();
                    }
                    finally
                    {
                        this.IsBusy = false;
                    }

                    return;
                }

                this.Messages.Add(m);
                MessageText = "";

                var page = App.NavigationFrame.Content as PrivateChatPage;
                if (page != null)
                    page.ScrollList();
            }
        }
    }
}
