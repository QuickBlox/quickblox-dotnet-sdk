using System;
using System.Collections.Generic;

using Xamarin.Forms;
using System.Threading.Tasks;
using XamarinForms.QbChat.ViewModels;

namespace XamarinForms.QbChat
{
	public partial class DefaultLoginsPage : ContentPage
	{
		public DefaultLoginsPage ()
		{
			InitializeComponent ();

            DefaultLoginsViewModels vm = new DefaultLoginsViewModels();
		    this.BindingContext = vm;
		}

		protected override async void OnAppearing ()
		{
			base.OnAppearing ();
            var vm = this.BindingContext as DefaultLoginsViewModels;
            vm.OnAppearing();
		}
	}
}

