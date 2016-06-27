using QbChat.Pcl;

namespace QbChat.UWP.ViewModels
{
    public class ViewModel : Observable
    {
        private bool isBusy;
        
        public bool IsBusy
        {
            get { return isBusy; }
            set { isBusy = value;
                RaisePropertyChanged();
            }
        }

        public virtual void OnAppearing()
        {
        }

    }
}
