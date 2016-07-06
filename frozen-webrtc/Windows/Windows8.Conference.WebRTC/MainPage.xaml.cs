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
    public sealed partial class MainPage : Page
    {
        private ConferenceApp App;
        
        public MainPage()
        {
            this.InitializeComponent();
            App = ConferenceApp.Instance;
            createSessionID.Text = new Randomizer().Next(100000, 999999).ToString();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs ne)
        {
            App.StartLocalMedia(this, (ex) =>
            {
                if (ex == null)
                {
                    App.StartSignalling((ex2) =>
                    {
                        if (ex2 != null)
                        {
                            Alert(ex2.Message);
                        }
                    });

                    App.StartConference(this, (ex2) =>
                    {
                        if (ex2 != null)
                        {
                            Alert(ex2.Message);
                        }
                    });
                }
                else
                {
                    Alert(ex.Message);
                }
            });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.PauseLocalMediaVideo();
            
            App.StopLocalMedia((ex) =>
            {
                if (ex != null)
                {
                    Alert(ex.Message);
                }
            });
            App.StopSignalling((ex) =>
            {
                if (ex != null)
                {
                    Alert(ex.Message);
                }
            });
        }

        public async void Alert(string format, params object[] args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var md = new MessageDialog(string.Format(format, args));
                md.Commands.Add(new UICommand("OK"));
                await md.ShowAsync();
            });
        }

        private void onLeaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (!SessionIDLabel.Text.Equals("No Session"))
            {
                App.LeaveConferenceSession(App.SessionID, (ex) =>
                {
                    if (ex != null)
                    {
                        Alert(ex.Message);
                    }
                    else
                    {
                        Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            SessionIDLabel.Text = "No Session";
                            inSession(false);
                        });
                    }
                });
            }
        }

        private void onJoinButtonClick(object sender, RoutedEventArgs e)
        {
            if(SessionIDLabel.Text.Equals("No Session"))
            {
                App.SessionID = "/" + joinSessionID.Text;
                App.JoinConferenceSession(App.SessionID, (ex) =>
                {
                    if (ex != null)
                    {
                        Alert(ex.Message);
                    }
                    else
                    {
                        Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            SessionIDLabel.Text = App.SessionID.Replace("/", "");
                            inSession(true);
                        });
                    }
                });
            }
        }

        private void onCreateButtonClick(object sender, RoutedEventArgs e)
        {
            if (SessionIDLabel.Text.Equals("No Session"))
            {
                App.SessionID = "/" + createSessionID.Text;
                App.JoinConferenceSession(App.SessionID, (ex) =>
                {
                    if (ex != null)
                    {
                        Alert(ex.Message);
                    }
                    else
                    {
                        Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            SessionIDLabel.Text = App.SessionID.Replace("/", "");
                            inSession(true);
                        });
                    }
                });
            }
        }

        private void onJoinTextKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                if(joinSessionID.Text.Length == 6)
                {
                    onJoinButtonClick(sender, new RoutedEventArgs());
                }
                else
                {
                    
                }
            }
        }

        private void onJoinSessionTextChanged(object sender, TextChangedEventArgs e)
        {
            String temp = Regex.Replace(joinSessionID.Text, "[^0-9]+", ""); //Regex.Replace(joinSessionID.Text, "[^0-9]+");
            joinSessionID.Text = temp;
        }

        private void inSession(bool inSession)
        {
            leaveButton.IsEnabled = inSession;
            createButton.IsEnabled = !inSession;
            joinButton.IsEnabled = !inSession;
            joinSessionID.IsEnabled = !inSession;
        }
    }
}
