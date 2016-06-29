
using Xamarin.Forms;
using XamarinForms.QbChat.ViewModels;

namespace XamarinForms.QbChat
{
    public partial class ChatInfoPage : ContentPage
	{
		private bool isLoading; 
		private string dialogId;

		public ChatInfoPage (string dialogId)
		{
			InitializeComponent ();
			this.dialogId = dialogId;

            listView.ItemTapped += (object sender, ItemTappedEventArgs e) => {
                listView.SelectedItem = null;
            };
        }

		protected override void OnAppearing ()
		{
			base.OnAppearing ();

            var vm = new ChatInfoViewModel(this.dialogId);
            this.BindingContext = vm;
            vm.OnAppearing();
		}
	}
}

