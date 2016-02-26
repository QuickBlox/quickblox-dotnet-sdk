using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XamarinForms.QbChat.Repository;
using Xamarin.Forms;

namespace XamarinForms.QbChat.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
			this.login.Text = "logic5@dmail.com";
			this.password.Text = "logic5@dmail.com";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            loginButton.Clicked += OnClicked;
        }

        private async void OnClicked(object sender, EventArgs e)
        {
            var loginValue = login.Text.Trim();
            var passwordValue = password.Text.Trim();
            if (!string.IsNullOrEmpty(loginValue) && !string.IsNullOrEmpty(passwordValue))
            {
                var userId = await App.QbProvider.LoginWithEmailAsync(loginValue, passwordValue);
                if (userId > 0)
                {
					App.SetMainPage();
                }
                else
                {
                    await DisplayAlert("Error", "The user name or password provided is incorrect.", "Ok");
                }
            }
        }
    }
}
