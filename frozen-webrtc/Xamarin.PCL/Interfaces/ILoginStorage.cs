using System;
using System.Collections.Generic;

namespace Xamarin.PCL
{
	public interface ILoginStorage
	{
		void Init(object context);

		void Save(string login, string password);

		KeyValuePair<string, string>? Load();
	}
}

