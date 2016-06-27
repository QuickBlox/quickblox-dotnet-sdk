
using Xamarin.Forms;
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

