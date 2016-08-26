using System;
using System.Threading.Tasks;
using FM;
using FM.IceLink.WebRTC;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

#if __IOS__
using AVFoundation;
#endif

namespace Xamarin.Forms.Conference.WebRTC
{
	public class LocalMedia
	{
		// We're going to need both audio and video
		// for this example. We can constrain the
		// video slightly for performance benefits.
		public bool Audio { get; set;}  = true;
		public bool Video { get; set;} = true;
		private int  VideoWidth = 320;
		private int  VideoHeight = 240;
		private int  VideoFrameRate = 27;

		public LocalMediaStream LocalMediaStream { get; private set; }

#if __IOS__ || __ANDROID__
        public FormsLayoutManager LayoutManager { get; private set; }
#elif WINDOWS_APP
        public Win8LayoutManager LayoutManager { get; private set; }
#endif


        public void Start(object videoContainer, Action<string> callback)
		{
#if __IOS__
            AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.PlayAndRecord,
                AVAudioSessionCategoryOptions.AllowBluetooth |
                AVAudioSessionCategoryOptions.DefaultToSpeaker);
#endif

			UserMedia.GetMedia(new GetMediaArgs(Audio, Video)
			{
#if __IOS__ || __ANDROID__
                
#elif WINDOWS_APP
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
#endif

                VideoWidth = VideoWidth,           // optional
				VideoHeight = VideoHeight,          // optional
				VideoFrameRate = VideoFrameRate,   // optional
				DefaultVideoPreviewScale = LayoutScale.Contain, // optional
				DefaultVideoScale = LayoutScale.Contain,        // optional
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

                    // This is our local video control, a UIView (iOS) or
                    // and NSView (Mac). It is constantly updated with our
                    // live video feed since we requested video above. Add
                    // it directly to the UI or use the IceLink layout manager,
                    // which we do below.
                    var localVideoControl = e.LocalVideoControl;

#if __IOS__ || __ANDROID__
                    // Create an IceLink layout manager, which makes the task
                    // of arranging video controls easy. Give it a reference
                    // to a UIView that can be filled with video feeds.
                    LayoutManager = new FormsLayoutManager((AbsoluteLayout)videoContainer);

                    // Position and display the local video control on-screen
                    // by passing it to the layout manager created above.
                    LayoutManager.SetLocalVideoControl(new FormsVideoControl(localVideoControl));
#elif WINDOWS_APP
                    var content = Windows.UI.Xaml.Window.Current.Content as Windows.UI.Xaml.Controls.Frame;
                    var list = new List<Windows.UI.Xaml.Controls.Canvas>();
                    FindChildren<Windows.UI.Xaml.Controls.Canvas>(list, content.Content as Windows.UI.Xaml.DependencyObject); 
                    
                    // Create an IceLink layout manager, which makes the task
                    // of arranging video controls easy. Give it a reference
                    // to a WinForms control that can be filled with video feeds.
                    // Use WpfLayoutManager for WPF-based applications.
                    LayoutManager = new Win8LayoutManager(list.Last());

                    // Position and display the local video control on-screen
                    // by passing it to the layout manager created above.
                    LayoutManager.SetLocalVideoControl(localVideoControl);
#endif



                    callback(null);
				}
			});
		}

#if WINDOWS_APP
        internal static void FindChildren<T>(List<T> results, Windows.UI.Xaml.DependencyObject startNode) where T : Windows.UI.Xaml.DependencyObject
        {
            int count = Windows.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(startNode);
            for (int i = 0; i < count; i++)
            {
                Windows.UI.Xaml.DependencyObject current = Windows.UI.Xaml.Media.VisualTreeHelper.GetChild(startNode, i);
                if ((current.GetType()).Equals(typeof(T)) || (current.GetType().GetTypeInfo().IsSubclassOf(typeof(T))))
                {
                    T asType = (T)current;
                    results.Add(asType);
                }
                FindChildren<T>(results, current);
            }
        }
#endif

        public void Stop(Action<string> callback)
		{
			// Clear out the layout manager.
			if (LayoutManager != null)
			{
				LayoutManager.UnsetLocalVideoControl();
				LayoutManager.RemoveRemoteVideoControls();
				LayoutManager = null;
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

