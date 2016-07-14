using QbChat.Pcl.Repository;
using QbChat.UWP.Views;
using Quickblox.Sdk.Modules.UsersModule.Models;
using System;
using System.Collections.Generic;

namespace QbChat.UWP.ViewModels
{
    public class ChatInfoViewModel : ViewModel
    {
        private string dialogId;
        private bool isLeaving;

        public ChatInfoViewModel(string dialogId)
        {
            this.dialogId = dialogId;
            this.LeaveChatCommand = new RelayCommand(LeaveChatCommandExecute);
            this.AddOccupantsCommand = new RelayCommand(this.AddOccupantsCommandExecute);
        }

        private List<User> users;
        private bool isLoading;

        public List<User> Users
        {
            get { return this.users; }
            set
            {
                this.users = value;
                this.RaisePropertyChanged();
            }
        }

        public RelayCommand LeaveChatCommand { get; private set; }

        public RelayCommand AddOccupantsCommand { get; private set; }

        public override async void OnAppearing()
        {
            base.OnAppearing();

            if (isLoading)
                return;

            this.isLoading = true;

            this.IsBusy = true;

            var dialog = Database.Instance().GetDialog(this.dialogId);

            var users = await App.QbProvider.GetUsersByIdsAsync(dialog.OccupantIds);
            Users = users;
            this.IsBusy = false;
        }

        private async void LeaveChatCommandExecute()
        {
            if (isLeaving)
                return;

            isLeaving = true;

            this.IsBusy = true;

            var dialog = new Windows.UI.Popups.MessageDialog("Do you really want to Leave Chat?", "Leave Chat");

            dialog.Commands.Add(new Windows.UI.Popups.UICommand("Yes") { Id = 0 });
            dialog.Commands.Add(new Windows.UI.Popups.UICommand("Cancel") { Id = 1 });

            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;
            var result = await dialog.ShowAsync();

            if ((int)result.Id == 0)
            {
                try
                {
                    var dialogInfo = await App.QbProvider.GetDialogAsync(this.dialogId);
                    if (dialogInfo != null)
                    {
                        dialogInfo.OccupantsIds.Remove(App.QbProvider.UserId);

                        var groupManager = App.QbProvider.GetXmppClient().GetGroupChatManager(dialogInfo.XmppRoomJid, dialogInfo.Id);
                        groupManager.NotifyAboutGroupUpdate(new List<int>(), new List<int>() { App.QbProvider.UserId }, dialogInfo);
                        groupManager.LeaveGroup(App.QbProvider.UserId.ToString());

                        await App.QbProvider.DeleteDialogAsync(dialogInfo.Id);
                        Database.Instance().DeleteDialog(dialogId);

                        while (App.NavigationFrame.CanGoBack)
                        {
                            App.NavigationFrame.GoBack();
                        }
                    }
                }
                catch (Exception ex)
                {
                    isLeaving = false;
                }
            }
            else
            {
                isLeaving = false;
            }

            this.IsBusy = false;
        }

        private void AddOccupantsCommandExecute()
        {
            App.NavigationFrame.Navigate(typeof(AddOccupantsIdsPage), this.dialogId);
        }
    }
}
