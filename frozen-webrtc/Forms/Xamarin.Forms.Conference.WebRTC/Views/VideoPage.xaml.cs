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

		public VideoPage(bool isCallInitiator, User mainUser, List<User> users, VideoChatMessage videoMessage)
		{
			InitializeComponent();

			this.mainUser = mainUser;
			this.users = users;
			this.isCallInitiator = isCallInitiator;
			this.initiateVideoMessage = videoMessage;

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

			var vm = new VideoViewModel(this.isCallInitiator, this.mainUser, this.users, this.initiateVideoMessage);
			this.BindingContext = vm;
			vm.OnAppearing();
		}
	}
}

