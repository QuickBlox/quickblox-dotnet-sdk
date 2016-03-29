using System;
using System.Collections.Generic;

using Xamarin.Forms;
using System.Threading.Tasks;
using System.Linq;
using XamarinForms.QbChat.Repository;

namespace XamarinForms.QbChat
{
	public partial class AddOccupantsIdsPage : ContentPage
	{
		private string dialogId;
		private List<SelectedUser> UsersForList { get; set;}

		public AddOccupantsIdsPage (string outDialogId)
		{
			InitializeComponent ();
			this.dialogId = outDialogId;

			var doneItem = new ToolbarItem ("Done", null, async () => {
				await this.UpdateDialogs();
				await App.Navigation.PopAsync(false);
				await App.Navigation.PopAsync();
			});

			ToolbarItems.Add (doneItem);
		}

		protected override void OnAppearing ()
		{
			base.OnAppearing ();

			busyIndicator.IsVisible = true; 

			var dialogTable = Database.Instance().GetDialog(dialogId);
			var dialogIds = dialogTable.OccupantIds.Split (',').Select(u => Int32.Parse(u));
			Task.Factory.StartNew (async () => {
				var users = await App.QbProvider.GetUserByTag("XamarinChat");
				UsersForList = users.Where(u => u.Id != App.QbProvider.UserId && !dialogIds.Contains(u.Id)).Select(u => new SelectedUser() { User = u }).ToList();

				Device.BeginInvokeOnMainThread(() => {
					listView.ItemSelected += (o, e) => { listView.SelectedItem = null; };
					listView.ItemsSource = UsersForList;
					busyIndicator.IsVisible = false; 
				});
			});
		}

		private async Task UpdateDialogs ()
		{
			var addedUserIds = UsersForList.Where (u => u.IsSelected).Select (u => u.User.Id).ToList();
			var dialog = await App.QbProvider.UpdateDialogAsync (this.dialogId, addedUserIds);
			if (dialog != null) {
				Database.Instance ().SaveDialog (new DialogTable (dialog));
				var groupManager = App.QbProvider.GetXmppClient ().GetGroupChatManager (dialog.XmppRoomJid, dialog.Id);
				groupManager.NotifyAboutGroupUpdate (addedUserIds, new List<int> (), dialog); 
			}
		}
	}
}

