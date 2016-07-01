using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FM;
using FM.IceLink.WebRTC;
using Windows.Phone.Media.Capture;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;
using Windows.Foundation;
using Microsoft.Phone.Controls;
using System.Windows.Threading;
using System.Windows.Shapes;

namespace WindowsPhone.Conference.WebRTC
{
    public class VideoCaptureProvider : FM.IceLink.WebRTC.VideoCaptureProvider
    {
        private PhoneApplicationPage Page;
        private VideoCaptureInitializeArgs CaptureArgs;
        private AudioVideoCaptureDevice Capture;
        private VideoRenderProvider Preview;
        private string Label;
        private byte[] PreviewYuv;
        private int BufferRotate = 0;
        private DispatcherTimer XnaTimer;
        private bool FrameAvailable;

        public VideoCaptureProvider(PhoneApplicationPage page)
        {
            Page = page;
        }

        public override int GetRearDeviceNumber()
        {
            return 0;
        }

        public override int GetFrontDeviceNumber()
        {
            return 1;
        }

        private void SetRotation(PageOrientation orientation)
        {
            if (orientation.HasFlag(PageOrientation.LandscapeLeft))
            {
                BufferRotate = 0;
            }
            else if (orientation.HasFlag(PageOrientation.LandscapeRight))
            {
                BufferRotate = 180;
            }
            else if (orientation.HasFlag(PageOrientation.PortraitUp))
            {
                //The back camera renders upsidedown so use a different rotate
                if (Capture != null && Capture.SensorLocation == CameraSensorLocation.Front)
                {
                    BufferRotate = 270;
                }
                else if (Capture != null && Capture.SensorLocation == CameraSensorLocation.Back)
                {
                    BufferRotate = 90;
                }
                else
                {
                    BufferRotate = 270;
                }
            }
            else if (orientation.HasFlag(PageOrientation.PortraitDown))
            {
                //The back camera renders upsidedown so use a different rotate
                if (Capture != null && Capture.SensorLocation == CameraSensorLocation.Front)
                {
                    BufferRotate = 90;
                }
                else if (Capture != null && Capture.SensorLocation == CameraSensorLocation.Back)
                {
                    BufferRotate = 270;
                }
                else
                {
                    BufferRotate = 90;
                }
            }
            if (Preview != null)
            {
                Preview.Rotate(BufferRotate);
            }
        }

        public override void Initialize(VideoCaptureInitializeArgs captureArgs)
        {
            CaptureArgs = captureArgs;
            Preview = new VideoRenderProvider();
            RunOnUIThread(() =>
            {
                Page.OrientationChanged += Page_OrientationChanged;
                SetRotation(Page.Orientation);
            });
        }

        void Page_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            SetRotation(e.Orientation);
        }

        public override bool Start()
        {
            StartInternal();
            return true;
        }

        private async void StartInternal()
        {
            var locations = GetCameraSensorLocations();
            var location = locations[0];
            if (CaptureArgs.DeviceNumber.HasValue && CaptureArgs.DeviceNumber.Value < locations.Length)
            {
                location = locations[CaptureArgs.DeviceNumber.Value];
            }
            else if (DesiredDeviceNumber.HasValue && DesiredDeviceNumber.Value < locations.Length)
            {
                location = locations[DesiredDeviceNumber.Value];
                DeviceNumber = DesiredDeviceNumber.Value;
            }

            Label = GetCameraSensorLocationName(location);

            Capture = await AudioVideoCaptureDevice.OpenForVideoOnlyAsync(location, GetInitialResolution(location, CaptureArgs.Width, CaptureArgs.Height));
            Capture.SetProperty(KnownCameraAudioVideoProperties.VideoFrameRate, CaptureArgs.FrameRate);
           
            Capture.PreviewFrameAvailable += Capture_PreviewFrameAvailable;
            if (Preview != null)
            {
                Preview.LoadCamera(Capture);
                RunOnUIThread(() =>
                {
                    SetRotation(Page.Orientation);
                    Preview.Rotate(BufferRotate);
                });
            }

            RunOnUIThread(() =>
            {
                XnaTimer = new DispatcherTimer();
                XnaTimer.Interval = TimeSpan.FromMilliseconds(10);
                XnaTimer.Tick += XnaTimer_Tick;
                XnaTimer.Start();
            });
        }

