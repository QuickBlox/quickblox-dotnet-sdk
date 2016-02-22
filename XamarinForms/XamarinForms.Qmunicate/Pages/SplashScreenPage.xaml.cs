using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XamarinForms.QbChat.Repository;
using Xamarin.Forms;

namespace XamarinForms.QbChat.Pages
{
    public partial class SplashScreenPage : ContentPage
    {
        public SplashScreenPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            this.IsBusy = true;
            //var login = "marina@dmail.com";
            //var password = "marina@dmail.com";

            var userSetting = Database.Instance().GetUserSettingTable();
            if (userSetting != null)
            {
                var userId = await App.QbProvider.LoginWithEmailAsync(userSetting.Login, userSetting.Password);
                if (userId > 0)
                {
                    this.IsBusy = false;
                    App.SetMainPage();
                }
                else
                {
                    this.IsBusy = false;
                    App.SetLoginPage();
                }
            }
            else
            {
                this.IsBusy = false;
                App.SetLoginPage();
            }

            this.IsBusy = false;
        }
    }
}
