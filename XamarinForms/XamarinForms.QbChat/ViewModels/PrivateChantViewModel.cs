using Quickblox.Sdk.Modules.ChatXmppModule;
using System;
using System.Collections.Generic;
using System.Linq;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Xamarin.Forms;
using XamarinForms.QbChat.Repository;
using System.Threading.Tasks;
using System.IO;
using XamarinForms.QbChat.Pages;

namespace XamarinForms.QbChat.ViewModels
{
    public class PrivateChantViewModel : BaseChatViewModel
    {
        private PrivateChatManager privateChatManager;

        public PrivateChantViewModel(string dialogId) : base (dialogId)
        {
            base.SendMessageCommand = new Command(this.SendMessageCommandExecute);
        }

        public override void OnAppearing()
        {
            base.OnAppearing();

            this.IsBusyIndicatorVisible = true;

            var dialog = Database.Instance().GetDialog(dialogId);
            var opponentId =
                dialog.OccupantIds.Split(',').Select(Int32.Parse).First(id => id != App.QbProvider.UserId);
            privateChatManager = App.QbProvider.GetXmppClient().GetPrivateChatManager(opponentId, dialogId);
            privateChatManager.MessageReceived += OnMessageReceived;

            DialogName = dialog.Name;
            ImageSource = Device.OnPlatform(iOS: ImageSource.FromFile("privateholder.png"),
                Android: ImageSource.FromFile("privateholder.png"),
                WinPhone: ImageSource.FromFile("privateholder.png"));

            Task.Factory.StartNew(async () =>
            {
                var users = await App.QbProvider.GetUsersByIdsAsync(dialog.OccupantIds);
                var opponentUser = users.FirstOrDefault(u => u.Id != App.QbProvider.UserId);
                if (opponentUser != null && opponentUser.BlobId.HasValue)
                {
                    await App.QbProvider.GetImageAsync(opponentUser.BlobId.Value).ContinueWith((task, result) =>
                    {
                        var bytes = task.ConfigureAwait(true).GetAwaiter().GetResult();
                        if (bytes != null)
                            Device.BeginInvokeOnMainThread(() =>
                                ImageSource = ImageSource.FromStream(() => new MemoryStream(bytes)));
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }

                this.opponentUsers = new List<User>() { opponentUser };
                await this.LoadMessages();

                Device.BeginInvokeOnMainThread(() =>
                    this.IsBusyIndicatorVisible = false
                );
            });
        }

        public async Task LoadMessages()
        {
            List<Message> messages;
            try
            {
                messages = await App.QbProvider.GetMessagesAsync(dialogId);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.ToString(), "Ok");
                return;
            }

            if (messages != null)
            {
                messages = messages.OrderBy(message => message.DateSent).ToList();
                foreach (var message in messages)
                {
                    var chatMessage = new MessageTable();
                    chatMessage.DateSent = message.DateSent;
                    chatMessage.SenderId = message.SenderId;
                    chatMessage.MessageId = message.Id;
                    if (message.RecipientId.HasValue)
                        chatMessage.RecepientId = message.RecipientId.Value;
                    chatMessage.DialogId = message.ChatDialogId;
                    chatMessage.IsRead = message.Read == 1;

                    await this.SetRecepientName(chatMessage);
                    
                    chatMessage.Text = System.Net.WebUtility.UrlDecode(message.MessageText);

                    Device.BeginInvokeOnMainThread(() =>
                        Messages.Add(chatMessage)
                    );
                }

                Device.BeginInvokeOnMainThread(() =>
                    ((App.Current.MainPage as NavigationPage).CurrentPage as PrivateChatPage).ScrollList()
                );
            }
        }

        private async void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            if (messageEventArgs.MessageType == MessageType.Chat ||
                messageEventArgs.MessageType == MessageType.Groupchat)
            {
                string decodedMessage = System.Net.WebUtility.UrlDecode(messageEventArgs.Message.MessageText);

                var messageTable = new MessageTable();
                messageTable.SenderId = messageEventArgs.Message.SenderId;
                messageTable.DialogId = messageEventArgs.Message.ChatDialogId;
                messageTable.DateSent = messageEventArgs.Message.DateSent;

                if (messageEventArgs.Message.NotificationType != 0)
                {
                    if (messageEventArgs.Message.NotificationType == NotificationTypes.GroupUpdate)
                    {
                        if (messageEventArgs.Message.AddedOccupantsIds.Any())
                        {
                            var userIds = new List<int>(messageEventArgs.Message.AddedOccupantsIds);
                            userIds.Add(messageEventArgs.Message.SenderId);
                            var users = await App.QbProvider.GetUsersByIdsAsync(string.Join(",", userIds));
                            var addedUsers = users.Where(u => u.Id != messageEventArgs.Message.SenderId);
                            var senderUser = users.First(u => u.Id == messageEventArgs.Message.SenderId);
                            messageTable.Text = senderUser.FullName + " added users: " +
                                                string.Join(",", addedUsers.Select(u => u.FullName));
                        }
                        else if (messageEventArgs.Message.DeletedOccupantsIds.Any())
                        {
                            var userIds = new List<int>(messageEventArgs.Message.DeletedOccupantsIds);
                            var users = await App.QbProvider.GetUsersByIdsAsync(string.Join(",", userIds));
                            messageTable.Text = string.Join(",", users.Select(u => u.FullName)) + " left this room";
                        }

                        //var dialogInfo = await App.QbProvider.GetDialogAsync(messageEventArgs.Message.ChatDialogId);
                        //if (dialogInfo == null)
                        //{
                        //    return;
                        //}
                        //var dialog = new DialogTable(dialogInfo);
                        //Database.Instance().SaveDialog(dialog);
                    }
                }
                else
                {
                    messageTable.Text = decodedMessage;
                }

                await SetRecepientName(messageTable);

                this.Messages.Add(messageTable);

                Device.BeginInvokeOnMainThread(() =>
                   ((App.Current.MainPage as NavigationPage).CurrentPage as PrivateChatPage).ScrollList()
               );
            }
        }

