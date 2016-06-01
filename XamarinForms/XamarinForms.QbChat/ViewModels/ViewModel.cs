using QbChat.Pcl;

namespace XamarinForms.QbChat.ViewModels
{
    public class ViewModel : Observable
    {
        private bool isBusyIndicatorVisible;

        public bool IsBusyIndicatorVisible
        {
            get { return isBusyIndicatorVisible; }
            set
            {
                isBusyIndicatorVisible = value;
                this.RaisePropertyChanged();
            }
        }

        public virtual void OnAppearing()
        {
        } 

    }
}
