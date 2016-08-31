using System;
using System.Collections.Generic;

namespace Xamarin.PCL
{
	public interface ILoginStorage
	{
		void Init(object context);

		void Save(string key, string value);

		KeyValuePair<string, string>? Load();

		void Clear();
	}
}

