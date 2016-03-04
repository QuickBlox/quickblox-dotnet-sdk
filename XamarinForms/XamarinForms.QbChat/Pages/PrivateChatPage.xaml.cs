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
        }

		protected override void OnDisappearing ()
		{
			base.OnDisappearing ();
			Database.Instance().UnSubscribeForMessages(OnMessagesChanged);
		}

        protected override async void OnAppearing()
        {
            base.OnAppearing();

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
			opponentUser = users.FirstOrDefault();
			if (opponentUser != null && opponentUser.BlobId.HasValue)
			{
				App.QbProvider.GetImageAsync (opponentUser.BlobId.Value).ContinueWith ((task, result) => {
					var bytes = task.ConfigureAwait(true).GetAwaiter().GetResult();
					if (bytes != null)
					Device.BeginInvokeOnMainThread(() =>
						chatPhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes)));
				}, TaskScheduler.FromCurrentSynchronizationContext ());
			}

			List<MessageTable> messages;
			try {
			   messages = await App.QbProvider.GetMessagesAsync(dialogId);
			} catch (Exception ex) {
				await App.Current.MainPage.DisplayAlert ("Error", ex.ToString(), "Ok");
				return;
			}

            if (messages != null)
            {
                messages = messages.OrderBy(message => message.DateSent).ToList();
					
				foreach (var message in messages) {
					if (message.SenderId == App.QbProvider.UserId) {
						message.RecepientFullName = "Me";
					} else {
						if (opponentUser != null) {
							message.RecepientFullName = opponentUser.FullName;
						}
					}
				}

                Database.Instance().SaveAllMessages(dialogId, messages);

				var template = new DataTemplate (typeof(MessageCell));
				listView.HasUnevenRows = true;
				listView.ItemTemplate = template;
				var sorted = messages.OrderBy(d => d.DateSent);
				listView.ItemsSource = sorted;

				try {
					if (messages != null && messages.Count > 10)
						listView.ScrollTo (messages [messages.Count - 1], ScrollToPosition.Start, false);
				} catch (Exception ex) {
				}
            }

            Database.Instance().SubscribeForMessages(OnMessagesChanged);

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
				m.DateSent = DateTime.UtcNow.Ticks / 1000;
				m.ID = Database.Instance ().SaveMessage (m);

				dialog.LastMessage = m.Text;
				dialog.LastMessageSent = DateTime.UtcNow;
				Database.Instance ().SaveDialog (dialog);

				try {
					var encodedMessage = System.Net.WebUtility.UrlEncode(message);
					privateChatManager.SendMessage(encodedMessage);
				} catch (Exception ex) {
					await App.Current.MainPage.DisplayAlert ("Error", ex.ToString (), "Ok");
				}

				messageEntry.Text = "";
			}
		}

        public void OnMessagesChanged()
        {
			var messages = Database.Instance().GetMessages(dialogId);
			var sorted = messages.OrderBy(d => d.DateSent).ToList();
			Device.BeginInvokeOnMainThread (() =>
				{ listView.ItemsSource = sorted;
					try {
						if (messages != null && messages.Count > 10)
							listView.ScrollTo (sorted [sorted.Count - 1], ScrollToPosition.Start, false);
					} catch (Exception ex) {
					}
			}
			);
        }
    }
}
