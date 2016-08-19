using FM.IceLink;
using FM.IceLink.WebRTC;
using System.Collections.ObjectModel;
using Quickblox.Sdk.Modules.ChatXmppModule.ExtraParameters;
using System.Collections.Generic;

#if __IOS__
using Xamarin.Forms.Conference.WebRTC.iOS.Opus;
using Xamarin.Forms.Conference.WebRTC.iOS.VP8;
#elif __ANDROID__
using Xamarin.Forms.Conference.WebRTC.Droid.Opus;
using Xamarin.Forms.Conference.WebRTC.Droid.VP8;
#elif WINDOWS_APP
using Windows8.Conference.WebRTC;
#endif

using System;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class SdpEventArgs : EventArgs
	{
		public string Sdp { get; set;}

		public bool IsOffer { get; set;}

		public string PeerId { get; set;}
	}

	public class IceCandidateEventArgs : EventArgs
	{
		public string Candidate { get; set; }

		public string SdpMLineIndex { get; set; }

		public string SdpMid { get; set; }
	}

	public class ConferenceWrapper
	{
		public static int ConferenceTimeout = 30000;
		private string[] IceServers =
		{
			"stun.l.google.com:19302",
			"turn.quickblox.com",
			"turn.quickblox.com:3478?transport=udp",
			"turn.quickblox.com:3478?transport=tcp",
			"turnsingapor.quickblox.com:3478?transport=udp",
			"turnsingapore.quickblox.com:3478?transport=tcp",
			"turnireland.quickblox.com:3478?transport=udp",
			"turnireland.quickblox.com:3478?transport=tcp"
		};

		private AudioStream audioStream;
		private VideoStream videoStream;
		private FM.IceLink.Conference conference;

#if __ANDROID__
		private static OpusEchoCanceller OpusEchoCanceller = null;
#endif

		private static int OpusClockRate = 48000;
		private static int OpusChannels = 2;
		private static Certificate Certificate;

		private LocalMedia LocalMedia;

		readonly string sessionId;
		readonly List<string> receiversId = new List<string>();

		public event EventHandler LinkDownEvent;
		public event EventHandler LinkUpEvent;
		public event EventHandler<SdpEventArgs> ReceiveSdpEvent;
		public event EventHandler<IceCandidateEventArgs> ReceiveIceCandidateEvent;

		static ConferenceWrapper()
		{
			// WebRTC has chosen VP8 as its mandatory video codec.
			// Since video encoding is best done using native code,
			// reference the video codec at the application-level.
			// This is required when using a WebRTC video stream.
			VideoStream.RegisterCodec("VP8", () =>
			{
				return new Vp8Codec();
			}, true);

#if !WINDOWS_APP
            // For improved audio quality, we can use Opus. By
            // setting it as the preferred audio codec, it will
            // override the default PCMU/PCMA codecs.
            AudioStream.RegisterCodec("opus", OpusClockRate, OpusChannels, () =>
			{
#if __ANDROID__
				return new OpusCodec(OpusEchoCanceller);
#elif __IOS__
				return new OpusCodec();

#endif
            }, true);
#endif

            // To save time, generate a DTLS certificate when the
            // app starts and reuse it for multiple conferences.
            Certificate = Certificate.GenerateCertificate();
		}

		public ConferenceWrapper(string sessionId, 
		                         LocalMedia localMedia)
		{
			this.LocalMedia = localMedia;
			InitAudioAndVideoStreams();

			// Create a conference using our stream descriptions.
			conference = new FM.IceLink.Conference(this.IceServers, new Stream[] { audioStream, videoStream });

			// Use our pre-generated DTLS certificate.
			conference.DtlsCertificate = Certificate;

			// Supply TURN relay credentials in case we are behind a
			// highly restrictive firewall. These credentials will be
			// verified by the TURN server.
			conference.RelayUsername = "quickblox";
			conference.RelayPassword = "baccb97ba2d92d71e26eb9886da5f1e0";
			conference.ServerPort = 3478;

			// Add a few event handlers to the conference so we can see
			// when a new P2P link is created or changes state.
			conference.OnLinkInit += LogLinkInit;
			conference.OnLinkUp += LogLinkUp;
			conference.OnLinkDown += LogLinkDown;

			conference.OnLinkOfferAnswer += OnLinkSendOfferAnswer;
			conference.OnLinkCandidate += OnLinkSendCandidate;
			conference.Timeout = ConferenceTimeout;

#if __ANDROID__
			// Start echo canceller.
			OpusEchoCanceller = new OpusEchoCanceller(OpusClockRate, OpusChannels);
			OpusEchoCanceller.Start();
#endif

			this.sessionId = sessionId;
		}

		/// <summary>
		/// Link this instance to conference.
		/// </summary>
		public bool Link(string peerId)
		{
			this.receiversId.Add(peerId);
			return conference.Link(peerId);
		}

		/// <summary>
		/// Unlink the specified peerId.
		/// </summary>
		/// <param name="peerId">Peer identifier.</param>
		public void Unlink(string peerId)
		{
			this.receiversId.Remove(peerId);
			this.conference.Unlink(peerId);
		}

		/// <summary>
		/// Unlinks all.
		/// </summary>
		/// <returns>The all.</returns>
		public void UnlinkAll()
		{
			this.receiversId.Clear();
			this.conference.UnlinkAll();
		}

		/// <summary>
		/// Receives the offer anwer.
		/// </summary>
		/// <returns>The offer anwer.</returns>
		/// <param name="sdp">Sdp.</param>
		/// <param name="isOffer">Is offer.</param>
		/// <param name="caller">Caller.</param>
		public void ReceiveOfferAnwer(string sdp, bool isOffer, string peerId)
		{
			OfferAnswer offer = new OfferAnswer();
			offer.SdpMessage = sdp;
			offer.IsOffer = isOffer;
			conference.ReceiveOfferAnswer(offer, peerId);
		}

		/// <summary>
		/// Ons the link send offer answer.
		/// </summary>
		/// <returns>The link send offer answer.</returns>
		/// <param name="e">E.</param>
		private void OnLinkSendOfferAnswer(LinkOfferAnswerArgs e)
		{
			var handler = this.ReceiveSdpEvent;
			if (handler != null)
			{
				var sdpEventArgs = new SdpEventArgs()
				{
					IsOffer = e.OfferAnswer.IsOffer,
					Sdp = e.OfferAnswer.SdpMessage,
					PeerId = e.PeerId
				};

				handler.Invoke(this, sdpEventArgs);
			}
		}

		/// <summary>
		/// Receives the ice candidate from other users in call
		/// </summary>
		/// <returns>The ice candidate.</returns>
		/// <param name="iceCandidates">Ice candidates.</param>
		/// <param name="caller">Caller.</param>
		public void ReceiveIceCandidate(Collection<IceCandidate> iceCandidates, string peerId)
		{
			foreach (var iceCandidate in iceCandidates)
			{
				var candidate = new Candidate();
				candidate.SdpCandidateAttribute = iceCandidate.Candidate;
				candidate.SdpMediaIndex = iceCandidate.SdpMLineIndex != null ? int.Parse(iceCandidate.SdpMLineIndex) : 0;
				conference.ReceiveCandidate(candidate, peerId);
			}
		}

		/// <summary>
		/// Ons the link send candidate.
		/// </summary>
		/// <returns>The link send candidate.</returns>
		/// <param name="e">E.</param>
		private void OnLinkSendCandidate(LinkCandidateArgs e)
		{
			var handler = this.ReceiveIceCandidateEvent;
			if (handler != null)
			{
				var iceCandidateEventArgs = new IceCandidateEventArgs()
				{
					SdpMLineIndex = e.Candidate.SdpMediaIndex.HasValue ? e.Candidate.SdpMediaIndex.Value.ToString() : "",
					Candidate =	e.Candidate.SdpCandidateAttribute,
				};

				handler.Invoke(this, iceCandidateEventArgs);
			}
		}

		// TODO: will move to dispose
		/// <summary>
		/// Stops the conference.
		/// </summary>
		/// <returns>The conference.</returns>
		private void StopConference()
		{
			try
			{
#if __ANDROID__
				// Stop echo canceller.
				OpusEchoCanceller.Stop();
				OpusEchoCanceller = null;
#endif
				conference.OnLinkInit -= LogLinkInit;
				conference.OnLinkUp -= LogLinkUp;
				conference.OnLinkDown -= LogLinkDown;

				conference.OnLinkOfferAnswer -= OnLinkSendOfferAnswer;
				conference.OnLinkCandidate -= OnLinkSendCandidate;
				conference = null;

				videoStream.OnLinkInit -= AddRemoteVideoControl;
				videoStream.OnLinkDown -= RemoveRemoteVideoControl;
				videoStream = null;

				audioStream = null;
			}
			catch (Exception ex)
			{
				FM.Log.Debug(ex.ToString());
			}
		}

		/// <summary>
		/// Inits the audio and video streams.
		/// </summary>
		/// <returns>The audio and video streams.</returns>
		private void InitAudioAndVideoStreams()
		{
			// Create a WebRTC audio stream description (requires a
			// reference to the local audio feed).
			audioStream = new AudioStream(LocalMedia.LocalMediaStream);

			// Create a WebRTC video stream description (requires a
			// reference to the local video feed). Whenever a P2P link
			// initializes using this description, position and display
			// the remote video control on-screen by passing it to the
			// layout manager created above. Whenever a P2P link goes
			// down, remove it.
			videoStream = new VideoStream(LocalMedia.LocalMediaStream);
			videoStream.OnLinkInit += AddRemoteVideoControl;
			videoStream.OnLinkDown += RemoveRemoteVideoControl;
		}

		/// <summary>
		/// Adds the remote video control.
		/// </summary>
		/// <returns>The remote video control.</returns>
		/// <param name="e">E.</param>
		private void AddRemoteVideoControl(StreamLinkInitArgs e)
		{
			try
			{
				var remoteVideoControl = e.Link.GetRemoteVideoControl();
#if __ANDROID__ || __IOS__
                LocalMedia.LayoutManager.AddRemoteVideoControl(e.PeerId, new FormsVideoControl(remoteVideoControl));
#elif WINDOWS_APP
                LocalMedia.LayoutManager.AddRemoteVideoControl(e.PeerId, remoteVideoControl);
#endif
            }
			catch (Exception ex)
			{
				FM.Log.Error("Could not add remote video control.", ex);
			}
		}

		/// <summary>
		/// Removes the remote video control.
		/// </summary>
		/// <returns>The remote video control.</returns>
		/// <param name="e">E.</param>
		private void RemoveRemoteVideoControl(StreamLinkDownArgs e)
		{
			try
			{
				if (LocalMedia != null && LocalMedia.LayoutManager != null)
				{
					LocalMedia.LayoutManager.RemoveRemoteVideoControl(e.PeerId);
				}
			}
			catch (Exception ex)
			{
				FM.Log.Error("Could not remove remote video control.", ex);
			}
		}

		/// <summary>
		/// Logs the link init.
		/// </summary>
		/// <returns>The link init.</returns>
		/// <param name="e">E.</param>
		private void LogLinkInit(LinkInitArgs e)
		{
			FM.Log.Info("Link to peer initializing...");
		}

		/// <summary>
		/// Logs the link up.
		/// </summary>
		/// <returns>The link up.</returns>
		/// <param name="e">E.</param>
		private void LogLinkUp(LinkUpArgs e)
		{
			FM.Log.Info("Link to peer is UP.");
			//linkUpAction.Invoke();

			var handler = this.LinkUpEvent;
			if (handler != null)
			{
				handler.Invoke(this, new EventArgs());
			}
		}

		/// <summary>
		/// Logs the link down.
		/// </summary>
		/// <returns>The link down.</returns>
		/// <param name="e">E.</param>
		private void LogLinkDown(LinkDownArgs e)
		{
			FM.Log.Info(string.Format("Link to peer is DOWN. {0}", e.Exception.Message));
			//linkDown.Invoke();

			var handler = this.LinkDownEvent;
			if (handler != null)
			{
				handler.Invoke(this, new EventArgs());
			}
		}
	}
}

