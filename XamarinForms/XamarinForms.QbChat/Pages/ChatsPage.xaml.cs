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

namespace XamarinForms.QbChat.Pages
{
    public partial class ChatsPage : ContentPage
    {
		Quickblox.Sdk.Modules.UsersModule.Models.User user;

		public ChatsPage()
        {
            InitializeComponent();

			this.CreateGroupChat.Clicked += async (object sender, EventArgs e) => {
//				            10195773    @xamarinuser1    Xamarin User 1                        
//				            10195779    @xamarinuser2    Xamarin User 2                        
//				            10195787    @xamarinuser3    Xamarin User 3                        
//				            10195790    @xamarinuser4    Xamarin User 4                        
//				            10195793    @xamarinuser5    Xamarin User 5

				            var list = new List<long> () { 10195773, 10195779, 10195787, 10195790, 10195793 };
				            
				var createDialog = await App.QbProvider.CreateDialogAsync ("Group name", string.Join(",", list), Quickblox.Sdk.Modules.ChatModule.Models.DialogType.Group);      // item.ToString());
				                try {
				                    var dialogId = createDialog.Id;
				                    var chatMessageExtraParameter = new ChatMessageExtraParameter (dialogId, true);
									App.QbProvider.GetXmppClient ().SendMessage (createDialog.XmppRoomJid, "Hello", chatMessageExtraParameter.Build(), dialogId, null);
				                } catch (Exception ex) {
				
				                }
				
				            //}
			};
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

			busyIndicator.IsVisible = true; 

			ToolbarItems.Clear ();
			ToolbarItems.Add (new ToolbarItem ("Logout", "ic_action_logout.png", async () => {
				var result = await DisplayAlert("Logout", "Do you really want to logout?", "Ok", "Cancel");
				if (result){
					try {
						Database.Instance().ResetAll();
						DisconnectToXmpp();
					} catch (Exception ex) {
					}
					finally{
						App.SetLoginPage();
					}
				}
			}));

			if (user == null && App.QbProvider.UserId != 0) {
				user = await App.QbProvider.GetUserAsync (App.QbProvider.UserId);
			}

			try {
				// uses login as password because it is the same
				ConnetToXmpp(user.Id, user.Login);
			} catch (Exception ex) {
			}

			if (myProfileImage.Source == null) {
				myNameLabel.Text = user.FullName;

				myProfileImage.Source = Device.OnPlatform (iOS: ImageSource.FromFile ("ic_user.png"),
					Android: ImageSource.FromFile ("ic_user.png"),
					WinPhone: ImageSource.FromFile ("Images/ic_user.png"));
				if (user.BlobId.HasValue)
				{
					App.QbProvider.GetImageAsync (user.BlobId.Value).ContinueWith ((task, result) => {
						var bytes = task.ConfigureAwait(true).GetAwaiter().GetResult();
						if (bytes != null)
						Device.BeginInvokeOnMainThread(() =>
							myProfileImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes)));
					}, TaskScheduler.FromCurrentSynchronizationContext ());
				}
			}

			if (listView.ItemsSource == null) {
				var template = new DataTemplate (typeof(TextCell));
				template.SetBinding (ImageCell.TextProperty, "Name");
				template.SetBinding (ImageCell.DetailProperty, "LastMessage");
				listView.ItemTemplate = template;

				listView.ItemTapped += OnItemTapped;
				var dialogs = await App.QbProvider.GetDialogsAsync ();
				var sorted = dialogs.Where(d => d.LastMessageSent != null). OrderByDescending(d => d.LastMessageSent.Value).Concat(dialogs.Where(d => d.LastMessageSent == null)). ToList();
				listView.ItemsSource = sorted;
				Database.Instance().SaveAllDialogs(sorted);
			}

			Database.Instance().SubscribeForDialogs(OnDialogsChanged);

			this.busyIndicator.IsVisible = false;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
			Database.Instance().UnSubscribeForDialogs(OnDialogsChanged);

        }

        private void OnDialogsChanged()
        {
            var dialogs = Database.Instance().GetDialogs();
			Device.BeginInvokeOnMainThread (() =>{ 
				var sorted = dialogs.OrderByDescending(d => d.LastMessageSent.Value).ToList();
				listView.ItemsSource = sorted;
			});
        }

        private void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            var dialogItem = e.Item as DialogTable;
			((ListView)sender).SelectedItem = null;
			App.Navigation.PushAsync(new ChatPage(dialogItem.DialogId));
        }

		private void ConnetToXmpp(int userId, string userPassword)
        {
            if (!App.QbProvider.GetXmppClient().IsConnected)
            {
                App.QbProvider.GetXmppClient().MessageReceived -= OnMessageReceived;
                App.QbProvider.GetXmppClient().MessageReceived += OnMessageReceived;

                App.QbProvider.GetXmppClient().ErrorReceived -= OnError;
                App.QbProvider.GetXmppClient().ErrorReceived += OnError;

                App.QbProvider.GetXmppClient().StatusChanged += OnStatusChanged;
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
				App.QbProvider.GetXmppClient().Disconnect();
			}
		}

        private void OnStatusChanged(object sender, StatusEventArgs statusEventArgs)
        {
            Debug.WriteLine("Xmpp Status: " + statusEventArgs.Jid + " Status: " + statusEventArgs.Status.Availability);
        }

        private void OnError(object sender, ErrorEventArgs errorsEventArgs)
        {
            Debug.WriteLine("Xmpp Error: " + errorsEventArgs.Exception + " Reason: " + errorsEventArgs.Reason);
        }

        private async void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
			var dialog = Database.Instance().GetDialog(messageEventArgs.Message.Thread);
			if (dialog == null)
			{
				dialog = await App.QbProvider.GetDialogAsync(messageEventArgs.Message.Thread);
			}

			if (dialog != null)
			{
				dialog.LastMessage = messageEventArgs.Message.Body;
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
			if (messageEventArgs.Message.MessageType == Quickblox.Sdk.Modules.ChatXmppModule.Models.MessageType.Groupchat) {
				var message = Database.Instance ().GetMessages (messageEventArgs.Message.Thread).FirstOrDefault (m => m.MessageId == messageEventArgs.Message.Id);
				if (message != null) {
					return;

				}
				var senderId = ChatXmppClient.GetUserIdFromRoomJid (messageEventArgs.Message.From.ToString ());
				messageTable.SenderId = senderId;
			} else if (messageEventArgs.Message.MessageType == Quickblox.Sdk.Modules.ChatXmppModule.Models.MessageType.Chat) {
				messageTable.SenderId = messageEventArgs.Message.From.GetUserId ();
			}

			messageTable.Text = messageEventArgs.Message.Body;
			messageTable.DialogId = messageEventArgs.Message.Thread;
			messageTable.DateSent = messageEventArgs.Message.Timestamp;

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

			Database.Instance().SaveMessage(messageTable);
        }
    }
}
