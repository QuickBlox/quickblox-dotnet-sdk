using QbChat.Pcl.Repository;
using QbChat.UWP.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace QbChat.UWP.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PrivateChatPage : Page
    {
        private PrivateChatViewModel vm;

        public PrivateChatPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var parameter = (string)e.Parameter;
            vm = new PrivateChatViewModel(parameter);
            this.DataContext = vm;
            vm.OnAppearing();

            await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().HideAsync();
        }

        public async void ScrollList()
        {
            var sorted = list.ItemsSource as ObservableCollection<MessageTable>;
            try
            {
                if (sorted != null && sorted.Count > 10)
                {
                    //await Task.Delay(500);
                    list.ScrollIntoView(sorted[sorted.Count - 1]);
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
