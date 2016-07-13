using System;

using FM;
using FM.IceLink;
using FM.IceLink.WebRTC;
using QbChat.Pcl;

#if __IOS__
using Xamarin.Forms.Conference.WebRTC.iOS.Opus;
using Xamarin.Forms.Conference.WebRTC.iOS.VP8;
#else
using Xamarin.Forms.Conference.WebRTC.Droid.Opus;
using Xamarin.Forms.Conference.WebRTC.Droid.VP8;
#endif

using Xamarin.Forms.Conference.WebRTC;
using Xamarin.PCL;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class App : Application
	{
		public static QbProvider QbProvider = new QbProvider(ShowInternetMessage);
		public static bool IsInternetAvaliable { get; set;}
		private static bool isInternetMessageShowing;
		public static int UserId { get; set;}
		public static INavigation Navigation { get; set;}

		private string IceLinkServerAddress = "demo.icelink.fm:3478";
		private string WebSyncServerUrl = "http://v4.websync.fm/websync.ashx"; // WebSync On-Demand

		private Signalling Signalling;
		private LocalMedia LocalMedia;

		private AudioStream AudioStream;
		private VideoStream VideoStream;
		private FM.IceLink.Conference Conference;

		public String SessionId { get; set; }

		#if __ANDROID__
		private OpusEchoCanceller OpusEchoCanceller = null;
		#endif

		private int OpusClockRate = 48000;
		private int OpusChannels = 2;

		private Certificate Certificate;

		private Label CreateSession;
		private Button CreateButton;
		private Entry JoinSession;
		private Button JoinButton;

		public App()
		{
			//MessageProvider.Instance.Init(App.QbProvider.GetXmppClient());

			//var page = new NavigationPage(new LoginPage());
			//Navigation = page.Navigation;
			//Current.MainPage = page;
			//return;

			#if __ANDROID__

			// Log to the console.
			FM.Log.Provider = new AndroidLogProvider(LogLevel.Info);

			#else

			// Log to the console.
			FM.Log.Provider = new NSLogProvider(LogLevel.Info);

			#endif

			// WebRTC has chosen VP8 as its mandatory video codec.
			// Since video encoding is best done using native code,
			// reference the video codec at the application-level.
			// This is required when using a WebRTC video stream.
			VideoStream.RegisterCodec("VP8", () =>
			{
				return new Vp8Codec();
			}, true);

			// For improved audio quality, we can use Opus. By
			// setting it as the preferred audio codec, it will
			// override the default PCMU/PCMA codecs.
			AudioStream.RegisterCodec("opus", OpusClockRate, OpusChannels, () =>
			{
				#if __ANDROID__
				return new OpusCodec(OpusEchoCanceller);
				#else
				return new OpusCodec();
				#endif
			}, true);

			// To save time, generate a DTLS certificate when the
			// app starts and reuse it for multiple conferences.
			Certificate = Certificate.GenerateCertificate();

			CreateSession = new Label
			{
				HorizontalTextAlignment = TextAlignment.Center,
				Text = new Randomizer().Next(100000, 999999).ToString()
			};
			CreateButton = new Button
			{
				Text = "Create"
			};
            JoinSession = new Entry
            {
                Keyboard = Keyboard.Numeric
            };
			JoinButton = new Button
			{
				Text = "Join"
			};

			// The root page of your application
			MainPage = new ContentPage
			{
				Content = new StackLayout
				{
                    Spacing = 0,
					Orientation = StackOrientation.Horizontal,
					HorizontalOptions = LayoutOptions.Center,
                    Padding = new Thickness(0, 20, 0, 0),
					Children =
					{
						new StackLayout
						{
							Orientation = StackOrientation.Vertical,
                            Padding = new Thickness(20, 0, 0, 0),
							Children =
							{
								new Label
								{
                                    HorizontalTextAlignment = TextAlignment.Center,
									Text = "Create Session\nwith the ID:"
								},
								CreateSession,
								CreateButton
							}
						},
						new StackLayout
						{
							Orientation = StackOrientation.Vertical,
                            Padding = new Thickness(20, 0, 20, 0),
							Children =
							{
								new Label
								{
									HorizontalTextAlignment = TextAlignment.Center,
									Text = "OR"
								}
							}
						},
						new StackLayout
						{
							Orientation = StackOrientation.Vertical,
                            Padding = new Thickness(0, 0, 20, 0),
							Children =
							{
								new Label
								{
									HorizontalTextAlignment = TextAlignment.Center,
									Text = "Join Session\nwith the ID:"
								},
								JoinSession,
								JoinButton
							}
						}
					}
				}
			};

			CreateButton.Clicked += (object sender, EventArgs e) =>
			{
				SwitchToVideoChat(CreateSession.Text);
			};
			JoinButton.Clicked += (object sender, EventArgs e) =>
			{
				SwitchToVideoChat(JoinSession.Text);
			};
		}

		protected override void OnStart()
		{
			Signalling = new Signalling(WebSyncServerUrl);
			Signalling.Start((error) =>
			{
				if (error != null)
				{
					Alert(error);
				}
			});
		}

		protected override void OnSleep()
		{
			if (LocalMedia != null && LocalMedia.LocalMediaStream != null)
			{
				LocalMedia.LocalMediaStream.PauseVideo();
			}
		}

		protected override void OnResume()
		{
			if (LocalMedia != null && LocalMedia.LocalMediaStream != null)
			{
				LocalMedia.LocalMediaStream.ResumeVideo();
			}
		}

		private AbsoluteLayout VideoContainer;
		private Button LeaveButton;
		private Label SessionIdText;

		private bool LocalMediaStarted;
		private bool ConferenceStarted;

		private async void SwitchToVideoChat(string sessionId)
		{
			if (sessionId.Length == 6)
			{
				SessionId = sessionId;

				VideoContainer = new AbsoluteLayout
				{
					BackgroundColor = Color.Black,
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand
				};

				var tapGestureRecognizer = new TapGestureRecognizer
				{
					NumberOfTapsRequired = 2
				};
				tapGestureRecognizer.Tapped += (object sender, EventArgs e) =>
				{
					if (LocalMedia != null && LocalMedia.LocalMediaStream != null)
					{
						LocalMedia.LocalMediaStream.UseNextVideoDevice();
					}
				};
				VideoContainer.GestureRecognizers.Add(tapGestureRecognizer);

				LeaveButton = new Button
				{
					Text = "Leave",
					HorizontalOptions = LayoutOptions.Start
				};

				SessionIdText = new Label
				{
					HorizontalTextAlignment = TextAlignment.End,
                    VerticalTextAlignment = TextAlignment.Center,
					Text = "Session ID: " + SessionId,
					HorizontalOptions = LayoutOptions.EndAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand
				};

				// Show the video chat.
				await MainPage.Navigation.PushModalAsync(new ContentPage
				{
					Content = new StackLayout
					{
						Spacing = 0,
						Orientation = StackOrientation.Vertical,
						Children = 
						{
							VideoContainer,
							new StackLayout
							{
								Orientation = StackOrientation.Horizontal,
								HorizontalOptions = LayoutOptions.FillAndExpand,
								Padding = new Thickness(10, 0),
								Children = 
								{
									LeaveButton,
									SessionIdText
								}
							}
						}
					}
				});

				LeaveButton.Clicked += (object sender, EventArgs e) =>
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

					MainPage.Navigation.PopModalAsync();
				};

				StartLocalMedia();
			}
			else
			{
				Alert("Session ID must be 6 digits long.");
			}
		}

		private void StartLocalMedia()
		{
			LocalMediaStarted = true;

			LocalMedia = new LocalMedia();
			LocalMedia.Start(VideoContainer, (error) =>
			{
				if (error != null)
				{
					Alert(error);
				}
				else
				{
					StartConference();
				}
			});
		}

		private void StopLocalMedia()
		{
			LocalMedia.Stop((error) =>
			{
				if (error != null)
				{
					Alert(error);
				}
			});
			LocalMedia = null;
		}

		private void StartConference()
		{
			ConferenceStarted = true;

			// Create a WebRTC audio stream description (requires a
			// reference to the local audio feed).
			AudioStream = new AudioStream(LocalMedia.LocalMediaStream);

			// Create a WebRTC video stream description (requires a
			// reference to the local video feed). Whenever a P2P link
			// initializes using this description, position and display
			// the remote video control on-screen by passing it to the
			// layout manager created above. Whenever a P2P link goes
			// down, remove it.
			VideoStream = new VideoStream(LocalMedia.LocalMediaStream);
			VideoStream.OnLinkInit += AddRemoteVideoControl;
			VideoStream.OnLinkDown += RemoveRemoteVideoControl;

			// Create a conference using our stream descriptions.
			Conference = new FM.IceLink.Conference(IceLinkServerAddress, new Stream[] { AudioStream, VideoStream });

			// Use our pre-generated DTLS certificate.
			Conference.DtlsCertificate = Certificate;

			// Supply TURN relay credentials in case we are behind a
			// highly restrictive firewall. These credentials will be
			// verified by the TURN server.
			Conference.RelayUsername = "test";
			Conference.RelayPassword = "pa55w0rd!";

			// Add a few event handlers to the conference so we can see
			// when a new P2P link is created or changes state.
			Conference.OnLinkInit += LogLinkInit;
			Conference.OnLinkUp += LogLinkUp;
			Conference.OnLinkDown += LogLinkDown;

			#if __ANDROID__
			// Start echo canceller.
			OpusEchoCanceller = new OpusEchoCanceller(OpusClockRate, OpusChannels);
			OpusEchoCanceller.Start();
			#endif

			Signalling.Attach(Conference, SessionId, (error) =>
			{
				if (error != null)
				{
					Alert(error);
				}
			});
		}

		private void AddRemoteVideoControl(StreamLinkInitArgs e)
		{
			try
			{
				var remoteVideoControl = e.Link.GetRemoteVideoControl();
				LocalMedia.LayoutManager.AddRemoteVideoControl(e.PeerId, new FormsVideoControl(remoteVideoControl));
			}
			catch (Exception ex)
			{
				FM.Log.Error("Could not add remote video control.", ex);
			}
		}

		private void RemoveRemoteVideoControl(StreamLinkDownArgs e)
		{
			try
			{
				if (LocalMedia != null && LocalMedia.LayoutManager != null)
				{
					LocalMedia.LayoutManager.RemoveRemoteVideoControl(e.PeerId);
				}
			}
			catch (Exception ex)
			{
				FM.Log.Error("Could not remove remote video control.", ex);
			}
		}

		private void LogLinkInit(LinkInitArgs e)
		{
			FM.Log.Info("Link to peer initializing...");
		}

		private void LogLinkUp(LinkUpArgs e)
		{
			FM.Log.Info("Link to peer is UP.");
		}

		private void LogLinkDown(LinkDownArgs e)
		{
			FM.Log.Info(string.Format("Link to peer is DOWN. {0}", e.Exception.Message));
		}

		private void StopConference()
		{
			// Detach signalling from the conference.
			Signalling.Detach((error) =>
			{
				if (error != null)
				{
					Alert(error);
				}
			});

			#if __ANDROID__
			// Stop echo canceller.
			OpusEchoCanceller.Stop();
			OpusEchoCanceller = null;
			#endif

			Conference.OnLinkInit -= LogLinkInit;
			Conference.OnLinkUp -= LogLinkUp;
			Conference.OnLinkDown -= LogLinkDown;
			Conference = null;

			VideoStream.OnLinkInit -= AddRemoteVideoControl;
			VideoStream.OnLinkDown -= RemoveRemoteVideoControl;
			VideoStream = null;

			AudioStream = null;
		}

		private void Alert(string format, params object[] args)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				MainPage.DisplayAlert("Alert", string.Format(format, args), "OK");
			});
		}

		private static void ShowInternetMessage()
		{
			Device.BeginInvokeOnMainThread(async () =>
			{
				if (!IsInternetAvaliable)
					if (!isInternetMessageShowing)
					{
						isInternetMessageShowing = true;
						await Current.MainPage.DisplayAlert("Internet connection", "Internet connection is lost. Please check it and restart the Application", "Ok");
						isInternetMessageShowing = false;
					}
			});
		}
	}
}

