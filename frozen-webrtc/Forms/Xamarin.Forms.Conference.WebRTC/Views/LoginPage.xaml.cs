using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace Xamarin.Forms.Conference.WebRTC
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

			var vm = new LoginViewModel();
			this.BindingContext = vm;
			vm.OnAppearing();
		}
	}
}

