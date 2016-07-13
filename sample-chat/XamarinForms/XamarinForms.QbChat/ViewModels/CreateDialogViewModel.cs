using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Quickblox.Sdk.Modules.Models;
using Xamarin.Forms;
using Quickblox.Sdk.Modules.ChatModule.Models;
using Acr.UserDialogs;
using XamarinForms.QbChat.Pages;
using QbChat.Pcl;
using QbChat.Pcl.Repository;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.GeneralDataModel.Models;

namespace XamarinForms.QbChat.ViewModels
{
	public class CreateDialogViewModel : ViewModel
	{
		GroupChatManager groupManager;

		private List<SelectedUser> users;
		private bool isCreating;

		public CreateDialogViewModel()
		{
			this.CreateChatCommand = new Command(this.CreateChatCommandExecute);
		}

		public List<SelectedUser> Users
		{
			get { return this.users; }
			set
			{
				this.users = value;
				this.RaisePropertyChanged();
			}
		}

		public ICommand CreateChatCommand { get; private set; }

		public override void OnAppearing()
		{
			base.OnAppearing();

			this.IsBusyIndicatorVisible = true;

			Task.Factory.StartNew(async () =>
			{
				var users = await App.QbProvider.GetUserByTag("XamarinChat");
				Device.BeginInvokeOnMainThread(() =>
				{
					Users =
						users.Where(u => u.Id != App.QbProvider.UserId)
							.Select(u => new SelectedUser() { User = u })
							.ToList();
					this.IsBusyIndicatorVisible = false;
				});
			});
		}

		private void SaveDialogToDb(Dialog dialog)
		{
			if (dialog != null)
			{
				var dialogTable = new DialogTable(dialog);
				dialogTable.LastMessageSent = DateTime.UtcNow;
				Database.Instance().SaveDialog(dialogTable);
			}
		}

		public async void CreateChatCommandExecute(object obj)
		{
			if (this.isCreating)
				return;

			this.isCreating = true;

			if (Users == null)
				return;

			try
			{
				this.IsBusyIndicatorVisible = true;
				var selectedUsers = Users.Where(u => u.IsSelected).Select(u => u.User).ToList();
				if (selectedUsers.Any())
				{
					string dialogName = null;
					DialogType dialogType = DialogType.Group;
					if (selectedUsers.Count == 1)
					{
						dialogType = DialogType.Private;
						dialogName = selectedUsers.First().FullName;
					}
					else
					{
						var promptResult = await
							UserDialogs.Instance.PromptAsync("Enter chat name:", null, "Create", "Cancel",
								"Enter chat name", InputType.Name);

						if (promptResult.Ok)
						{
							dialogName = promptResult.Text;

							if (string.IsNullOrWhiteSpace(dialogName))
								dialogName = App.UserName + "_" +
											 string.Join(", ", selectedUsers.Select(u => u.FullName));
						}
						else
						{
							this.IsBusyIndicatorVisible = false;
							this.isCreating = false;
							return;
						}
					}

					var userIds = selectedUsers.Select(u => u.Id).ToList();
					var userIdsString = string.Join(",", userIds);

					Dialog dialog = null;
					if (dialogType == DialogType.Group)
					{
						dialog = await App.QbProvider.CreateDialogAsync(dialogName.Trim(), userIdsString, dialogType);
						if (dialog != null)
						{
							SaveDialogToDb(dialog);

							groupManager = App.QbProvider.GetXmppClient()
								.GetGroupChatManager(dialog.XmppRoomJid, dialog.Id);
							groupManager.MessageReceived += OnMessageReceived;
							groupManager.JoinGroup(App.QbProvider.UserId.ToString());
							await Task.Delay(1000);
							groupManager.NotifyAboutGroupCreation(userIds, dialog);
							return;
						}
					}
					else if (dialogType == DialogType.Private)
					{
						dialog = await App.QbProvider.GetDialogAsync(new int[] { App.QbProvider.UserId, userIds.First() });
						if (dialog == null)
						{
							dialog = await App.QbProvider.CreateDialogAsync(dialogName, userIdsString, dialogType);
							var privateManager =
								App.QbProvider.GetXmppClient()
									.GetPrivateChatManager(selectedUsers.First().Id, dialog.Id);
							var message = "Hello, I created chat with you!";
							privateManager.SendMessage(message);
							dialog.LastMessage = message;
						}

						SaveDialogToDb(dialog);
						var privateChantPage = new PrivateChatPage(dialog.Id);
						App.Navigation.InsertPageBefore(privateChantPage,
							(App.Current.MainPage as NavigationPage).CurrentPage);
						App.Navigation.PopAsync();
					}
				}
				else
				{
					App.Current.MainPage.DisplayAlert("Error", "Please, select any of users", "Ok");
				}

				this.IsBusyIndicatorVisible = false;
			}
			catch (Exception ex)
			{
			}

			this.isCreating = false;
		}

		private void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
		{
			var message = messageEventArgs.Message;
			if (message.NotificationType == NotificationTypes.GroupCreate ||
				message.NotificationType == NotificationTypes.GroupUpdate)
			{
				if (message.AddedOccupantsIds.Any())
				{
					groupManager.MessageReceived -= OnMessageReceived;

					Device.BeginInvokeOnMainThread(() =>
					{
						var groupChantPage = new GroupChatPage(message.ChatDialogId);
						App.Navigation.InsertPageBefore(groupChantPage,
							(App.Current.MainPage as NavigationPage).CurrentPage);
						App.Navigation.PopAsync();
					});
				}
			}
		}
	}
}