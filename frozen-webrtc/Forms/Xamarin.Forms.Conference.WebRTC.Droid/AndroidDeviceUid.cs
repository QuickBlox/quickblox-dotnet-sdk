using Android.Provider;
using QbChat.Pcl.Interfaces;
using Xamarin.Forms.Conference.WebRTC.Droid;

[assembly: Xamarin.Forms.Dependency(typeof(AndroidDeviceUid))]
namespace Xamarin.Forms.Conference.WebRTC.Droid
{
	public class AndroidDeviceUid : IDeviceIdentifier
	{
		public string GetIdentifier()
		{
			return Settings.Secure.GetString(Forms.Context.ContentResolver, Settings.Secure.AndroidId);
		}
	}
}

