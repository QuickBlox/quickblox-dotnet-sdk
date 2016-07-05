using QbChat.Pcl;
using QbChat.Pcl.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace XamarinForms.QbChat.ViewModels
{
    public class AddOccupantsIdsViewModel : ViewModel
    {
        private string dialogId;
        private List<SelectedUser> users;
        private bool isLoading;

        public AddOccupantsIdsViewModel(string dialogId)
        {
            this.dialogId = dialogId;
            SaveDialogChangesCommand = new Command(this.SaveDialogChangesCommandExecute);
        }

        public List<SelectedUser> Users
        {
            get { return users; }
            set
            {
                users = value;
                this.RaisePropertyChanged();
            }
        }

        public ICommand SaveDialogChangesCommand { get; set; }

        public override void OnAppearing()
        {
            base.OnAppearing();

            this.IsBusyIndicatorVisible = true;

            var dialogTable = Database.Instance().GetDialog(dialogId);
            var dialogIds = dialogTable.OccupantIds.Split(',').Select(u => Int32.Parse(u));
            Task.Factory.StartNew(async () => {
                var users = await App.QbProvider.GetUserByTag("XamarinChat");
                Device.BeginInvokeOnMainThread(() => {
                    Users = users.Where(u => u.Id != App.QbProvider.UserId && !dialogIds.Contains(u.Id)).Select(u => new SelectedUser() { User = u }).ToList();
                    this.IsBusyIndicatorVisible = false;
                });
            });
        }

        private async Task UpdateDialogs()
        {
            var addedUserIds = Users.Where(u => u.IsSelected).Select(u => u.User.Id).ToList();
            if (addedUserIds.Any())
            {
                var dialog = await App.QbProvider.UpdateDialogAsync(this.dialogId, addedUserIds);
				if (dialog != null)
				{
					Database.Instance().SaveDialog(new DialogTable(dialog));
					var groupManager = App.QbProvider.GetXmppClient().GetGroupChatManager(dialog.XmppRoomJid, dialog.Id);
					groupManager.NotifyAboutGroupUpdate(addedUserIds, new List<int>(), dialog);
				}
				else {
					await App.Current.MainPage.DisplayAlert("Internet connection", "Internet connection is lost. Please check it and restart the Application", "Ok");

				}
            }
        }

        private async void SaveDialogChangesCommandExecute(object obj)
        {
			if (Users == null)
				return;
			
            if (isLoading)
                return;

            this.isLoading = true;
			await this.UpdateDialogs();
            await App.Navigation.PopAsync(false);
            await App.Navigation.PopAsync();
        }
    }
}
