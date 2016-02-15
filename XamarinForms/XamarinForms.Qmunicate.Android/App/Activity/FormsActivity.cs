using Android.App;
using Android.Widget;
using Android.OS;
using Xamarin.Forms.Platform.Android;
using Android.Content.PM;
using Xamarin.Forms;
using XamarinForms.Qmunicate;
using XamarinForms.Qmunicate.Repository;
using XamainForms.Qmunicate;

namespace XamarinForms.Qmunicate.Android
{
	[Activity (Label = "Qmunicate", MainLauncher = true, LaunchMode=LaunchMode.SingleTop)]
	public class FormsActivity : FormsApplicationActivity
	{
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			this.RequestedOrientation = ScreenOrientation.Portrait;

			Forms.Init (this, savedInstanceState);
			Database.Instance ().Init (DependencyService.Get<ISqlite> ().GetConnection ());

			var app = new App ();
			LoadApplication (app);
		}
	}
}


