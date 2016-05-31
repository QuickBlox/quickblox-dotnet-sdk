using QbChat.Pcl;
using QbChat.Pcl.Repository;
using QbChat.UWP.Providers;
using QbChat.UWP.Views;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatModule.Models;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using Quickblox.Sdk.Modules.UsersModule.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Xmpp.Im;

namespace QbChat.UWP.ViewModels
{
    public class ChatsViewModel : ViewModel
    {
        private string title;
        private bool isLogoutClicked;
        private object dialogAddLock = new object();

        public ChatsViewModel()
        {
            this.Dialogs = new ObservableCollection<DialogTable>();
            this.LogoutCommand = new RelayCommand(this.LogoutCommandExecute);
            this.CreateNewChatCommand = new RelayCommand(this.CreateNewChatCommandExecute);
            this.TappedCommand = new RelayCommand<DialogTable>(this.TappedCommandExecute);
        }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                this.RaisePropertyChanged();
            }
        }

        public RelayCommand LogoutCommand { get; set; }

        public RelayCommand CreateNewChatCommand { get; set; }

        public RelayCommand<DialogTable> TappedCommand { get; set; }

        public ObservableCollection<DialogTable> Dialogs { get; set; }

        public override async void OnAppearing()
        {
            base.OnAppearing();
            
            this.IsBusy = true;
            User user = null;
            if (user == null && App.QbProvider.UserId != 0)
            {
                user = await App.QbProvider.GetUserAsync(App.QbProvider.UserId);
                Title = user.FullName;

                App.UserId = user.Id;
                App.UserName = user.FullName;
                App.UserLogin = user.Login;
                App.UserPassword = user.Login;
            }

            try
            {
                // uses login as password because it is the same
                MessageProvider.Instance.ConnetToXmpp(user.Id, user.Login);
            }
            catch (Exception ex)
            {
            }

            List<DialogTable> sorted = null;
            try
            {
                await Task.Delay(1000);
                var dialogs = await App.QbProvider.GetDialogsAsync(new List<DialogType>() { DialogType.Private, DialogType.Group });
                sorted = dialogs.OrderByDescending(d => d.LastMessageSent).ToList();

                Debug.WriteLine("sorted");
                foreach (var dialog in sorted)
                {
                    Debug.WriteLine("dialog Name" + dialog.Name);
                    Debug.WriteLine("dialog DialogId" + dialog.DialogId);

                    if (dialog.DialogType == DialogType.Group)
                    {
                        App.QbProvider.GetXmppClient().JoinToGroup(dialog.XmppRoomJid, App.QbProvider.UserId.ToString());
                    }

                    dialog.LastMessage = System.Net.WebUtility.UrlDecode(dialog.LastMessage);
                    Debug.WriteLine("dialog LastMessage" + dialog.LastMessage);
                }
            }
            catch (Exception ex)
            {
            }


            Database.Instance().SaveAllDialogs(sorted);
            this.Dialogs.Clear();
            foreach (var dialogTable in sorted)
            {
                this.Dialogs.Add(dialogTable);
            }

            //if (myProfileImage.Source == null)
            //{
            //    myNameLabel.Text = user.FullName;
            //    InitializeProfilePhoto();
            //}

            this.IsBusy = false;

            App.QbProvider.GetXmppClient().MessageReceived += OnMessageReceived;
            App.QbProvider.GetXmppClient().SystemMessageReceived += OnSystemMessageReceived;
        }

        private void CreateNewChatCommandExecute()
        {
            App.NavigationFrame.Navigate(typeof(CreateDialogPage));
        }

        private async void LogoutCommandExecute()
        {
            if (this.isLogoutClicked)
                return;
            this.isLogoutClicked = true;

            var dialog = new Windows.UI.Popups.MessageDialog("Do you really want to Log Out?", "Log Out");

            dialog.Commands.Add(new Windows.UI.Popups.UICommand("Yes") { Id = 0 });
            dialog.Commands.Add(new Windows.UI.Popups.UICommand("No") { Id = 1 });
                 
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;
            var result = await dialog.ShowAsync();

            if ((int)result.Id == 0)
            {
                try
                {
                    App.QbProvider.GetXmppClient().MessageReceived -= OnMessageReceived;
                    App.QbProvider.GetXmppClient().SystemMessageReceived -= OnSystemMessageReceived;
                    Database.Instance().ResetAll();
                    App.UserLogin = null;
                    App.UserId = 0;
                    App.UserPassword = null;
                    MessageProvider.Instance.DisconnectToXmpp();
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    while (App.NavigationFrame.CanGoBack)
                    {
                        App.NavigationFrame.GoBack();
                    }
                }
            }

            this.isLogoutClicked = false;
        }

        private void TappedCommandExecute(DialogTable dialogItem)
        {
            if (dialogItem.DialogType == Quickblox.Sdk.Modules.ChatModule.Models.DialogType.Private)
                App.NavigationFrame.Navigate(typeof(PrivateChatPage), dialogItem.DialogId);
            else
                App.NavigationFrame.Navigate(typeof(GroupChatPage), dialogItem.DialogId);
        }

        private async void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            if (messageEventArgs.MessageType == MessageType.Chat ||
                messageEventArgs.MessageType == MessageType.Groupchat)
            {
                if (messageEventArgs.Message.NotificationType != 0)
                {
                    if (messageEventArgs.Message.NotificationType == NotificationTypes.GroupUpdate)
                    {
                        if (messageEventArgs.Message.DeletedOccupantsIds.Contains(App.QbProvider.UserId))
                        {
                            //var deleteResult = 
                            await App.QbProvider.DeleteDialogAsync(messageEventArgs.Message.ChatDialogId);
                            //if (deleteResult){
                            Database.Instance().DeleteDialog(messageEventArgs.Message.ChatDialogId);
                            //}

                            var seletedDialog = this.Dialogs.FirstOrDefault(d => d.DialogId == messageEventArgs.Message.ChatDialogId);
                            if (seletedDialog != null)
                            {
                                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   this.Dialogs.Remove(seletedDialog);
                               });
                            }

                            return;
                        }
                    }
                }

                string decodedMessage = System.Net.WebUtility.UrlDecode(messageEventArgs.Message.MessageText);
                var dialog = await MessageProvider.Instance.UpdateInDialogMessage(messageEventArgs.Message.ChatDialogId, decodedMessage);

                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    AddDialogToList(dialog);
                });
                Database.Instance().SaveDialog(dialog);
            }
        }

        private void OnSystemMessageReceived(object sender, SystemMessageEventArgs messageEventArgs)
        {
            var groupMessage = messageEventArgs.Message as GroupInfoMessage;
            if (groupMessage != null)
            {
                var dialog = new DialogTable
                {
                    DialogId = groupMessage.DialogId,
                    DialogType = groupMessage.DialogType,
                    LastMessage = "Notification message",
                    LastMessageSent = groupMessage.DateSent,
                    Name = groupMessage.RoomName,
                    Photo = groupMessage.RoomPhoto,
                    OccupantIds = string.Join(",", groupMessage.CurrentOccupantsIds),
                    XmppRoomJid = String.Format("{0}_{1}@{2}", ApplicationKeys.ApplicationId, groupMessage.DialogId, ApplicationKeys.ChatMucEndpoint)
                };

                App.QbProvider.GetXmppClient().JoinToGroup(dialog.XmppRoomJid, App.QbProvider.UserId.ToString());
                AddDialogToList(dialog);
                Database.Instance().SaveDialog(dialog);
            }
        }

        public void AddDialogToList(DialogTable dialogTable)
        {
            lock (dialogAddLock)
            {
                var dialog = Dialogs.FirstOrDefault((d => d.DialogId == dialogTable.DialogId));
                if (dialog == null)
                {
                    Dialogs.Insert(0, dialogTable);
                }
                else
                {
                    if (Dialogs.IndexOf(dialog) == 0)
                    {
                        dialog.LastMessage = dialogTable.LastMessage;
                        dialog.LastMessageSent = dialogTable.LastMessageSent;
                    }
                    else
                    {
                        Dialogs.Remove(dialog);
                        Dialogs.Insert(0, dialogTable);
                    }
                }
            }
        }
    }
}
