using Foundation;
using UIKit;
using MugenMvvmToolkit.iOS.Infrastructure;
using MugenMvvmToolkit;
using MugenMvvmToolkit.iOS;

namespace Qmunicate.Xamarin.iOS
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register ("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		private const string RootViewControllerKey = "RootViewControllerKey";
		private UIWindow _window;
		private TouchBootstrapperBase _bootstrapper;


		public override bool WillFinishLaunching (UIApplication application, NSDictionary launchOptions)
		{
			_window = new UIWindow (UIScreen.MainScreen.Bounds);
			_bootstrapper = new Bootstrapper<App> (_window, new AutofacContainer ());
			_bootstrapper.Initialize ();
			return true;
		}


		public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
		{
			if (_window.RootViewController == null)
				_bootstrapper.Start ();

			_window.MakeKeyAndVisible ();
			return true;
		}

		public override void WillEncodeRestorableState (UIApplication application, NSCoder coder)
		{
			if (_window.RootViewController != null)
				coder.Encode (_window.RootViewController, RootViewControllerKey);
		}

		public override void DidDecodeRestorableState (UIApplication application, NSCoder coder)
		{
			var controller = (UIViewController)coder.DecodeObject (RootViewControllerKey);
			if (controller != null)
				_window.RootViewController = controller;
		}

		public override UIViewController GetViewController (UIApplication application, string[] restorationIdentifierComponents, NSCoder coder)
		{
			return PlatformExtensions.ApplicationStateManager.GetViewController (restorationIdentifierComponents, coder);
		}

		public override bool ShouldRestoreApplicationState (UIApplication application, NSCoder coder)
		{
			return true;
		}

		public override bool ShouldSaveApplicationState (UIApplication application, NSCoder coder)
		{
			return true;
		}
	}
}


