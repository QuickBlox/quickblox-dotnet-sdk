using System;
using FM;
using FM.IceLink.WebRTC;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Win8.Wave.WaveOutputs;

namespace Xamarin.Forms.Conference.WebRTC
{
    class NAudioRenderProvider : AudioRenderProvider
    {
        private BufferedWaveProvider WaveProvider = null;

        /// <summary>
        /// Initializes the audio render provider.
        /// </summary>
        /// <param name="renderArgs">The arguments.</param>
        public override void Initialize(AudioRenderInitializeArgs renderArgs)
        {
            WaveProvider = new BufferedWaveProvider(new WaveFormat(ClockRate, 16, Channels));
            WaveProvider.DiscardOnBufferOverflow = true;
            WaveProvider.BufferDuration = new TimeSpan(0, 0, 0, 0, 250);

            InitializeWasapi();

            Log.Info("Audio render initialized using WASAPI.");
        }

        /// <summary>
        /// Plays back an audio frame.
        /// </summary>
        /// <param name="buffer">The frame.</param>
        public override void Render(AudioBuffer buffer)
        {
            WaveProvider.AddSamples(buffer.Data, buffer.Index, buffer.Length);
        }

        /// <summary>
        /// Destroys this instance. No additional rendering will take place.
        /// </summary>
        public override void Destroy()
        {
            DestroyWasapi();
        }

        #region WASAPI

        private WasapiOutRT WasapiOut = null;

        private void InitializeWasapi()
        {
            WasapiOut = new WasapiOutRT(AudioClientShareMode.Shared, 100);
            WasapiOut.Init(WaveProvider);
            WasapiOut.Play();
        }

        private void DestroyWasapi()
        {
            if (WasapiOut != null)
            {
                WasapiOut.Stop();
                WasapiOut.Dispose();
            }
        }

        #endregion
    }
}
