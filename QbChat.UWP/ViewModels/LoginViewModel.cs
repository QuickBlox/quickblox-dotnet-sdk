using QbChat.UWP.Views;
using Quickblox.Sdk.Modules.UsersModule.Requests;

namespace QbChat.UWP.ViewModels
{
    public class LoginViewModel : ViewModel
    {
        private string login;
        private string group;

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(this.LoginCommandExecute, () => !string.IsNullOrEmpty(Login) &&  !string.IsNullOrEmpty(Group));
        }


        public RelayCommand LoginCommand { get; set; }

        public string Login
        {
            get { return login; }
            set { login = value;
                this.RaisePropertyChanged();
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        public string Group
        {
            get { return group; }
            set { group = value;
                RaisePropertyChanged();
            }
        }

        private async void LoginCommandExecute()
        {
            var login = new DeviceUid_Uwp().GetIdentifier(); // Calculate login
            var password = "";

            var loginRepsonse = await App.QbProvider.LoginWithLoginValueAsync(login, password, Quickblox.Sdk.GeneralDataModel.Models.Platform.windows_phone, login);
            if (loginRepsonse > 0)
            {
                var updateUserRequest = new UserRequest();
                updateUserRequest.TagList = Group;

                var updateUserResponse = await App.QbProvider.UpdateUserDataAsync(App.QbProvider.UserId, updateUserRequest);
                if (updateUserResponse != null)
                {
                    App.NavigationFrame.Navigate(typeof(ChatsPage));
                }
            }
        }
    }
}
