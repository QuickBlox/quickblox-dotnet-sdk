using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Xamarin.PCL;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class UsersInGroupViewModel : ViewModel
	{
		string title;
		User mainUser;
		string roomName;

		public UsersInGroupViewModel()
		{
			this.LogoutCommand = new Command(this.LogoutCommandExecute);
			this.SettingsCommand = new Command(this.SettingsCommandExecute);

			ReloadUsersCommand = new Command(ReloadUsersCommandExecute, CanCommandExecute);
			AudioCallCommand = new Command(AudioCallCommandExecute, CanCommandExecute);
			VideoCallCommand = new Command(VideoCallCommandExecute, CanCommandExecute);
			Users = new ObservableCollection<SelectableUser>();
			RoomName = "Room name: ";
		}

		public string Title
		{
			get { return title; }
			set
			{
				title = value;
				this.RaisePropertyChanged();
			}
		}

		public string RoomName
		{
			get { return roomName; }
			set
			{
				roomName = value;
				this.RaisePropertyChanged();
			}
		}


		public ObservableCollection<SelectableUser> Users { get; set; }

		public ICommand LogoutCommand { get; private set; }

		public Command SettingsCommand { get; private set; }

		public ICommand ReloadUsersCommand { get; private set; }

		public ICommand AudioCallCommand { get; private set; }

		public ICommand VideoCallCommand { get; private set;}

		public override async void OnAppearing()
		{
			base.OnAppearing();

			if (this.isLoaded)
				return;

			this.isLoaded = true;
			this.IsBusy = true;


		    mainUser = await App.QbProvider.GetUserAsync(App.QbProvider.UserId);
			if (mainUser != null)
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					Title = mainUser.FullName;
					RoomName = string.Format("Room name: {0}", mainUser.UserTags);
				});
				await LoadUsersByTag();
			}

			this.IsBusy = false;
		}

		private async Task LoadUsersByTag()
		{
			if (mainUser == null) return;

			var users = await App.QbProvider.GetUserByTag(mainUser.UserTags);
			if (users.Any())
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					this.Users.Clear();
					foreach (var user in users.Where(u => u.Id != App.QbProvider.UserId))
					{
						this.Users.Add(new SelectableUser() { User = user });
					}
				});
			}
		}

		private async void SettingsCommandExecute(object obj)
		{
			await App.Navigation.PushAsync(new Settings());
		}

		private async void LogoutCommandExecute(object obj)
		{
			if (IsBusy)
				return;
			
			this.IsBusy = true;

			var isDeleted = await App.QbProvider.DeleteUserById(App.QbProvider.UserId);
			if (isDeleted)
			{
				var result = await App.Current.MainPage.DisplayAlert("Log Out", "Do you really want to Log Out?", "Ok", "Cancel");
				if (result)
				{
					DependencyService.Get<ILoginStorage>().Clear();
					App.SetLogin();
				}
			}

			this.IsBusy = false;
		}

		private bool CanCommandExecute(object arg)
		{
			return !IsBusy;
		}

		private async void ReloadUsersCommandExecute(object obj)
		{
			if (IsBusy)
				return;

			IsBusy = true;

			await LoadUsersByTag();

			IsBusy = false;
		}

		private async void VideoCallCommandExecute(object obj)
		{
			var users = Users.Where(u => u.IsSelected).Select(u => u.User).ToList();
			if (users.Count > 0 && users.Count < 5)
			{
				await App.Navigation.PushAsync(new VideoPage(users));
			}
			else 
			{
				await App.Current.MainPage.DisplayAlert("Error", "Please, select users from one till five", "Ok");
			}

		}

		private async void AudioCallCommandExecute(object obj)
		{
			var result = await ((App)App.Current).ShowInputCallDialog(mainUser.FullName);
			if (result)
			{
			}
			//var users = Users.Where(u => u.IsSelected).Select(u => u.User).ToList();
			//if (users.Count > 0 && users.Count < 5)
			//{
			//	await App.Navigation.PushAsync(new VideoPage(users));
			//}
			//else
			//{
			//	await App.Current.MainPage.DisplayAlert("Error", "Please, select users from one till five", "Ok");
			//}
		}
	}
}

