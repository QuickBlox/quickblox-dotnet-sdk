using System;
using MugenMvvmToolkit;
using Autofac;

namespace Qmunicate.Xamarin
{
	public class App : MvvmApplication
	{
		public override Type GetStartViewModelType ()
		{
			return typeof(SplashScreenViewModel);
		}
	}
}

