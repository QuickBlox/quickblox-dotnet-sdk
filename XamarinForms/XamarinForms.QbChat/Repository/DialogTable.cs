using Quickblox.Sdk.Modules.ChatModule.Models;
using Quickblox.Sdk.Modules.Models;
using SQLite.Net.Attributes;
using System;
using Xamarin.Forms;

namespace XamarinForms.QbChat.Repository
{
    public class DialogTable
    {
        private string lastMessage;
        private DateTime lastMessageSent;
        private string name;
        private ImageSource image;

        #region Ctor

        public DialogTable()
        { }

        public DialogTable(Dialog dialog)
        {
            DialogId = dialog.Id;
            XmppRoomJid = dialog.XmppRoomJid;
            Name = dialog.Name;
            LastMessageUserId = dialog.LastMessageUserId;
            LastMessageSent = dialog.LastMessageDateSent.HasValue
                ? dialog.LastMessageDateSent.Value.ToDateTime()
				: DateTime.MinValue;
            LastMessage = dialog.LastMessage;
            UnreadMessageCount = dialog.UnreadMessagesCount;
            OccupantIds = String.Join(",", dialog.OccupantsIds);
            DialogType = dialog.Type;
            Photo = dialog.Photo;
            DialogOwnerId = dialog.UserId;
        }

        #endregion

        #region Properties

        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string DialogId { get; set; }
        public string XmppRoomJid { get; set; }
        public string Photo { get; set; }
        public int? PrivatePhotoId { get; set; }
        public int? UnreadMessageCount { get; set; }
        public string OccupantIds { get; set; }
        public int DialogOwnerId { get; set; }
        public DialogType DialogType { get; set; }
        public int? LastMessageUserId { get; set; }

        public string LastMessage
        {
            get { return lastMessage; }
            set { lastMessage = value; }
        }

        public DateTime LastMessageSent
        {
            get { return lastMessageSent; }
            set { lastMessageSent = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [Ignore]
        public ImageSource Image
        {
            get { return image; }
            set { image = value; }
        }

        #endregion

        #region Public methods

        public static DialogTable FromDialog(Dialog dialog)
        {
            return new DialogTable(dialog);
        }

        #endregion
    }
}
