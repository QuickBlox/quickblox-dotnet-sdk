using FM;
using FM.IceLink.WebRTC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml.Controls;

namespace Windows8.Conference.WebRTC
{
    public class VideoCaptureProvider : FM.IceLink.WebRTC.VideoCaptureProvider
    {
        private CaptureElement Preview = null;
        private MediaCapture Capture = null;
        private string Label = null;

        public override void Initialize(VideoCaptureInitializeArgs captureArgs)
        {
            Preview = new CaptureElement();
        }

        private int GetSizeDistance(VideoEncodingProperties vep)
        {
            if (DesiredWidth > 0 && DesiredHeight > 0)
            {
                return Math.Abs(DesiredWidth - (int)vep.Width) + Math.Abs(DesiredHeight - (int)vep.Height);
            }
            else if (DesiredWidth <= 0)
            {
                return Math.Abs(DesiredHeight - (int)vep.Height);
            }
            else if (DesiredHeight <= 0)
            {
                return Math.Abs(DesiredWidth - (int)vep.Width);
            }
            return -1;
        }

        private int GetFrameRateDistance(VideoEncodingProperties vep)
        {
            if (DesiredFrameRate > 0)
            {
                var frameRate = vep.FrameRate.Numerator / vep.FrameRate.Denominator;
                return Math.Abs(DesiredFrameRate - (int)frameRate);
            }
            return -1;
        }

        public override bool Start()
        {
            Capture = new MediaCapture();

            var settings = new MediaCaptureInitializationSettings
            {
                MediaCategory = MediaCategory.Communications,
                PhotoCaptureSource = PhotoCaptureSource.VideoPreview,
                StreamingCaptureMode = StreamingCaptureMode.Video
            };

            DeviceInformation videoDeviceInfo = null;
            if (DesiredDeviceNumber.HasValue)
            {
                videoDeviceInfo = GetVideoDeviceInfo(DesiredDeviceNumber.Value);
                DeviceNumber = DesiredDeviceNumber.Value;
            }
            
            if (videoDeviceInfo == null)
            {
                var videoDeviceInfos = GetVideoDeviceInfos();
                if (videoDeviceInfos.Count > 0)
                {
                    videoDeviceInfo = videoDeviceInfos[0];
                }
                DeviceNumber = 0;
            }

            if (videoDeviceInfo != null)
            {
                Label = videoDeviceInfo.Name;
                settings.VideoDeviceId = videoDeviceInfo.Id;
            }
            else
            {
                Label = "Unknown Video Device";
            }

            StartAsync(settings);
            return true;
        }

        private async void StartAsync(MediaCaptureInitializationSettings settings)
        {
            await Capture.InitializeAsync(settings);

            var veps = Capture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).Where(x => x is VideoEncodingProperties).Cast<VideoEncodingProperties>().ToList();
            if (veps.Count > 0)
            {
                // find the closest resolution
                if (DesiredWidth > 0 || DesiredHeight > 0)
                {
                    var closest = new List<VideoEncodingProperties>();

                    var distance = -1;
                    foreach (var vep in veps)
                    {
                        if (distance < 0)
                        {
                            closest.Add(vep);
                            distance = GetSizeDistance(vep);
                        }
                        else
                        {
                            var d = GetSizeDistance(vep);
                            if (d == distance)
                            {
                                closest.Add(vep);
                            }
                            else if (d < distance)
                            {
                                closest = new List<VideoEncodingProperties>();
                                closest.Add(vep);
                                distance = d;
                            }
                        }
                    }

                    veps = closest;
                }

                // find the closest frame rate
                if (DesiredFrameRate > 0)
                {
                    var closest = new List<VideoEncodingProperties>();

                    var distance = -1;
                    foreach (var vep in veps)
                    {
                        if (distance < 0)
                        {
                            closest.Add(vep);
                            distance = GetFrameRateDistance(vep);
                        }
                        else
                        {
                            var d = GetFrameRateDistance(vep);
                            if (d == distance)
                            {
                                closest.Add(vep);
                            }
                            else if (d < distance)
                            {
                                closest = new List<VideoEncodingProperties>();
                                closest.Add(vep);
                                distance = d;
                            }
                        }
                    }

                    veps = closest;
                }

                Log.DebugFormat("Found {0} matching video profile(s).", veps.Count.ToString());

                if (veps.Count > 0)
                {
                    var vep = veps[0];
                    Log.DebugFormat("Using ({0} x {1}, {2} fps, {3}) video profile.", vep.Width.ToString(), vep.Height.ToString(), (vep.FrameRate.Numerator / vep.FrameRate.Denominator).ToString(), vep.Subtype);

                    // Set properties.
                    await Capture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, vep);
                }
            }

