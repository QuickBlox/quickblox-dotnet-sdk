using Xamarin.PCL;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class ViewModel : Xamarin.PCL.Observable
    {
		private bool isLoaded;
		private bool isBusy;

		public ViewModel()
		{
		}

		public bool IsBusy
		{
			get
			{
				return isBusy;
			}
			set
			{
				isBusy = value;
				RaisePropertyChanged();
			}
		}

		public bool IsLoaded
		{
			get { return isLoaded; }
			set
			{
				isLoaded = value;
				RaisePropertyChanged();
			}
		}

		public virtual void OnAppearing()
		{
			IsLoaded = true;
		}

		public virtual void OnDisappearing()
		{
		}
	}
}

