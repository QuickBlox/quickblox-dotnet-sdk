using System;

using UIKit;
using MugenMvvmToolkit.iOS.Views;
using MugenMvvmToolkit.Binding.Builders;
using CoreGraphics;

namespace Qmunicate.Xamarin.iOS
{
	public partial class SplashScreenViewController : MvvmViewController
	{
//		UIActivityIndicatorView busyIndicator;
//		UILabel label;
		public SplashScreenViewController () : base ("SplashScreenViewController", null)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			View.Bounds = UIScreen.MainScreen.Bounds;

//			busyIndicator = new UIActivityIndicatorView (new CGRect(50, 50, 10, 10));
//			View.AddSubview (busyIndicator);
			busyIndicator.StartAnimating ();


//			label = new UILabel(new CGRect(bounds.Width / 2, bounds.Height / 2, 100, 100));
//			label.Text = "svabra";
//			View.AddSubview (label);
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


