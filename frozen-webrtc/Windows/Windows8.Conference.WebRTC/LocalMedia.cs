using System;
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
    public class LocalMedia
    {
        // We're going to use both audio and video
        // for this example.
        private bool Audio = true;
        private bool Video = true;
        private int VideoWidth = 320;
        private int VideoHeight = 240;
        private int VideoFrameRate = 15;

        public LocalMediaStream LocalMediaStream { get; private set; }
        public Win8LayoutManager LayoutManager { get; private set; }

        private VideoChat VideoChat;

        public void Start(VideoChat videoChat, Action<string> callback)
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
                AudioCaptureProvider = new NAudioCaptureProvider(),
                VideoCaptureProvider = new VideoCaptureProvider(),
                CreateAudioRenderProvider = (e) =>
                {
                    return new NAudioRenderProvider();
                },
                CreateVideoRenderProvider = (e) =>
                {
                    return new VideoRenderProvider(LayoutScale.Contain);
                },
                VideoWidth = VideoWidth,     // optional
                VideoHeight = VideoHeight,    // optional
                VideoFrameRate = VideoFrameRate, // optional
                OnFailure = (e) =>
                {
                    callback(string.Format("Could not get media. {0}", e.Exception.Message));
                },
                OnSuccess = (e) =>
                {
                    // We have successfully acquired access to the local
                    // audio/video device! Grab a reference to the media.
                    // Internally, it maintains access to the local audio
                    // and video feeds coming from the device hardware.
                    LocalMediaStream = e.LocalStream;

                    // Wire up the UI.
                    VideoChat = videoChat;
                    Win8LayoutManager.SafeInvoke(() =>
                    {
                        VideoChat.GetAudioDevices().ItemsSource = LocalMediaStream.GetAudioDeviceNames();
                        VideoChat.GetVideoDevices().ItemsSource = LocalMediaStream.GetVideoDeviceNames();
                        VideoChat.GetAudioDevices().SelectionChanged += SwitchAudioDevice;
                        VideoChat.GetVideoDevices().SelectionChanged += SwitchVideoDevice;
                        VideoChat.GetAudioDevices().SelectedIndex = LocalMediaStream.GetAudioDeviceNumber();
                        VideoChat.GetVideoDevices().SelectedIndex = LocalMediaStream.GetVideoDeviceNumber();
                    });

                    // Keep the UI updated if devices are switched.
                    LocalMediaStream.OnAudioDeviceNumberChanged += UpdateSelectedAudioDevice;
                    LocalMediaStream.OnVideoDeviceNumberChanged += UpdateSelectedVideoDevice;

                    // This is our local video control, a WinForms control
                    // that displays video coming from the capture source.
                    var localVideoControl = (FrameworkElement)e.LocalVideoControl;

                    // Create an IceLink layout manager, which makes the task
                    // of arranging video controls easy. Give it a reference
                    // to a WinForms control that can be filled with video feeds.
                    // Use WpfLayoutManager for WPF-based applications.
                    LayoutManager = new Win8LayoutManager(VideoChat.GetContainer());

                    // Display the local video control.
                    LayoutManager.SetLocalVideoControl(localVideoControl);

                    callback(null);
                }
            });
        }

        private void SwitchAudioDevice(object sender, SelectionChangedEventArgs e)
        {
            if (VideoChat.GetAudioDevices().SelectedIndex >= 0)
            {
                LocalMediaStream.SetAudioDeviceNumber(VideoChat.GetAudioDevices().SelectedIndex);
            }
        }

        private void SwitchVideoDevice(object sender, SelectionChangedEventArgs e)
        {
            if (VideoChat.GetVideoDevices().SelectedIndex >= 0)
            {
                LocalMediaStream.SetVideoDeviceNumber(VideoChat.GetVideoDevices().SelectedIndex);
            }
        }

        private void UpdateSelectedAudioDevice(AudioDeviceNumberChangedArgs e)
        {
            Win8LayoutManager.SafeInvoke(() =>
            {
                VideoChat.GetAudioDevices().SelectedIndex = e.DeviceNumber;
            });
        }

        private void UpdateSelectedVideoDevice(VideoDeviceNumberChangedArgs e)
        {
            Win8LayoutManager.SafeInvoke(() =>
            {
                VideoChat.GetVideoDevices().SelectedIndex = e.DeviceNumber;
            });
        }

        public void Stop(Action<string> callback)
        {
            // Clear out the layout manager.
            if (LayoutManager != null)
            {
                LayoutManager.RemoveRemoteVideoControls();
                LayoutManager.UnsetLocalVideoControl();
                LayoutManager = null;
            }

            if (LocalMediaStream != null)
            {
                LocalMediaStream.OnAudioDeviceNumberChanged -= UpdateSelectedAudioDevice;
                LocalMediaStream.OnVideoDeviceNumberChanged -= UpdateSelectedVideoDevice;
            }

            if (VideoChat != null)
            {
                VideoChat.GetAudioDevices().SelectionChanged -= SwitchAudioDevice;
                VideoChat.GetVideoDevices().SelectionChanged -= SwitchVideoDevice;
                VideoChat = null;
            }

            // Stop the local media stream.
            if (LocalMediaStream != null)
            {
                LocalMediaStream.Stop();
                LocalMediaStream = null;
            }

            callback(null);
        }
    }
}
