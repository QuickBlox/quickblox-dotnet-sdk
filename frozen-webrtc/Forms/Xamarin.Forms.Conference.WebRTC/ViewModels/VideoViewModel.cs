using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.Modules.UsersModule.Models;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class VideoViewModel : ViewModel
	{
		List<User> users;
		readonly User mainUser;
		readonly bool isCallInitiator;
		readonly VideoChatMessage videoMessage;

		private bool isCallNotificationVisible;
		private bool isCallConnected;
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

			this.AnswerCommand = new Command(this.AnswerCommandExecute,CanCommandExecute);
			this.EndOfCallCommand = new Command(this.EndOfCallCommandExecute, CanCommandExecute);
			this.RejectCommand = new Command(this.RejectCommandExecute,CanCommandExecute);
		}

		public ICommand AnswerCommand { get; set; }
		public ICommand RejectCommand { get; set; }
		public ICommand EndOfCallCommand { get; set; }

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

		public bool IsCallConnected
		{
			get { return isCallConnected; }
			set
			{
				isCallConnected = value;
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

		public override void OnAppearing()
		{
			this.IsCallNotificationVisible = true;
			this.IsCallConnected = this.isCallInitiator;
			this.IsIncomingCall = !this.isCallInitiator;

			var sessionId = string.Empty;
			if (videoMessage != null)
			{
				sessionId = videoMessage.SessionId;
			}
			else
			{
				sessionId = Guid.NewGuid().ToString();
			}

			App.CallHelperProvider.InitCall(sessionId, this.mainUser, this.users);
			App.CallHelperProvider.IncomingDropMessageEvent += IncomingDropMessage;
			App.CallHelperProvider.CallUpEvent += OnCallUpEvent;
			App.CallHelperProvider.CallDownEvent += OnCallDownEvent;

			Image = ImageSource.FromFile("alfa_placeholder.png");

		    LoadCallInfo();

			if (this.isCallInitiator)
			{
				App.CallHelperProvider.CallToUsers();
			}


			base.OnAppearing();
		}

		public override void OnDisappearing()
		{
			App.CallHelperProvider.IncomingDropMessageEvent -= IncomingDropMessage;
			App.CallHelperProvider.CallUpEvent -= OnCallUpEvent;
			App.CallHelperProvider.CallDownEvent -= OnCallDownEvent;
			App.CallHelperProvider.StopLocalMedia();
		}

		private void OnCallUpEvent(object sender, EventArgs e)
		{
			this.IsCallNotificationVisible = false;
			this.IsCallConnected = true;
		}

		private void OnCallDownEvent(object sender, EventArgs e)
		{
			Device.BeginInvokeOnMainThread(() => App.Navigation.PopAsync());
		}

		private void IncomingDropMessage(object sender, VideoChatMessage e)
		{
			if (e.Caller == e.Sender.ToString())
			{
				Device.BeginInvokeOnMainThread(() => App.Navigation.PopAsync());
			}
			else 
			{
				users = users.Where(u => u.Id != e.Sender).ToList();
				if (users.Any())
				{
					Device.BeginInvokeOnMainThread(() => App.Navigation.PopAsync());
				}
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

		private void EndOfCallCommandExecute(object obj)
		{
			this.IsBusy = true;

			App.CallHelperProvider.HangUpVideoCall();
			Device.BeginInvokeOnMainThread(() => App.Navigation.PopAsync());

			this.IsBusy = false;
		}

		private void RejectCommandExecute(object obj)
		{
			this.IsBusy = true;

			App.CallHelperProvider.RejectVideoCall();
			Device.BeginInvokeOnMainThread(() => App.Navigation.PopAsync());

			this.IsBusy = false;
		}

		private void AnswerCommandExecute(object obj)
		{
			this.IsBusy = true;

			// Show incoming call
			App.CallHelperProvider.ConnectToIncomingCall(videoMessage);

			this.IsCallNotificationVisible = false;
			this.IsCallConnected = true;

			this.IsBusy = false;
		}

	}
}

