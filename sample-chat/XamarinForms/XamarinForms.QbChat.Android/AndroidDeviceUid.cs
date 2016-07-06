using System;
using XamarinForms.QbChat.Android;
using Android.Provider;
using Xamarin.Forms;
using QbChat.Pcl.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(AndroidDeviceUid))]
namespace XamarinForms.QbChat.Android
{
	public class AndroidDeviceUid : IDeviceIdentifier
	{
		#region IDeviceIdentifier implementation

		public string GetIdentifier ()
		{
			return Settings.Secure.GetString (Forms.Context.ContentResolver, Settings.Secure.AndroidId);
		}

		#endregion
	}
}

