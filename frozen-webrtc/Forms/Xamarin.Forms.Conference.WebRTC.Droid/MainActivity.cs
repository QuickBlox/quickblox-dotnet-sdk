
using Android.App;
using Android.OS;
using FM.IceLink.WebRTC;
using Xamarin.PCL;

namespace Xamarin.Forms.Conference.WebRTC.Droid
{
	[Activity(Theme = "@android:style/Theme.Holo.Light", Label = "", Icon = "@android:color/transparent")]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
	{
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// It's annoying, but we need to make sure all the native
            // libraries load up using JavaSystem on the main thread.
            if (!Build.CpuAbi.ToLower().Contains("x86") && !Build.CpuAbi.ToLower().Contains("arm64"))
            {
                Java.Lang.JavaSystem.LoadLibrary("audioprocessing");
                Java.Lang.JavaSystem.LoadLibrary("audioprocessingJNI");
            }
			Java.Lang.JavaSystem.LoadLibrary("opus");
			Java.Lang.JavaSystem.LoadLibrary("opusJNI");
			Java.Lang.JavaSystem.LoadLibrary("vpx");
			Java.Lang.JavaSystem.LoadLibrary("vpxJNI");

			// Android requires a context for a number of operations,
			// so we need to provide one to IceLink for the default
			// audio/video providers so they can interact with the
			// application context as needed.
			DefaultProviders.AndroidContext = this;

			global::Xamarin.Forms.Forms.Init(this, bundle);
			Acr.UserDialogs.UserDialogs.Init(this);
			Quickblox.Sdk.Platform.QuickbloxPlatform.Init();
			DependencyService.Get<ILoginStorage>().Init(Application.ApplicationContext);

			LoadApplication(new App());
		}
	}
}

