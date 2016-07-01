using System;
using System.Drawing;

using CoreFoundation;
using Foundation;
using UIKit;

namespace Xamarin.iOS.Conference.WebRTC
{
    public partial class SessionViewController : UIViewController
    {
        private App App;

        public SessionViewController()
            : base("SessionViewController", null)
        { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            App = App.Instance;

            // Create a random 6 digit number for the new session ID.
            _createSession.Text = new FM.Randomizer().Next(100000, 999999).ToString();
            
            _joinSession.ShouldChangeCharacters = (textField, range, replacement) =>
            {
                if(textField == _joinSession)
                {
					int newLength = textField.Text.Length + replacement.Length - (int)range.Length;
                    return newLength <= 6;
                }
                return false;
            };
            _createButton.TouchUpInside += new EventHandler(_createButton_TouchUpInside);
            _joinButton.TouchUpInside += new EventHandler(_joinButton_TouchUpInside);

            // Start signalling when the view loads.
            StartSignalling();
        }

        private void StartSignalling()
        {
            App.StartSignalling((error) =>
            {
                if (error != null)
                {
                    Alert(error);
                }
            });
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);
            foreach (var view in View.Subviews)
            {
                if ((view is UITextField) && view.IsFirstResponder)
                {
                    view.ResignFirstResponder();
                }
            }
        }

        private void SwitchToVideoChat(string sessionId)
        {
            if (sessionId.Length == 6)
            {
                App.SessionId = sessionId;

                // Show the video chat.
                PresentViewController(new VideoViewController(), false, null);
            }
            else
            {
                Alert("Session ID must be 6 digits long.");
            }
        }

        void _joinButton_TouchUpInside(object sender, EventArgs e)
        {
            SwitchToVideoChat(_joinSession.Text);
            View.EndEditing(true);
        }

        void _createButton_TouchUpInside(object sender, EventArgs e)
        {
            SwitchToVideoChat(_createSession.Text);
            View.EndEditing(true);
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