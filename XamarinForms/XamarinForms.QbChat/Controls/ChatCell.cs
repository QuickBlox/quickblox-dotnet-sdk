using System;
using Xamarin.Forms;
using Quickblox.Sdk.Modules.ChatModule.Models;
using QbChat.Pcl.Repository;

namespace XamarinForms.QbChat
{
	public class ChatCell : ImageCell
	{
		protected override void OnBindingContextChanged ()
		{
			base.OnBindingContextChanged ();

			var dialog = this.BindingContext as DialogTable;
			if (dialog != null) {
				if (dialog.DialogType == DialogType.Private) {
					this.ImageSource = Device.OnPlatform (
						iOS: ImageSource.FromFile ("privateholder.png"), 
						Android: ImageSource.FromFile ("privateholder.png"), 
						WinPhone: ImageSource.FromFile ("privateholder.png"));
				} else {
					this.ImageSource = Device.OnPlatform (
						iOS: ImageSource.FromFile ("groupholder.png"), 
						Android: ImageSource.FromFile ("groupholder.png"), 
						WinPhone: ImageSource.FromFile ("groupholder.png"));
				}
			}
		}
	}
}

