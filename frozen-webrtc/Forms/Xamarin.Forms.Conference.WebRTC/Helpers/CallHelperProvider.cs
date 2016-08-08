using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Quickblox.Sdk;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.Modules.ChatXmppModule.ExtraParameters;
using Quickblox.Sdk.Modules.UsersModule.Models;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class CallHelperProvider
	{
		// Message client
		private ChatXmppClient chatXmppClient;
		private WebSyncClient webSyncClient;

		// General conference data
		private string sessionId;
		private User caller;
		private List<User> receivers;

		private Action<VideoChatMessage> showIncomingCallPage;
		private Action showVideoPage;
		private Action showOutGoingCallPage;
		private Action backNavigationAction;

		// Video
		private LocalMedia localMedia;
		private AbsoluteLayout videoContainer;

		public VideoChatState VideoChatState { get; private set; } = VideoChatState.None;

		public ConferenceWrapper CurrentCall { get; private set; }

		//public List<ConferenceWrapper> CurrentCall { get; private set; }

		public CallHelperProvider(QuickbloxClient client)
		{
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			chatXmppClient = client.ChatXmppClient;
			webSyncClient = client.WebSyncClient;

			chatXmppClient.MessageReceived += OnMessageReceived;
			webSyncClient.VideoChatMessage += OnVideoChatMessageReceived;
		}

		#region Register UI Action

		//public void RegisterIncomingCallPage(Action<VideoChatMessage> showIncomingCallPage)
		//{
		//	this.showIncomingCallPage = showIncomingCallPage;
		//}

		//public void RegisterOutGoingCallPage(Action showOutGoingCallPage)
		//{
		//	this.showOutGoingCallPage = showOutGoingCallPage;
		//}

		public void RegisterShowVideoCall(Action showVideoPage)
		{
			this.showVideoPage = showVideoPage;
		}

		public void RegisterBackToUsersPage(Action backNavigationAction)
		{
			this.backNavigationAction = backNavigationAction;
		}

		#endregion

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
		public void Call(string sessionId, User caller, List<User> receivers)
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

				this.CurrentCall = new ConferenceWrapper(sessionId,
				                                         ReceiveSdpFromConference,
														 ReceiveIceCandidateFromConference,
														 showVideoPage,
														 backNavigationAction,
														 localMedia);
				foreach (var user in receivers)
				{
					this.CurrentCall.Link(user.Id.ToString());
				}

				VideoChatState = VideoChatState.WaitOffer;

				Debug.WriteLine("End Call method");
			});
		}

		public void IncomingCall(User caller, List<User> receivers, VideoChatMessage e)
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
				this.CurrentCall = new ConferenceWrapper(e.SessionId, ReceiveSdpFromConference, ReceiveIceCandidateFromConference, showVideoPage, backNavigationAction, localMedia);
				this.CurrentCall.ReceiveOfferAnwer(e.Sdp, true, e.Caller);
			});
		}

		public void RejectVideoCall()
		{
			VideoChatState = VideoChatState.None;

			foreach (var user in receivers)
			{
				this.webSyncClient.Reject(this.sessionId, caller.Id.ToString(), user.Id.ToString(), receivers.Select(u => u.Id.ToString()).ToList(), Device.OS.ToString().ToLower());
			}

			this.CurrentCall = null;
		}

		private void ReceiveSdpFromConference(string sdp, bool isOffer, int receiverId)
		{
			if (VideoChatState == VideoChatState.WaitOffer)
			{
				// TODO: send to all users maybe
				if (isOffer)
				{
					this.webSyncClient.Call(sessionId, sdp, caller.Id.ToString(), receiverId.ToString(), this.receivers.Select(u => u.Id.ToString()).ToList(), Device.OS.ToString().ToLower(), userInfo: "Temp user info");
				}

				VideoChatState = VideoChatState.SendOffer;
			}
			if (VideoChatState == VideoChatState.WaitAnswer)
			{
				// receiver id - is me
				this.webSyncClient.Accept(sessionId, sdp, caller.Id.ToString(), receiverId.ToString(), this.receivers.Select(u => u.Id.ToString()).ToList(), Device.OS.ToString().ToLower(), userInfo: "Temp user info");
				VideoChatState = VideoChatState.Complete;
			}
		}

		/// <summary>
		/// Receives the ice candidate wrapper from conference.
		/// </summary>
		/// <returns>The ice candidate method.</returns>
		/// <param name="sdpMediaIndex">Sdp media index.</param>
		/// <param name="sdpCandidateAttribute">Sdp candidate attribute.</param>
		/// <param name="receiverId">Receiver identifier.</param>
		private void ReceiveIceCandidateFromConference(string sdpMediaIndex, string sdpCandidateAttribute, int receiverId)
		{
			var iceCandidates = new Collection<IceCandidate>();
			iceCandidates.Add(new IceCandidate()
			{
				SdpMLineIndex = sdpMediaIndex,
				Candidate = sdpCandidateAttribute
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
					this.CurrentCall.UnlinkAll();
				}
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

						Device.BeginInvokeOnMainThread(() =>
													   App.Navigation.PushAsync(new VideoPage(false, callerUser, opponents, e)));
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



		private static void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
		{

		}
	}
}

