using System.ComponentModel;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Conference.WebRTC;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms.Conference.WebRTC.Controls;

[assembly: ExportRenderer(typeof(CheckBoxExtended), typeof(CheckBoxRenderer))]
namespace Xamarin.Forms.Conference.WebRTC
{
	public class CheckBoxRenderer: ViewRenderer<CheckBoxExtended, CheckBoxView>
	{
		/// <summary>
		/// Handles the Element Changed event
		/// </summary>
		/// <param name="e">The e.</param>
		protected override void OnElementChanged(ElementChangedEventArgs<CheckBoxExtended> e)
		{
			base.OnElementChanged(e);

			if (Element == null) return;

			BackgroundColor = Element.BackgroundColor.ToUIColor();
			if (e.NewElement != null)
			{
				if (Control == null)
				{
					var checkBox = new CheckBoxView (Bounds);
					checkBox.VerticalAlignment = UIControlContentVerticalAlignment.Center;
					checkBox.TouchUpInside += (s, args) => Element.Checked = Control.Checked;
					SetNativeControl (checkBox);
				}
				Control.LineBreakMode = UILineBreakMode.CharacterWrap;
				Control.VerticalAlignment = UIControlContentVerticalAlignment.Center;
				Control.CheckedTitle = string.IsNullOrEmpty (e.NewElement.CheckedText) ? e.NewElement.DefaultText : e.NewElement.CheckedText;
				Control.UncheckedTitle = string.IsNullOrEmpty (e.NewElement.UncheckedText) ? e.NewElement.DefaultText : e.NewElement.UncheckedText;
				Control.Checked = e.NewElement.Checked;
			}

			Control.Frame = Frame;
			Control.Bounds = Bounds;
		}

		/// <summary>
		/// Handles the <see cref="E:ElementPropertyChanged" /> event.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			switch (e.PropertyName)
			{
			case "Checked":
				Control.Checked = Element.Checked;
				break;
			default:
				System.Diagnostics.Debug.WriteLine("Property change for {0} has not been implemented.", e.PropertyName);
				return;
			}
		}
	}
}

