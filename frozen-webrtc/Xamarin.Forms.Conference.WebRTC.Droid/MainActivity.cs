using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using FM;
using FM.IceLink;
using FM.IceLink.WebRTC;

namespace Xamarin.Forms.Conference.WebRTC.Droid
{
	[Activity(Label = "Xamarin.Forms.Conference.WebRTC.Droid", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
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

			LoadApplication(new App());
		}
	}
}

