using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FM.IceLink.WebRTC;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Threading;
using Windows.Phone.Media.Capture;

using SharpDX;
using SharpDX.Toolkit;

namespace WindowsPhone.Conference.WebRTC
{
    public class Direct3DRenderProvider : FM.IceLink.WebRTC.VideoRenderProvider
    {
        DrawingSurface DrawingSurface;
        SharpDXRenderProvider SharpDXRenderProvider;
        
        public Direct3DRenderProvider()
        {
            RunOnUIThread(() =>
            {
                DrawingSurface = new System.Windows.Controls.DrawingSurface();
                DrawingSurface.HorizontalAlignment = HorizontalAlignment.Stretch;
                DrawingSurface.VerticalAlignment = VerticalAlignment.Stretch;
                SharpDXRenderProvider = new SharpDXRenderProvider();
                SharpDXRenderProvider.Run(DrawingSurface);
            });
        }

        public override void Initialize(VideoRenderInitializeArgs renderArgs)
        { }

        public void Rotate(int rotate)
        {
            //RunOnUIThread(() =>
            //{
            //    RotateTransform rotateTransform = new RotateTransform();
            //    rotateTransform.Angle = rotate;
            //    rotateTransform.CenterX = DrawingSurface.Width / 2;
            //    rotateTransform.CenterY = DrawingSurface.Height / 2;
            //    if (DrawingSurface.RenderTransform != rotateTransform)
            //    {
            //        DrawingSurface.RenderTransform = rotateTransform;
            //    }
            //});
        }

        public override void Render(VideoBuffer frame)
        {
            RunOnUIThread(() =>
            {
                //if (DrawingSurface.Width != frame.Width || DrawingSurface.Height != frame.Height)
                //{
                //    DrawingSurface.Width = frame.Width;
                //    DrawingSurface.Height = frame.Height;
                //}
                SharpDXRenderProvider.BindRectangleToSurface(frame, (int)DrawingSurface.Width, (int)DrawingSurface.Height);
                SharpDXRenderProvider.Render(frame);
            });
        }

        public override object GetControl()
        {
            return DrawingSurface;
        }

        public override void Destroy()
        { }

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
