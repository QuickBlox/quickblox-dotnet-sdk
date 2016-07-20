using Xamarin.PCL;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class ViewModel : Observable
	{
		protected bool isLoaded;
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

		public virtual void OnAppearing()
		{
		}
	}
}

