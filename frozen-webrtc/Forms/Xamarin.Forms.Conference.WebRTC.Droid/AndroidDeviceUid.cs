
using Xamarin.Forms.Conference.WebRTC.Droid;
using Xamarin.PCL.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(AndroidDeviceUid))]
namespace Xamarin.Forms.Conference.WebRTC.Droid
{
	public class AndroidDeviceUid : IDeviceIdentifier
	{
		public string GetIdentifier()
		{
			return global::Android.Provider.Settings.Secure.GetString(Forms.Context.ContentResolver, global::Android.Provider.Settings.Secure.AndroidId);
		}
	}
}

