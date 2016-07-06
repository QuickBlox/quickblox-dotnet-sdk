using QbChat.Pcl;
using QbChat.Pcl.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace XamarinForms.QbChat.ViewModels
{
    public class DefaultLoginsViewModels : ViewModel
    {
        private string login;
        private string password;
        private List<DefaultUser> users;

        public DefaultLoginsViewModels()
        {
            TappedCommand = new Command<DefaultUser>(this.TappedCommandExecute);
        }

        public string Login
        {
            get { return login; }
            set
            {
                login = value;
                this.RaisePropertyChanged();
            }
        }

        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                this.RaisePropertyChanged();
            }
        }

        public List<DefaultUser> Users
        {
            get { return users; }
            set
            {
                users = value;
                this.RaisePropertyChanged();
            }
        }

        public ICommand TappedCommand { get; set; }

        public override void OnAppearing()
		{
			base.OnAppearing ();
			this.IsBusyIndicatorVisible = true;

			Task.Factory.StartNew (async () => {
				var list = new List<DefaultUser> ();
				var baseSesionResult = await App.QbProvider.GetBaseSession ();
				if (baseSesionResult) {
					var users = await App.QbProvider.GetUserByTag ("XamarinChat");
					foreach (var user in users) {
						list.Add (new DefaultUser () { Name = user.FullName, Login = user.Login, Password = user.Login });
					}
				}

				Device.BeginInvokeOnMainThread (() => {
					Users = list;
					this.IsBusyIndicatorVisible = false;
				});
			});
		}



        private async void TappedCommandExecute(DefaultUser user)
        {
            this.IsBusyIndicatorVisible = true;

            var loginValue = user.Login;
            var passwordValue = user.Password;
            //await Task.Factory.StartNew(async () =>
            //{
            var deviceUid = DependencyService.Get<IDeviceIdentifier>().GetIdentifier();
            var platform = Device.OS == TargetPlatform.Android ? Quickblox.Sdk.GeneralDataModel.Models.Platform.android : Quickblox.Sdk.GeneralDataModel.Models.Platform.ios;
            var userId = await App.QbProvider.LoginWithLoginValueAsync(loginValue, passwordValue, platform, deviceUid);

            Device.BeginInvokeOnMainThread(() =>
            {
                if (userId > 0)
                {
                    this.IsBusyIndicatorVisible = false;
                    App.SetMainPage();
                }
                //else
                //{
                //    App.Current.MainPage.DisplayAlert("Error", "Try to repeat login", "Ok");
                //}

                this.IsBusyIndicatorVisible = false;
            });
            //});
        }


        //InitDefault ();
        private void InitDefault()
        {
            var list = new List<DefaultUser>();
            list.Add(new DefaultUser() { Name = "Xamarin User 1", Login = "@xamarinuser1", Password = "@xamarinuser1" });
            list.Add(new DefaultUser() { Name = "Xamarin User 2", Login = "@xamarinuser2", Password = "@xamarinuser2" });
            list.Add(new DefaultUser() { Name = "Xamarin User 3", Login = "@xamarinuser3", Password = "@xamarinuser3" });
            list.Add(new DefaultUser() { Name = "Xamarin User 4", Login = "@xamarinuser4", Password = "@xamarinuser4" });
            list.Add(new DefaultUser() { Name = "Xamarin User 5", Login = "@xamarinuser5", Password = "@xamarinuser5" });

            this.Users = list;
        }
    }
}
