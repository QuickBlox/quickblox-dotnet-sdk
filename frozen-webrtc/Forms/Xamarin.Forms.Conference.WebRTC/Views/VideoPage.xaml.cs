using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Xamarin.Forms;

namespace Xamarin.Forms.Conference.WebRTC
{
	public partial class VideoPage : ContentPage
	{
		User mainUser;
		List<User> users;
		TaskCompletionSource<bool> inComingCallResultSource;
		readonly bool isCallInitiator;
		readonly VideoChatMessage initiateVideoMessage;

		public VideoPage(bool isCallInitiator, User mainUser, List<User> users, VideoChatMessage videoMessage)
		{
			InitializeComponent();

			this.mainUser = mainUser;
			this.users = users;
			this.isCallInitiator = isCallInitiator;
			this.initiateVideoMessage = videoMessage;

			var tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += (s, e) =>
			{
				this.InComingCallPage.IsVisible = false;
				this.OutgongCallPage.IsVisible = false;

				inComingCallResultSource.SetResult(false);
			};
			RejectImage.GestureRecognizers.Add(tapGestureRecognizer);

			var tapGestureRecognizer2 = new TapGestureRecognizer();
			tapGestureRecognizer2.Tapped += (s, e) =>
			{
				this.InComingCallPage.IsVisible = false;
				this.OutgongCallPage.IsVisible = false;
				inComingCallResultSource.SetResult(true);
			};
			AnswerImage.GestureRecognizers.Add(tapGestureRecognizer2);

			App.CallHelperProvider.InitVideoContainer(videoContainer);

			App.CallHelperProvider.RegisterOutGoingCallPage(() =>
			{
				this.InComingCallPage.IsVisible = false;
				this.OutgongCallPage.IsVisible = true;
			});

			App.CallHelperProvider.RegisterBackToVideoPage(() =>
			{
				this.InComingCallPage.IsVisible = false;
				this.OutgongCallPage.IsVisible = false;
			});

			App.CallHelperProvider.RegisterBackToUsersPage(() =>
			{
				App.Navigation.PopAsync();
			});

			var tapGestureRecognizer3 = new TapGestureRecognizer();
			tapGestureRecognizer3.Tapped += (s, e) =>
			{
				//App.CallHelperProvider.RejectCall();
			};

			RejectOutgongCallButton.GestureRecognizers.Add(tapGestureRecognizer3);

		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();

			App.CallHelperProvider.StopLocalMedia();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			var vm = new VideoViewModel(this.isCallInitiator, this.mainUser, this.users, this.initiateVideoMessage);
			this.BindingContext = vm;
			vm.OnAppearing();

			this.InComingCallPage.IsVisible = !this.isCallInitiator;
			this.OutgongCallPage.IsVisible = this.isCallInitiator;

			LoadCallInfo();
		}

		public async Task LoadCallInfo()
		{
			IncomingNameLabel.Text = mainUser.FullName;
			InitiatorCallName.Text = mainUser.FullName;

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
							InitiatorImage.Source = (Xamarin.Forms.FileImageSource)ImageSource.FromStream(() => new MemoryStream(imageAsBytes));
						});
					}
				});
			}

			var manyUserInCall = users.Any();
			UsersInCallStack.IsVisible = manyUserInCall;

			if (manyUserInCall)
			{
				UsersInCallLabel.Text = string.Join(",", users.Select(u => u.FullName));
				UsersInCallOutgongCallLabel.Text = UsersInCallLabel.Text;
			}
		}
	}
}

