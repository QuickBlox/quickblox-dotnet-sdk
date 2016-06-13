using System;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using XamarinForms.QbChat.ViewModels;
using System.Threading.Tasks;
using QbChat.Pcl.Repository;

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
            listView.ItemTapped += (object sender, ItemTappedEventArgs e) => ((ListView) sender).SelectedItem = null;
            this.messageEntry.Focused += async (sender, args) =>
            {
                ScrollList();
            };

            //listView.PropertyChanged += (o, args) =>
            //{
            //    if (args.PropertyName == "ItemsSource")
            //    {
            //        ((INotifyCollectionChanged)listView.ItemsSource).CollectionChanged +=
            //            (s, e) =>
            //            {
            //                if (e.Action ==
            //                    System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            //                {
            //                    var collection = s as ObservableCollection<MessageTable>;
            //                    listView.ScrollTo(collection.Last(), ScrollToPosition.End, false);
            //                }
            //            };
            //    }
            //};

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
        
        public async void ScrollList ()
		{
            var sorted = listView.ItemsSource as ObservableCollection<MessageTable>;
			try {
				if (sorted != null && sorted.Count > 1) {
                    await Task.Delay(500);
                    listView.ScrollTo (sorted [sorted.Count - 1], ScrollToPosition.End, false);
				}
			}
			catch (Exception ex) {
			}
		}
    }
}
