using System;
//using Quickblox.Sdk.Modules.Models;
//using Xamarin.Forms;
//using Quickblox.Sdk.Modules.ChatModule.Models;
using MugenMvvmToolkit.Models;
using System.Collections.Generic;

namespace Qmunicate.Xamarin
{
	public class DialogTable : NotifyPropertyChangedBase
	{
		#region Nested types

		private static readonly IEqualityComparer<DialogTable> KeyComparerInstance = new KeyEqualityComparer();

		public static IEqualityComparer<DialogTable> KeyComparer
		{
			get { return KeyComparerInstance; }
		}

		private sealed class KeyEqualityComparer : IEqualityComparer<DialogTable>
		{
			public bool Equals(DialogTable x, DialogTable y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (ReferenceEquals(x, null)) return false;
				if (ReferenceEquals(y, null)) return false;
				if (x.GetType() != y.GetType()) return false;
				return x.DialogId.Equals(y.DialogId);
			}

			public int GetHashCode(DialogTable obj)
			{
				return obj.DialogId.GetHashCode();
			}
		}

		#endregion

		private string name;
		private string lastMessageSent;

		#region Ctor

		public DialogTable()
		{}

//		public DialogTable(Dialog dialog)
//		{
//			DialogId = dialog.Id;
//			XmppRoomJid = dialog.XmppRoomJid;
//			Name = dialog.Name;
//			LastMessageUserId = dialog.LastMessageUserId;
//			LastMessageSent = dialog.LastMessageDateSent.HasValue
//				? dialog.LastMessageDateSent.Value.ToDateTime()
//				: (DateTime?) null;
//			LastMessage = dialog.LastMessage;
//			UnreadMessageCount = dialog.UnreadMessagesCount;
//			OccupantIds = String.Join(",", dialog.OccupantsIds);
//			DialogType = dialog.Type;
//			Photo = dialog.Photo;
//			DialogOwnerId = dialog.UserId;
//		}

		#endregion

		#region Properties

		//[PrimaryKey, AutoIncrement]
		public int ID { get; set; }
		public string DialogId { get; set; }
		public string XmppRoomJid { get; set; }
		public string Photo { get; set; }
		public int? PrivatePhotoId { get; set; }
		public int? UnreadMessageCount { get; set; }
		public string OccupantIds { get; set; }
		public int DialogOwnerId { get; set; }
		//public DialogType DialogType { get; set; }
		public int OpponentUserId { get; set; }
		public int? LastMessageUserId { get; set; }
		

		public string LastMessage { 
			get { return lastMessageSent; } 
			set { 
				if (value == lastMessageSent)
					return;
					
				lastMessageSent = value; 
				OnPropertyChanged ();
			} 
		}

		public DateTime? LastMessageSent{ get; set;}

		public string Name {
			get { return name; } 
			set { 
				if (value == name)
					return;

				name = value; 
				OnPropertyChanged ();
			} 
		}

//		[Ignore]
//		public ImageSource Image
//		{
//			get { return image; }
//			set { image = value; }
//		}

		#endregion

		#region Public methods

//		public static DialogTable FromDialog(Dialog dialog)
//		{
//			return new DialogTable(dialog);
//		}

		#endregion
	}
}

