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
using QbChat.Pcl.Helpers;

namespace XamarinForms.QbChat.ViewModels
{
    public class GroupChatViewModel : BaseChatViewModel
    {
        private GroupChatManager groupChatManager;
		private AsyncLock lockObject = new AsyncLock();
        
        public GroupChatViewModel(string dialogId) : base(dialogId)
        {
            this.OpenChatInfoCommand = new Command(this.OpenChatInfoCommandExecute);
            base.SendMessageCommand = new Command(this.SendMessageCommandExecute);

			//Database.Instance().SubscribeForMessages(ReceiveMessageFromDB);
        }

		public ICommand OpenChatInfoCommand { get; private set; }

        public override async void OnAppearing()
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

            //Task.Factory.StartNew(async () =>
            //{
                await this.LoadMessages();

                Device.BeginInvokeOnMainThread(() =>
                    this.IsBusyIndicatorVisible = false
                );
            //});

        }

        private async Task LoadMessages()
        {
            List<Message> messages = null;
			List<MessageTable> chatMessages = new List<MessageTable>();
            try
            {
                messages = await App.QbProvider.GetMessagesAsync(this.dialogId);
				var userIds = messages.Select(u => u.SenderId).Distinct().ToList();
				userIds.Add(App.UserId);
				this.opponentUsers = await App.QbProvider.GetUsersByIdsAsync(string.Join(",", userIds));
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.ToString(), "Ok");
            }

			using (await lockObject.LockAsync())
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
								await LoadMessageText(message, chatMessage);
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

					var databaseMessages = Database.Instance().GetMessages(this.dialogId);
					Device.BeginInvokeOnMainThread(() =>
					{
						var messagesJoined = chatMessages.GroupJoin(databaseMessages,        //  inner sequence
														 e => e.MessageId,          //  outerKeySelector
														 o => o.MessageId,          //  innerKeySelector
														  (e, o) => e);

						this.Messages.Clear();
						foreach (var item in messagesJoined)
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
        }

		private async Task LoadMessageText(Message message, MessageTable chatMessage)
		{
			var userIds = new List<int>(message.AddedOccupantsIds);

			// Check if we loaded this users 
			var isNeedLoad = false;
			foreach (var userInAddedCollectionId in message.AddedOccupantsIds)
			{
				var user = opponentUsers.FirstOrDefault(u => u.Id == userInAddedCollectionId);
				if (user == null)
				{
					isNeedLoad = true;
					break;
				}
			}

			userIds.Add(message.SenderId);

			if (isNeedLoad)
			{
				var users = await App.QbProvider.GetUsersByIdsAsync(string.Join(",", userIds));
				foreach (var user in users)
				{
					var userInCollection = opponentUsers.FirstOrDefault(u => u.Id == user.Id);
					if (userInCollection == null)
						opponentUsers.Add(user);
				}
			}

			var addedUsers = opponentUsers.Where(u => userIds.Contains(u.Id) && u.Id != message.SenderId);
			var senderUser = opponentUsers.First(u => userIds.Contains(u.Id) && u.Id == message.SenderId);
			chatMessage.Text = senderUser.FullName + " added users: " +
			string.Join(",", addedUsers.Select(u => u.FullName));
			
		}

		private async void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
			if (messageEventArgs.MessageType == MessageType.Chat ||
				messageEventArgs.MessageType == MessageType.Groupchat)
			{
				using (await lockObject.LockAsync())
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
								await LoadMessageText(messageEventArgs.Message, messageTable);
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

		//private void ReceiveMessageFromDB()
		//{
		//	var messages = Database.Instance().GetMessages(dialogId);
		//	var messagesJoined = Messages.Join(messages,        //  inner sequence
		//											 e => e.MessageId,          //  outerKeySelector
		//											 o => o.MessageId,          //  innerKeySelector
		//											  (e, o) => e);

		//	this.Messages.Clear();
		//	foreach (var item in messagesJoined)
		//	{
		//		this.Messages.Add(item);
		//	}
		//}
    }
}
