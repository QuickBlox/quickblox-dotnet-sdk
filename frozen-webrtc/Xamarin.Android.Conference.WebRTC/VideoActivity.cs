using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;

using FM.IceLink.WebRTC;

namespace Xamarin.Android.Conference.WebRTC
{
    [Activity(Label = "IceLink Conference - WebRTC", Icon = "@drawable/icon")]
    public class VideoActivity : Activity
    {
        private App App;

        private static bool LocalMediaStarted;
        private static bool ConferenceStarted;

        private RelativeLayout Layout;
        private Button LeaveButton;
        private TextView SessionId;
        private static RelativeLayout Container;
        private GestureDetector GestureDetector;
        
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            RequestWindowFeature(WindowFeatures.NoTitle);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            SetContentView(Resource.Layout.Video);

            Layout = (RelativeLayout)FindViewById(Resource.Id.layout);
            LeaveButton = (Button)FindViewById(Resource.Id.leaveButton);
            SessionId = (TextView)FindViewById(Resource.Id.sessionId);

            App = App.Instance;

            // Load native libraries.
            if (!Build.CpuAbi.ToLower().Contains("x86") && !Build.CpuAbi.ToLower().Contains("arm64"))
            {
                Java.Lang.JavaSystem.LoadLibrary("audioprocessing");
                Java.Lang.JavaSystem.LoadLibrary("audioprocessingJNI");
            }
            Java.Lang.JavaSystem.LoadLibrary("opus");
            Java.Lang.JavaSystem.LoadLibrary("opusJNI");
            Java.Lang.JavaSystem.LoadLibrary("vpx");
            Java.Lang.JavaSystem.LoadLibrary("vpxJNI");

            LeaveButton.Click += new EventHandler(LeaveButton_Click);

            SessionId.Text = "Session ID: " + App.SessionId;

            // For demonstration purposes, use the double-tap gesture
            // to switch between the front and rear camera.
            GestureDetector = new GestureDetector(this, new OnGestureListener(App));

            // Preserve a static container across
            // activity destruction/recreation.
            var c = (RelativeLayout)FindViewById(Resource.Id.container);
            if (Container == null)
            {
                Container = c;

                Toast.MakeText(this, "Double tap to switch camera.", ToastLength.Short).Show();
            }
            Layout.RemoveView(c);

            if (!LocalMediaStarted)
            {
                StartLocalMedia();
            }
        }

        private void StartLocalMedia()
		{
            // Android's video providers need a context
            // in order to create surface views for video
            // rendering, so we need to supply one before
            // we start up the local media.
            DefaultProviders.AndroidContext = this;

            LocalMediaStarted = true;
            App.StartLocalMedia(Container, (error) =>
            {
                if (error != null)
                {
                    Alert(error);
                }
                else
                {
                    // Start conference now that the local media is available.
                    if (!ConferenceStarted)
                    {
                        StartConference();
                    }
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

        private void StopLocalMedia()
        {
			if (LocalMediaStarted)
			{
				LocalMediaStarted = false;
	            App.StopLocalMedia((error) =>
				{
					Container = null;
					
					Finish();
	            });
        	}
			else
			{
				Container = null;

				Finish();
			}
		}

        private void StopConference()
        {
			if (ConferenceStarted)
			{
				ConferenceStarted = false;
				App.StopConference((error) =>
				{
					StopLocalMedia();
				});
			}
			else
			{
				StopLocalMedia();
			}
        }

        void LeaveButton_Click(object sender, EventArgs e)
        {
			StopConference();
        }

		public override void OnBackPressed()
		{
			StopConference();
		}

        protected override void OnPause()
        {
            // Android requires us to pause the local
            // video feed when pausing the activity.
            // Not doing this can cause unexpected side
            // effects and crashes.
            if (LocalMediaStarted)
            {
                App.PauseLocalVideo();
            }
            
            // Remove the static container from the current layout.
            if (Container != null)
            {
                Layout.RemoveView(Container);
            }

            base.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Add the static container to the current layout.
            if (Container != null)
            {
                Layout.AddView(Container);
            }

            // Resume the local video feed.
            if (LocalMediaStarted)
            {
                App.ResumeLocalVideo();
            }
        }
    
        public override bool OnTouchEvent(MotionEvent evt)
        {
            // Handle the double-tap event.
            if (GestureDetector == null || !GestureDetector.OnTouchEvent(evt))
            {
                return base.OnTouchEvent(evt);
            }
            return true;
        }

        private void Alert(string format, params object[] args)
        {
            RunOnUiThread(() =>
            {
                if (!IsFinishing)
                {
                    var alert = new AlertDialog.Builder(this);
                    alert.SetMessage(string.Format(format, args));
                    alert.SetPositiveButton("OK", (sender, e) => { });
                    alert.Show();
                }
            });
        }

        class OnGestureListener : GestureDetector.SimpleOnGestureListener
        {
            private App App;

            public OnGestureListener(App app)
            {
                App = app;
            }

            public override bool OnDoubleTap(MotionEvent e)
            {
                App.UseNextVideoDevice();
                return true;
            }
        }
    }
}

