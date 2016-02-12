using System;
using MugenMvvmToolkit;
using Autofac;

namespace Qmunicate.Xamarin
{
	public class App : MvvmApplication
	{
		//public static QbProvider QbProvider;

		public App()
		{
			//QbProvider = new QbProvider ();
		}

		public override Type GetStartViewModelType ()
		{
			return typeof(ChatsViewModel);
		}
	}
}

