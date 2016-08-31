using System;
using System.Diagnostics;
using Android.App;
using Android.Runtime;

namespace Xamarin.Forms.Conference.WebRTC.Droid
{
	[Application(Theme = "@android:style/Theme.Material.Light")]
	public class DroidApplication : global::Android.App.Application
	{
		public DroidApplication(IntPtr handle, JniHandleOwnership transfer)
			: base(handle, transfer)
		{
			//App.Version = AppVersionNumber(this);
		}

		public string AppVersionNumber(global::Android.Content.Context context)
		{
			var version = "";
			try
			{
				var pkgInfo = context.PackageManager.GetPackageInfo(context.PackageName, (global::Android.Content.PM.PackageInfoFlags)0);
				version = pkgInfo.VersionName;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}

			return version;
		}
	}
}

