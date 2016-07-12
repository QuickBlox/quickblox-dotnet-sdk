using QbChat.Pcl;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class ViewModel : Observable
	{
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

