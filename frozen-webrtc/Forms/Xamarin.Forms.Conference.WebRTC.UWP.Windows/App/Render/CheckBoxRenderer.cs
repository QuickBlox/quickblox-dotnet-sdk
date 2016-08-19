using System.ComponentModel;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms.Conference.WebRTC;
using Xamarin.Forms.Conference.WebRTC.Controls;
using Xamarin.Forms.Platform.WinRT;

[assembly: ExportRenderer(typeof(CheckBoxExtended), typeof(CheckBoxRenderer))]
namespace Xamarin.Forms.Conference.WebRTC
{
    public class CheckBoxRenderer : ViewRenderer<CheckBoxExtended, CheckBox>
    {
        /// <summary>
        /// Handles the Element Changed event
        /// </summary>
        /// <param name="e">The e.</param>
        protected override void OnElementChanged(ElementChangedEventArgs<CheckBoxExtended> e)
        {
            base.OnElementChanged(e);

            if (Element == null) return;

            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    var checkBox = new CheckBox();
                    SetNativeControl(checkBox);
                }
                
                Control.IsChecked = e.NewElement.Checked;
            }
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
                    Control.IsChecked = Element.Checked;
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Property change for {0} has not been implemented.", e.PropertyName);
                    return;
            }
        }
    }
}

