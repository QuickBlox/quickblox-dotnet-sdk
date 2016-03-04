using System;
using Xamarin.Forms;
using XamarinForms.QbChat.Repository;
using System.Diagnostics;

namespace XamarinForms.QbChat
{
	public class MessageCell: ViewCell
	{
		Label title, label;
		StackLayout layout;

		public MessageCell ()
		{
			title = new Label {
				VerticalTextAlignment = TextAlignment.Center,
				TextColor = Color.FromHex("#01B6FF"),
				FontSize = 14
			};

			title.SetBinding (Label.TextProperty, "RecepientFullName");

			label = new Label {
				YAlign = TextAlignment.Center,
				FontSize = 14,

			};
			label.SetBinding (Label.TextProperty, "Text");

			var text = new StackLayout {
				Orientation = StackOrientation.Vertical,
				Padding = new Thickness(0, 0, 0, 0),
				Children = {title, label}
			};

			layout = new StackLayout {
				Padding = new Thickness(20, 0, 0, 0),
				Children = {text}
			};
			View = layout;
		}

		protected override void OnBindingContextChanged ()
		{
			base.OnBindingContextChanged ();

			var message = this.BindingContext as MessageTable;

			if (message != null && message.Text != null) {
				if (message.Text.Length > 75)
					this.Height = 110;
				else if (message.Text.Length > 60)
					this.Height = 80; 
				else if (message.Text.Length > 30)
					this.Height = 60;
				else
					this.Height = 40;
			}
		}
	}
}

