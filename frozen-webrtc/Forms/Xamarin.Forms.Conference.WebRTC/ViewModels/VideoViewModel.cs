using System;
using System.Collections.Generic;
using Quickblox.Sdk.Modules.ChatXmppModule;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Xamarin.PCL;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class VideoViewModel : ViewModel
	{
		List<User> users;

		public VideoViewModel(List<User> users)
		{
			this.users = users;
		}

		public override void OnAppearing()
		{
			base.OnAppearing();

			MessageProvider.Instance.ChatXmppClient.MessageReceived -= OnMessageReceived;
			MessageProvider.Instance.ChatXmppClient.MessageReceived += OnMessageReceived;
		}

		private void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
		{
			
		}
	}
}

