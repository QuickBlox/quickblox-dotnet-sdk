using System;
using Android.App;
using Android.Content;
using XamarinForms.Qmunicate;
using XamarinForms.Qmunicate.Repository;
using Xamarin.Forms;
using XamainForms.Qmunicate;

namespace XamarinForms.Qmunicate.Android
{
	[Application()]
	public class QmunicateApplication : global::Android.App.Application
	{
		public QmunicateApplication ()
		{
			App.Version = AppVersionNumber (this);

			Database.Instance ().Init (DependencyService.Get<ISqlite> ().GetConnection ());
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

