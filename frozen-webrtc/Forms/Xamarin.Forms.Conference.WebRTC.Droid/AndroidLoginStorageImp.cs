using System;
using System.Collections.Generic;
using Android.Content;
using Xamarin.Forms.Conference.WebRTC.Droid;
using Xamarin.PCL;

[assembly: Xamarin.Forms.Dependency(typeof(AndroidLoginStorageImp))]
namespace Xamarin.Forms.Conference.WebRTC.Droid
{
	public class AndroidLoginStorageImp : ILoginStorage
	{
		Context context;
		public AndroidLoginStorageImp()
		{
		}

		public void Init(object context)
		{
			this.context = (Context)context; 
		}

		public KeyValuePair<string, string>? Load()
		{
			if (context == null) throw new NullReferenceException(nameof(context));
			var prefs = context.GetSharedPreferences("Xamarin.WebRTC.Sample", FileCreationMode.Private);
			var login = prefs.GetString("Login", null);
			var password = prefs.GetString("Password", null);

			if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
			{
				return new KeyValuePair<string, string>(login, password);
			}

			return null;
		}

		public void Save(string login, string password)
		{
			if (context == null) throw new NullReferenceException(nameof(context));
			var prefs = this.context.GetSharedPreferences("Xamarin.WebRTC.Sample", FileCreationMode.Private);
			var prefEditor = prefs.Edit();
			prefEditor.PutString("Login", login);
			prefEditor.PutString("Password", password);
			prefEditor.Commit();
		}
	}
}

