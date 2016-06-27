using Xamarin.Forms.Platform.iOS;
using XamarinForms.QbChat;
using XamarinForms.QbChat.iOS;
using Xamarin.Forms;
using UIKit;

[assembly: ExportRenderer(typeof(WrapLabel), typeof(WrapLabelRender))]
namespace XamarinForms.QbChat.iOS
{
	public class WrapLabelRender : LabelRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
		{
			base.OnElementChanged(e);

			if (Control != null)
			{
				Control.LineBreakMode = UILineBreakMode.WordWrap;
				Control.Lines = 0;
			}
		}
	}
}

