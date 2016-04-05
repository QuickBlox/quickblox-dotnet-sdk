using System;
using System.Collections.Generic;

using Xamarin.Forms;
using System.Threading.Tasks;
using XamarinForms.QbChat.Repository;
using System.Linq;

namespace XamarinForms.QbChat
{
	public partial class ChatInfoPage : ContentPage
	{
		private bool isLoading; 
		private string dialogId;
		private DialogTable dialog;

		public ChatInfoPage (string dialogId)
		{
			InitializeComponent ();

			this.dialogId = dialogId;
			this.dialog = Database.Instance ().GetDialog (this.dialogId);

			leaveChatButton.Clicked += OnLeaveChat;

			var addOccupantsItem = new ToolbarItem ("Add occupants", null, async () => {
				if (isLoading)
					return;
				App.Navigation.PushAsync (new AddOccupantsIdsPage (this.dialogId));
			});

			ToolbarItems.Add (addOccupantsItem);
		}

		protected override void OnAppearing ()
		{
			base.OnAppearing ();

			this.busyIndicator.IsVisible = true;

			var template = new DataTemplate (typeof(TextCell));
			template.SetBinding (TextCell.TextProperty, "FullName");
			listView.ItemTemplate = template;
			listView.ItemTapped += (object sender, ItemTappedEventArgs e) => {
				listView.SelectedItem = null;
			};

			Task.Factory.StartNew (async () => {
				var users = await App.QbProvider.GetUsersByIdsAsync(dialog.OccupantIds);
				Device.BeginInvokeOnMainThread(() => {
					listView.ItemsSource = users;
					this.busyIndicator.IsVisible = false;
				});
			});
		}

		private async void OnLeaveChat (object sender, EventArgs e)
		{
			if (isLoading)
				return;

			this.isLoading = true;

			this.busyIndicator.IsVisible = true;
			var result = await DisplayAlert ("Leave Chat", "Do you really want to Leave Chat?", "Yes", "Cancel");
			if (result) {
				try {
					var dialogInfo = await App.QbProvider.GetDialogAsync(dialog.DialogId);
					if (dialogInfo != null){
						var groupManager = App.QbProvider.GetXmppClient().GetGroupChatManager(dialog.XmppRoomJid, dialog.DialogId);
						//dialogInfo.OccupantsIds.Remove(App.QbProvider.UserId);
						groupManager.LeaveGroup(App.QbProvider.UserId.ToString ());
						groupManager.NotifyAboutGroupUpdate(new List<int>(), new List<int>() { App.QbProvider.UserId }, dialogInfo);

						var deleteResult = await App.QbProvider.DeleteDialogAsync(dialog.DialogId);
						if (deleteResult){
							Database.Instance().DeleteDialog(dialogId);
						}
						App.Navigation.PopToRootAsync ();
					}
				} catch (Exception ex) {
				} 
			}

			this.busyIndicator.IsVisible = false;
		}
	}
}

