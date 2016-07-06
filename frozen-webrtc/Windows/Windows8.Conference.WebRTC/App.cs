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
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Windows8.Conference.WebRTC
{
    public class ConferenceApp
    {
        private string IceLinkServerAddress = "demo.icelink.fm:3478";
        private string WebSyncServerUrl = "http://v4.websync.fm/websync.ashx"; // WebSync On-Demand

        private LocalMedia localMedia;
        private Signalling signalling;
        private FM.IceLink.Conference conference;
        private string sessionID = "/121585";
        private Certificate Certificate;

        public LocalMedia LocalMedia { get { return localMedia; } }
        public Signalling Signalling { get { return signalling; } }
        public string SessionID { get { return sessionID; } set { sessionID = value; } }
        
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

            // To save time, generate a DTLS certificate once when the app starts
            Certificate = Certificate.GenerateCertificate();
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

        public bool SignallingExists()
        {
            return (signalling != null);
        }

        public void StartSignalling(Action<Exception> callback)
        {
            if (signalling == null)
            {
                signalling = new Signalling(WebSyncServerUrl);
                signalling.Start(callback);
            }
            else
            {
                callback(new Exception(string.Format("Error starting signalling. {0}", Signalling.LastStartException.Message )));
            }
        }

        public void StopSignalling(Action<Exception> callback)
        {
            if (signalling != null)
            {
                signalling.Stop(callback);
            }
        }

        public bool LocalMediaExists()
        {
            return (localMedia != null);
        }

        public void StartLocalMedia(MainPage videoWindow, Action<Exception> callback)
        {
            if (localMedia == null)
            {
                localMedia = new LocalMedia();
                localMedia.Start(videoWindow, callback);
            }
            else
            {
                callback(new Exception(string.Format("Error starting local media. {0}", localMedia.LastStartException.Message)));
            }
        }

        public void StopLocalMedia(Action<Exception> callback)
        {
            if (localMedia != null)
            {
                localMedia.Stop(callback);
            }
        }

        public bool ConferenceExists()
        {
            return (conference != null);
        }

        //Video Chat is the main form
        public void StartConference(MainPage videoWindow, Action<Exception> callback)
        {
            if (!SignallingExists())
            {
                callback(new Exception("Signalling must exist before starting a conference."));
            }
            else if (!LocalMediaExists())
            {
                callback(new Exception("Local media must exist before starting a conference."));
            }
            else if (ConferenceExists())
            {
                //trying to start a conference again
                callback(signalling.LastConferenceException);
            }
            else
            {
                try
                {
                    var localMediaStream = localMedia.LocalStream;
                    
                    // This is our local video control, a WinForms Control or
                    // WPF FrameworkElement. It is constantly updated with
                    // our live video feed since we requested video above.
                    // Add it directly to the UI or use the IceLink layout
                    // manager, which we do below.
                    var localVideoControl = localMedia.LocalVideoControl;

                    // Create an IceLink layout manager, which makes the task
                    // of arranging video controls easy. Give it a reference
                    // to a WinForms control that can be filled with video feeds.
                    // For WPF users, the WebRTC extension includes
                    // WpfLayoutManager, which accepts a Canvas.
                    var layoutManager = localMedia.LayoutManager;

                    // Create a WebRTC audio stream description (requires a
                    // reference to the local audio feed).
                    var audioStream = new AudioStream(localMediaStream);

                    // Create a WebRTC video stream description (requires a
                    // reference to the local video feed). Whenever a P2P link
                    // initializes using this description, position and display
                    // the remote video control on-screen by passing it to the
                    // layout manager created above. Whenever a P2P link goes
                    // down, remove it.
                    var videoStream = new VideoStream(localMediaStream);
                    videoStream.OnLinkInit += (e) =>
                    {
                        var remoteVideoControl = (FrameworkElement)e.Link.GetRemoteVideoControl();
                        layoutManager.AddRemoteVideoControl(e.PeerId, remoteVideoControl);

                        // When double-clicked, mute/unmute the remote video.
                        videoWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            // When double-tapped, mute/unmute the remote video.
                            remoteVideoControl.DoubleTapped += (sender, ce) =>
                            {
                                if (e.Link.RemoteVideoIsMuted())
                                {
                                    // Resume rendering incoming video.
                                    e.Link.UnmuteRemoteVideo();
                                }
                                else
                                {
                                    // Stop rendering incoming video.
                                    e.Link.MuteRemoteVideo();
                                }
                            };
                        });
                    };
                    videoStream.OnLinkDown += (e) =>
                    {
                        layoutManager.RemoveRemoteVideoControl(e.PeerId);
                    };

                    // Create a new IceLink conference.
                    conference = new FM.IceLink.Conference(IceLinkServerAddress, new Stream[] { audioStream, videoStream });

                    //Use our generated DTLS certificate.
                    conference.DtlsCertificate = Certificate;
                    
                    // Supply TURN relay credentials in case we are behind a
                    // highly restrictive firewall. These credentials will be
                    // verified by the TURN server.
                    conference.RelayUsername = "test";
                    conference.RelayPassword = "pa55w0rd!";

                    // Add a few event handlers to the conference so we can see
                    // when a new P2P link is created or changes state.
                    conference.OnLinkInit += (e) =>
                    {
                        Log.Info("Link to peer initializing...");
                    };
                    conference.OnLinkUp += (e) =>
                    {
                        Log.Info("Link to peer is UP.");
                    };
                    conference.OnLinkDown += (e) =>
                    {
                        Log.InfoFormat("Link to peer is DOWN. {0}", e.Exception.Message);
                    };
                    callback(null);
                }
                catch (Exception ex)
                {
                    callback(ex);
                }
            }
        }

        public void JoinConferenceSession(string conferenceSessionID, Action<Exception> callback)
        {
            if (!SignallingExists())
            {
                callback(new Exception("Signalling must exist before joining a conference."));
            }
            else if (!LocalMediaExists())
            {
                callback(new Exception("Local media must exist before joining a conference."));
            }

            if (ConferenceExists())
            {
                signalling.Attach(conference, conferenceSessionID, (ex) =>
                {
                    if (ex != null)
                    {
                        callback(ex);
                    }
                    else
                    {
                        callback(null);
                        Log.InfoFormat("Conference Session is now {0}", conferenceSessionID);
                    }
                });
            }
        }

        public void LeaveConferenceSession(string conferenceSessionID, Action<Exception> callback)
        {
            if (!SignallingExists())
            {
                callback(new Exception("Signalling must exist before leaving a conference."));
            }
            else if (!LocalMediaExists())
            {
                callback(new Exception("Local media must exist before leaving a conference."));
            }

            if (ConferenceExists())
            {
                signalling.LeaveConference(conferenceSessionID, (ex) => 
                {
                    if (ex != null)
                    {
                        callback(ex);
                    }
                    else
                    {
                        callback(null);
                        Log.InfoFormat("Left Conference Session {0}", sessionID);
                    }
                });
            }
        }

        public void PauseLocalMediaVideo()
        {
            if (LocalMediaExists())
            {
                localMedia.PauseVideo();
            }
        }

        public void ResumeLocalMediaVideo()
        {
            if (LocalMediaExists())
            {
                localMedia.ResumeVideo();
            }
        }
    }
}
