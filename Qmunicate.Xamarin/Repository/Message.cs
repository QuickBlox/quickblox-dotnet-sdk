using System;

namespace Qmunicate.Xamarin
{
	public class MessageTable
	{
		public MessageTable ()
		{
		}

		public string Text {
			get;
			set;
		}

		public DateTime DateSent {
			get;
			set;
		}

		public int SenderId {
			get;
			set;
		}

		public string MessageId {
			get;
			set;
		}

		public int RecepientId {
			get;
			set;
		}

		public string DialogId {
			get;
			set;
		}

		public bool IsRead {
			get;
			set;
		}
	}
}

