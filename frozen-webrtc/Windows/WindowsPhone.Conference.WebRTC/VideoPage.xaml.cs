using FM;
using FM.IceLink;
using FM.IceLink.WebRTC;
using FM.IceLink.WebSync;
using FM.WebSync;
using FM.WebSync.Subscribers;
using Microsoft.Phone.Controls;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;

namespace WindowsPhone.Conference.WebRTC
{
    public partial class VideoPage : PhoneApplicationPage
    {
        private ConferenceApp App;

        private bool _StopLocalMedia;
        private bool _StopConference;

        // Constructor
        public VideoPage()
        {
            InitializeComponent();
            App = ConferenceApp.Instance;
            SessionID.Text = "Session ID: " + App.SessionId;
        }

        protected override void OnNavigatedTo(NavigationEventArgs ne)
        {
            // Since this example creates InputPrompts, we have to let
            // the control finish loading while we proceed.
            ThreadPool.QueueUserWorkItem((unused) =>
            {
                StartLocalMedia();
            });
        }

        private void StartLocalMedia()
        {
            App.StartLocalMedia(this, (error) =>
            {
                if (error != null)
                {
                    Alert(error);
                }
                else
                {
                    _StopLocalMedia = true;
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
                else
                {
                    _StopLocalMedia = false;
                }
            });
        }

        private void StartConference()
        {
            App.StartConference((error) =>
            {
                if (error != null)
                {
                    Alert(error);
                }
                else
                {
                    _StopConference = true;
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
                else
                {
                    _StopConference = false;
                }
            });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_StopConference)
            {
                StopConference();
            }

            if (_StopLocalMedia)
            {
                StopLocalMedia();
            }
        }

        private void Alert(string format, params object[] args)
        {
            Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(string.Format(format, args));
            });
        }

        private void LeaveButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void LayoutRoot_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (App.LocalMedia != null)
            {
                App.LocalMedia.LocalMediaStream.UseNextVideoDevice();
            }
        }
    }
}