using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using UIKit;
using System.Collections.Generic;
using Xamarin.Forms.Conference.WebRTC.PageRender;
using Xamarin.Forms.Conference.WebRTC;

[assembly:ExportRenderer (typeof(UsersInGroup), typeof(UsersInGroupPageRender))]
namespace Xamarin.Forms.Conference.WebRTC.PageRender
{
	public class UsersInGroupPageRender : PageRenderer
	{
		public new UsersInGroup Element {
			get{ return (UsersInGroup)base.Element; }
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);

			var LeftNavList = new List<UIBarButtonItem> ();
			var rightNavList = new List<UIBarButtonItem> ();

			var navigationItem =this.NavigationController.TopViewController.NavigationItem;

			if (navigationItem.RightBarButtonItems.Length > 1) {

				for (var i = 0; i < Element.ToolbarItems.Count; i++) {

					var reorder = (Element.ToolbarItems.Count - 1);
					var Order = Element.ToolbarItems [reorder - i].Order;

					if (Order == ToolbarItemOrder.Primary) {
						UIBarButtonItem LeftNavItems = navigationItem.RightBarButtonItems [i];
						LeftNavList.Add (LeftNavItems);
					} else if (Order == ToolbarItemOrder.Default || Order == ToolbarItemOrder.Secondary) {
						UIBarButtonItem RightNavItems = navigationItem.RightBarButtonItems [i];
						rightNavList.Add (RightNavItems);
					}
				}

				navigationItem.SetLeftBarButtonItems (LeftNavList.ToArray (), false);
				navigationItem.SetRightBarButtonItems (rightNavList.ToArray (), false);
			}
		}
	}
}

