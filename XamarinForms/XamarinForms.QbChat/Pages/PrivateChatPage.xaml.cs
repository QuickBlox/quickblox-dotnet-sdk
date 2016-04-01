using Quickblox.Sdk.Modules.ChatXmppModule.ExtraParameters;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using System;
using System.Linq;
using XamarinForms.QbChat.Repository;
using Xamarin.Forms;
using Quickblox.Sdk.Modules.ChatModule.Models;
using System.Runtime.Serialization;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Quickblox.Sdk.GeneralDataModel.Models;

namespace XamarinForms.QbChat.Pages
{
    public partial class PrivateChatPage : ContentPage
    {
        private string dialogId;
		int opponentId;
		private bool isLoaded;
		DialogTable dialog;
		User opponentUser;
		Quickblox.Sdk.Modules.ChatXmppModule.PrivateChatManager privateChatManager;

		public PrivateChatPage(String dialogId)
        {
            InitializeComponent();
            this.dialogId = dialogId;

			listView.ItemTapped += (object sender, ItemTappedEventArgs e) => ((ListView)sender).SelectedItem = null;
        }

		protected override void OnDisappearing ()
		{
			base.OnDisappearing ();
			Database.Instance().UnSubscribeForMessages(OnMessagesChanged);
		}

        protected override async void OnAppearing()
        {
            base.OnAppearing();
			Database.Instance().SubscribeForMessages(OnMessagesChanged);

			if (isLoaded)
				return;
			
			this.busyIndicator.IsVisible = true;

			dialog = Database.Instance().GetDialog(dialogId);
			opponentId = dialog.OccupantIds.Split (',').Select (Int32.Parse).First (id => id != App.QbProvider.UserId);
			privateChatManager = App.QbProvider.GetXmppClient ().GetPrivateChatManager (opponentId, dialogId);

            chatNameLabel.Text = dialog.Name;
			chatPhotoImage.Source = Device.OnPlatform (iOS: ImageSource.FromFile ("privateholder.png"),
				Android: ImageSource.FromFile ("privateholder.png"),
				WinPhone: ImageSource.FromFile ("privateholder.png"));
			
			var users = await App.QbProvider.GetUsersByIdsAsync (dialog.OccupantIds);
			opponentUser = users.FirstOrDefault (u => u.Id != App.QbProvider.UserId);
			if (opponentUser != null && opponentUser.BlobId.HasValue)
			{
				App.QbProvider.GetImageAsync (opponentUser.BlobId.Value).ContinueWith ((task, result) => {
					var bytes = task.ConfigureAwait(true).GetAwaiter().GetResult();
					if (bytes != null)
					Device.BeginInvokeOnMainThread(() =>
						chatPhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes)));
				}, TaskScheduler.FromCurrentSynchronizationContext ());
			}

			List<Message> messages;
			try {
			    messages = await App.QbProvider.GetMessagesAsync(dialogId);
			} catch (Exception ex) {
				await App.Current.MainPage.DisplayAlert ("Error", ex.ToString(), "Ok");
				return;
			}

            if (messages != null)
            {
                messages = messages.OrderBy(message => message.DateSent).ToList();
				List<MessageTable> messageTableList = new List<MessageTable> ();

				foreach (var message in messages) {
					var chatMessage = new MessageTable ();
					chatMessage.DateSent = message.DateSent;
					chatMessage.SenderId = message.SenderId;
					chatMessage.MessageId = message.Id;
					if (message.RecipientId.HasValue)
						chatMessage.RecepientId = message.RecipientId.Value;
					chatMessage.DialogId = message.ChatDialogId;
					chatMessage.IsRead = message.Read == 1;


					if (message.SenderId == App.QbProvider.UserId) {
						chatMessage.RecepientFullName = "Me";
					} else {
						if (opponentUser != null) {
							chatMessage.RecepientFullName = opponentUser.FullName;
						}
					}

					chatMessage.Text = System.Net.WebUtility.UrlDecode (message.MessageText);
					messageTableList.Add (chatMessage);
				}

				Database.Instance().SaveAllMessages(dialogId, messageTableList);
            }

            sendButton.Clicked += SendClicked;

			this.busyIndicator.IsVisible = false;
			this.isLoaded = true;
        }

        private async void SendClicked(object sender, EventArgs e)
		{
			var message = messageEntry.Text != null ? messageEntry.Text.Trim () : string.Empty;
			if (!string.IsNullOrEmpty (message)) {
				var m = new MessageTable ();
				m.SenderId = (int)App.QbProvider.UserId;
				m.Text = message;
				m.DialogId = dialogId;
				m.RecepientFullName = "Me";

				long unixTimestamp = DateTime.UtcNow.Ticks - new DateTime(1970, 1, 1).Ticks;
				unixTimestamp /= TimeSpan.TicksPerSecond;
				m.DateSent = unixTimestamp;
				m.ID = Database.Instance ().SaveMessage (m);

				dialog.LastMessage = m.Text;
				dialog.LastMessageSent = DateTime.UtcNow;
				Database.Instance ().SaveDialog (dialog);

				try {
					var encodedMessage = System.Net.WebUtility.UrlEncode(message);
					privateChatManager.SendMessage(encodedMessage);
				} catch (Exception ex) {
					this.busyIndicator.IsVisible = true;
					try {
						App.QbProvider.GetXmppClient ().Connect (App.UserId, App.UserPassword);
					} catch (Exception ex2) {
						App.Current.MainPage.DisplayAlert ("Error", "Please, check your internet connection", "Ok");
					}
					finally{
						this.busyIndicator.IsVisible = false;
					}

					return;
					//await App.Current.MainPage.DisplayAlert ("Error", ex.ToString (), "Ok");
				}

				messageEntry.Text = "";
			}
		}

		public void ScrollList ()
		{
			var sorted = listView.ItemsSource as List<MessageTable>;
			try {
				if (sorted != null && sorted.Count > 10) {
					listView.ScrollTo (sorted [sorted.Count - 1], ScrollToPosition.End, false);
				}
			}
			catch (Exception ex) {
			}
		}

        public void OnMessagesChanged()
        {
			var messages = Database.Instance().GetMessages(dialogId);
			var sorted = messages.OrderBy(d => d.DateSent).ToList();
			Device.BeginInvokeOnMainThread (() =>
				{ 
					listView.ItemsSource = sorted;
					ScrollList ();
			}
			);
        }
    }
}
