using Quickblox.Sdk.Modules.ChatXmppModule.ExtraParameters;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using System;
using System.Linq;
using XamarinForms.Qmunicate.Repository;
using Xamarin.Forms;

namespace XamarinForms.Qmunicate.Pages
{
    public partial class ChatPage : ContentPage
    {
        private string dialogId;
        private int opponentId;

        public ChatPage(string dialogId)
        {
            InitializeComponent();
            this.dialogId = dialogId;

            if (Device.OS == TargetPlatform.iOS)
            {
                Title = "Chat";
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            this.IsBusy = true;

            var dialog = Database.Instance().GetDialog(dialogId);
            chatNameLabel.Text = dialog.Name;
            chatPhotoImage.Source = ImageSource.FromUri(new Uri(dialog.Photo));

            var messages = await App.QbProvider.GetMessages(dialogId);
            if (messages != null)
            {
                messages = messages.OrderBy(message => message.DateSent).ToList();
                Database.Instance().SaveAllMessages(dialogId, messages);
                listView.ItemsSource = messages;
            }

            Database.Instance().SubscribeForMessages(OnMessagesChanged);

            sendButton.Clicked += SendClicked;
            this.IsBusy = false;
        }

        private void SendClicked(object sender, EventArgs e)
        {
            var message = messageEntry.Text.Trim();
            if (!string.IsNullOrWhiteSpace(message))
            {
                var m = new MessageTable();
                m.RecepientId = opponentId;
                m.SenderId = (int)App.QbProvider.UserId;
                m.Text = message;
                m.DialogId = dialogId;
                m.ID = Database.Instance().SaveMessage(m);

                var dialog = Database.Instance().GetDialog(dialogId);
                dialog.LastMessage = m.Text;
                dialog.LastMessageSent = DateTime.UtcNow;
                Database.Instance().SaveDialog(dialog);

                var chatMessageExtraParameter = new ChatMessageExtraParameter(dialogId, true);
                App.QbProvider.GetXmppClient().SendMessage(opponentId, message, chatMessageExtraParameter.ToString(), dialogId, null, MessageType.Chat);
            }
        }

        private void OnMessagesChanged()
        {
            var messages = Database.Instance().GetMessages(dialogId);
            listView.ItemsSource = messages;
        }
    }
}
