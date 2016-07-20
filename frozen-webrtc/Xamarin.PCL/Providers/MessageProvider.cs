using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Quickblox.Sdk.Modules.ChatXmppModule;

namespace Xamarin.PCL
{
	public class MessageProvider
	{
		private ChatXmppClient chatXmppClient;
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

		public ChatXmppClient ChatXmppClient
		{
			get
			{
				return chatXmppClient;
			}
		}

		public void Init(ChatXmppClient chatXmppClient)
		{
			this.chatXmppClient = chatXmppClient;
		}

		public Task ConnetToXmpp(int userId, string userPassword)
		{
			if (chatXmppClient == null) throw new NullReferenceException(nameof(chatXmppClient));
			chatXmppClient.ErrorReceived -= OnError;
			chatXmppClient.ErrorReceived += OnError;
			chatXmppClient.StatusChanged -= OnStatusChanged;
			chatXmppClient.StatusChanged += OnStatusChanged;

			return chatXmppClient.Connect(userId, userPassword);
		}

		public void DisconnectToXmpp()
		{
			if (chatXmppClient == null) throw new NullReferenceException(nameof(chatXmppClient));
			chatXmppClient.ErrorReceived -= OnError;
			chatXmppClient.StatusChanged -= OnStatusChanged;
			chatXmppClient.Close();
		}

		private void OnStatusChanged(object sender, StatusEventArgs statusEventArgs)
		{
			Debug.WriteLine("Xmpp Status: " + statusEventArgs.Jid + " Status: " + statusEventArgs.Status.Availability);
		}

		private void OnError(object sender, ErrorEventArgs errorsEventArgs)
		{
			Debug.WriteLine("Xmpp Error: " + errorsEventArgs.Exception + " Reason: " + errorsEventArgs.Reason);
		}
	}
}



