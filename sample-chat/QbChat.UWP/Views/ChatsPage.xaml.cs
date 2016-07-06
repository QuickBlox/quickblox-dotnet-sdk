using QbChat.UWP.ViewModels;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace QbChat.UWP.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatsPage : Page
    {
        private bool isLoading;
        private ChatsViewModel vm;

        public ChatsPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.Back)
                return;

            App.NavigationFrame.BackStack.Clear();

            vm = new ChatsViewModel();
            this.DataContext = vm;
            vm.OnAppearing();

            //await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().HideAsync();
        }

        private void OnItemClicked(object sender, ItemClickEventArgs e)
        {
            vm.TappedCommand.Execute(e.ClickedItem);
        }
    }
}
