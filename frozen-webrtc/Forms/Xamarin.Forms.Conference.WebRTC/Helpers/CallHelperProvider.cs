using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Quickblox.Sdk;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.Modules.ChatXmppModule.ExtraParameters;
using Quickblox.Sdk.Modules.UsersModule.Models;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class IncomingCall
	{
		public VideoChatMessage VideoChatMessage { get; set;}
		public User Caller { get; set;}
		public List<User> Opponents { get; set;}
	}

	public class CallHelperProvider
	{
		// Message client
		private ChatXmppClient chatXmppClient;
		private WebSyncClient webSyncClient;

		// General conference data
		private string sessionId;
		private User caller;
		private List<User> receivers;

		// Video
		private LocalMedia localMedia;
		private AbsoluteLayout videoContainer;

		public event EventHandler<IncomingCall> IncomingCallMessageEvent;
		public event EventHandler<VideoChatMessage> IncomingDropMessageEvent;
		public event EventHandler CallUpEvent;
		public event EventHandler CallDownEvent;

		public VideoChatState VideoChatState { get; private set; } = VideoChatState.None;

		public ConferenceWrapper CurrentCall { get; private set; }

		public CallHelperProvider(QuickbloxClient client)
		{
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			chatXmppClient = client.ChatXmppClient;
			webSyncClient = client.WebSyncClient;

			chatXmppClient.MessageReceived += OnMessageReceived;
			webSyncClient.VideoChatMessage += OnVideoChatMessageReceived;
		}

		public void InitVideoContainer(AbsoluteLayout videoContainer)
		{
			this.videoContainer = videoContainer;
		}

		public void UseNextVideoDevice()
		{
			localMedia.LocalMediaStream.UseNextVideoDevice();
		}

		public void PauseLocalVideo()
		{
			localMedia.LocalMediaStream.PauseVideo();
		}

		public void ResumeLocalVideo()
		{
			localMedia.LocalMediaStream.ResumeVideo();
		}

		/// <summary>
		/// Call users the specified sdp.
		/// </summary>
		/// <param name="sdp">Sdp.</param>
		public void CallToUsers(string sessionId, User caller, List<User> receivers)
		{
			StartLocalMedia((error) =>
			{
				// TODO: temp err
				if (!string.IsNullOrEmpty(error))
				{
					Debug.WriteLine("Error CAll: " + error);
					return;
				}

				this.sessionId = sessionId;
				this.caller = caller;
				this.receivers = receivers;

				Debug.WriteLine("Start Call method");

				this.CurrentCall = new ConferenceWrapper(sessionId, localMedia);
				this.CurrentCall.LinkUpEvent += OnLinkUp;
				this.CurrentCall.LinkDownEvent += OnLinkDown;
				this.CurrentCall.ReceiveSdpEvent += OnReceiveSdp;
				this.CurrentCall.ReceiveIceCandidateEvent += OnReceiveIceCandidate;

				foreach (var user in receivers)
				{
					this.CurrentCall.Link(user.Id.ToString());
				}

				VideoChatState = VideoChatState.WaitOffer;

				Debug.WriteLine("End Call method");
			});
		}

		public void ConnectToIncomingCall(User caller, List<User> receivers, VideoChatMessage e)
		{
			StartLocalMedia((error) =>
			{
				// TODO: temp err
				if (!string.IsNullOrEmpty(error))
				{
					Debug.WriteLine("Error CAll: " + error);
					return;
				}

				this.sessionId = e.SessionId;
				this.caller = caller;
				this.receivers = receivers;

				// Incoming new call
				this.CurrentCall = new ConferenceWrapper(e.SessionId, localMedia);
				this.CurrentCall.LinkUpEvent += OnLinkUp;
				this.CurrentCall.LinkDownEvent += OnLinkDown;
				this.CurrentCall.ReceiveSdpEvent += OnReceiveSdp;
				this.CurrentCall.ReceiveIceCandidateEvent += OnReceiveIceCandidate;
				this.CurrentCall.ReceiveOfferAnwer(e.Sdp, true, e.Caller);
			});
		}

		public void RejectVideoCall()
		{
			VideoChatState = VideoChatState.None;

			if (receivers != null)
			{
				foreach (var user in receivers)
				{
					this.webSyncClient.Reject(this.sessionId, caller.Id.ToString(), user.Id.ToString(), receivers.Select(u => u.Id.ToString()).ToList(), Device.OS.ToString().ToLower());
				}
			}

			if (this.CurrentCall != null)
			{
				this.CurrentCall.LinkUpEvent -= OnLinkUp;
				this.CurrentCall.LinkDownEvent -= OnLinkDown;
				this.CurrentCall.ReceiveSdpEvent -= OnReceiveSdp;
				this.CurrentCall.ReceiveIceCandidateEvent -= OnReceiveIceCandidate;
				this.CurrentCall = null;
			}
		}

		private void OnReceiveSdp(object sender, SdpEventArgs e)
		{
			if (VideoChatState == VideoChatState.WaitOffer)
			{
				// TODO: send to all users maybe
				if (e.IsOffer)
				{
					this.webSyncClient.Call(sessionId, e.Sdp, caller.Id.ToString(), e.PeerId, this.receivers.Select(u => u.Id.ToString()).ToList(), Device.OS.ToString().ToLower());
				}

				VideoChatState = VideoChatState.SendOffer;
			}
			if (VideoChatState == VideoChatState.WaitAnswer)
			{
				this.webSyncClient.Accept(sessionId, e.Sdp, caller.Id.ToString(), e.PeerId, this.receivers.Select(u => u.Id.ToString()).ToList(), Device.OS.ToString().ToLower());
				VideoChatState = VideoChatState.Complete;
			}
		}

		private void OnReceiveIceCandidate(object sender, IceCandidateEventArgs e)
		{
			var iceCandidates = new Collection<IceCandidate>();
			iceCandidates.Add(new IceCandidate()
			{
				SdpMLineIndex = e.SdpMLineIndex,
				Candidate = e.Candidate
			});

			foreach (var user in this.receivers.Where(u => u.Id != App.UserId))
			{
				this.webSyncClient.IceCandidates(this.sessionId, iceCandidates, caller.Id.ToString(), user.Id.ToString(), this.receivers.Select(u => u.Id.ToString()).ToList(), Device.OS.ToString().ToLower());
			}
		}

		/// <summary>
		/// Starts the local media.
		/// </summary>
		/// <returns>The local media.</returns>
		/// <param name="callback">Callback.</param>
		public void StartLocalMedia(Action<string> callback)
		{
			Debug.WriteLine("StartLocalMedia method");

			if (videoContainer == null)
				throw new ArgumentNullException(nameof(videoContainer));

			if (localMedia != null)
				return;

			localMedia = new LocalMedia();
			localMedia.Start(videoContainer, callback);
		}

		public void StopLocalMedia()
		{
			if (localMedia == null)
				return;

			localMedia.Stop((error) =>
			{
				if (error != null)
				{
					Debug.WriteLine("StopLocalMedia method: " + error);
				}
			});
			localMedia = null;


		}

		private void OnLinkDown(object sender, EventArgs e)
		{
			var handler = this.CallDownEvent;
			if (handler != null)
			{
				handler.Invoke(this, new EventArgs());
			}
		}

		private void OnLinkUp(object sender, EventArgs e)
		{
			var handler = this.CallUpEvent;
			if (handler != null)
			{
				handler.Invoke(this, new EventArgs());
			}
		}

		private void OnVideoChatMessageReceived(object sender, VideoChatMessage e)
		{
			//if (e.Caller != OpponentId)
			//    return;

			if (VideoChatState == VideoChatState.SendOffer && e.Signal == SignalType.call)
				return;

			if (e.Signal == SignalType.reject || e.Signal == SignalType.hangUp)
			{
				VideoChatState = VideoChatState.None;
				if (this.CurrentCall != null)
				{
					if (e.Sender == Int32.Parse(e.Caller))
					{
						this.CurrentCall.UnlinkAll();
					}
					else {
						this.CurrentCall.Unlink(e.Sender.ToString());
					}
				}

				InvokeDropCallMessage(sender, e);
			}
			else if (e.Signal == SignalType.call || e.Signal == SignalType.accept)
			{
				// Входящий сигнал, и текущее состояние не активен
				if (VideoChatState == VideoChatState.None)
				{
					VideoChatState = VideoChatState.WaitAnswer;

					var callerIdInRequestCall = e.Caller;
					var callerUser = App.UsersInRoom.FirstOrDefault(u => u.Id == Int32.Parse(callerIdInRequestCall));
					if (callerUser != null)
					{
						var opponents = new List<User>();
						foreach (var id in e.OpponentsIds)
						{
							var user = App.UsersInRoom.FirstOrDefault(u => u.Id == Int32.Parse(id));
							if (user != null)
							{
								opponents.Add(user);
							}
						}

						InvokeIncomingEvent(sender, e, callerUser, opponents);
					}

					//if (await requestStartvideo ()) {
					//VideoChatState = VideoChatState.WaitAnswer;
					//OfferAnswer offer = new OfferAnswer();
					//offer.SdpMessage = e.Sdp;
					//offer.IsOffer = true;
					//Conference.ReceiveOfferAnswer(offer, e.Caller.ToString());
					//} else {
					//	WebSyncClient.Reject (SessionId, OpponentId);
					//}			
				}
				else if (VideoChatState == VideoChatState.SendOffer)
				{
					VideoChatState = VideoChatState.Complete;
					this.CurrentCall.ReceiveOfferAnwer(e.Sdp, false, e.Sender.ToString());
					//OfferAnswer answer = new OfferAnswer();
					//answer.SdpMessage = e.Sdp;
					//answer.IsOffer = false;
					//Conference.ReceiveOfferAnswer(answer, e.Caller.ToString());
				}
			}
			else if (e.Signal == SignalType.iceCandidates)
			{
				//foreach (var iceCandidate in e.IceCandidates)
				//{
				//	var candidate = new Candidate();
				//	candidate.SdpCandidateAttribute = iceCandidate.Candidate;
				//	candidate.SdpMediaIndex = iceCandidate.SdpMLineIndex != null ? int.Parse(iceCandidate.SdpMLineIndex) : 0;
				//	SessionId = e.Guid;
				//	Conference.ReceiveCandidate(candidate, e.Caller.ToString());
				//}

				if (e.IceCandidates == null)
					return;
				
				this.CurrentCall.ReceiveIceCandidate(e.IceCandidates, e.Sender.ToString());
			}
		}

		private void InvokeDropCallMessage(object sender, VideoChatMessage e)
		{
			var handler = IncomingDropMessageEvent;
			if (handler != null)
			{
				handler.Invoke(sender, e);
			}
		}

		private void InvokeIncomingEvent(object sender, VideoChatMessage e, User callerUser, List<User> opponents)
		{
			var handler = IncomingCallMessageEvent;
			if (handler != null)
			{
				var incomingCall = new IncomingCall()
				{
					VideoChatMessage = e,
					Caller = callerUser,
					Opponents = opponents
				};

				handler.Invoke(sender, incomingCall);
			}
		}

		private static void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
		{

		}
	}
}

