using System;

using UIKit;
using MugenMvvmToolkit.iOS.Views;
using MugenMvvmToolkit.Binding.Builders;
using MugenMvvmToolkit.iOS;
using MugenMvvmToolkit.iOS.Binding;
using MugenMvvmToolkit.Binding;
using CoreGraphics;
using MugenMvvmToolkit.Binding.Extensions.Syntax;
using Foundation;

namespace Qmunicate.Xamarin.iOS
{
	[Register("ChatsViewController")]
	public class ChatsViewController : MvvmTableViewController
	{
		public ChatsViewController()
		{
			Title = "Chats";
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			View.BackgroundColor = UIColor.White;

			TableView.AllowsSelection = true;
			TableView.AllowsMultipleSelection = false;
			TableView.SetCellStyle(UITableViewCellStyle.Subtitle);
			using (var set = new BindingSet<UITableView, ChatsViewModel>(TableView))
			{
				var searchBar = new UISearchBar(new CGRect(0, 0, 320, 44)) { Placeholder = "Filter..." };
				set.Bind(searchBar).To(() => (vm, ctx) => vm.FilterText).TwoWay();
				TableView.TableHeaderView = searchBar;

				set.Bind(AttachedMemberConstants.ItemsSource)
					.To(() => (vm, ctx) => vm.GridViewModel.ItemsSource);
				set.Bind(AttachedMemberConstants.SelectedItem)
					.To(() => (vm, ctx) => vm.GridViewModel.SelectedItem)
					.TwoWay();
			}

			TableView.SetCellBind (cell => {
				cell.SetEditingStyle (UITableViewCellEditingStyle.Delete);
				cell.Accessory = UITableViewCellAccessory.DetailDisclosureButton;
				using (var set = new BindingSet<DialogTable> ()) {
					set.Bind (cell, AttachedMembers.UITableViewCell.AccessoryButtonTappedEvent)
												.To (() => (m, ctx) => ctx.Relative<UIViewController> ().DataContext<ChatsViewModel> ().OpenDialogCommand)
												.OneTime ()
												.WithCommandParameter (() => (m, ctx) => m)
												.ToggleEnabledState (false);
					set.Bind (cell, AttachedMembers.UITableViewCell.DeleteClickEvent)
						.To (() => (m, ctx) => ctx.Relative<UIViewController> ().DataContext<ChatsViewModel> ().RemoveDialogCommand)
												.OneTime ()
												.WithCommandParameter (() => (m, ctx) => m)
												.ToggleEnabledState (false);
					set.Bind (cell.TextLabel)
												.To (() => (m, ctx) => m.Name);
					set.Bind (cell.DetailTextLabel)
												.To (() => (m, ctx) => m.LastMessage);
//					set.Bind (cell.ImageView)
//												.To (() => (m, ctx) => m.Photo);
//					set.Bind (cell, AttachedMembers.UITableViewCell.TitleForDeleteConfirmation)
//												.To (() => (m, ctx) => string.Format ("Delete {0}", m.Name));
				}
			});

		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}


