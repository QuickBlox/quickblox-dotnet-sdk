using System;
using System.Linq;
using XamarinForms.Qmunicate.Pages;
using XamarinForms.Qmunicate.Repository;
using Xamarin.Forms;

namespace XamarinForms.Qmunicate
{
    public class App : Application
    {
        public static QbProvider QbProvider { get; set; }
        public static INavigation Navigation { get; set; }
        public static Action<string> LogConsole;
        
        /// <summary>
        /// Use only when main page riched
        /// </summary>
        public static Action GotoMainPage;

        /// <summary>
        /// Use for navigation to main page after login
        /// </summary>
        public static Action SuccessfulLoginAction;

        public static Action<bool> ToggleChatDraw;

		public static string Version {
			get;
			set;
		}

        public App()
        {
            //DBTest.FillAllDBProfiles();
            QbProvider = new QbProvider();

            SuccessfulLoginAction = SetLoginPage;

            SetLoadingPage();
        }

        private void SetLoadingPage()
        {
            ((App)App.Current).MainPage = new NavigationPage(new SplashScreenPage());
        }

        protected override async void OnStart()
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
            //MainPage =new NavigationPage (new MainPage());
            ((App)App.Current).MainPage = new NavigationPage(new LoginPage());
            //Navigation = MainPage.Navigation;
        }

		public static void SetMainPage()
        {
            //((App)App.Current).MainPage = new NavigationPage(new MainPage());
			var page = new NavigationPage(new ChatsPage());
			Navigation = page.Navigation;
			((App)App.Current).MainPage = page;
        }
    }
}
