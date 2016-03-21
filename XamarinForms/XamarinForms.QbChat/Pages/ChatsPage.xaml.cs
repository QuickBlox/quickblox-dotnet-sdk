using System;
using System.Linq;
using Quickblox.Sdk.Modules.ChatXmppModule;
using XamarinForms.QbChat.Repository;
using Xamarin.Forms;
using System.Xml.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Quickblox.Sdk.Modules.ChatXmppModule.ExtraParameters;
using System.Collections.Generic;
using Quickblox.Sdk.Modules.ChatModule.Models;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using Quickblox.Sdk.GeneralDataModel.Models;

namespace XamarinForms.QbChat.Pages
{
    public partial class ChatsPage : ContentPage
    {
		Quickblox.Sdk.Modules.UsersModule.Models.User user;
		private bool isLoaded;
		private bool isLogoutClicked;

		public ChatsPage()
        {
            InitializeComponent();

			ToolbarItems.Clear ();

			var logoutItem = new ToolbarItem ("Logout", null, async () => {
				var result = await DisplayAlert ("Logout", "Do you really want to logout?", "Ok", "Cancel");
				if (result) {
					try {
						Database.Instance ().UnSubscribeForDialogs (OnDialogsChanged);
						isLogoutClicked = true;
						Database.Instance ().ResetAll ();
						App.UserLogin = 0;
						App.UserPassword = null;
						DisconnectToXmpp ();
					} catch (Exception ex) {
					} finally {
						App.SetLoginPage ();
					}
				}
			});

			var createNewChat = new ToolbarItem ("New Chat", null, async () => {
				App.Navigation.PushAsync (new CreateDialogPage ());
			});

			if (Device.OS == TargetPlatform.Android) {
				logoutItem.Order = ToolbarItemOrder.Secondary;
				createNewChat.Order = ToolbarItemOrder.Secondary;
			} else if (Device.OS == TargetPlatform.iOS) {
				logoutItem.Order = ToolbarItemOrder.Primary;
			}

			ToolbarItems.Add (createNewChat);
			ToolbarItems.Add (logoutItem);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
			if (isLoaded)
				return;

			isLoaded = true;

			busyIndicator.IsVisible = true; 

			Task.Factory.StartNew (async () => {
				if (user == null && App.QbProvider.UserId != 0) {
					user = await App.QbProvider.GetUserAsync (App.QbProvider.UserId);
					Device.BeginInvokeOnMainThread(() => {
						Title = string.Format("Logged In as {0}", user.FullName);
					});

					App.UserLogin = user.Id;
					App.UserPassword = user.Login;
				}

				try {
					// uses login as password because it is the same
					ConnetToXmpp (user.Id, user.Login);
				} catch (Exception ex) {
				}

				var dialogs = await App.QbProvider.GetDialogsAsync (new List<DialogType>() { DialogType.Private, DialogType.Group });
				var sorted = dialogs.OrderByDescending (d => d.LastMessageSent).ToList ();

				foreach (var dialog in sorted) {
					if (dialog.DialogType == DialogType.Group){
						var groupdManager =App.QbProvider.GetXmppClient().GetGroupChatManager(dialog.XmppRoomJid, dialog.DialogId);
						groupdManager.JoinGroup(App.QbProvider.UserId.ToString());
					}

					dialog.LastMessage = System.Net.WebUtility.UrlDecode (dialog.LastMessage);
				}

				Device.BeginInvokeOnMainThread(() => {
//					if (myProfileImage.Source == null) {
//						myNameLabel.Text = user.FullName;
//						InitializeProfilePhoto ();
//					}

					InitializeDialogsList (sorted);

					Database.Instance ().SubscribeForDialogs (OnDialogsChanged);

					this.busyIndicator.IsVisible = false;
				});
			});
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

		private void InitializeDialogsList (List<DialogTable> sorted)
		{
			var template = new DataTemplate (typeof(ChatCell));
			template.SetBinding (TextCell.TextProperty, "Name");
			template.SetBinding (TextCell.DetailProperty, "LastMessage");
			listView.ItemTemplate = template;
			listView.ItemTapped += OnItemTapped;
			listView.ItemsSource = sorted;
			Database.Instance ().SaveAllDialogs (sorted);
		}

//		private void InitializeProfilePhoto ()
//		{
//			myProfileImage.Source = Device.OnPlatform (iOS: ImageSource.FromFile ("ic_user.png"), Android: ImageSource.FromFile ("ic_user.png"), WinPhone: ImageSource.FromFile ("Images/ic_user.png"));
//			if (user.BlobId.HasValue) {
//				App.QbProvider.GetImageAsync (user.BlobId.Value).ContinueWith ((task, result) =>  {
//					var bytes = task.ConfigureAwait (true).GetAwaiter ().GetResult ();
//					if (bytes != null)
//						Device.BeginInvokeOnMainThread (() => myProfileImage.Source = ImageSource.FromStream (() => new MemoryStream (bytes)));
//				}, TaskScheduler.FromCurrentSynchronizationContext ());
//			}
//		}

        private void OnDialogsChanged()
        {
            var dialogs = Database.Instance().GetDialogs();
			Device.BeginInvokeOnMainThread (() =>{ 
				var sorted = dialogs.OrderByDescending(d => d.LastMessageSent).ToList();
				listView.ItemsSource = sorted;
			});
        }

        private void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            var dialogItem = e.Item as DialogTable;
			((ListView)sender).SelectedItem = null;

			if (dialogItem.DialogType == Quickblox.Sdk.Modules.ChatModule.Models.DialogType.Private)
				App.Navigation.PushAsync(new PrivateChatPage(dialogItem.DialogId));
			else
				App.Navigation.PushAsync(new GroupChatPage(dialogItem.DialogId));
        }

