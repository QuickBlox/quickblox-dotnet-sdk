using FM;
using FM.IceLink;
using FM.IceLink.WebRTC;
using FM.IceLink.WebSync;
using FM.WebSync;
using FM.WebSync.Subscribers;
using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Text.RegularExpressions;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Windows8.Conference.WebRTC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideoChat : Page
    {
        private Application App;

        private bool LocalMediaStarted;
        private bool ConferenceStarted;

        public Canvas GetContainer() { return Container; }
        public ComboBox GetAudioDevices() { return AudioDevices; }
        public ComboBox GetVideoDevices() { return VideoDevices; }

        public VideoChat()
        {
            InitializeComponent();
            App = Application.Instance;

            SessionIdLabel.Text = App.SessionId;

            LeaveButton.Click += LeaveButton_Click;

            StartLocalMedia();
        }

        private void StartLocalMedia()
        {
            LocalMediaStarted = true;
            App.StartLocalMedia(this, (error) =>
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

        private void LeaveButton_Click(object sender, RoutedEventArgs e)
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

            Frame.GoBack();
        }

        private async void Alert(string format, params object[] args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var md = new MessageDialog(string.Format(format, args));
                md.Commands.Add(new UICommand("OK"));
                await md.ShowAsync();
            });
        }
    }
}
