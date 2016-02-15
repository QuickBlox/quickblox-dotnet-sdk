using System;
using Android.App;
using Android.Content;
using XamarinForms.Qmunicate;

namespace XamarinForms.Qmunicate.Android
{
	[Application()]
	public class QmunicateApplication : Application
	{
		public QmunicateApplication ()
		{
			App.Version = AppVersionNumber (this);
		}

		public string AppVersionNumber(Context context){
			var version = "";
			try {
				var pkgInfo = context.PackageManager.GetPackageInfo(context.PackageName, (global::Android.Content.PM.PackageInfoFlags)0);
				version = pkgInfo.VersionName;
			} catch (Exception ex) {
				
			}

			return version;
		}
	}
}