		private void ConnetToXmpp(int userId, string userPassword)
        {
            if (!App.QbProvider.GetXmppClient().IsConnected)
            {
                App.QbProvider.GetXmppClient().MessageReceived -= OnMessageReceived;
                App.QbProvider.GetXmppClient().MessageReceived += OnMessageReceived;

                App.QbProvider.GetXmppClient().ErrorReceived -= OnError;
                App.QbProvider.GetXmppClient().ErrorReceived += OnError;

                App.QbProvider.GetXmppClient().StatusChanged -= OnStatusChanged;
                App.QbProvider.GetXmppClient().StatusChanged += OnStatusChanged;
				App.QbProvider.GetXmppClient().Connect(userId, userPassword);
            }
        }

		private void DisconnectToXmpp()
		{
			if (App.QbProvider.GetXmppClient().IsConnected)
			{
				App.QbProvider.GetXmppClient().MessageReceived -= OnMessageReceived;
				App.QbProvider.GetXmppClient().ErrorReceived -= OnError;
				App.QbProvider.GetXmppClient().StatusChanged -= OnStatusChanged;
				App.QbProvider.GetXmppClient().Close();
			}
		}

        private void OnStatusChanged(object sender, StatusEventArgs statusEventArgs)
        {
            Debug.WriteLine("Xmpp Status: " + statusEventArgs.Jid + " Status: " + statusEventArgs.Status.Availability);
        }

        private async void OnError(object sender, ErrorEventArgs errorsEventArgs)
        {
            Debug.WriteLine("Xmpp Error: " + errorsEventArgs.Exception + " Reason: " + errorsEventArgs.Reason);

			Reconnect ();
        }

        private async void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
			var dialog = Database.Instance().GetDialog(messageEventArgs.Message.ChatDialogId);
			if (dialog == null)
			{
				dialog = await App.QbProvider.GetDialogAsync(messageEventArgs.Message.ChatDialogId);
			}

			if (messageEventArgs.MessageType == MessageType.Headline){
				if(messageEventArgs.Message.NotificationType == NotificationTypes.GroupCreate) {
					if (dialog != null) {
						dialog.LastMessage = messageEventArgs.Message.MessageText;
						dialog.LastMessageSent = DateTime.UtcNow;
						Database.Instance().SaveDialog(dialog);

						var groupChatManager = App.QbProvider.GetXmppClient ().GetGroupChatManager (dialog.XmppRoomJid, dialog.DialogId);
						groupChatManager.JoinGroup (App.QbProvider.UserId.ToString ());
					}
				}
			}
			else{
				string decodedMessage = System.Net.WebUtility.UrlDecode (messageEventArgs.Message.MessageText);
				if (dialog != null)
				{
					dialog.LastMessage = decodedMessage;
					dialog.LastMessageSent = DateTime.UtcNow;

					if (dialog.UnreadMessageCount != null)
					{
						dialog.UnreadMessageCount++;
					}
					else
					{
						dialog.UnreadMessageCount = 1;
					}

					Database.Instance().SaveDialog(dialog);
				}

				var messageTable = new MessageTable ();
				messageTable.SenderId = messageEventArgs.Message.SenderId;
				messageTable.DialogId = messageEventArgs.Message.ChatDialogId;
				messageTable.DateSent = messageEventArgs.Message.DateSent;

				if(messageEventArgs.Message.NotificationType != 0){
					if (messageEventArgs.Message.NotificationType == NotificationTypes.GroupUpdate) {
						var users = await App.QbProvider.GetUsersByIdsAsync (dialog.OccupantIds);
						var filteredUsers = users.Where (u => u.Id != messageTable.SenderId); 
						var senderUser = users.First (u => u.Id == messageTable.SenderId);
						messageTable.Text = senderUser.FullName + " added users: " + string.Join (",", filteredUsers.Select (u => u.FullName));
					}
				}
				else{
					messageTable.Text = decodedMessage;
				}

				if (messageTable.SenderId == App.QbProvider.UserId) {
					messageTable.RecepientFullName = "Me";
				} else {
					var user = Database.Instance ().GetUser (messageTable.SenderId);
					if (user == null) {
						var userRespose = await App.QbProvider.GetUserAsync (messageTable.SenderId);
						if (userRespose != null) {
							user = new UserTable ();
							user.FullName = userRespose.FullName;
							user.UserId = userRespose.Id;
							user.PhotoId = userRespose.BlobId.HasValue ? userRespose.BlobId.Value : 0;
							Database.Instance ().SaveUser (user);

							messageTable.RecepientFullName = user.FullName;
						}
					} else {
						messageTable.RecepientFullName = user.FullName;
					}
				}

				Database.Instance ().SaveMessage (messageTable);
			}
        }

		async void Reconnect ()
		{
			// Reconecting:
			while (!App.QbProvider.GetXmppClient ().IsConnected) {
				bool isWait = false;
				try {
					// Logout action
					if (isLogoutClicked)
						return;
					App.QbProvider.GetXmppClient ().Connect (App.UserLogin, App.UserPassword);

					var dialogs = await App.QbProvider.GetDialogsAsync (new List<DialogType>() { DialogType.Group });
					foreach (var dialog in dialogs) {
						var groupdManager =App.QbProvider.GetXmppClient().GetGroupChatManager(dialog.XmppRoomJid, dialog.DialogId);
						groupdManager.JoinGroup(App.QbProvider.UserId.ToString());
					}
				}
				catch (Exception ex) {
					isWait = true;
				}
				if (isWait) {
					await Task.Delay (3000);
				}
			}
		}
    }
}
