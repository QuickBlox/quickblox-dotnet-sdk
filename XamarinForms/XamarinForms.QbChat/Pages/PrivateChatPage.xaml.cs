using System;
using XamarinForms.QbChat.Repository;
using Xamarin.Forms;
using System.Collections.Generic;
using XamarinForms.QbChat.ViewModels;

namespace XamarinForms.QbChat.Pages
{
    public partial class PrivateChatPage : ContentPage
    {
        private string dialogId;
		private bool isLoaded;

		public PrivateChatPage(String dialogId)
        {
            InitializeComponent();
            this.dialogId = dialogId;
			listView.ItemTapped += (object sender, ItemTappedEventArgs e) => ((ListView)sender).SelectedItem = null;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

			if (isLoaded)
				return;

			this.isLoaded = true;

            var vm = new PrivateChantViewModel(this.dialogId);
            this.BindingContext = vm;
            vm.OnAppearing();
        }
        
        public void ScrollList ()
		{
			var sorted = listView.ItemsSource as List<MessageTable>;
			try {
				if (sorted != null && sorted.Count > 10) {
					listView.ScrollTo (sorted [sorted.Count - 1], ScrollToPosition.End, false);
				}
			}
			catch (Exception ex) {
			}
		}
    }
}
