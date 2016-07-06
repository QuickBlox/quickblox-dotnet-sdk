using System;

using Xamarin.Forms;

#if __IOS__
using Xamarin.Forms.Platform.iOS;
#else
using Xamarin.Forms.Platform.Android;
using Views = Android.Views;
#endif

using Xamarin.Forms.Conference.WebRTC;

[assembly: ExportRenderer(typeof(FormsVideoControl), typeof(FormsVideoControlRenderer))]

namespace Xamarin.Forms.Conference.WebRTC
{
    public class FormsVideoControl : View
    {
#if __IOS__
        public UIKit.UIView NativeControl
        {
        get { return (UIKit.UIView)WeakNativeControl; }
        }
#else
        public Views.View NativeControl
        {
            get { return (Views.View)WeakNativeControl; }
        }
#endif

        public object WeakNativeControl { get; set; }

        public FormsVideoControl(object weakNativeControl)
        {
            WeakNativeControl = weakNativeControl;
        }
    }

    public class FormsVideoControlRenderer : ViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<View> e)
        {
            base.OnElementChanged(e);

            var videoControl = e.NewElement as FormsVideoControl;
            if (videoControl != null)
            {
                SetNativeControl(videoControl.NativeControl);
            }
        }
    }
}