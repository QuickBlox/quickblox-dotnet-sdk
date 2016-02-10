using System;

using UIKit;
using MugenMvvmToolkit.iOS.Views;
using MugenMvvmToolkit.Binding.Builders;

namespace Qmunicate.Xamarin.iOS
{
	public partial class SplashScreenViewController : MvvmViewController
	{
		public SplashScreenViewController () : base ("SplashScreenViewController", null)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			using (var set = new BindingSet<SplashScreenViewModel> ()) {
				
			}
		}

		public override void ViewDidUnload ()
		{
			base.ViewDidUnload ();
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}


