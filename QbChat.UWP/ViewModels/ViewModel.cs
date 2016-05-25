using QbChat.Pcl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QbChat.UWP.ViewModels
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
