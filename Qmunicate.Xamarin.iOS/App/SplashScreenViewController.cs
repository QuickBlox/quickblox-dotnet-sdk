using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;
using System.Threading.Tasks;

namespace Qmunicate.Xamarin.iOS
{
	partial class SplashScreenViewController : UIViewController
	{
		public SplashScreenViewController (IntPtr handle) : base (handle)
		{
		}
			
		public override async void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			busyIndicator.StartAnimating ();

			await Task.Delay (3000);
			var window = UIApplication.SharedApplication.Windows[0];

			UIStoryboard storyboard = UIStoryboard.FromName ("Main", null);
			var loginController = storyboard.InstantiateViewController ("LoginViewController") as LoginViewController;
			window.RootViewController = loginController;
		}

		public override void ViewDidUnload ()
		{
			busyIndicator.StopAnimating ();
			base.ViewDidUnload ();
		}
	}
}
