using System;
using XamarinForms.Qmunicate.Android;
using Android.Provider;
using Xamarin.Forms;
using XamarinForms.Qmunicate.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(AndroidDeviceUid))]
namespace XamarinForms.Qmunicate.Android
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

