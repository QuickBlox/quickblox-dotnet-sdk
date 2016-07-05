using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Quickblox.Sdk.Modules.ChatModule.Models;
using Quickblox.Sdk.Modules.ChatXmppModule;
using QbChat.Pcl.Repository;
using QbChat.UWP;
using Quickblox.Sdk.GeneralDataModel.Models;

namespace QbChat.UWP.Providers
{
    public class MessageProvider
    {
        private static MessageProvider instance = new MessageProvider();
        private MessageProvider() { }

        public static MessageProvider Instance
        {
            get
            {
                if (instance == null) instance = new MessageProvider();
                return instance;
            }
        }

        public void ConnetToXmpp(int userId, string userPassword)
        {
            if (!App.QbProvider.GetXmppClient().IsConnected)
            {
                App.QbProvider.GetXmppClient().ErrorReceived -= OnError;
                App.QbProvider.GetXmppClient().ErrorReceived += OnError;

                App.QbProvider.GetXmppClient().StatusChanged -= OnStatusChanged;
                App.QbProvider.GetXmppClient().StatusChanged += OnStatusChanged;
                App.QbProvider.GetXmppClient().Connect(userId, userPassword);
            }
        }

        public void DisconnectToXmpp()
        {
            if (App.QbProvider.GetXmppClient().IsConnected)
            {
                App.QbProvider.GetXmppClient().ErrorReceived -= OnError;
                App.QbProvider.GetXmppClient().StatusChanged -= OnStatusChanged;
                App.QbProvider.GetXmppClient().Close();
            }
        }

        private void OnStatusChanged(object sender, StatusEventArgs statusEventArgs)
        {
            Debug.WriteLine("Xmpp Status: " + statusEventArgs.Jid + " Status: " + statusEventArgs.Status.Availability);
        }

        private async void OnError(object sender, ErrorEventArgs errorsEventArgs)
        {
            Debug.WriteLine("Xmpp Error: " + errorsEventArgs.Exception + " Reason: " + errorsEventArgs.Reason);
            //Reconnect();
        }

        public async Task<DialogTable> UpdateInDialogMessage(string chatDialogId, Message message)
        {
            var dialog = Database.Instance().GetDialog(chatDialogId);
            if (dialog == null)
            {
                var dialogInfo = await App.QbProvider.GetDialogAsync(chatDialogId);
                if (dialogInfo == null)
                {
                    return null;
                }
                dialog = new DialogTable(dialogInfo);
            }
            if (dialog != null)
            {
                dialog.LastMessage = System.Net.WebUtility.UrlDecode(message.MessageText);
                dialog.LastMessageSent = DateTime.UtcNow;
                if (dialog.UnreadMessageCount != null)
                {
                    dialog.UnreadMessageCount++;
                }
                else {
                    dialog.UnreadMessageCount = 1;
                }
                Database.Instance().SaveDialog(dialog);
            }

            return dialog;
        }

        public async void Reconnect()
        {
            // Reconecting:
            while (!App.QbProvider.GetXmppClient().IsConnected)
            {
                bool isWait = false;
                try
                {
                    // Logout action
                    //if (isLogoutClicked)
                    //    return;
                    App.QbProvider.GetXmppClient().Connect(App.UserId, App.UserPassword);

                    var dialogs = await App.QbProvider.GetDialogsAsync(new List<DialogType>() { DialogType.Group });
                    foreach (var dialog in dialogs)
                    {
                        var groupdManager = App.QbProvider.GetXmppClient().GetGroupChatManager(dialog.XmppRoomJid, dialog.DialogId);
                        groupdManager.JoinGroup(App.QbProvider.UserId.ToString());
                    }
                }
                catch (Exception ex)
                {
                    isWait = true;
                }
                if (isWait)
                {
                    await Task.Delay(3000);
                }
            }
        }
    }
}
