using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XamarinForms.Qmunicate.Repository;
using Xamarin.Forms;

namespace XamarinForms.Qmunicate.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            loginButton.Clicked += OnClicked;
        }

        private async void OnClicked(object sender, EventArgs e)
        {
            this.IsBusy = true;
            var loginValue = login.Text.Trim();
            var passwordValue = password.Text.Trim();
            if (!string.IsNullOrEmpty(loginValue) && !string.IsNullOrEmpty(passwordValue))
            {
                var userId = await App.QbProvider.LoginWithEmailAsync(loginValue, passwordValue);
                if (userId > 0)
                {
                    this.IsBusy = false;

					if (savePasswordSwitch.IsToggled)
						Database.Instance ().SaveUserSetting (new UserSettingTable () { Login = loginValue, Password = passwordValue });
                    App.SetMainPage();
                }
                else
                {
                    await DisplayAlert("Error", "The user name or password provided is incorrect.", "Ok");
                }
            }

            this.IsBusy = false;
        }
    }
}
