using Windows.UI.Xaml.Controls;
using Xamarin.Forms;
using Xamarin.Forms.Conference.WebRTC;
using Xamarin.Forms.Platform.WinRT;

[assembly: ExportRenderer(typeof(AbsoluteLayout), typeof(CanvasRender))]
namespace Xamarin.Forms.Conference.WebRTC
{
    public class CanvasRender : ViewRenderer<AbsoluteLayout, Canvas>
    {
        /// <summary>
        /// Handles the Element Changed event
        /// </summary>
        /// <param name="e">The e.</param>
        protected override void OnElementChanged(ElementChangedEventArgs<AbsoluteLayout> e)
        {
            base.OnElementChanged(e);

            if (Element == null) return;

            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    var canvas = new Canvas();
                    SetNativeControl(canvas);
                }
            }
        }
    }
}

