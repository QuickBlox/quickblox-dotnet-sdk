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
using Android.Graphics;

using FM.IceLink.WebRTC;
using Android.Text;

namespace Xamarin.Android.Conference.WebRTC
{
    [Activity(Label = "IceLink Conference - WebRTC", MainLauncher = true, Icon = "@drawable/icon")]
    public class SessionActivity : Activity
    {
        private App App;

        private static bool SignallingStarted;

        private TextView CreateSession;
        private Button CreateButton;
        private EditText JoinSession;
        private Button JoinButton;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            RequestWindowFeature(WindowFeatures.NoTitle);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            Window.SetSoftInputMode(SoftInput.StateAlwaysHidden);
            SetContentView(Resource.Layout.Session);

            CreateSession = (TextView)FindViewById(Resource.Id.createSession);
            CreateButton = (Button)FindViewById(Resource.Id.createButton);
            JoinSession = (EditText)FindViewById(Resource.Id.joinSession);
            JoinButton = (Button)FindViewById(Resource.Id.joinButton);

            App = App.Instance;

            // Create a random 6 digit number for the new session ID.
            CreateSession.Text = new FM.Randomizer().Next(100000, 999999).ToString();

            JoinSession.SetFilters(new[] { new InputFilterLengthFilter(6) });

            CreateButton.Click += new EventHandler(CreateButton_Click);
            JoinButton.Click += new EventHandler(JoinButton_Click);

            // Start signalling when the view loads
            // (if it hasn't started already).
            if (!SignallingStarted)
            {
                StartSignalling();
            }
        }

        private void StartSignalling()
        {
            SignallingStarted = true;
            App.StartSignalling((error) =>
            {
                if (error != null)
                {
                    Alert(error);
                }
            });
        }

        private void SwitchToVideoChat(string sessionId)
        {
            if (sessionId.Length == 6)
            {
                App.SessionId = sessionId;

                // Show the video chat.
                StartActivity(new Intent(ApplicationContext, typeof(VideoActivity)));
            }
            else
            {
                Alert("Session ID must be 6 digits long.");
            }
        }

        void JoinButton_Click(object sender, EventArgs e)
        {
            SwitchToVideoChat(JoinSession.Text);
        }

        void CreateButton_Click(object sender, EventArgs e)
        {
            SwitchToVideoChat(CreateSession.Text);
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
    }
}