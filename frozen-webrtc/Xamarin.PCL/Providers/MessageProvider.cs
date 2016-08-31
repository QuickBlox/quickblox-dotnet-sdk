using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Quickblox.Sdk;
using Quickblox.Sdk.Modules.ChatXmppModule;

namespace Xamarin.PCL
{
	public class MessageProvider
	{
		private ChatXmppClient chatXmppClient;
		private WebSyncClient webSyncClient;
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

		public WebSyncClient WebSyncClient
		{
			get
			{
				return webSyncClient;
			}
		}

		public void Init(QuickbloxClient client)
		{
			this.chatXmppClient = client.ChatXmppClient;
			this.webSyncClient = client.WebSyncClient;
		}

		public Task ConnetToXmpp(int userId, string userPassword)
		{
			if (chatXmppClient == null) throw new NullReferenceException(nameof(chatXmppClient));
			chatXmppClient.ErrorReceived -= OnError;
			chatXmppClient.ErrorReceived += OnError;
			chatXmppClient.StatusChanged -= OnStatusChanged;
			chatXmppClient.StatusChanged += OnStatusChanged;

			webSyncClient.VideoChatMessage += OnVideoMessageReceived;

			return chatXmppClient.Connect(userId, userPassword);
		}

		public void DisconnectToXmpp()
		{
			if (chatXmppClient == null) throw new NullReferenceException(nameof(chatXmppClient));
			chatXmppClient.ErrorReceived -= OnError;
			chatXmppClient.StatusChanged -= OnStatusChanged;
			webSyncClient.VideoChatMessage -= OnVideoMessageReceived;

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

		private void OnVideoMessageReceived(object sender, VideoChatMessage e)
		{
			//if (e.Caller != OpponentId)
			//	return;

			//Dateifi.LogProvider.Instance().LogEvent("Signal type: " + e.Signal + " Guid: " + e.Guid + "; Receiver: " + e.Receiver + "; Caller: " + e.Caller);

			//if (VideoChatState == VideoChatState.SendOffer && e.Signal == SignalType.call)
			//	return;

			//if (e.Signal == SignalType.reject || e.Signal == SignalType.hangUp)
			//{
			//	hangUpCall.Invoke();
			//	VideoChatState = VideoChatState.None;
			//	if (conference != null)
			//	{
			//		conference.UnlinkAll();
			//	}

			//	if (localMedia != null)
			//	{
			//		localMedia.LocalMediaStream.MuteAudio();
			//	}
			//}
			//else if (e.Signal == SignalType.call || e.Signal == SignalType.accept)
			//{
			//	// Входящий сигнал, и текущее состояние не активен
			//	if (VideoChatState == VideoChatState.None)
			//	{
			//		SessionId = e.Guid;

			//		//if (await requestStartvideo ()) {
			//		VideoChatState = VideoChatState.WaitAnswer;
			//		OfferAnswer offer = new OfferAnswer();
			//		offer.SdpMessage = e.Sdp;
			//		offer.IsOffer = true;
			//		conference.ReceiveOfferAnswer(offer, e.Caller.ToString());
			//		//} else {
			//		//	webSyncClient.Reject (SessionId, OpponentId);
			//		//}			
			//	}
			//	else if (VideoChatState == VideoChatState.SendOffer)
			//	{
			//		VideoChatState = VideoChatState.Complete;
			//		OfferAnswer answer = new OfferAnswer();
			//		answer.SdpMessage = e.Sdp;
			//		answer.IsOffer = false;
			//		conference.ReceiveOfferAnswer(answer, e.Caller.ToString());
			//	}
			//}
			//else if (e.Signal == SignalType.iceCandidates)
			//{
			//	foreach (var iceCandidate in e.IceCandidates)
			//	{
			//		var candidate = new Candidate();
			//		candidate.SdpCandidateAttribute = iceCandidate.Candidate;
			//		candidate.SdpMediaIndex = iceCandidate.SdpMLineIndex != null ? int.Parse(iceCandidate.SdpMLineIndex) : 0;
			//		SessionId = e.Guid;
			//		conference.ReceiveCandidate(candidate, e.Caller.ToString());
			//	}
			//}

			//Dateifi.LogProvider.Instance().LogEvent("End OnHeadlineMessageReceive");



		}
	}
}



