using System;
using MugenMvvmToolkit.ViewModels;
using MugenMvvmToolkit.Interfaces.ViewModels;

namespace Qmunicate.Xamarin
{
	public class SplashScreenViewModel : ViewModelBase, INavigableViewModel
	{
		public SplashScreenViewModel ()
		{
		}


		public void OnNavigatedTo (MugenMvvmToolkit.Interfaces.Navigation.INavigationContext context)
		{
			
		}

		public async System.Threading.Tasks.Task<bool> OnNavigatingFrom (MugenMvvmToolkit.Interfaces.Navigation.INavigationContext context)
		{
			return true;
		}

		public void OnNavigatedFrom (MugenMvvmToolkit.Interfaces.Navigation.INavigationContext context)
		{
		}
	}
}

