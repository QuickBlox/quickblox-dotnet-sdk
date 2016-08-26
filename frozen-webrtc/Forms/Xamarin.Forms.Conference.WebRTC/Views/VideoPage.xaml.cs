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
		readonly User mainUser;
		readonly List<User> users;
		readonly bool isCallInitiator;
		readonly VideoChatMessage initiateVideoMessage;
		readonly bool isVideoCall;

		public VideoPage(bool isCallInitiator, User mainUser, List<User> users, VideoChatMessage videoMessage, bool isVideoCall)
		{
			InitializeComponent();

			this.mainUser = mainUser;
			this.users = users;
			this.isCallInitiator = isCallInitiator;
			this.initiateVideoMessage = videoMessage;
			this.isVideoCall = isVideoCall;

			App.CallHelperProvider.InitVideoContainer(videoContainer);
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();

			var vm = this.BindingContext as VideoViewModel;
			if (vm != null)
			{
				vm.OnDisappearing();
			}
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			var vm = new VideoViewModel(this.isCallInitiator, this.mainUser, this.users, this.initiateVideoMessage, isVideoCall);
			this.BindingContext = vm;
			vm.OnAppearing();

			buttonRoot.IsVisible = true;
		}

		protected override bool OnBackButtonPressed()
		{
			var vm = this.BindingContext as VideoViewModel;
			if (vm != null)
			{
				return vm.OnBackButtonPressed();
			}
			else {
				return false;
			}
		}
	}
}