        void XnaTimer_Tick(object sender, EventArgs e)
        {
            if (Capture != null && FrameAvailable)
            {
                var width = (int)Capture.PreviewResolution.Width;
                var height = (int)Capture.PreviewResolution.Height;
                var byteCount = (int)(width * height * 1.5);
                if (PreviewYuv == null || PreviewYuv.Length != byteCount)
                {
                    PreviewYuv = new byte[byteCount];
                }
                Capture.GetPreviewBufferYCbCr(PreviewYuv);
                
                RaiseFrame(new VideoBuffer(width, height, new VideoPlane(PreviewYuv))
                {
                    Rotate = BufferRotate
                });
                FrameAvailable = false;

                //if (Preview != null)
                //{
                //    Rectangle control = (Rectangle)Preview.GetControl();
                //    FM.Log.Info("Preview W: " + control.Width + " H: " + control.Height + " Visible: " + control.Visibility + " Top: " + Canvas.GetTop(control) + " Left: " + Canvas.GetLeft(control));
                //}
            }
        }

        private void Capture_PreviewFrameAvailable(ICameraCaptureDevice camera, object e)
        {
            FrameAvailable = true;
        }

        public override void Stop()
        {
            RunOnUIThread(() =>
            {
                if (Preview != null)
                {
                    Preview.UnloadCamera();
                }
                Capture.PreviewFrameAvailable -= Capture_PreviewFrameAvailable;
                Capture.Dispose();
                FrameAvailable = false;
            });
        }

        public override void Destroy()
        {
            XnaTimer.Stop();
            RunOnUIThread(() =>
            {
                Page.OrientationChanged -= Page_OrientationChanged;
            });
        }

        public override string[] GetDeviceNames()
        {
            return GetCameraSensorLocations().Select(x => GetCameraSensorLocationName(x)).ToArray();
        }

        public override string GetLabel()
        {
            return Label;
        }

        public override object GetPreviewControl()
        {
            return Preview.GetControl();
        }

        private static string GetCameraSensorLocationName(CameraSensorLocation location)
        {
            return Enum.GetName(location.GetType(), location);
        }

        private static CameraSensorLocation[] GetCameraSensorLocations()
        {
            return AudioVideoCaptureDevice.AvailableSensorLocations.ToArray();
        }

        private static Size GetInitialResolution(CameraSensorLocation location, int videoWidth, int videoHeight)
        {
            var resolutions = AudioVideoCaptureDevice.GetAvailablePreviewResolutions(location);

            // find the closest resolution
            if (videoWidth > 0 || videoHeight > 0)
            {
                var closest = new List<Size>();

                var distance = -1;
                foreach (var res in resolutions)
                {
                    if (distance < 0)
                    {
                        closest.Add(res);
                        distance = GetSizeDistance(res, videoWidth, videoHeight);
                    }
                    else
                    {
                        var d = GetSizeDistance(res, videoWidth, videoHeight);
                        if (d == distance)
                        {
                            closest.Add(res);
                        }
                        else if (d < distance)
                        {
                            closest = new List<Size>();
                            closest.Add(res);
                            distance = d;
                        }
                    }
                }

                resolutions = closest;
            }

            if (resolutions.Count == 0)
            {
                var error = "The camera does not support any resolutions.";
                Log.Error(error);
                throw new Exception(error);
            }

            return resolutions[0];
        }

        private static int GetSizeDistance(Size size, int width, int height)
        {
            if (width > 0 && height > 0)
            {
                return Math.Abs(width - (int)size.Width) + Math.Abs(height - (int)size.Height);
            }
            else if (width <= 0)
            {
                return Math.Abs(height - (int)size.Height);
            }
            else if (height <= 0)
            {
                return Math.Abs(width - (int)size.Width);
            }
            return -1;
        }

        private static void RunOnUIThread(Action action)
        {
            var dispatcher = System.Windows.Deployment.Current.Dispatcher;
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