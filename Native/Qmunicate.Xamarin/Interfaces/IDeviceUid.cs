using System;

namespace Qmunicate.Xamarin
{
	public interface IDeviceUid
	{
		string GetDeviceIdentifier();

		void Initialize (object parameters);
	}
}

