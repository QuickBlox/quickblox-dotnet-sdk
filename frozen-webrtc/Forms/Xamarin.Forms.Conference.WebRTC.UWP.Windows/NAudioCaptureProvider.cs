using FM;
using FM.IceLink.WebRTC;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace Xamarin.Forms.Conference.WebRTC
{
    public class NAudioCaptureProvider : AudioCaptureProvider
    {
        static NAudioCaptureProvider()
        {
            MediaFoundationApi.Startup();
        }

        private string Label = null;

        /// <summary>
        /// Initializes the audio capture provider.
        /// </summary>
        /// <param name="captureArgs">The arguments.</param>
        public override void Initialize(AudioCaptureInitializeArgs captureArgs)
        { }

        /// <summary>
        /// Destroys this instance.
        /// </summary>
        public override void Destroy()
        { }

        /// <summary>
        /// Starts the audio capture.
        /// </summary>
        public override bool Start()
        {
            StartWasapi();

            Log.Info("Audio capture started using WASAPI.");

            return true;
        }

        /// <summary>
        /// Stops the audio capture.
        /// </summary>
        public override void Stop()
        {
            StopWasapi();
        }

        /// <summary>
        /// Gets the label of the audio device.
        /// </summary>
        /// <returns></returns>
        public override string GetLabel()
        {
            return Label;
        }

        /// <summary>
        /// Gets the connected device names.
        /// </summary>
        /// <returns></returns>
        public override string[] GetDeviceNames()
        {
            var deviceInfos = GetAudioDeviceInfos();
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

        public static DeviceInformation GetAudioDeviceInfo(int audioDeviceNumber)
        {
            var deviceInfos = GetAudioDeviceInfos();
            if (audioDeviceNumber < deviceInfos.Count)
            {
                return deviceInfos[audioDeviceNumber];
            }
            return null;
        }

        public static DeviceInformationCollection GetAudioDeviceInfos()
        {
            return Task.Factory.StartNew(async () =>
            {
                return await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            }).Result.Result;
        }

        #region WASAPI

        private WasapiCaptureRT WasapiIn = null;
        private Resampler Resampler = null;

        private void StartWasapi()
        {
            var deviceInfos = GetAudioDeviceInfos();
            if (deviceInfos.Count == 0)
            {
                throw new Exception("No audio devices found.");
            }

            DeviceInformation deviceInfo;
            if (DesiredDeviceNumber.HasValue && DesiredDeviceNumber.Value < deviceInfos.Count)
            {
                deviceInfo = deviceInfos[DesiredDeviceNumber.Value];
            }
            else
            {
                deviceInfo = deviceInfos[0];
            }

            Label = deviceInfo.Name;

            WasapiIn = new WasapiCaptureRT(deviceInfo.Id);
            WasapiIn.DataAvailable += new EventHandler<WaveInEventArgs>(WasapiIn_DataAvailable);
            WasapiIn.StartRecording();
        }

        private void StopWasapi()
        {
            if (WasapiIn != null)
            {
                WasapiIn.StopRecording();
                WasapiIn.Dispose();
            }
        }

        private byte[] WasapiInBuffer;
        private byte[] WasapiOutBuffer;

        private void WasapiIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (WasapiInBuffer == null)
            {
                WasapiInBuffer = new byte[48000];
                WasapiOutBuffer = new byte[48000];

                if (DesiredClockRate != WasapiIn.WaveFormat.SampleRate)
                {
                    Resampler = new Resampler(WasapiIn.WaveFormat.SampleRate, DesiredClockRate);
                }
            }

            byte[] inputBuffer;
            int inputLength = 0;
            if (WasapiIn.WaveFormat.BitsPerSample == 32)
            {
                // convert to shorts
                var sampleCount = e.BytesRecorded / 4;
                FloatBytesToShortBytes(e.Buffer, 0, WasapiInBuffer, 0, sampleCount);
                inputBuffer = WasapiInBuffer;
                inputLength = sampleCount * 2;
            }
            else
            {
                inputBuffer = e.Buffer;
                inputLength = e.BytesRecorded;
            }

            var buffer = new AudioBuffer(inputBuffer, 0, inputLength);
            if (Resampler.ResampleAndConvert(buffer, Resampler, WasapiIn.WaveFormat.Channels, DesiredChannels))
            {
                RaiseFrame(buffer);
            }
            else
            {
                Log.Error("Could not resample captured audio.");
            }
        }

        #endregion

        private void FloatBytesToShortBytes(byte[] floatData, int floatIndex, byte[] shortData, int shortIndex, int sampleCount)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var floatValue = SoundUtility.ReadPcmFloat(floatData, floatIndex);
                floatIndex += 4;

                var shortValue = AudioBuffer.ShortFromFloat(floatValue);

                SoundUtility.WritePcmShort(shortValue, shortData, shortIndex);
                shortIndex += 2;
            }
        }
    }
}