            Preview.Source = Capture;

            var properties = new PropertySet();
            properties.MapChanged += PropertiesChanges;

            await Capture.AddEffectAsync(MediaStreamType.VideoPreview, "VideoCaptureTransform.CaptureEffect", properties);
            await Capture.StartPreviewAsync();
        }

        private void PropertiesChanges(IObservableMap<string, object> sender, IMapChangedEventArgs<string> @event)
        {
            if (!IsMuted)
            {
                if (@event.Key == "videoData")
                {
                    object dataObj = null;
                    if (sender.TryGetValue("videoData", out dataObj))
                    {
                        var width = (int)sender["videoWidth"];
                        var height = (int)sender["videoHeight"];
                        var stride = (int)sender["videoStride"];
                        var data = (byte[])dataObj;

                        RaiseFrame(new VideoBuffer(width, height, new VideoPlane((byte[])data, stride)));
                    }
                }
            }
        }

        public override void Stop()
        {
            StopAsync();
        }

        private async void StopAsync()
        {
            await Capture.StopPreviewAsync();
        }

        public override void Destroy()
        { }

        public override string GetLabel()
        {
            return Label;
        }

        public override object GetPreviewControl()
        {
            return Preview;
        }

        public override string[] GetDeviceNames()
        {
            var deviceInfos = GetVideoDeviceInfos();
            var deviceNames = new string[deviceInfos.Count];
            for (var i = 0; i < deviceInfos.Count; i++)
            {
                var deviceInfo = deviceInfos[i];
                var deviceName = deviceInfo.Name;
                var location = deviceInfo.EnclosureLocation;
                if (location != null)
                {
                    if (location.Panel == Windows.Devices.Enumeration.Panel.Front)
                    {
                        deviceName += " (Front)";
                    }
                    else if (location.Panel == Windows.Devices.Enumeration.Panel.Back)
                    {
                        deviceName += " (Back)";
                    }
                }
                deviceNames[i] = deviceName;
            }
            return deviceNames;
        }

        public static DeviceInformation GetVideoDeviceInfo(int videoDeviceNumber)
        {
            var deviceInfos = GetVideoDeviceInfos();
            if (videoDeviceNumber < deviceInfos.Count)
            {
                return deviceInfos[videoDeviceNumber];
            }
            return null;
        }

        public static DeviceInformationCollection GetVideoDeviceInfos()
        {
            return Task.Factory.StartNew(async () =>
            {
                return await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            }).Result.Result;
        }

        /// <summary>
        /// Gets the front device number.
        /// </summary>
        /// <returns></returns>
        public override int GetFrontDeviceNumber()
        {
            var deviceInfos = GetVideoDeviceInfos();
            for (var i = 0; i < deviceInfos.Count; i++)
            {
                var location = deviceInfos[i].EnclosureLocation;
                if (location != null)
                {
                    if (location.Panel == Windows.Devices.Enumeration.Panel.Front)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the rear device number.
        /// </summary>
        /// <returns></returns>
        public override int GetRearDeviceNumber()
        {
            var deviceInfos = GetVideoDeviceInfos();
            for (var i = 0; i < deviceInfos.Count; i++)
            {
                var location = deviceInfos[i].EnclosureLocation;
                if (location != null)
                {
                    if (location.Panel == Windows.Devices.Enumeration.Panel.Back)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }
    }
}
