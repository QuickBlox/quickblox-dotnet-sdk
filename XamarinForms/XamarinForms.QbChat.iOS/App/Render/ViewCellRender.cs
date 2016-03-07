using System;
using Xamarin.Forms;
using XamarinForms.QbChat.iOS;
using Xamarin.Forms.Platform.iOS;
using UIKit;
using Foundation;
using System.Drawing;
using XamarinForms.QbChat.Repository;

[assembly: ExportRenderer(typeof(ViewCell), typeof(ViewCellRender))]
namespace XamarinForms.QbChat.iOS
{
	public class ViewCellRender : ViewCellRenderer
	{
		public override UITableViewCell GetCell(Cell item, UITableViewCell reusableCell, UITableView tv)
		{

			var cell = base.GetCell(item, reusableCell, tv);
			if (cell != null)
			{
				OnBindingContextChanged(item);
			}
			return cell;
		}

		protected void OnBindingContextChanged(Cell cell)
		{
			var context = cell.BindingContext as MessageTable;
			if (context != null)
			{
				const int totalPadding = 20; // Width Padding
				cell.Height = Convert.ToDouble(EstimateHeight(context.Text, Convert.ToInt32(UIScreen.MainScreen.Bounds.Width) - totalPadding, UIFont.FromName("Helvetica Neue", 14)));
			}
		}

		private Decimal EstimateHeight(String text, Int32 width, UIFont font)
		{
			var size = ((NSString)text).StringSize(font, new SizeF(width, float.MaxValue), UILineBreakMode.WordWrap);
			return (Decimal)size.Height + 40; // The +40 is for extra height padding

		}

	}
}