        private void SendMessageCommandExecute(object obj)
        {
            var message = this.MessageText != null ? this.MessageText.Trim() : string.Empty;
            if (!string.IsNullOrEmpty(message))
            {
                var m = new MessageTable();
                m.SenderId = (int)App.QbProvider.UserId;
                m.Text = message;
                m.DialogId = dialogId;
                m.RecepientFullName = "Me";

                long unixTimestamp = DateTime.UtcNow.Ticks - new DateTime(1970, 1, 1).Ticks;
                unixTimestamp /= TimeSpan.TicksPerSecond;
                m.DateSent = unixTimestamp;
                m.ID = Database.Instance().SaveMessage(m);

                var dialog = Database.Instance().GetDialog(this.dialogId);
                dialog.LastMessage = m.Text;
                dialog.LastMessageSent = DateTime.UtcNow;
                Database.Instance().SaveDialog(dialog);

                try
                {
                    var encodedMessage = System.Net.WebUtility.UrlEncode(message);
                    privateChatManager.SendMessage(encodedMessage);
                }
                catch (Exception ex)
                {
                    this.IsBusyIndicatorVisible = true;
                    try
                    {
                        App.QbProvider.GetXmppClient().Connect(App.UserId, App.UserPassword);
                    }
                    catch (Exception ex2)
                    {
                        App.Current.MainPage.DisplayAlert("Error", "Please, check your internet connection", "Ok");
                    }
                    finally
                    {
                        this.IsBusyIndicatorVisible = false;
                    }

                    return;
                    //await App.Current.MainPage.DisplayAlert ("Error", ex.ToString (), "Ok");
                }

                this.Messages.Add(m);
                MessageText = "";
            }
        }
    }
}
