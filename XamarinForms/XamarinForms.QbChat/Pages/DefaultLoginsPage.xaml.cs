using System;
using System.Collections.Generic;

using Xamarin.Forms;
using System.Threading.Tasks;

namespace XamarinForms.QbChat
{
	public partial class DefaultLoginsPage : ContentPage
	{
		public DefaultLoginsPage ()
		{
			InitializeComponent ();
		}

		protected override async void OnAppearing ()
		{
			base.OnAppearing ();

			this.busyIndicator.IsVisible = true;

			var template = new DataTemplate (typeof(TextCell));
			template.SetBinding (ImageCell.TextProperty, "Name");
			//template.SetBinding (ImageCell.DetailProperty, "LastMessage");
			listView.ItemTemplate = template;
			listView.ItemTapped += (object sender, ItemTappedEventArgs e) => {
				Login(e.Item as DefaultUser);
			};

			Task.Factory.StartNew (async () => {
				var list = new List<DefaultUser> ();
				var baseSesionResult = await App.QbProvider.GetBaseSession();
				if (baseSesionResult){
					var users = await App.QbProvider.GetUserByTag("XamarinChat");
					foreach (var user in users) {
						list.Add (new DefaultUser () { Name = user.FullName, Login = user.Login, Password = user.Login });
					}
				}

				Device.BeginInvokeOnMainThread(() => {
					listView.ItemsSource = list;
					this.busyIndicator.IsVisible = false;
				});
			});

			//InitDefault ();
		}

		public void InitDefault(){
			var list = new List<DefaultUser> ();
			list.Add (new DefaultUser () { Name = "Xamarin User 1", Login = "@xamarinuser1", Password = "@xamarinuser1" });
			list.Add (new DefaultUser () { Name = "Xamarin User 2", Login = "@xamarinuser2", Password = "@xamarinuser2" });
			list.Add (new DefaultUser () { Name = "Xamarin User 3", Login = "@xamarinuser3", Password = "@xamarinuser3" });
			list.Add (new DefaultUser () { Name = "Xamarin User 4", Login = "@xamarinuser4", Password = "@xamarinuser4" });
			list.Add (new DefaultUser () { Name = "Xamarin User 5", Login = "@xamarinuser5", Password = "@xamarinuser5" });

			listView.ItemsSource = list;
		}

		private async void Login(DefaultUser user){
			busyIndicator.IsVisible = true;
			var loginValue = user.Login;
			var passwordValue = user.Password;
			Task.Factory.StartNew (async () => {
				var userId = await App.QbProvider.LoginWithLoginValueAsync (loginValue, passwordValue);

				Device.BeginInvokeOnMainThread(() =>
					{if (userId > 0) {
					busyIndicator.IsVisible = false;
					App.SetMainPage ();
				} else {
				    DisplayAlert ("Error", "Try to repeat login", "Ok");
				}

					busyIndicator.IsVisible = false;
				});
			});
		}
	}
}

