using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Xamarin.PCL;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class UsersInGroupViewModel : ViewModel
	{ 
		string title;

		public UsersInGroupViewModel()
		{
			this.LogoutCommand = new Command(this.LogoutCommandExecute);
			this.SettingsCommand = new Command(this.SettingsCommandExecute);

			Users = new ObservableCollection<SelectableUser>();
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

		public ObservableCollection<SelectableUser> Users { get; set; }

		public ICommand LogoutCommand { get; set; }

		public Command SettingsCommand { get; private set; }

		public override async void OnAppearing()
		{
			base.OnAppearing();

			this.IsBusy = true;

			var mainUser = await App.QbProvider.GetUserAsync(App.QbProvider.UserId);
			if (mainUser != null)
			{
				Device.BeginInvokeOnMainThread(() => Title = mainUser.FullName);

				var users = await App.QbProvider.GetUserByTag(mainUser.UserTags);
				if (users.Any())
				{
					Device.BeginInvokeOnMainThread(() =>
					{
						this.Users.Clear();
						foreach (var user in users)
						{
							this.Users.Add(new SelectableUser() { User = user } );
						}
					});
				}
			}

			this.IsBusy = false;
		}

		void SettingsCommandExecute(object obj)
		{
			App.Navigation.PushAsync(new Settings());
		}

		void LogoutCommandExecute(object obj)
		{
			
		}
	}
}

