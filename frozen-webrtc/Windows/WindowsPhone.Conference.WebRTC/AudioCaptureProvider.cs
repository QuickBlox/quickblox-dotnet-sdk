using FM;
using FM.IceLink.WebRTC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace WindowsPhone.Conference.WebRTC
{
    class AudioCaptureProvider : FM.IceLink.WebRTC.AudioCaptureProvider
    {
        private Microphone Microphone;
        private Resampler Resampler;
        private DispatcherTimer XnaTimer;

        private byte[] OutputBuffer = new byte[16384];

        public override void Initialize(AudioCaptureInitializeArgs captureArgs)
        {
            Microphone = Microphone.Default;
            Microphone.BufferDuration = TimeSpan.FromMilliseconds(100); // 100 minimum
            Microphone.BufferReady += Microphone_BufferReady;

            Resampler = new Resampler(DesiredClockRate / 16000);

            RunOnUIThread(() =>
            {
                XnaTimer = new DispatcherTimer();
                XnaTimer.Interval = TimeSpan.FromMilliseconds(50);
                XnaTimer.Tick += delegate { try { FrameworkDispatcher.Update(); } catch { } };
                XnaTimer.Start();
            });
        }

        public override bool Start()
        {
            Microphone.Start();
            return true;
        }

        void Microphone_BufferReady(object sender, EventArgs e)
        {
            var outputLength = Microphone.GetData(OutputBuffer);

            var frame = new AudioBuffer(OutputBuffer, 0, outputLength);
            if (!Resampler.Resample(frame, true))
            {
                Log.Error("Could not resample XNA audio.");
            }

            if (!frame.ConvertMonoToStereo())
            {
                Log.Error("Could not convert XNA audio to stereo.");
            }

            RaiseFrame(frame);
        }

        public override void Stop()
        {
            Microphone.Stop();
        }

        public override void Destroy()
        {
            XnaTimer.Stop();
        }

        public override string[] GetDeviceNames()
        {
            return new[] { GetLabel() };
        }

        public override string GetLabel()
        {
            return "Microphone";
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
