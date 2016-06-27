using QbChat.Pcl;
using QbChat.Pcl.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QbChat.UWP.ViewModels
{
    public class AddOccupantsIdsViewModel : ViewModel
    {
        private string dialogId;
        private List<SelectedUser> users;
        private bool isLoading;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddOccupantsIdsViewModel"/> class.
        /// </summary>
        /// <param name="dialogId">The dialog identifier.</param>
        public AddOccupantsIdsViewModel(string dialogId)
        {
            this.dialogId = dialogId;
            SaveDialogChangesCommand = new RelayCommand(this.SaveDialogChangesCommandExecute);
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

        public RelayCommand SaveDialogChangesCommand { get; set; }

        public override async void OnAppearing()
        {
            base.OnAppearing();

            this.IsBusy = true;

            var dialogTable = Database.Instance().GetDialog(dialogId);
            var dialogIds = dialogTable.OccupantIds.Split(',').Select(u => Int32.Parse(u));
            var users = await App.QbProvider.GetUserByTag("XamarinChat");
            Users = users.Where(u => u.Id != App.QbProvider.UserId && !dialogIds.Contains(u.Id)).Select(u => new SelectedUser() { User = u }).ToList();
            this.IsBusy = false;
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
            }
        }

        private async void SaveDialogChangesCommandExecute()
        {
            if (isLoading)
                return;

            this.isLoading = true;
            await this.UpdateDialogs();
            App.NavigationFrame.GoBack();
            App.NavigationFrame.GoBack();
        }
    }
}
