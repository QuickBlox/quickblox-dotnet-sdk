using System;
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

    public class LocalMedia
    {
        // We're going to need both audio and video
        // for this example. We can constrain the
        // video slightly for performance benefits.
        private bool Audio = true;
        private bool Video = true;
        private int VideoWidth = 320;
        private int VideoHeight = 240;
        private int VideoFrameRate = 15;

        public LocalMediaStream LocalMediaStream { get; private set; }
        public WpfLayoutManager LayoutManager { get; private set; }

        private FrameworkElement LocalVideoControl;

        public void Start(VideoPage videoPage, Action<string> callback)
        {
            // WebRTC audio and video streams require us to first get access to
            // the local media (microphone, camera, or both).
            UserMedia.GetMedia(new GetMediaArgs(Audio, Video)
            {
                // Specify an audio capture provider, a video
                // capture provider, an audio render provider,
                // and a video render provider. The IceLink
                // SDK comes bundled with video support for
                // WinForms and WPF, and uses NAudio for audio
                // support. For lower latency audio, use ASIO.
                AudioCaptureProvider = new AudioCaptureProvider(),
                VideoCaptureProvider = new VideoCaptureProvider(videoPage),
                CreateAudioRenderProvider = (e) =>
                {
                    return new AudioRenderProvider();
                },
                CreateVideoRenderProvider = (e) =>
                {
                    return new Direct3DRenderProvider();
                },
                VideoWidth = VideoWidth,    // optional
                VideoHeight = VideoHeight,   // optional
                VideoFrameRate = VideoFrameRate, // optional
                OnFailure = (e) =>
                {
                    callback(string.Format("Could not get media. {0}", e.Exception.Message));
                },
                OnSuccess = (ea) =>
                {
                    // We have successfully acquired access to the local
                    // audio/video device! Grab a reference to the media.
                    // Internally, it maintains access to the local audio
                    // and video feeds coming from the device hardware.
                    LocalMediaStream = ea.LocalStream;

                    // This is our local video control, a WinForms Control or
                    // WPF FrameworkElement. It is constantly updated with
                    // our live video feed since we requested video above.
                    // Add it directly to the UI or use the IceLink layout
                    // manager, which we do below.
                    LocalVideoControl = (FrameworkElement)ea.LocalVideoControl;

                    // Create an IceLink layout manager, which makes the task
                    // of arranging video controls easy. Give it a reference
                    // to a WinForms control that can be filled with video feeds.
                    // For WPF users, the WebRTC extension includes
                    // WpfLayoutManager, which accepts a Canvas.
                    LayoutManager = new WpfLayoutManager(videoPage.Container);

                    // Position and display the local video control on-screen
                    // by passing it to the layout manager created above.
                    LayoutManager.SetLocalVideoControl(LocalVideoControl);

                    // When double-clicked, mute/unmute the local video.
                    videoPage.Dispatcher.BeginInvoke(() =>
                    {
                        LocalVideoControl.DoubleTap += (sender, ce) =>
                        {
                            LocalMediaStream.ToggleVideoMute();
                        };
                    });

                    //call back of null will let the app know to continue on to the signalling stage
                    callback(null);
                }
            });
        }

        public void Stop(Action<string> callback)
        {
            LayoutManager.UnsetLocalVideoControl();
            LayoutManager.RemoveRemoteVideoControls();
            LayoutManager = null;

            LocalMediaStream.Stop();
            LocalMediaStream = null;
            
            LocalVideoControl = null;
            
            callback(null);
        }

        public bool PauseVideo()
        {
            if (LocalMediaStream != null)
            {
                return LocalMediaStream.PauseVideo();
            }
            return false;
        }

        public bool ResumeVideo()
        {
            if (LocalMediaStream != null)
            {
                return LocalMediaStream.ResumeVideo();
            }
            return false;
        }
    }
}
