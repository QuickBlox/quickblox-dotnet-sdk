using System;
using Quickblox.Sdk.Modules.ChatModule.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using Xamarin.Forms;
using XamarinForms.QbChat.Providers;
using Quickblox.Sdk.Modules.UsersModule.Models;
using XamarinForms.QbChat.Pages;
using Xmpp.Im;
using QbChat.Pcl.Repository;
using QbChat.Pcl;

namespace XamarinForms.QbChat.ViewModels
{
    public class ChatsViewModel : ViewModel
    {
        private string title;
        private bool isLogoutClicked;
        private object dialogAddLock = new object();
        private bool isLoaded;

        public ChatsViewModel()
        {
            this.Dialogs = new ObservableCollection<DialogTable>();    
            this.LogoutCommand = new Command(this.LogoutCommandExecute);
            this.CreateNewChatCommand = new Command(this.CreateNewChatCommandExecute);
            this.TappedCommand = new Command<DialogTable>(this.TappedCommandExecute);
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

        public ICommand LogoutCommand { get; set; }

        public ICommand CreateNewChatCommand { get; set; }

        public ICommand TappedCommand { get; set; }

        public ObservableCollection<DialogTable> Dialogs { get; set; }

        public override async void OnAppearing()
        {
            base.OnAppearing();

            if (isLoaded)
                return;

            isLoaded = true;

            this.IsBusyIndicatorVisible = true;
            await Task.Factory.StartNew(async () =>
            {
                User user = null;
                if (user == null && App.QbProvider.UserId != 0)
                {
                    user = await App.QbProvider.GetUserAsync(App.QbProvider.UserId);
                    Device.BeginInvokeOnMainThread(() => {
                        Title = user.FullName;
                    });

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
                Device.BeginInvokeOnMainThread(() =>
                {
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

                    this.IsBusyIndicatorVisible = false;
                });

                App.QbProvider.GetXmppClient().MessageReceived += OnMessageReceived;
                App.QbProvider.GetXmppClient().SystemMessageReceived += OnSystemMessageReceived;
            });
        }

        private void CreateNewChatCommandExecute(object obj)
        {
            App.Navigation.PushAsync(new CreateDialogPage());
        }

        private async void LogoutCommandExecute(object obj)
        {
            if (this.isLogoutClicked)
                return;
            this.isLogoutClicked = true;

            var result = await App.Current.MainPage.DisplayAlert("Log Out", "Do you really want to Log Out?", "Ok", "Cancel");
            if (result)
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
                    App.SetLoginPage();
                }
            }

            this.isLogoutClicked = false;
        }

        private void TappedCommandExecute(DialogTable dialogItem)
        {
            if (dialogItem.DialogType == Quickblox.Sdk.Modules.ChatModule.Models.DialogType.Private)
                App.Navigation.PushAsync(new PrivateChatPage(dialogItem.DialogId));
            else
                App.Navigation.PushAsync(new GroupChatPage(dialogItem.DialogId));
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
                                this.Dialogs.Remove(seletedDialog);
                            }

                            return;
                        }
                    }
                }

                var dialog = await MessageProvider.Instance.UpdateInDialogMessage(messageEventArgs.Message.ChatDialogId, messageEventArgs.Message);
                AddDialogToList(dialog);
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

        //		private void InitializeProfilePhoto ()
        //		{
        //			myProfileImage.Source = Device.OnPlatform (iOS: ImageSource.FromFile ("ic_user.png"), Android: ImageSource.FromFile ("ic_user.png"), WinPhone: ImageSource.FromFile ("Images/ic_user.png"));
        //			if (user.BlobId.HasValue) {
        //				App.QbProvider.GetImageAsync (user.BlobId.Value).ContinueWith ((task, result) =>  {
        //					var bytes = task.ConfigureAwait (true).GetAwaiter ().GetResult ();
        //					if (bytes != null)
        //						Device.BeginInvokeOnMainThread (() => myProfileImage.Source = ImageSource.FromStream (() => new MemoryStream (bytes)));
        //				}, TaskScheduler.FromCurrentSynchronizationContext ());
        //			}
        //		}

    }
}
