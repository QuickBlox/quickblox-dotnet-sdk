using System;
using FM.IceLink.WebRTC;

namespace Xamarin.iOS.Conference.WebRTC
{
    /// <summary>
    /// An IVF audio capture provider.
    /// </summary>
    public class IvfVideoCaptureProvider : AVCaptureProvider
    {
        private VideoCodec _Codec;
        private IvfVideoRecorder _Recorder;

        /// <summary>
        /// Initializes a new instance of the <see cref="Xamarin.iOS.Conference.WebRTC.IvfVideoCaptureProvider"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="codec">The codec.</param>
        public IvfVideoCaptureProvider(string path, VideoCodec codec)
        {
            if (!codec.Initialized)
            {
                throw new Exception("Codec must be initialized first (VideoCodec.Initialize).");
            }

            _Codec = codec;
            _Recorder = new IvfVideoRecorder(path, _Codec.EncodingName, _Codec.ClockRate);
        }

        /// <summary>
        /// Initializes the video capture provider.
        /// </summary>
        /// <param name="captureArgs">The arguments.</param>
        public override void Initialize(VideoCaptureInitializeArgs captureArgs)
        {
            _Recorder.Open();
            base.Initialize(captureArgs);
        }

        /// <summary>
        /// Destroys the video capture provider.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();
            _Codec.Destroy();
            _Recorder.Close();
        }

        /// <summary>
        /// Raises a captured video buffer for processing to specific peers.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="peerIds">Peer identifiers.</param>
        protected override void RaiseFrame(VideoBuffer buffer, string[] peerIds)
        {
            // encode
            var encodedFrame = buffer.Encode(_Codec);

            // record
            _Recorder.Write(encodedFrame, buffer.Width, buffer.Height);

            // send to peers (if any)
            base.RaiseFrame(new VideoBuffer(buffer.Width, buffer.Height, new VideoPlane(encodedFrame))
            {
                Encoded = true
            }, peerIds);
        }
    }
}

