using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace WindowsPhone.Conference.WebRTC
{
    public partial class SessionPage : PhoneApplicationPage
    {
        ConferenceApp App;

        private bool _StopSignalling;

        public SessionPage()
        {
            InitializeComponent();
            App = ConferenceApp.Instance;
            
            // Create a random 6 digit number for the new session ID.
            createSession.Text = new FM.Randomizer().Next(100000, 999999).ToString();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            StartSignalling();
            base.OnNavigatedTo(e);
        }

        private void StartSignalling()
        {
            App.StartSignalling((error) =>
            {
                if (error != null)
                {
                    Alert(error);
                }
                else
                {
                    _StopSignalling = true;
                }
            });
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            if (_StopSignalling)
            {
                StopSignalling();
            }
            base.OnRemovedFromJournal(e);
        }

        private void StopSignalling()
        {
            App.StopSignalling((error) =>
            {
                if (error != null)
                {
                    Alert(error);
                }
                else
                {
                    _StopSignalling = false;
                }
            });
        }

        private void SwitchToVideoPage(string sessionId)
        {
            if (sessionId.Length == 6)
            {
                App.SessionId = sessionId;
                NavigationService.Navigate(new Uri("/VideoPage.xaml?sessionID=" + sessionId, UriKind.Relative));
            }
            else
            {
                Alert("Session ID must be 6 digits long.");
            }
        }

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToVideoPage(createSession.Text);
        }

        private void joinButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToVideoPage(joinSession.Text);
        }

        private void Alert(string format, params object[] args)
        {
            Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(string.Format(format, args));
            });
        }
    }
}