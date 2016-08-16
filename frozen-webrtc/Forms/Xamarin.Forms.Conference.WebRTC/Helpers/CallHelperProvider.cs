using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using AudioPlayEx;
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
		public const string CallFileName = "calling.wav";
		public const string RingtoneFileName = "ringtone.wav";
		public const string BusyFileName = "busy.wav";
		public const string EndCallFileName = "end_of_call.wav";

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
		Timer callOutgoingAudioTimer;
		Timer callIncomingAudioTimer;

		public event EventHandler<IncomingCall> IncomingCallMessageEvent;
		public event EventHandler<VideoChatMessage> IncomingDropMessageEvent;
		public event EventHandler CallUpEvent;
		public event EventHandler CallDownEvent;

		public VideoChatState VideoChatState { get; private set; } = VideoChatState.None;

		public ConferenceWrapper CurrentCall { get; private set; }

		public bool IsConnecting { get; private set; }

		public CallHelperProvider(QuickbloxClient client)
		{
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			chatXmppClient = client.ChatXmppClient;
			webSyncClient = client.WebSyncClient;

			chatXmppClient.MessageReceived += OnMessageReceived;
			webSyncClient.VideoChatMessage += OnVideoChatMessageReceived;

			callOutgoingAudioTimer = new Timer();
			callOutgoingAudioTimer.Elapsed += OnCallOutgoingAudioTimer;
			callOutgoingAudioTimer.Interval = 5000;

			callIncomingAudioTimer = new Timer();
			callIncomingAudioTimer.Elapsed += OnCallIncomingAudioTimer;
			callIncomingAudioTimer.Interval = 5000;
		}

		public void InitCall(string sessionId, User caller, List<User> receivers)
		{
			this.sessionId = sessionId;
			this.caller = caller;
			this.receivers = receivers;
		}

		private void OnCallIncomingAudioTimer(object sender, ElapsedEventArgs e)
		{
			DependencyService.Get<IAudio>().PlayAudioFile(RingtoneFileName);
		}

		private void OnCallOutgoingAudioTimer(object sender, ElapsedEventArgs e)
		{
			DependencyService.Get<IAudio>().PlayAudioFile(CallFileName);
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
		public void CallToUsers()
		{
			callOutgoingAudioTimer.Start();
			callIncomingAudioTimer.Stop();

			StartLocalMedia((error) =>
			{
				// TODO: temp err
				if (!string.IsNullOrEmpty(error))
				{
					Debug.WriteLine("Error CAll: " + error);
					return;
				}

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

				this.IsConnecting = true;
				VideoChatState = VideoChatState.WaitOffer;

				Debug.WriteLine("End Call method");
			});
		}

		public void ConnectToIncomingCall(VideoChatMessage e)
		{
			StartLocalMedia((error) =>
			{
				// TODO: temp err
				if (!string.IsNullOrEmpty(error))
				{
					Debug.WriteLine("Error CAll: " + error);
					return;
				}

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
			callOutgoingAudioTimer.Stop();
			callIncomingAudioTimer.Stop();

			if (this.IsConnecting)
			{
				if (this.caller.Id == App.UserId)
				{
					foreach (var user in receivers)
					{
						this.webSyncClient.Reject(this.sessionId, caller.Id.ToString(), user.Id.ToString(), receivers.Select(u => u.Id.ToString()).ToList(), Device.OS.ToString().ToLower());
					}				
				}
				else 
				{
					this.webSyncClient.Reject(this.sessionId, caller.Id.ToString(), caller.Id.ToString(), receivers.Select(u => u.Id.ToString()).ToList(), Device.OS.ToString().ToLower());
				}
			}

			DisposeConference();
		}

		public void HangUpVideoCall()
		{
			callOutgoingAudioTimer.Stop();
			callIncomingAudioTimer.Stop();

			VideoChatState = VideoChatState.None;

			if (this.IsConnecting)
			{
				if (this.caller.Id == App.UserId)
				{
					foreach (var user in receivers)
					{
						this.webSyncClient.HangUp(this.sessionId, caller.Id.ToString(), user.Id.ToString(), receivers.Select(u => u.Id.ToString()).ToList(), Device.OS.ToString().ToLower());
					}
				}
				else
				{
					this.webSyncClient.HangUp(this.sessionId, caller.Id.ToString(), caller.Id.ToString(), receivers.Select(u => u.Id.ToString()).ToList(), Device.OS.ToString().ToLower());
				}
			}

			DisposeConference();
		}

		private void DisposeConference()
		{
			if (this.CurrentCall != null)
			{
				this.CurrentCall.LinkUpEvent -= OnLinkUp;
				this.CurrentCall.LinkDownEvent -= OnLinkDown;
				this.CurrentCall.ReceiveSdpEvent -= OnReceiveSdp;
				this.CurrentCall.ReceiveIceCandidateEvent -= OnReceiveIceCandidate;
				this.CurrentCall = null;
			}


			this.IsConnecting = false;
			this.sessionId = null;
			VideoChatState = VideoChatState.None;
		}


		private void OnReceiveSdp(object sender, SdpEventArgs e)
		{
			if (sessionId == null)
				return;
			
			if (VideoChatState == VideoChatState.WaitOffer)
			{
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
			if (sessionId == null)
				return;
			
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

			if (this.localMedia != null)
				return;

			this.localMedia = new LocalMedia();
			this.localMedia.Start(videoContainer, callback);
		}

		public void StopLocalMedia()
		{
			if (this.localMedia == null)
				return;

			this.localMedia.Stop((error) =>
			{
				if (error != null)
				{
					Debug.WriteLine("StopLocalMedia method: " + error);
				}
			});
			this.localMedia = null;
			this.sessionId = null;
		}

		private void OnLinkDown(object sender, EventArgs e)
		{
			var handler = this.CallDownEvent;
			if (handler != null)
			{
				handler.Invoke(this, new EventArgs());
			}

			callOutgoingAudioTimer.Stop();
			callIncomingAudioTimer.Stop();

			DisposeConference();
		}

		private void OnLinkUp(object sender, EventArgs e)
		{
			var handler = this.CallUpEvent;
			if (handler != null)
			{
				handler.Invoke(this, new EventArgs());
			}

			callOutgoingAudioTimer.Stop();
			callIncomingAudioTimer.Stop();
		}

		private void OnVideoChatMessageReceived(object sender, VideoChatMessage e)
		{
			//if (e.Caller != OpponentId)
			//    return;

			// Check if message missing and not actual from other call
			if (e.SessionId != this.sessionId && this.sessionId != null)
				return;
			
			if (VideoChatState == VideoChatState.SendOffer && e.Signal == SignalType.call)
				return;

			if ((e.Signal == SignalType.reject || e.Signal == SignalType.hangUp) && this.sessionId != null)
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
			else if ((e.Signal == SignalType.call &&  this.sessionId == null ) || (e.Signal == SignalType.accept && this.sessionId != null))
			{
				// Входящий сигнал, и текущее состояние не активен
				if (VideoChatState == VideoChatState.None)
				{
					var callerIdInRequestCall = e.Caller;
					var callerUser = App.UsersInRoom.FirstOrDefault(u => u.Id == Int32.Parse(callerIdInRequestCall));
					if (callerUser != null)
					{
						this.IsConnecting = true;
						VideoChatState = VideoChatState.WaitAnswer;

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
			else if ((e.Signal == SignalType.iceCandidates) && this.sessionId != null)
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

			callOutgoingAudioTimer.Stop();
			callIncomingAudioTimer.Stop();

			if (e.Signal == SignalType.hangUp)
			{
				DependencyService.Get<IAudio>().PlayAudioFile(EndCallFileName);
			}
			else if (e.Signal == SignalType.reject)
			{
				DependencyService.Get<IAudio>().PlayAudioFile(BusyFileName);
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

			callOutgoingAudioTimer.Stop();
			callIncomingAudioTimer.Start();
		}

		private static void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
		{

		}
	}
}


