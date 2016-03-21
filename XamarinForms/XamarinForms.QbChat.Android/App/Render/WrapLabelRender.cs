using System;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms;
using XamarinForms.QbChat.Android;
using XamarinForms.QbChat;
using Android.Text;

[assembly: ExportRenderer(typeof(WrapLabel), typeof(WrapLabelRender))]
namespace XamarinForms.QbChat.Android
{
	public class WrapLabelRender : LabelRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
		{
			base.OnElementChanged(e);

			if (Control != null)
			{
				Control.LayoutChange += (s, args) =>
				{
					Control.Ellipsize = TextUtils.TruncateAt.End;
					Control.SetMaxLines((int)((args.Bottom - args.Top) / Control.LineHeight));
				};
			}
		}
	}
}

