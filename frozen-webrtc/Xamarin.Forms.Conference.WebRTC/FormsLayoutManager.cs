using System;

using FM;
using FM.IceLink;
using FM.IceLink.WebRTC;

namespace Xamarin.Forms.Conference.WebRTC
{
    public class FormsLayoutManager : BaseLayoutManager
    {
        public AbsoluteLayout Container { get; private set; }

        private bool InLayout = false;

        public FormsLayoutManager(AbsoluteLayout container)
            : this(container, null)
        { }

        public FormsLayoutManager(AbsoluteLayout container, LayoutPreset preset)
            : base(preset)
        {
            Container = container;

            Container.LayoutChanged += UpdateLayout;
            Container.SizeChanged += UpdateLayout;
        }

        private void UpdateLayout(object sender, EventArgs e)
        {
            if (!InLayout)
            {
                System.Threading.ThreadPool.QueueUserWorkItem((state) =>
                {
                    Device.BeginInvokeOnMainThread(DoLayout);
                }, null);
            }
        }

        public override void AddToContainer(object control)
        {
            Container.Children.Add((View)control);
        }

        public override void RemoveFromContainer(object control)
        {
            Container.Children.Remove((View)control);
        }

        public override void RunOnUIThread(DoubleAction<object, object> action, object arg1, object arg2)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                action(arg1, arg2);
            });
        }

        public override void ApplyLayout()
        {
            InLayout = true;

            try
            {
                var localVideoControl = (View)GetLocalVideoControl();
                var remoteVideoControls = GetRemoteVideoControls();

                var layoutWidth = (int)Container.Width;
                var layoutHeight = (int)Container.Height;

                // Get the new layout.
                var layout = GetLayout(layoutWidth, layoutHeight, remoteVideoControls.Length);

                // Apply the local video frame.
                if (localVideoControl != null)
                {
                    var localFrame = layout.LocalFrame;
                    AbsoluteLayout.SetLayoutBounds(localVideoControl, new Rectangle(localFrame.X, localFrame.Y, localFrame.Width, localFrame.Height));

                    if (Mode == LayoutMode.FloatLocal)
                    {
                        Container.RaiseChild(localVideoControl);
                    }
                }

                // Apply the remote video frames.
                var remoteFrames = layout.RemoteFrames;
                for (int i = 0; i < remoteFrames.Length; i++)
                {
                    var remoteFrame = remoteFrames[i];
                    var remoteVideoControl = (View)remoteVideoControls[i];
                    AbsoluteLayout.SetLayoutBounds(remoteVideoControl, new Rectangle(remoteFrame.X, remoteFrame.Y, remoteFrame.Width, remoteFrame.Height));

                    if (Mode == LayoutMode.FloatRemote)
                    {
                        Container.RaiseChild(remoteVideoControl);
                    }
                }

                Container.ForceLayout();
            }
            finally
            {
                InLayout = false;
            }
        }
    }
}

