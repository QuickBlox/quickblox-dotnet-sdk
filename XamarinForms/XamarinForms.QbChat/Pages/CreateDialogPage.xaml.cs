using System;
using System.Collections.Generic;

using Xamarin.Forms;
using System.Threading.Tasks;
using System.Linq;
using Quickblox.Sdk.Modules.ChatModule.Models;
using XamarinForms.QbChat.Repository;
using XamarinForms.QbChat.Pages;
using Quickblox.Sdk.Modules.Models;
using Acr.UserDialogs;
using XamarinForms.QbChat.ViewModels;

namespace XamarinForms.QbChat
{
    public partial class CreateDialogPage : ContentPage
    {
        private bool isLoaded;

        public CreateDialogPage()
        {
            InitializeComponent();
            listView.ItemSelected += (o, e) => { listView.SelectedItem = null; };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (isLoaded)
                return;

            isLoaded = true;

            var vm = new CreateDialogViewModel();
            this.BindingContext = vm;
            vm.OnAppearing();
        }
    }
}

