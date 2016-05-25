using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Quickblox.Sdk.Modules.ChatModule.Models;
using Quickblox.Sdk.Modules.ChatXmppModule;
using QbChat.Pcl.Repository;
using QbChat.UWP;

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
            Reconnect();
        }

        //private async void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        //{
        //    if (messageEventArgs.MessageType == MessageType.Chat ||
        //        messageEventArgs.MessageType == MessageType.Groupchat)
        //    {
        //        string decodedMessage = System.Net.WebUtility.UrlDecode(messageEventArgs.Message.MessageText);

        //        var messageTable = new MessageTable();
        //        messageTable.SenderId = messageEventArgs.Message.SenderId;
        //        messageTable.DialogId = messageEventArgs.Message.ChatDialogId;
        //        messageTable.DateSent = messageEventArgs.Message.DateSent;

        //        if (messageEventArgs.Message.NotificationType != 0)
        //        {
        //            if (messageEventArgs.Message.NotificationType == NotificationTypes.GroupUpdate)
        //            {
        //                if (messageEventArgs.Message.DeletedOccupantsIds.Contains(App.QbProvider.UserId))
        //                {
        //                    await App.QbProvider.DeleteDialogAsync(messageEventArgs.Message.ChatDialogId);
        //                    Database.Instance().DeleteDialog(messageEventArgs.Message.ChatDialogId);
        //                    return;
        //                }

        //                if (messageEventArgs.Message.AddedOccupantsIds.Any())
        //                {
        //                    var userIds = new List<int>(messageEventArgs.Message.AddedOccupantsIds);
        //                    userIds.Add(messageEventArgs.Message.SenderId);
        //                    var users = await App.QbProvider.GetUsersByIdsAsync(string.Join(",", userIds));
        //                    var addedUsers = users.Where(u => u.Id != messageEventArgs.Message.SenderId);
        //                    var senderUser = users.First(u => u.Id == messageEventArgs.Message.SenderId);
        //                    messageTable.Text = senderUser.FullName + " added users: " + string.Join(",", addedUsers.Select(u => u.FullName));
        //                }
        //                else if (messageEventArgs.Message.DeletedOccupantsIds.Any())
        //                {
        //                    var userIds = new List<int>(messageEventArgs.Message.DeletedOccupantsIds);
        //                    var users = await App.QbProvider.GetUsersByIdsAsync(string.Join(",", userIds));
        //                    messageTable.Text = string.Join(",", users.Select(u => u.FullName)) + " left this room";
        //                }

        //                var dialogInfo = await App.QbProvider.GetDialogAsync(messageEventArgs.Message.ChatDialogId);
        //                if (dialogInfo == null)
        //                {
        //                    return;
        //                }
        //                var dialog = new DialogTable(dialogInfo);
        //                Database.Instance().SaveDialog(dialog);
        //            }
        //        }
        //        else {
        //            messageTable.Text = decodedMessage;
        //        }

        //       // await SetRecepientName(messageTable);
        //        Database.Instance().SaveMessage(messageTable);

        //        UpdateInDialogMessage(messageEventArgs.Message.ChatDialogId, decodedMessage);
        //    }
        //}

        //private void OnSystemMessageReceived(object sender, SystemMessageEventArgs messageEventArgs)
        //{
        //    var groupMessage = messageEventArgs.Message as GroupInfoMessage;
        //    if (groupMessage != null)
        //    {
        //        var dialog = new DialogTable
        //        {
        //            DialogId = groupMessage.DialogId,
        //            DialogType = groupMessage.DialogType,
        //            LastMessage = "Notification message",
        //            LastMessageSent = groupMessage.DateSent,
        //            Name = groupMessage.RoomName,
        //            Photo = groupMessage.RoomPhoto,
        //            OccupantIds = string.Join(",", groupMessage.CurrentOccupantsIds),
        //            XmppRoomJid = String.Format("{0}_{1}@{2}", ApplicationKeys.ApplicationId, groupMessage.DialogId, ApplicationKeys.ChatMucEndpoint)
        //        };

        //        App.QbProvider.GetXmppClient().JoinToGroup(dialog.XmppRoomJid, App.QbProvider.UserId.ToString());
        //        Database.Instance().SaveDialog(dialog);
        //    }
        //}

        public async Task<DialogTable> UpdateInDialogMessage(string chatDialogId, string decodedMessage)
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
                dialog.LastMessage = decodedMessage;
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
