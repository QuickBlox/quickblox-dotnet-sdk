using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Xamarin.Forms;
using XamarinForms.QbChat.Repository;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using Quickblox.Sdk.Modules.UsersModule.Models;

namespace XamarinForms.QbChat.ViewModels
{
    public class GroupChatViewModel : BaseChatViewModel
    {
        private GroupChatManager groupChatManager;
        
        public GroupChatViewModel(string dialogId) : base(dialogId)
        {
            this.OpenChatInfoCommand = new Command(this.OpenChatInfoCommandExecute);
            base.SendMessageCommand = new Command(this.SendMessageCommandExecute);
        }

        public ICommand OpenChatInfoCommand { get; private set; }

        public override void OnAppearing()
        {
            base.OnAppearing();

            this.IsBusyIndicatorVisible = true;

            var dialog = Database.Instance().GetDialog(dialogId);
            groupChatManager = App.QbProvider.GetXmppClient().GetGroupChatManager(dialog.XmppRoomJid, dialogId);
            groupChatManager.MessageReceived += OnMessageReceived;
            DialogName = dialog.Name;
            ImageSource = Device.OnPlatform(iOS: ImageSource.FromFile("groupholder.png"),
                Android: ImageSource.FromFile("groupholder.png"),
                WinPhone: ImageSource.FromFile("groupholder.png"));

            if (!string.IsNullOrEmpty(dialog.Photo))
            {
                ImageSource = ImageSource.FromUri(new Uri(dialog.Photo));
            }

            Task.Factory.StartNew(async () =>
            {
                await this.LoadMessages();

                Device.BeginInvokeOnMainThread(() =>
                    this.IsBusyIndicatorVisible = false
                );
            });

        }

        private async Task LoadMessages()
        {
            List<Message> messages = null;
            try
            {
                messages = await App.QbProvider.GetMessagesAsync(this.dialogId);
                var userIds = messages.Select(u => u.SenderId).Distinct();
                this.opponentUsers = await App.QbProvider.GetUsersByIdsAsync(string.Join(",", userIds));
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.ToString(), "Ok");
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

                    Device.BeginInvokeOnMainThread(() =>
                        this.Messages.Add(chatMessage)
                    );
                }
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

                this.Messages.Add(messageTable);
            }
        }

        private void SendMessageCommandExecute(object obj)
        {
            var message = this.MessageText != null ? this.MessageText.Trim() : string.Empty;
            if (!string.IsNullOrEmpty(message))
            {
                //var dialog = Database.Instance().GetDialog(dialogId);
                //dialog.LastMessage = message;
                //dialog.LastMessageSent = DateTime.UtcNow;
                //Database.Instance().SaveDialog(dialog);

                try
                {
                    var encodedMessage = System.Net.WebUtility.UrlEncode(message);
                    groupChatManager.SendMessage(encodedMessage);
                }
                catch (Exception ex)
                {
                    this.IsBusyIndicatorVisible = true;
                    try
                    {
                        App.QbProvider.GetXmppClient().Connect(App.UserId, App.UserPassword);
                    }
                    catch (Exception ex2)
                    {
                        App.Current.MainPage.DisplayAlert("Error", "Please, check your internet connection", "Ok");
                    }
                    finally
                    {
                        this.IsBusyIndicatorVisible = false;
                    }

                    return;
                    //await App.Current.MainPage.DisplayAlert ("Error", ex.ToString(), "Ok");
                }

                this.MessageText = "";
            }
        }

        private void OpenChatInfoCommandExecute(object obj)
        {
            App.Navigation.PushAsync(new ChatInfoPage(this.dialogId));
        }
    }
}
