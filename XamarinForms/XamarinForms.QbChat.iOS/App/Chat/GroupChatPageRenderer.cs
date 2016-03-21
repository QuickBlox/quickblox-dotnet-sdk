using System;
using Xamarin.Forms.Platform.iOS;
using XamarinForms.QbChat.Pages;
using Xamarin.Forms;
using XamarinForms.QbChat.iOS;
using UIKit;
using Foundation;

[assembly:ExportRenderer(typeof(GroupChatPage), typeof(GroupChatPageRender))]
namespace XamarinForms.QbChat.iOS
{
	public class GroupChatPageRender : PageRenderer
	{
		private NSObject _show;
		private NSObject _hide;
		GroupChatPage page;
		private float _scrollAmount;
		private bool _isKeyboardShown = false;

		protected override void OnElementChanged (VisualElementChangedEventArgs e)
		{
			base.OnElementChanged (e);
			page = (GroupChatPage)e.NewElement;
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);

			_show = UIKeyboard.Notifications.ObserveWillShow((sender, e) => {
				if (!_isKeyboardShown)
				{var r = UIKeyboard.FrameBeginFromNotification(e.Notification);
				_scrollAmount = (float)r.Height;
				Scroll(sender, e, true);
					this._isKeyboardShown = true;}
			});
			_hide = UIKeyboard.Notifications.ObserveWillHide((sender, e) => 
				{
					if (_isKeyboardShown){
						Scroll(sender, e, false);
						_scrollAmount = 0;
						_isKeyboardShown = false;
					}
				});

			View.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
		}

		public override void ViewWillDisappear (bool animated)
		{
			base.ViewWillDisappear (animated);
			_show.Dispose();
			_hide.Dispose ();
		}

		private void Scroll (object sender, UIKeyboardEventArgs e, bool scale)
		{
			UIView.BeginAnimations(string.Empty, IntPtr.Zero);
			UIView.SetAnimationCurve(e.AnimationCurve);
			UIView.SetAnimationDuration(e.AnimationDuration);

			ChangeFrameSize (scale);

			UIView.CommitAnimations();
		}

		void ChangeFrameSize (bool scale)
		{
			var frame = View.Frame;
			if (scale)
				frame.Height -= _scrollAmount;
			else
				frame.Height += _scrollAmount;
			page.Content.HorizontalOptions = LayoutOptions.StartAndExpand;
			page.Layout (new Rectangle (0, 0, frame.Width, frame.Height));
			page.ForceLayout ();
			View.Frame = frame;

			page.OnMessagesChanged();
		}

		public override void ViewDidDisappear (bool animated)
		{
			base.ViewDidDisappear (animated);
			ChangeFrameSize (false);
		}
	}
}

