using System;
using MugenMvvmToolkit.ViewModels;
using MugenMvvmToolkit.Interfaces.Presenters;
using MugenMvvmToolkit.Infrastructure.Presenters;

namespace Qmunicate.Xamarin
{
	public class MainViewModel : MultiViewModel
	{
		public MainViewModel (IViewModelPresenter viewModelPresenter)
		{
			viewModelPresenter.DynamicPresenters.Add (new DynamicMultiViewModelPresenter (this));
		}

		protected override async void OnInitialized ()
		{
//			using (var splashScreenViewModel = GetViewModel<SplashScreenViewModel> ()) {
//				var asyncOperation = splashScreenViewModel.ShowAsync ().NavigationCompletedTask;
//				await asyncOperation;
//
//				if (splashScreenViewModel.IsUserLogged) {
//					using (var chatsViewModel = GetViewModel<ChatsViewModel> ()) {
//						chatsViewModel.ShowAsync ();
//					}
//				} else {
//					using (var loginViewModel = GetViewModel<LoginViewModel> ()) {
//						loginViewModel.ShowAsync ();
//					}
//				}
//			}
		}
	}
}

