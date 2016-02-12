using MugenMvvmToolkit.ViewModels;
using MugenMvvmToolkit.DataConstants;

namespace Qmunicate.Xamarin
{
	public class SplashViewModel : ViewModelBase
	{
		public SplashViewModel ()
		{
		}

		protected override async void OnInitialized ()
		{
			var login = "marina@dmail.com";
			var password = "marina@dmail.com";
			//var userId = await App.QbProvider.LoginWithEmailAsync (login, password);
			var userId = 1;
			if (userId > 0) {
				using (var chatsViewModel = GetViewModel<ChatsViewModel> ()) {
					chatsViewModel.ShowAsync (NavigationConstants.ClearBackStack.ToValue(true));
					//chatsViewModel.ShowAsync();
				}
			} else {
				using (var loginViewModel = GetViewModel<LoginViewModel> ()) {
				 loginViewModel.ShowAsync (NavigationConstants.ClearBackStack.ToValue(true));
				}
			}
		}
	}
}

