using System;
using Android.Provider;
using Qmunicate.Xamarin;
using Android.Content;

namespace Qmunicate.Xamarin.Android
{
	public class AndroidDeviceUid : IDeviceUid
	{
		ContentResolver resolver;
		public void Initialize(object parameters){
			resolver = parameters as ContentResolver;
		}

		#region IDeviceUid implementation

		public string GetDeviceIdentifier ()
		{
			return Settings.Secure.GetString (resolver, Settings.Secure.AndroidId);
		}

		#endregion

	}
}

