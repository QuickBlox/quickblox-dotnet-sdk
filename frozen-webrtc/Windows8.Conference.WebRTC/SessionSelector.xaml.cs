using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Windows8.Conference.WebRTC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SessionSelector : Page
    {
        private Application App;

        public SessionSelector()
        {
            InitializeComponent();
            App = Application.Instance;

            // Create a random 6 digit number for the new session ID.
            CreateSessionLabel.Text = new Random().Next(100000, 999999).ToString();

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

        private void SwitchToVideoChat(string sessionId)
        {
            if (sessionId.Length == 6)
            {
                App.SessionId = sessionId;

                // Show the video chat.
                Frame.Navigate(typeof(VideoChat));
            }
            else
            {
                Alert("Session ID must be 6 digits long.");
            }
        }

        private void CreateSessionButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToVideoChat(CreateSessionLabel.Text);
        }

        private void JoinSessionButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToVideoChat(JoinSessionTextBox.Text);
        }

        private void JoinSessionTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Only accept digit- and control-keys.
            e.Handled = (!char.IsDigit(Convert.ToChar(e.Key)) && !char.IsControl(Convert.ToChar(e.Key)));

            if (e.Key == VirtualKey.Enter)
            {
                SwitchToVideoChat(JoinSessionTextBox.Text);
            }
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
