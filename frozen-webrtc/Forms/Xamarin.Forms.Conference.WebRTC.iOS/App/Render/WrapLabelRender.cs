using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using UIKit;
using Xamarin.Forms.Conference.WebRTC;

[assembly: ExportRenderer(typeof(WrapLabel), typeof(WrapLabelRender))]
namespace Xamarin.Forms.Conference.WebRTC
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

