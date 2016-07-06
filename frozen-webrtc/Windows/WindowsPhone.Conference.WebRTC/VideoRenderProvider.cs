using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FM.IceLink.WebRTC;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Phone.Media.Capture;
using Microsoft.Devices;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;

namespace WindowsPhone.Conference.WebRTC
{
    public class VideoRenderProvider : FM.IceLink.WebRTC.VideoRenderProvider
    {
        private Rectangle Rectangle;
        private VideoBrush VideoBrush;

        public VideoRenderProvider()
        {
            RunOnUIThread(() =>
            {
                Rectangle = new Rectangle();
                Rectangle.Stretch = Stretch.Fill;
                Rectangle.HorizontalAlignment = HorizontalAlignment.Stretch;
                Rectangle.VerticalAlignment = VerticalAlignment.Stretch;
            });
        }

        public override void Initialize(VideoRenderInitializeArgs renderArgs)
        { }

        public override void Render(VideoBuffer frame)
        {
        }

        public void LoadCamera(ICameraCaptureDevice camera)
        {
            if (VideoBrush == null && camera != null)
            {
                RunOnUIThread(() =>
                {
                    VideoBrush = new VideoBrush();
                    VideoBrush.Stretch = Stretch.Uniform;
                    VideoBrush.SetSource(camera);
                    Rectangle.Fill = VideoBrush;
                });
            }
        }

        public void UnloadCamera()
        {
            if (VideoBrush != null)
            {
                RunOnUIThread(() =>
                {
                    Rectangle.Fill = null;
                    Rectangle = null;
                    VideoBrush = null;
                });
            }
        }

        public void Rotate(int rotate)
        {
            RunOnUIThread(() =>
            {
                RotateTransform rotateTransform = new RotateTransform();
                rotateTransform.Angle = rotate;
                rotateTransform.CenterX = Rectangle.Width / 2;
                rotateTransform.CenterY = Rectangle.Height / 2;
                if (Rectangle.RenderTransform != rotateTransform)
                {
                    Rectangle.RenderTransform = rotateTransform;
                }
            });
        }

        public override void Destroy()
        { }

        public override object GetControl()
        {
            return Rectangle;
        }

        private static void RunOnUIThread(Action action)
        {
            var dispatcher = Deployment.Current.Dispatcher;
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                var wait = new AutoResetEvent(false);
                dispatcher.BeginInvoke(() =>
                {
                    action();
                    wait.Set();
                });
                wait.WaitOne();
            }
        }
    }
}
