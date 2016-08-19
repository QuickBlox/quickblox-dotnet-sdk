using System;

using Xamarin.Forms;

#if __IOS__
using Xamarin.Forms.Platform.iOS;
#elif __ANDROID__
using Xamarin.Forms.Platform.Android;
using Views = Android.Views;
#elif WINDOWS_APP
using Xamarin.Forms.Platform.WinRT;
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
#elif __ANDROID__
        public Views.View NativeControl
        {
            get { return (Views.View)WeakNativeControl; }
        }
#elif WINDOWS_APP
        public Windows.UI.Xaml.Controls.Canvas NativeControl
        {
            get { return (Windows.UI.Xaml.Controls.Canvas)WeakNativeControl; }
        }
#endif
        public object WeakNativeControl { get; set; }

        public FormsVideoControl(object weakNativeControl)
        {
            WeakNativeControl = weakNativeControl;
        }
    }

#if WINDOWS_APP
    public class FormsVideoControlRenderer : Platform.WinRT.ViewRenderer<View, Windows.UI.Xaml.Controls.Canvas>
#else
    public class FormsVideoControlRenderer : ViewRenderer
#endif
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