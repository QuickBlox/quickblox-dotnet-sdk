using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Xamarin.Forms;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using Quickblox.Sdk.Modules.UsersModule.Models;
using XamarinForms.QbChat.Pages;
using Xmpp.Im;
using QbChat.Pcl.Repository;

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
			List<MessageTable> chatMessages = new List<MessageTable>();
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

					chatMessages.Add(chatMessage);
                }

                Device.BeginInvokeOnMainThread(() =>
                {
					this.Messages.Clear();
					foreach (var item in chatMessages)
					{
						this.Messages.Add(item);
					}

                    var page = (App.Current.MainPage as NavigationPage).CurrentPage as GroupChatPage;
                    if (page != null)
                    {
                        page.ScrollList();
                    }
                }
            );
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

				var message = this.Messages.FirstOrDefault(m => m.MessageId == messageTable.MessageId);
				if (message == null)
                	this.Messages.Add(messageTable);

                Device.BeginInvokeOnMainThread(() =>
                {
                    var groupPage = ((App.Current.MainPage as NavigationPage).CurrentPage as GroupChatPage);
                    if (groupPage != null)
                        groupPage.ScrollList();
                }
               );
            }
        }

        private void SendMessageCommandExecute(object obj)
        {
            var message = this.MessageText != null ? this.MessageText.Trim() : string.Empty;
            if (!string.IsNullOrEmpty(message))
            {
                try
                {
                    var encodedMessage = System.Net.WebUtility.UrlEncode(message);
                    groupChatManager.SendMessage(encodedMessage);
                }
                catch (Exception ex)
                {
                    App.Current.MainPage.DisplayAlert("Internet connection", "Internet connection is lost. Please check it and restart the Application", "Ok");
                }
                finally
                {
                    this.MessageText = "";
                }
            }
        }

        private void OpenChatInfoCommandExecute(object obj)
        {
            App.Navigation.PushAsync(new ChatInfoPage(this.dialogId));
        }
    }
}
