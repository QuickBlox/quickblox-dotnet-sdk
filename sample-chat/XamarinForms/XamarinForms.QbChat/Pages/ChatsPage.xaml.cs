using Xamarin.Forms;
using XamarinForms.QbChat.ViewModels;

namespace XamarinForms.QbChat.Pages
{
    public partial class ChatsPage : ContentPage
    {
		private bool isLoaded;

		public ChatsPage()
        {
            InitializeComponent();

            listView.ItemTapped += (sender, args) => this.listView.SelectedItem = null;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
			if (isLoaded)
				return;

			isLoaded = true;

            var vm = new ChatsViewModel();
            this.BindingContext = vm;

            vm.OnAppearing();
        }
    }
}
