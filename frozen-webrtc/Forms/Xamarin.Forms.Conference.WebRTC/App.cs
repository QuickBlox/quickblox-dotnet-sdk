using System;
using System.Collections.Generic;
using FM;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Xamarin.PCL;

namespace Xamarin.Forms.Conference.WebRTC
{
	public enum VideoChatState
	{
		None,
		WaitOffer,
		SendOffer,
		WaitAnswer,
		Complete
	}

	public class App : Application
	{
		public static QbProvider QbProvider = new QbProvider(ShowInternetMessage);

		public static CallHelperProvider CallHelperProvider { get; private set; }

		public static bool IsInternetAvaliable { get; set; }
		private static bool isInternetMessageShowing;

		public static int UserId { get; set; }

		public static INavigation Navigation { get; set; }
		public static List<User> UsersInRoom { get; set; }
		public static User MainUser { get; set; }

		public String SessionId { get; set; }

		internal static string Version;

		public App()
		{
			App.SetLogin();

#if __ANDROID__
			FM.Log.Provider = new AndroidLogProvider(LogLevel.Debug);
#elif __ISO__
			FM.Log.Provider = new NSLogProvider(LogLevel.Debug);
#elif WINDOWS_APP
            FM.Log.Provider = new DebugLogProvider(LogLevel.Debug);
#endif

        }

		public void InitChatClient()
		{
			// TODO: delete messageprovider
			MessageProvider.Instance.Init(App.QbProvider.GetClient());

			CallHelperProvider = new CallHelperProvider(App.QbProvider.GetClient());
		}

		protected override void OnSleep()
		{
			//if (LocalMedia != null && LocalMedia.LocalMediaStream != null)
			//{
			//	LocalMedia.LocalMediaStream.PauseVideo();
			//}
		}

		protected override void OnResume()
		{
			//if (LocalMedia != null && LocalMedia.LocalMediaStream != null)
			//{
			//	LocalMedia.LocalMediaStream.ResumeVideo();
			//}
		}

		public void GoToVideoPage()
		{
			
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

		internal static void SetLogin()
		{
			var page = new NavigationPage(new LoginPage());
			Navigation = page.Navigation;
			Current.MainPage = page;
		}

		internal static void SetUsersPage()
		{
			var page = new NavigationPage(new UsersInGroup());
			Navigation = page.Navigation;
			Current.MainPage = page;
		}

		public static void SetVideoCall(bool isCallInitiator, User mainUser, List<User> opponents, VideoChatMessage videoMessage, bool isVideoCall = true)
		{
			var page = new VideoPage(isCallInitiator, mainUser, opponents, videoMessage, isVideoCall);
			Current.MainPage = page;
		}
	}
}

