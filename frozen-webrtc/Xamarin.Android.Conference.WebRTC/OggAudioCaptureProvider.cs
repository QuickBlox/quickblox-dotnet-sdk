using System;
using FM.IceLink.WebRTC;

using Android.Content;

namespace Xamarin.Android.Conference.WebRTC
{
    /// <summary>
    /// An Ogg audio capture provider.
    /// </summary>
    public class OggAudioCaptureProvider : AndroidAudioCaptureProvider
    {
        private AudioCodec _Codec;
        private OggAudioRecorder _Recorder;

        /// <summary>
        /// Initializes a new instance of the <see cref="Xamarin.iOS.Conference.WebRTC.OggAudioCaptureProvider"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="codec">The codec.</param>
        public OggAudioCaptureProvider(string path, AudioCodec codec)
            : this(DefaultProviders.AndroidContext, path, codec)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Xamarin.iOS.Conference.WebRTC.OggAudioCaptureProvider"/> class.
        /// </summary>
        /// <param name="context">The Android context.</param>
        /// <param name="path">The path.</param>
        /// <param name="codec">The codec.</param>
        public OggAudioCaptureProvider(Context context, string path, AudioCodec codec)
            : base(context)
        {
            if (!codec.Initialized)
            {
                throw new Exception("Codec must be initialized first (AudioCodec.Initialize).");
            }

            _Codec = codec;
            _Recorder = new OggAudioRecorder(path, _Codec.EncodingName, _Codec.ClockRate, _Codec.Channels);
        }

        /// <summary>
        /// Initializes the audio capture provider.
        /// </summary>
        /// <param name="captureArgs">The arguments.</param>
        public override void Initialize(AudioCaptureInitializeArgs captureArgs)
        {
            _Recorder.Open();
            base.Initialize(captureArgs);
        }

        /// <summary>
        /// Destroys the audio capture provider.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();
            _Codec.Destroy();
            _Recorder.Close();
        }

        /// <summary>
        /// Raises a captured audio buffer for processing to specific peers.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="peerIds">Peer identifiers.</param>
        protected override void RaiseFrame(AudioBuffer buffer, string[] peerIds)
        {
            // encode
            var encodedFrames = buffer.Encode(_Codec);
            foreach (var encodedFrame in encodedFrames)
            {
                // record
                _Recorder.Write(encodedFrame);

                // send to peers (if any)
                base.RaiseFrame(new AudioBuffer(encodedFrame)
                {
                    Encoded = true
                }, peerIds);
            }
        }
    }
}

