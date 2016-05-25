using System;
using Android.App;
using Android.Content;
using Xamarin.Forms;
using Android.Runtime;

namespace XamarinForms.QbChat.Android
{
	[Application(Theme="@android:style/Theme.Material.Light")]
	public class QmunicateApplication : global::Android.App.Application
	{ 
		public QmunicateApplication (IntPtr handle, JniHandleOwnership transfer)
			: base (handle, transfer)
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

