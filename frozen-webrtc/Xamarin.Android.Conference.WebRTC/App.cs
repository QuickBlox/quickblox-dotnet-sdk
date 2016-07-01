using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using FM;
using FM.IceLink;
using FM.IceLink.WebRTC;

using Xamarin.Android.Conference.WebRTC.Opus;
using Xamarin.Android.Conference.WebRTC.VP8;

namespace Xamarin.Android.Conference.WebRTC
{
    class App
    {
        private string IceLinkServerAddress = "demo.icelink.fm:3478";
        private string WebSyncServerUrl = "http://v4.websync.fm/websync.ashx"; // WebSync On-Demand

        private Signalling Signalling;
        private LocalMedia LocalMedia;

        private AudioStream AudioStream;
        private VideoStream VideoStream;
        private FM.IceLink.Conference Conference;

		public String SessionId { get; set; }

		private bool EnableSoftwareEchoCancellation = OpusEchoCanceller.IsSupported; // experimental
		private OpusEchoCanceller OpusEchoCanceller = null;
		private int OpusClockRate = 48000;
		private int OpusChannels = 2;

        private Certificate Certificate;

        private App()
        {
            // Log to the Android console.
            Log.Provider = new AndroidLogProvider(LogLevel.Info);

            // WebRTC has chosen VP8 as its mandatory video codec.
            // Since video encoding is best done using native code,
            // reference the video codec at the application-level.
            // This is required when using a WebRTC video stream.
            VideoStream.RegisterCodec("VP8", () =>
            {
                return new Vp8Codec();
            }, true);

            // For improved audio quality, we can use Opus. By
            // setting it as the preferred audio codec, it will
            // override the default PCMU/PCMA codecs.
			AudioStream.RegisterCodec("opus", OpusClockRate, OpusChannels, () =>
            {
                if (EnableSoftwareEchoCancellation)
                {
                    return new OpusCodec(OpusEchoCanceller);
                }
                else
                {
                    return new OpusCodec();
                }
			}, true);

			if (!EnableSoftwareEchoCancellation)
			{
				AndroidAudioCaptureProvider.DefaultUseAcousticEchoCanceler = true;
			}

            // To save time, generate a DTLS certificate when the
            // app starts and reuse it for multiple conferences.
            Certificate = Certificate.GenerateCertificate();
        }

        private static App _App;
        private static object AppLock = new object();
        public static App Instance
        {
            get
            {
                lock (AppLock)
                {
                    if (_App == null)
                    {
                        _App = new App();
                    }
                    return _App;
                }
            }
        }

        public void StartSignalling(Action<string> callback)
        {
            Signalling = new Signalling(WebSyncServerUrl);
            Signalling.Start(callback);
        }

        public void StopSignalling(Action<string> callback)
        {
            Signalling.Stop(callback);
            Signalling = null;
        }

        public void StartLocalMedia(ViewGroup videoContainer, Action<string> callback)
        {
            LocalMedia = new LocalMedia();
            LocalMedia.Start(videoContainer, callback);
        }

        public void StopLocalMedia(Action<string> callback)
        {
            LocalMedia.Stop(callback);
            LocalMedia = null;
        }

        public void StartConference(Action<string> callback)
        {
            // Create a WebRTC audio stream description (requires a
            // reference to the local audio feed).
            AudioStream = new AudioStream(LocalMedia.LocalMediaStream);

            // Create a WebRTC video stream description (requires a
            // reference to the local video feed). Whenever a P2P link
            // initializes using this description, position and display
            // the remote video control on-screen by passing it to the
            // layout manager created above. Whenever a P2P link goes
            // down, remove it.
            VideoStream = new VideoStream(LocalMedia.LocalMediaStream);
            VideoStream.OnLinkInit += AddRemoteVideoControl;
            VideoStream.OnLinkDown += RemoveRemoteVideoControl;

            // Create a conference using our stream descriptions.
            Conference = new FM.IceLink.Conference(IceLinkServerAddress, new Stream[] { AudioStream, VideoStream });

            // Use our pre-generated DTLS certificate.
            Conference.DtlsCertificate = Certificate;

            // Supply TURN relay credentials in case we are behind a
            // highly restrictive firewall. These credentials will be
            // verified by the TURN server.
            Conference.RelayUsername = "test";
            Conference.RelayPassword = "pa55w0rd!";

            // Add a few event handlers to the conference so we can see
            // when a new P2P link is created or changes state.
            Conference.OnLinkInit += LogLinkInit;
            Conference.OnLinkUp += LogLinkUp;
			Conference.OnLinkDown += LogLinkDown;

			// Start echo canceller.
            if (EnableSoftwareEchoCancellation)
            {
                OpusEchoCanceller = new OpusEchoCanceller(OpusClockRate, OpusChannels);
                OpusEchoCanceller.Start();
            }

            Signalling.Attach(Conference, SessionId, callback);
        }

        private void AddRemoteVideoControl(StreamLinkInitArgs e)
        {
            try
            {
                var remoteVideoControl = (View)e.Link.GetRemoteVideoControl();
                LocalMedia.LayoutManager.AddRemoteVideoControl(e.PeerId, remoteVideoControl);
            }
            catch (Exception ex)
            {
                Log.Error("Could not add remote video control.", ex);
            }
        }

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
                Log.Error("Could not remove remote video control.", ex);
            }
        }

        private void LogLinkInit(LinkInitArgs e)
        {
            Log.Info("Link to peer initializing...");
        }

        private void LogLinkUp(LinkUpArgs e)
        {
            Log.Info("Link to peer is UP.");
        }

        private void LogLinkDown(LinkDownArgs e)
        {
            Log.Info(string.Format("Link to peer is DOWN. {0}", e.Exception.Message));
        }

        public void StopConference(Action<string> callback)
        {
            // Detach signalling from the conference.
            Signalling.Detach((error) =>
            {
                // Stop echo canceller.
                if (EnableSoftwareEchoCancellation)
                {
                    OpusEchoCanceller.Stop();
                    OpusEchoCanceller = null;
                }

                Conference.OnLinkInit -= LogLinkInit;
                Conference.OnLinkUp   -= LogLinkUp;
                Conference.OnLinkDown -= LogLinkDown;
                Conference = null;

                VideoStream.OnLinkInit -= AddRemoteVideoControl;
                VideoStream.OnLinkDown -= RemoveRemoteVideoControl;
                VideoStream = null;

                AudioStream = null;
                
                callback(error);
            });
        }

        public void UseNextVideoDevice()
        {
            LocalMedia.LocalMediaStream.UseNextVideoDevice();
        }

        public void PauseLocalVideo()
        {
            LocalMedia.LocalMediaStream.PauseVideo();
        }

        public void ResumeLocalVideo()
        {
            LocalMedia.LocalMediaStream.ResumeVideo();
        }
    }
}