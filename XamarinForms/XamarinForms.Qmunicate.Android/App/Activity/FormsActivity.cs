using Android.App;
using Android.Widget;
using Android.OS;
using Xamarin.Forms.Platform.Android;
using Android.Content.PM;
using Xamarin.Forms;
using XamarinForms.Qmunicate;

namespace XamarinForms.Qmunicate.Android
{
	[Activity (Label = "XamarinForms.Qmunicate", MainLauncher = true, Icon = "@mipmap/icon", LaunchMode=LaunchMode.SingleTop)]
	public class FormsActivity : FormsApplicationActivity
	{
		int count = 1;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			this.RequestedOrientation = ScreenOrientation.Portrait;

			Forms.Init (this, savedInstanceState);

			var app = new App ();
			LoadApplication (app);
		}
	}
}


