using System;
using System.Collections.Generic;

using Xamarin.Forms;
using System.Threading.Tasks;
using System.Linq;
using Quickblox.Sdk.Modules.ChatModule.Models;
using XamarinForms.QbChat.Repository;
using XamarinForms.QbChat.Pages;
using Quickblox.Sdk.Modules.Models;

namespace XamarinForms.QbChat
{
	public partial class CreateDialogPage : ContentPage
	{
		bool isLoaded;
		List<SelectedUser> UsersForList { get; set;}

		public CreateDialogPage ()
		{
			InitializeComponent ();

			var createNewChat = new ToolbarItem ("Create", null, CreateChat);
			ToolbarItems.Add (createNewChat);
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			if (isLoaded)
				return;

			isLoaded = true;

			busyIndicator.IsVisible = true; 

			Task.Factory.StartNew (async () => {
				var users = await App.QbProvider.GetUserByTag("XamarinChat");
				UsersForList = users.Where(u => u.Id != App.QbProvider.UserId).Select(u => new SelectedUser() { User = u }).ToList();

				Device.BeginInvokeOnMainThread(() => {
					//var cellTemplate = new DataTemplate(typeof(ChatCell));
					//cellTemplate.SetBinding (TextCell.TextProperty, "FullName");
					//cellTemplate.SetBinding(CustomSelectableCell.TextProperty, "FullName");
					//listView.ItemTemplate = cellTemplate;
					listView.ItemSelected += (o, e) => { listView.SelectedItem = null; };
					listView.ItemsSource = UsersForList;
					busyIndicator.IsVisible = false; 
				});
			});
		}

		static void SaveDialogToDb (Dialog dialog)
		{
			if (dialog != null) {
				var dialogTable = new DialogTable (dialog);
				dialogTable.LastMessageSent = DateTime.UtcNow;
				Database.Instance ().SaveDialog (dialogTable);
			}
		}

		private async void CreateChat ()
		{
			this.busyIndicator.IsVisible = true;
			var selectedUsers = UsersForList.Where (u => u.IsSelected).Select(u => u.User).ToList ();
			if (selectedUsers.Any()) {
				DialogType dialogType = DialogType.Group;
				if (selectedUsers.Count == 1){
					dialogType = DialogType.Private;
				}

				var dialogName = string.Join (", ", selectedUsers.Select (u => u.FullName));
				var userIds = selectedUsers.Select (u => u.Id).ToList ();
				var userIdsString = string.Join (",", userIds);

				Dialog dialog = null;
				if (dialogType == DialogType.Group) {
					dialog = await App.QbProvider.CreateDialogAsync (dialogName, userIdsString, dialogType);
					SaveDialogToDb (dialog);

					var groupManager = App.QbProvider.GetXmppClient ().GetGroupChatManager (dialog.XmppRoomJid, dialog.Id);
					groupManager.JoinGroup (App.QbProvider.UserId.ToString());
					groupManager.NotifyAboutGroupCreation (userIds, dialog);

					var groupChantPage = new GroupChatPage (dialog.Id);
					App.Navigation.InsertPageBefore (groupChantPage, this);
					App.Navigation.PopAsync ();
				} else if (dialogType == DialogType.Private) {
					dialog = await App.QbProvider.GetDialogAsync (new int[] { App.QbProvider.UserId, userIds.First () });
					if (dialog == null) {
						dialog = await App.QbProvider.CreateDialogAsync (dialogName, userIdsString, dialogType);
						var privateManager = App.QbProvider.GetXmppClient ().GetPrivateChatManager (selectedUsers.First ().Id, dialog.Id);
						var message = "Hello, I created chat with you!";
						privateManager.SendMessage (message);
						dialog.LastMessage = message;
					} 

					SaveDialogToDb (dialog);
					var privateChantPage = new PrivateChatPage (dialog.Id);
					App.Navigation.InsertPageBefore (privateChantPage, this);
					App.Navigation.PopAsync ();
				}
			} else {
				DisplayAlert ("Error", "Please, select any of users", "Ok");
			}

			this.busyIndicator.IsVisible = false;
		}
	}
}

