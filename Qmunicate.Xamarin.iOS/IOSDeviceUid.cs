using System;
using Qmunicate.Xamarin;
using Security;
using Foundation;

namespace Qmunicate.Xamarin.iOS
{
	public class IOSDeviceUid : IDeviceUid
	{
		public IOSDeviceUid ()
		{
		}

		#region IDeviceUid implementation

		public void Initialize (object parameters)
		{
			throw new NotImplementedException ();
		}

		public string GetDeviceIdentifier ()
		{
			string serial = string.Empty;
			var rec = new SecRecord(SecKind.GenericPassword)
			{
				Generic = NSData.FromString("uidNumber")
			};

			SecStatusCode res;
			var match = SecKeyChain.QueryAsRecord(rec, out res);
			if (res == SecStatusCode.Success)
			{
				serial = match.ValueData.ToString();
			}
			else
			{
				var uidNumberRecord = new SecRecord(SecKind.GenericPassword)
				{
					Label = "uid",
					ValueData = NSData.FromString(Guid.NewGuid().ToString()),
					Generic = NSData.FromString("uidNumber")
				};

				var err = SecKeyChain.Add(uidNumberRecord);
				serial = uidNumberRecord.ValueData.ToString();
			}

			return serial;

		}

		#endregion
	}
}

