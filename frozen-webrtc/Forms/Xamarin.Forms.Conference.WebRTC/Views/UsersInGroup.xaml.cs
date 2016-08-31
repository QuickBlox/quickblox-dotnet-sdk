using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace Xamarin.Forms.Conference.WebRTC
{
	public partial class UsersInGroup : ContentPage
	{
		bool isLoaded;

		public UsersInGroup()
		{
			InitializeComponent();

			listView.ItemSelected += (sender, e) => { listView.SelectedItem = null; };
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();


			if (this.isLoaded)
				return;

			this.isLoaded = true;
			var vm = new UsersInGroupViewModel();
			this.BindingContext = vm;
			vm.OnAppearing();
		}
	}
}

