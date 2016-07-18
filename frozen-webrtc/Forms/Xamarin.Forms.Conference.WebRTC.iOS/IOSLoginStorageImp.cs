using System;
using System.Collections.Generic;
using Foundation;
using Xamarin.PCL;
using Security;
using Xamarin.Forms.Conference.WebRTC.iOS;

[assembly: Xamarin.Forms.Dependency(typeof(IOSLoginStorageImp))]

namespace Xamarin.Forms.Conference.WebRTC.iOS
{
	public class IOSLoginStorageImp : ILoginStorage
	{
		public IOSLoginStorageImp()
		{
		}

		public void Clear()
		{
			SecRecord query2 = new SecRecord(SecKind.GenericPassword);
			query2.Account = "login";
			query2.Label = "login";
			query2.Service = "login";
			var err2 = SecKeyChain.Remove(query2);

			SecRecord query3 = new SecRecord(SecKind.GenericPassword);
			query3.Account = "password";
			query3.Label = "password";
			query3.Service = "password";
			var err3 = SecKeyChain.Remove(query2);
		}

		public void Init(object context)
		{
			throw new NotImplementedException();
		}

		public KeyValuePair<string, string>? Load()
		{
			SecStatusCode res;
			var rec = new SecRecord(SecKind.GenericPassword)
			{
				Account = "login",
				Label = "login",
				Service = "login",
			};
			var match = SecKeyChain.QueryAsRecord(rec, out res);

			SecStatusCode res2;
			var rec2 = new SecRecord(SecKind.GenericPassword)
			{
				Account = "password",
				Label = "password",
				Service = "password",
			};

			var match2 = SecKeyChain.QueryAsRecord(rec2, out res2);


			if (match != null && match2 != null)
			{
				return new KeyValuePair<string, string>(match.ValueData.ToString(), match2.ValueData.ToString());
			}

			return null;
		}

		public void Save(string key, string value)
		{
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
			{
				var s = new SecRecord(SecKind.GenericPassword)
				{
					ValueData = NSData.FromString(key),
					Account = "login",
					Label = "login",
					Service = "login"
					                
				};
				var err = SecKeyChain.Add(s);

				var s2 = new SecRecord(SecKind.GenericPassword)
				{
					ValueData = NSData.FromString(value),
					Account = "password",
					Label = "password",
					Service = "password"
				};
				var err2 = SecKeyChain.Add(s2);
			}
		}
	}
}

