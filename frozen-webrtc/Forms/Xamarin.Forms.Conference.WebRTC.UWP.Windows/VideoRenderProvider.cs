using FM;
using FM.IceLink.WebRTC;
using System;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Xamarin.Forms.Conference.WebRTC
{
    public class VideoRenderProvider : FM.IceLink.WebRTC.VideoRenderProvider
    {
        private Win8ContextMenu ContextMenu;
        private Windows.UI.Xaml.Controls.Grid Grid;
        private Windows.UI.Xaml.Controls.Image Image;
        private WriteableBitmap Bitmap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageVideoRenderProvider"/> class.
        /// </summary>
        public VideoRenderProvider()
            : this(false)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageVideoRenderProvider"/> class.
        /// </summary>
        /// <param name="scale">The scaling algorithm to use.</param>
        public VideoRenderProvider(LayoutScale scale)
            : this(false, scale)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageVideoRenderProvider"/> class.
        /// </summary>
        /// <param name="disableContextMenu">Whether or not to disable the context menu.</param>
        public VideoRenderProvider(bool disableContextMenu)
            : this(disableContextMenu, LayoutScale.Contain)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageVideoRenderProvider"/> class.
        /// </summary>
        /// <param name="disableContextMenu">Whether or not to disable the context menu.</param>
        /// <param name="scale">The scaling algorithm to use.</param>
        public VideoRenderProvider(bool disableContextMenu, LayoutScale scale)
        {
            Win8LayoutManager.SafeInvoke(() =>
            {
                Image = new Windows.UI.Xaml.Controls.Image();
                Initialize(disableContextMenu, scale);
            }, true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageVideoRenderProvider"/> class.
        /// </summary>
        /// <param name="image">The Image to target.</param>
        public VideoRenderProvider(Windows.UI.Xaml.Controls.Image image)
            : this(image, false)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageVideoRenderProvider"/> class.
        /// </summary>
        /// <param name="image">The Image to target.</param>
        /// <param name="scale">The scaling algorithm to use.</param>
        public VideoRenderProvider(Windows.UI.Xaml.Controls.Image image, LayoutScale scale)
            : this(image, false, scale)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageVideoRenderProvider"/> class.
        /// </summary>
        /// <param name="image">The Image to target.</param>
        /// <param name="disableContextMenu">Whether or not to disable the context menu.</param>
        public VideoRenderProvider(Windows.UI.Xaml.Controls.Image image, bool disableContextMenu)
            : this(image, disableContextMenu, LayoutScale.Contain)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageVideoRenderProvider"/> class.
        /// </summary>
        /// <param name="image">The Image to target.</param>
        /// <param name="disableContextMenu">Whether or not to disable the context menu.</param>
        /// <param name="scale">The scaling algorithm to use.</param>
        public VideoRenderProvider(Windows.UI.Xaml.Controls.Image image, bool disableContextMenu, LayoutScale scale)
        {
            Win8LayoutManager.SafeInvoke(() =>
            {
                Image = image;
                Initialize(disableContextMenu, scale);
            }, true);
        }

        private void Initialize(bool disableContextMenu, LayoutScale scale)
        {
            Image.HorizontalAlignment = HorizontalAlignment.Center;
            Image.VerticalAlignment = VerticalAlignment.Center;
            if (scale == LayoutScale.Contain)
            {
                Image.Stretch = Stretch.Uniform;
            }
            else if (scale == LayoutScale.Cover)
            {
                Image.Stretch = Stretch.UniformToFill;
            }
            else
            {
                Image.Stretch = Stretch.Fill;
            }

            Bitmap = new WriteableBitmap(1, 1);
            Image.Source = Bitmap;

            Grid = new Windows.UI.Xaml.Controls.Grid();
            Grid.Children.Add(Image);

            if (!disableContextMenu)
            {
                ContextMenu = new Win8ContextMenu();
            }
        }

        public override void Initialize(VideoRenderInitializeArgs renderArgs)
        {
            Win8LayoutManager.SafeInvoke(() =>
            {
                if (ContextMenu != null)
                {
                    var mediaStream = renderArgs.RemoteStream == null ? renderArgs.LocalStream : renderArgs.RemoteStream;
                    ContextMenu.Attach(Image, mediaStream);
                }
            }, true);
        }

        public override void Destroy()
        {
            Win8LayoutManager.SafeInvoke(() =>
            {
                if (ContextMenu != null)
                {
                    ContextMenu.Detach();
                }
            }, true);
        }

        private bool Rendering = false;
        public override async void Render(VideoBuffer frame)
        {
            if (!Rendering)
            {
                Rendering = true;
                var stream = new InMemoryRandomAccessStream();
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore, (uint)frame.Width, (uint)frame.Height, 96, 96, frame.Plane.Data);
                await encoder.FlushAsync();
                stream.Seek(0);

                Win8LayoutManager.SafeInvoke(async () =>
                {
                    await Bitmap.SetSourceAsync(stream);
                    Rendering = false;
                });
            }
        }

        public override object GetControl()
        {
            return Grid;
        }
    }
}
