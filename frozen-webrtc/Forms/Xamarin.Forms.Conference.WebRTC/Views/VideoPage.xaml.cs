using System;
using System.Collections.Generic;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Xamarin.Forms;

namespace Xamarin.Forms.Conference.WebRTC
{
	public partial class VideoPage : ContentPage
	{
		List<User> users;

		public VideoPage(List<User> users)
		{
			InitializeComponent();

			this.users = users;
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			var vm = new VideoViewModel(users);
			this.BindingContext = vm;
			vm.OnAppearing();
		}
	}
}

