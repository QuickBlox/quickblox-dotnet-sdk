using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.PCL;
using Xamarin.PCL.Interfaces;
using Xamarin.PCL;
using System.Diagnostics;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class LoginViewModel : ViewModel
	{
		private const string UserNameMatchPattern = "^[a-zA-Z][a-zA-Z0-9-_\\.]{1,20}";
		private const string ChatRoomNameMathcPattern = "^[a-zA-Z0-9]{3,15}";
		private string userName;
		private string chatRoomName;
		private Quickblox.Sdk.GeneralDataModel.Models.Platform platform;
		private string uid;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Xamarin.Forms.Conference.WebRTC.LoginViewModel"/> class.
		/// </summary>
		public LoginViewModel()
		{
			LoginCommand = new Command(LoginExecute, CanLoginExecute);

			platform = Quickblox.Sdk.GeneralDataModel.Models.Platform.android;
			switch (Device.OS)
			{
				case TargetPlatform.iOS:
					platform = Quickblox.Sdk.GeneralDataModel.Models.Platform.ios;
					break;
				case TargetPlatform.Windows:
				case TargetPlatform.WinPhone:
					platform = Quickblox.Sdk.GeneralDataModel.Models.Platform.windows_phone;
					break;
				default:
					break;
			}

			uid = DependencyService.Get<IDeviceIdentifier>().GetIdentifier();
		}

		/// <summary>
		/// Invokes when page was shown.
		/// </summary>
		/// <returns>The appearing.</returns>
		public override async void OnAppearing()
		{
			this.IsBusy = true;

			await App.QbProvider.GetBaseSession();
			var loginPasswordPair = DependencyService.Get<ILoginStorage>().Load();
			if (loginPasswordPair != null)
			{
				await TryStartLogin(loginPasswordPair.Value.Key, loginPasswordPair.Value.Value);
			}

			this.IsBusy = false;
		}

		/// <summary>
		/// Gets or sets the name of the user.
		/// </summary>
		/// <value>The name of the user.</value>
		public string UserName
		{
			get
			{
				return userName;
			}
			set
			{
				userName = value;
				RaisePropertyChanged();
				LoginCommand.ChangeCanExecute();
			}
		}

		/// <summary>
		/// Gets or sets the name of the chat room.
		/// </summary>
		/// <value>The name of the chat room.</value>
		public string ChatRoomName
		{
			get
			{
				return chatRoomName;
			}
			set
			{
				chatRoomName = value;
				RaisePropertyChanged();
				LoginCommand.ChangeCanExecute();
			}
		}

		/// <summary>
		/// Gets or sets the login command.
		/// </summary>
		/// <value>The login command.</value>
		public Command LoginCommand { get; set; }

		/// <summary>
		/// Check is login fields valid.
		/// </summary>
		/// <returns>The login execute.</returns>
		/// <param name="arg">Argument.</param>
		private bool CanLoginExecute(object arg)
		{
			var isUserNameValid = !string.IsNullOrEmpty(this.userName) && Regex.IsMatch(this.userName, UserNameMatchPattern);
			var isChatRoomNameValid = !string.IsNullOrEmpty(this.chatRoomName) && Regex.IsMatch(this.chatRoomName, ChatRoomNameMathcPattern);
			return isUserNameValid && isChatRoomNameValid;
		}

		/// <summary>
		/// Logins the execute.
		/// </summary>
		/// <returns>The execute.</returns>
		/// <param name="obj">Object.</param>
		private async void LoginExecute(object obj)
		{
			this.IsBusy = true;

			var user = await App.QbProvider.SignUpUserWithLoginAsync(uid, ApplicationKeys.PasswordToLogin, UserName, ChatRoomName);
			if (user != null)
			{
				await TryStartLogin(user.Login, ApplicationKeys.PasswordToLogin);
			}
			else {
				// TODO: Add notification message
			}

			this.IsBusy = false;
		}

		/// <summary>
		/// Try login user when login page displayed
		/// </summary>
		/// <returns>The start login.</returns>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		private async Task TryStartLogin(string login, string password)
		{
			try
			{
				var userId = await App.QbProvider.LoginWithLoginValueAsync(login, password, platform, uid);
				if (userId > 0)
				{
					DependencyService.Get<ILoginStorage>().Save(login, password); 
					App.UserId = userId;
					MessageProvider.Instance.Init(App.QbProvider.GetClient());
					await MessageProvider.Instance.ConnetToXmpp(userId, password);

//#if __ANDROID__ || __IOS__
					//App.Navigation.InsertPageBefore(new UsersInGroup(), (App.Current.MainPage as NavigationPage).CurrentPage);
					//await App.Navigation.PopAsync();

					App.SetUsersPage();
//#endif
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("TryStartLogin: " + ex.ToString());
			}
		}
	}
}

