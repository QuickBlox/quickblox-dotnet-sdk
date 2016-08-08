using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Xamarin.PCL;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class VideoViewModel : ViewModel
	{
		List<User> users;
		readonly User mainUser;
		readonly bool isCallInitiator;
		readonly VideoChatMessage videoMessage;

		private bool isCallNotificationVisible;
		private bool isIncomingCall;
		private string usersInCall;
		private string usersToCall;
		private ImageSource image;

		public VideoViewModel(bool isCallInitiator, User mainUser, List<User> users, VideoChatMessage videoMessage)
		{
			this.videoMessage = videoMessage;
			this.isCallInitiator = isCallInitiator;
			this.mainUser = mainUser;
			this.users = users;

			App.CallHelperProvider.RegisterShowVideoCall(() =>
			   {
					this.IsCallNotificationVisible = false;
			   });

			this.AnswerCommand = new Command(this.AnswerCommandExecute,CanCommandExecute);
			this.RejectCommand = new Command(this.RejectCommandExecute,CanCommandExecute);
		}

		public ICommand AnswerCommand { get; set; }
		public ICommand RejectCommand { get; set; }

		public ImageSource Image
		{
			get { return image; }
			set
			{
				image = value;
				RaisePropertyChanged();
			}
		}

		public bool IsCallNotificationVisible
		{
			get { return isCallNotificationVisible; }
			set
			{
				isCallNotificationVisible = value;
				RaisePropertyChanged();
			}
		}

		public bool IsIncomingCall
		{
			get { return isIncomingCall; }
			set
			{
				isIncomingCall = value;
				RaisePropertyChanged();
			}
		}

		public string UsersInCall
		{
			get { return usersInCall; }
			set
			{
				usersInCall = value;
				RaisePropertyChanged();
			}
		}

		public string UsersToCall
		{
			get { return usersToCall; }
			set
			{
				usersToCall = value;
				RaisePropertyChanged();
			}
		}

		public override async void OnAppearing()
		{
			base.OnAppearing();

			this.IsCallNotificationVisible = true;
			this.IsIncomingCall = !this.isCallInitiator;

			Image = ImageSource.FromFile("alfa_placeholder.png");

		    LoadCallInfo();
			if (this.isCallInitiator)
			{
				App.CallHelperProvider.Call(Guid.NewGuid().ToString(), this.mainUser, this.users);
			}
		}

		private bool CanCommandExecute(object arg)
		{
			return !IsBusy;
		}

		private void LoadCallInfo()
		{
			UsersInCall = mainUser.FullName;

			if (mainUser.BlobId.HasValue)
			{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				Task.Factory.StartNew(async () =>

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				{
					var imageAsBytes = await App.QbProvider.GetImageAsync(mainUser.BlobId.Value);
					if (imageAsBytes != null)
					{
						Device.BeginInvokeOnMainThread(() =>
						{
							Image = (Xamarin.Forms.FileImageSource)ImageSource.FromStream(() => new MemoryStream(imageAsBytes));
						});
					}
				});
			}

			UsersToCall = string.Join(",", users.Select(u => u.FullName));
		}

		private void RejectCommandExecute(object obj)
		{
			this.IsBusy = true;

			App.CallHelperProvider.RejectVideoCall();
			App.CallHelperProvider.StopLocalMedia();
			App.Navigation.PopAsync();

			this.IsBusy = false;
		}

		private void AnswerCommandExecute(object obj)
		{
			this.IsBusy = true;

			// Show incoming call
			App.CallHelperProvider.IncomingCall(this.mainUser, this.users, videoMessage);
			this.IsCallNotificationVisible = false;
			this.IsIncomingCall = false;

			this.IsBusy = false;
		}

	}
}

