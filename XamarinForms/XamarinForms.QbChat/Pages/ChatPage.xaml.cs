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

namespace XamarinForms.QbChat.Pages
{
    public partial class ChatPage : ContentPage
    {
        private string dialogId;
		private bool isLoaded;
		DialogTable dialog;
		List<Quickblox.Sdk.Modules.UsersModule.Models.User> users;

        public ChatPage(string dialogId)
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
            chatNameLabel.Text = dialog.Name;
			chatPhotoImage.Source = Device.OnPlatform (iOS: ImageSource.FromFile ("ic_user.png"),
				Android: ImageSource.FromFile ("ic_user.png"),
				WinPhone: ImageSource.FromFile ("Images/ic_user.png"));
			
			users = await App.QbProvider.GetUsersByIdsAsync (dialog.OccupantIds);
			if (dialog.DialogType == DialogType.Group) {
				if (!string.IsNullOrEmpty (dialog.Photo)) {
					chatPhotoImage.Source = ImageSource.FromUri (new Uri (dialog.Photo));
				}
			} else if (dialog.DialogType == DialogType.Private) {
				var user = users.FirstOrDefault (u => u.Id != App.QbProvider.UserId);
				if (user != null && user.BlobId.HasValue)
				{
					App.QbProvider.GetImageAsync (user.BlobId.Value).ContinueWith ((task, result) => {
						var bytes = task.ConfigureAwait(true).GetAwaiter().GetResult();
						if (bytes != null)
						Device.BeginInvokeOnMainThread(() =>
							chatPhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes)));
					}, TaskScheduler.FromCurrentSynchronizationContext ());
				}
			}

			if (dialog.DialogType == DialogType.Group) {
				App.QbProvider.GetXmppClient ().JoinGroup (dialog.XmppRoomJid, App.QbProvider.UserId.ToString());
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
						var user = users.FirstOrDefault (u => u.Id == message.SenderId);
						if (user != null) {
							message.RecepientFullName = user.FullName;
						}
					}
				}

                Database.Instance().SaveAllMessages(dialogId, messages);

				var template = new DataTemplate (typeof(TextCell));
				template.SetBinding (ImageCell.TextProperty, "RecepientFullName");
				template.SetBinding (ImageCell.DetailProperty, "Text");
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
//			if (dialog != null && 
//				(dialog.DialogType == DialogType.Group ||
//				 dialog.DialogType == DialogType.PublicGroup)) {
//				await App.Current.MainPage.DisplayAlert ("Error", "Comming soon. Use private chat for testing.", "Ok");
//				return;
//			}
            
			var message = messageEntry.Text != null ? messageEntry.Text.Trim() : string.Empty;
			if (!string.IsNullOrEmpty(message))
            {
				//if (dialog.DialogType == DialogType.Private) {
					

					var m = new MessageTable ();
					m.SenderId = (int)App.QbProvider.UserId;
					m.Text = message;
					m.DialogId = dialogId;
					m.RecepientFullName = "Me";
					m.DateSent = DateTime.UtcNow;

				if (dialog.DialogType == DialogType.Private)
					m.ID = Database.Instance ().SaveMessage (m);

					dialog.LastMessage = m.Text;
					dialog.LastMessageSent = DateTime.UtcNow;
					Database.Instance ().SaveDialog (dialog);

					try {
						var chatMessageExtraParameter = new ChatMessageExtraParameter (dialogId, true);

					if (dialog.DialogType == DialogType.Private){
							var opponentId = dialog.OccupantIds.Split (',').Select (Int32.Parse).First (id => id != App.QbProvider.UserId);
							App.QbProvider.GetXmppClient ().SendMessage (opponentId, message, chatMessageExtraParameter.Build(), dialogId, null);
					}
					if (dialog.DialogType == DialogType.Group){
						App.QbProvider.GetXmppClient ().SendMessage (dialog.XmppRoomJid, message, chatMessageExtraParameter.Build(), dialogId, null, MessageType.Groupchat);
					}
					} catch (Exception ex) {
						await App.Current.MainPage.DisplayAlert ("Error", ex.ToString(), "Ok");
					}

					messageEntry.Text = "";
				//}
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
