using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using FM;
using FM.IceLink;
using FM.IceLink.WebRTC;
using Windows.UI.Core;

namespace WindowsPhone.Conference.WebRTC
{
    public class ConferenceApp
    {
        private string IceLinkServerAddress = "demo.icelink.fm:3478";
        private string WebSyncServerUrl = "http://v4.websync.fm/websync.ashx"; // WebSync On-Demand

        public LocalMedia LocalMedia { get; private set; }
        private Signalling Signalling;

        private AudioStream AudioStream;
        private VideoStream VideoStream;
        private FM.IceLink.Conference Conference;

        public string SessionId { get; set; } 
        
        public ConferenceApp()
        {
            // Log to the console.
            FM.Log.Provider = new DebugLogProvider(LogLevel.Info);
            
            // WebRTC has chosen VP8 as its mandatory video codec.
            // Since video encoding is best done using native code,
            // reference the video codec at the application-level.
            // This is required when using a WebRTC video stream.
            VideoStream.RegisterCodec("VP8", () =>
            {
                return new Vp8Codec();
            }, true);
        }

        private static ConferenceApp app;
        private static object AppLock = new object();
        public static ConferenceApp Instance
        {
            get
            {
                lock (AppLock)
                {
                    if (app == null)
                    {
                        app = new ConferenceApp();
                    }
                    return app;
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

        public void StartLocalMedia(VideoPage videoWindow, Action<string> callback)
        {
            LocalMedia = new LocalMedia();
            LocalMedia.Start(videoWindow, callback);
        }

        public void StopLocalMedia(Action<string> callback)
        {
            LocalMedia.Stop(callback);
            LocalMedia = null;
        }

        //Video Chat is the main form
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

            // Create a new IceLink conference.
            Conference = new FM.IceLink.Conference(IceLinkServerAddress, new Stream[] { AudioStream, VideoStream });

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

            // Attach signalling to the conference.
            Signalling.Attach(Conference, SessionId, callback);
        }

        private void AddRemoteVideoControl(StreamLinkInitArgs e)
        {
            try
            {
                var remoteVideoControl = (FrameworkElement)e.Link.GetRemoteVideoControl();
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

        public void PauseLocalMediaVideo()
        {
            LocalMedia.PauseVideo();
        }

        public void ResumeLocalMediaVideo()
        {
            LocalMedia.ResumeVideo();
        }
    }
}
