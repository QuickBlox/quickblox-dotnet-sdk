using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Util;
using System.Threading.Tasks;

namespace Xamarin.Forms.Conference.WebRTC.Droid
{
	[Activity(Theme = "@style/MyTheme.Splash", MainLauncher = true, NoHistory = true, Label ="Xamarin WebRTC")]
	public class SplashActivity : AppCompatActivity
	{
		static readonly string TAG = "X:" + typeof(SplashActivity).Name;

		public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
		{
			base.OnCreate(savedInstanceState, persistentState);
			Log.Debug(TAG, "SplashActivity.OnCreate");
		}

		protected override void OnResume()
		{
			base.OnResume();

			Task startupWork = new Task(() => {
				Log.Debug(TAG, "Performing some startup work that takes a bit of time.");
				Task.Delay(5000);  // Simulate a bit of startup work.
				Log.Debug(TAG, "Working in the background - important stuff.");
			});

			startupWork.ContinueWith(t => {
				Log.Debug(TAG, "Work is finished - start Activity1.");
				StartActivity(new Intent(global::Android.App.Application.Context, typeof(MainActivity)));
			}, TaskScheduler.FromCurrentSynchronizationContext());

			startupWork.Start();
		}
	}
}

