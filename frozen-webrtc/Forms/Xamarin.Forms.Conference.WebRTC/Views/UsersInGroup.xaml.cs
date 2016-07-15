using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace Xamarin.Forms.Conference.WebRTC
{
	public partial class UsersInGroup : ContentPage
	{
		public UsersInGroup()
		{
			InitializeComponent();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			var vm = new UsersInGroupViewModel();
			this.BindingContext = vm;
			vm.OnAppearing();
		}
	}
}

