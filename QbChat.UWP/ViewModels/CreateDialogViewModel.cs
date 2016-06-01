using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Quickblox.Sdk.Modules.Models;
using Quickblox.Sdk.Modules.ChatModule.Models;
using QbChat.Pcl;
using QbChat.Pcl.Repository;
using QbChat.UWP.ViewModels;
using QbChat.UWP;
using QbChat.UWP.Views;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace QbChat.UWP.ViewModels
{
    public class CreateDialogViewModel : ViewModel
    {
        private List<SelectedUser> users;

        public CreateDialogViewModel()
        {
            this.CreateChatCommand = new RelayCommand(this.CreateChatCommandExecute);
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

        public RelayCommand CreateChatCommand { get; private set; }

        public override async void OnAppearing()
        {
            base.OnAppearing();

            this.IsBusy = true;

            var users = await App.QbProvider.GetUserByTag("XamarinChat");
            Users =
                users.Where(u => u.Id != App.QbProvider.UserId)
                    .Select(u => new SelectedUser() { User = u })
                    .ToList();
            this.IsBusy = false;
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

        public async void CreateChatCommandExecute()
        {
            if (Users == null)
                return;

            try
            {
                this.IsBusy = true;
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
                        //var promptResult = await
                        //    UserDialogs.Instance.PromptAsync("Enter chat name:", null, "Create", "Cancel",
                        //        "Enter chat name", InputType.Name);

                        var text = await ShowDialog();
                        if (!string.IsNullOrEmpty(text))
                        {
                            dialogName = text;
                            if (string.IsNullOrEmpty(dialogName))
                                dialogName = App.UserName + "_" +
                                             string.Join(", ", selectedUsers.Select(u => u.FullName));
                        }
                        else
                        {
                            this.IsBusy = false;
                            return;
                        }
                    }

                    var userIds = selectedUsers.Select(u => u.Id).ToList();
                    var userIdsString = string.Join(",", userIds);

                    Dialog dialog = null;
                    if (dialogType == DialogType.Group)
                    {
                        dialog = await App.QbProvider.CreateDialogAsync(dialogName, userIdsString, dialogType);
                        SaveDialogToDb(dialog);

                        var groupManager = App.QbProvider.GetXmppClient()
                            .GetGroupChatManager(dialog.XmppRoomJid, dialog.Id);
                        groupManager.JoinGroup(App.QbProvider.UserId.ToString());
                        groupManager.NotifyAboutGroupCreation(userIds, dialog);

                        App.NavigationFrame.GoBack();
                        App.NavigationFrame.Navigate(typeof(GroupChatPage), dialog.Id);
                    }
                    else if (dialogType == DialogType.Private)
                    {
                        dialog = await App.QbProvider.GetDialogAsync(new int[] {App.QbProvider.UserId, userIds.First()});
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

                        App.NavigationFrame.GoBack();
                        App.NavigationFrame.Navigate(typeof(PrivateChatPage), dialog.Id);
                    }
                }
                else
                {
                    await new Windows.UI.Popups.MessageDialog("Please, select any of users", "Error").ShowAsync();
                }

                this.IsBusy = false;
            }
            catch (Exception ex)
            {
                
            }
        }

        private async Task<string> ShowDialog()
        {
            var dialog = new ContentDialog()
            {
                Title = "Enter chat name:",
                //RequestedTheme = ElementTheme.Dark,
                //FullSizeDesired = true,
                //MaxWidth = this.ActualWidth // Required for Mobile!
            };

            // Setup Content
            var panel = new StackPanel();
            
            var textEdit = new TextBox();

            panel.Children.Add(textEdit);
            dialog.Content = panel;

            // Add Buttons
            dialog.PrimaryButtonText = "Create";
            dialog.SecondaryButtonText = "Cancel";

            // Show Dialog
            var result = await dialog.ShowAsync();
            return textEdit.Text;
        }
    }
}