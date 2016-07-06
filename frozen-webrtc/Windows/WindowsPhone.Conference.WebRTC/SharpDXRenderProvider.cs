using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FM.IceLink.WebRTC;
using Windows.Phone.Media.Capture;

using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Direct3D11;

namespace WindowsPhone.Conference.WebRTC
{
    using SharpDX.Toolkit.Graphics;

    public class SharpDXRenderProvider : Game
    {
        private GraphicsDeviceManager GraphicsDeviceManager;
        private RenderTarget2D RenderTarget2D;
        private SpriteBatch SpriteBatch;
        private Rectangle Rectangle;

        public SharpDXRenderProvider()
        {
            GraphicsDeviceManager = new GraphicsDeviceManager(this);

            //no vsync
            GraphicsDeviceManager.SynchronizeWithVerticalRetrace = false;
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Rectangle = new Rectangle(0, 0, (int)GraphicsDevice.Viewport.Width, (int)GraphicsDevice.Viewport.Height);
            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        protected override void BeginRun()
        {
            base.BeginRun();
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        public void BindRectangleToSurface(VideoBuffer frame, int width, int height)
        {
            if (Rectangle.Width != width)
            {
                Rectangle.Width = width;
                Rectangle.Height = (int)(((float)frame.Height / (float)frame.Width) * width);
            }
        }

        public void Render(VideoBuffer videoBuffer)
        {
            if(GraphicsDevice == null)
            {
                return;
            }

            if (RenderTarget2D == null)
            {
                RenderTarget2D = RenderTarget2D.New(GraphicsDevice, videoBuffer.Width, videoBuffer.Height, PixelFormat.B8G8R8A8.UNorm);
            }
            else if (RenderTarget2D != null && (RenderTarget2D.Width != videoBuffer.Width || RenderTarget2D.Height != videoBuffer.Height))
            {
                RenderTarget2D.Dispose();
                RenderTarget2D = null;
                RenderTarget2D = RenderTarget2D.New(GraphicsDevice, videoBuffer.Width, videoBuffer.Height, PixelFormat.B8G8R8A8.UNorm);
            }

            if (RenderTarget2D != null)
            {
                var x = 0;
                var data = videoBuffer.Plane.Data;
                var argb = new int[videoBuffer.Width * videoBuffer.Height];
                for (var i = 0; i < argb.Length; i++)
                {
                    var r = data[x++];
                    var g = data[x++];
                    var b = data[x++];
                    var a = data[x++];
                    argb[i] = ((a & 0xFF) << 24) |
                              ((r & 0xFF) << 16) |
                              ((g & 0xFF) << 8) |
                              ((b & 0xFF) << 0);
                }
                
                RenderTarget2D.SetData<int>(argb);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            if (RenderTarget2D != null)
            {
                GraphicsDevice.Clear(Color.Black);

                SpriteBatch.Begin(SpriteSortMode.Deferred, GraphicsDevice.BlendStates.NonPremultiplied);
                SpriteBatch.Draw(RenderTarget2D, Rectangle, Color.White);
                SpriteBatch.End();
                base.Draw(gameTime);
            }            
        }
    }
}
