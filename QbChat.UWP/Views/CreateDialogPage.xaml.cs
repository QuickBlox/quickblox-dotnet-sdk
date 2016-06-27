using QbChat.UWP.ViewModels;
using Windows.UI.Xaml.Controls;
using System;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace QbChat.UWP.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CreateDialogPage : Page
    {
        private bool isLoading;
        private CreateDialogViewModel vm;

        public CreateDialogPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (isLoading)
                return;

            isLoading = true;

            vm = new CreateDialogViewModel();
            this.DataContext = vm;
            vm.OnAppearing();

            //await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().HideAsync();
        }
    }
}
