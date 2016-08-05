using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

		public VideoViewModel(bool isCallInitiator, User mainUser, List<User> users, VideoChatMessage videoMessage)
		{
			this.videoMessage = videoMessage;
			this.isCallInitiator = isCallInitiator;
			this.mainUser = mainUser;
			this.users = users;
		}

		public override void OnAppearing()
		{
			base.OnAppearing();

			if (this.isCallInitiator)
			{
				App.CallHelperProvider.Call(Guid.NewGuid().ToString(), this.mainUser, this.users);
			}
			else 
			{
				App.CallHelperProvider.IncomingCall(this.mainUser, this.users, videoMessage);
			}
		}
	}
}

