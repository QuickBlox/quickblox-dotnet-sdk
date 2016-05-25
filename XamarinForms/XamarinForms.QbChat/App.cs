using System;
using XamarinForms.QbChat.Pages;
using Xamarin.Forms;
using QbChat.Pcl;

namespace XamarinForms.QbChat
{
    public class App : Application
    {
        public static QbProvider QbProvider { get; set; }
        public static INavigation Navigation { get; set; }
        public static Action<string> LogConsole;

		public static string UserName {
			get;
			set;
		}

		public static int UserId {
			get;
			set;
		}

		public static string UserLogin {
			get;
			set;
		}

		public static string UserPassword {
			get;
			set;
		}
        
		public static string Version {
			get;
			set;
		}

        public App()
        {
            QbProvider = new QbProvider();
			SetLoginPage();
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnSleep()
        {
            base.OnSleep();
        }

        protected override void OnResume()
        {
            base.OnResume();
        }

		public static void SetLoginPage()
		{
			((App)App.Current).MainPage = new NavigationPage(new DefaultLoginsPage());
		}

		public static void SetMainPage()
        {
			var page = new NavigationPage(new ChatsPage());
			Navigation = page.Navigation;
			((App)App.Current).MainPage = page;
        }
    }
}
