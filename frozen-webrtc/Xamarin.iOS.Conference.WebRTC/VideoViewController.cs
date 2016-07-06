using System;
using System.Drawing;

using CoreFoundation;
using UIKit;
using Foundation;

using FM;
using FM.IceLink;
using FM.IceLink.WebRTC;
using FM.IceLink.WebSync;
using FM.WebSync;
using FM.WebSync.Subscribers;
using AVFoundation;

namespace Xamarin.iOS.Conference.WebRTC
{
    public partial class VideoViewController : UIViewController
    {
        private App App;

        private bool LocalMediaStarted;
        private bool ConferenceStarted;

        public VideoViewController()
            : base("VideoViewController", null)
        { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Hide the status bar to give us more screen real estate.
            // Also disable the idle timer since there isn't much touch
            // screen interaction during a video chat.
            UIApplication.SharedApplication.StatusBarHidden = true;
            UIApplication.SharedApplication.IdleTimerDisabled = true;

            App = App.Instance;

            _leaveButton.Clicked += new EventHandler(_leaveButton_Clicked);

            _sessionID.Title = "Session ID: " + App.SessionId;

            StartLocalMedia();
        }

        private void StartLocalMedia()
        {
            LocalMediaStarted = true;
            App.StartLocalMedia(_videoView, (error) =>
            {
                if (error != null)
                {
                    Alert(error);
                }
                else
                {
                    // Start conference now that the local media is available.
                    StartConference();
                }
            });
        }

        private void StopLocalMedia()
        {
            App.StopLocalMedia((error) =>
            {
                if (error != null)
                {
                    Alert(error);
                }
            });
        }

        private void StartConference()
        {
            ConferenceStarted = true;
            App.StartConference((error) =>
            {
                if (error != null)
                {
                    Alert(error);
                }
            });
        }

        private void StopConference()
        {
            App.StopConference((error) =>
            {
                if (error != null)
                {
                    Alert(error);
                }
            });
        }

        void _leaveButton_Clicked(object sender, EventArgs e)
        {
            if (ConferenceStarted)
            {
                StopConference();
                ConferenceStarted = false;
            }

            if (LocalMediaStarted)
            {
                StopLocalMedia();
                LocalMediaStarted = false;
            }

            DismissViewController(false, null);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            
            // LayoutManager likes to bring our view to the front.
            // Let's make sure the tool bar is still in the front
            // so clicks will still register.
            View.BringSubviewToFront(_toolBar);
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            View.UserInteractionEnabled = true;
            View.Toast(new NSString("Double-tap to switch camera."));
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);
            var touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                if (touch.TapCount == 2)
                {
                    App.UseNextVideoDevice();
                }
            }
        }

        private void Alert(string format, params object[] args)
        {
            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                new UIAlertView("Alert", string.Format(format, args), null, "OK").Show();
            });
        }
    }
}