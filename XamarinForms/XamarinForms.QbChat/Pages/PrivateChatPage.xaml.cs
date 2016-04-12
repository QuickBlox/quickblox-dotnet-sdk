using Quickblox.Sdk.Modules.ChatXmppModule.ExtraParameters;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using System;
using System.Linq;
using XamarinForms.QbChat.Repository;
using Xamarin.Forms;
using Quickblox.Sdk.Modules.ChatModule.Models;
using System.Runtime.Serialization;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Quickblox.Sdk.GeneralDataModel.Models;
using XamarinForms.QbChat.ViewModels;

namespace XamarinForms.QbChat.Pages
{
    public partial class PrivateChatPage : ContentPage
    {
        private string dialogId;
		int opponentId;
		private bool isLoaded;
		DialogTable dialog;
		User opponentUser;

		public PrivateChatPage(String dialogId)
        {
            InitializeComponent();
            this.dialogId = dialogId;
			listView.ItemTapped += (object sender, ItemTappedEventArgs e) => ((ListView)sender).SelectedItem = null;
        }

        protected override async void OnAppearing()
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
