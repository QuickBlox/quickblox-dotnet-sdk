using System;
using MugenMvvmToolkit;

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

