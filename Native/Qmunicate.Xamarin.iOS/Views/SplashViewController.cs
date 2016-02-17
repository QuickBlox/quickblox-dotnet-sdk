using System;
using MugenMvvmToolkit.iOS.Views;
using UIKit;
using Foundation;
using CoreGraphics;

namespace Qmunicate.Xamarin.iOS
{
	[Register("SplashViewController")]
	public class SplashViewController : MvvmViewController
	{
		UIActivityIndicatorView busyIndicator;
		UILabel label;

		public SplashViewController ()
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			var bounds = View.Bounds = UIScreen.MainScreen.Bounds;
			View.BackgroundColor = UIColor.White;

			label = new UILabel(new CGRect(bounds.Width / 2 - 30, bounds.Height / 2 - 100, 100, 100));
			label.Text = "Loading...";
			View.AddSubview (label);

			busyIndicator = new UIActivityIndicatorView (new CGRect(bounds.Width / 2 - 5, bounds.Height / 2 - 5, 10, 10));
			busyIndicator.Color = UIColor.Black;
			View.AddSubview (busyIndicator);

			busyIndicator.StartAnimating ();
		}

		public override void ViewDidUnload ()
		{
			busyIndicator.StopAnimating ();
			base.ViewDidUnload ();
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}

