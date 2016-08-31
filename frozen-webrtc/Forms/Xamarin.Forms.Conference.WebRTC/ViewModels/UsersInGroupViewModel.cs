using System;
using System.Collections.Generic;
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

			this.IsBusy = true;

		    App.MainUser = await App.QbProvider.GetUserAsync(App.QbProvider.UserId);
			if (App.MainUser != null)
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					Title = App.MainUser.FullName;
					RoomName = string.Format("Room name: {0}", App.MainUser.UserTags);
				});
				await LoadUsersByTag();

				((App)App.Current).InitChatClient();
				App.CallHelperProvider.IncomingCallMessageEvent += IncomingCallMethod;
			}

			//App.CallHelperProvider.RegisterIncomingCallPage((videoMessage) =>
			//{
			//	var callerId = videoMessage.Caller;
			//	var caller = this.Users.FirstOrDefault(u => u.User.Id == Int32.Parse(callerId));
			//	if (caller != null)
			//	{
			//		var opponents = new List<User>();
			//		foreach (var id in videoMessage.OpponentsIds)
			//		{
			//			var user = this.Users.FirstOrDefault(u => u.User.Id == Int32.Parse(id));
			//			if (user != null)
			//			{
			//				opponents.Add(user.User);
			//			}
			//		}

			//		// Add himself
			//		opponents.Add(mainUser);

			//		Device.BeginInvokeOnMainThread(() =>
			//									   App.Navigation.PushAsync(new VideoPage(false, caller.User, opponents, videoMessage)));
			//	}
			//});
				
			this.IsBusy = false;
		}

		private async Task LoadUsersByTag()
		{
			if (App.MainUser == null) return;

			var users = await App.QbProvider.GetUserByTag(App.MainUser.UserTags);
			if (users.Any())
			{
				App.UsersInRoom = users;
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

			var result = await App.Current.MainPage.DisplayAlert("Log Out", "Do you really want to Log Out?", "Ok", "Cancel");
			if (result)
			{
				var isDeleted = await App.QbProvider.DeleteUserById(App.QbProvider.UserId);
				if (isDeleted)
				{
					App.CallHelperProvider.IncomingCallMessageEvent -= IncomingCallMethod;
					App.CallHelperProvider.Disconnect();
					DependencyService.Get<ILoginStorage>().Clear();

					//((App)App.Current).RemoveChatClient();
					App.SetLogin();
				}
			}

			this.IsBusy = false;
		}

		private void IncomingCallMethod(object sender, IncomingCall incomingCall)
		{
			Device.BeginInvokeOnMainThread(() =>
										   App.SetVideoCall(false, incomingCall.Caller, incomingCall.Opponents, incomingCall.VideoChatMessage));
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
			if (IsBusy)
				return;

			this.IsBusy = true;
			var users = Users.Where(u => u.IsSelected).Select(u => u.User).ToList();
			if (users.Count > 0 && users.Count < 2)
			{
				App.SetVideoCall(true, App.MainUser, users, null);
			}
			else 
			{
				await App.Current.MainPage.DisplayAlert("Error", "Please, select users from one till five", "Ok");
			}

			this.IsBusy = false;
		}

		private async void AudioCallCommandExecute(object obj)
		{
			if (IsBusy)
				return;

			this.IsBusy = true;

			var users = Users.Where(u => u.IsSelected).Select(u => u.User).ToList();
			if (users.Count > 0 && users.Count < 2)
			{
				App.SetVideoCall(true, App.MainUser, users, null);
			}
			else
			{
				await App.Current.MainPage.DisplayAlert("Error", "Please, select users from one till five", "Ok");
			}

			this.IsBusy = false;
		}
	}
}

