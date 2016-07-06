using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Foundation;
using ObjCRuntime;

using FM;
using FM.IceLink;
using FM.IceLink.WebRTC;

using Xamarin.iOS.VP8;

namespace Xamarin.Forms.Conference.WebRTC.iOS.VP8
{
    /// <summary>
    /// VP8 codec wrapper for Xamarin.iOS.
    /// </summary>
    class Vp8Codec : VideoCodec
    {
        private CocoaVp8Encoder _Encoder;
        private CocoaVp8Decoder _Decoder;
        private Vp8Padep _Padep;

        /// <summary>
        /// Initializes a new instance of the <see cref="Vp8Codec"/> class.
        /// </summary>
        public Vp8Codec()
		{
            _Padep = new Vp8Padep();
        }

        /// <summary>
        /// Encodes a frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <returns></returns>
        public override byte[] Encode(VideoBuffer frame)
		{
			if (_Encoder == null)
			{
				_Encoder = new CocoaVp8Encoder();
                _Encoder.Quality = 0.5;
                _Encoder.Bitrate = 320;
                //_Encoder.Scale = 1.0;
			}

            if (frame.ResetKeyFrame)
            {
                _Encoder.SendKeyframe();
            }

			using (var pool = new NSAutoreleasePool())
			{
				VideoPlane planeY = frame.Planes[0];
				VideoPlane planeU = frame.Planes[1];
				VideoPlane planeV = frame.Planes[2];

				GCHandle planeYDataHandle = GCHandle.Alloc(planeY.Data, GCHandleType.Pinned);
				GCHandle planeUDataHandle = GCHandle.Alloc(planeU.Data, GCHandleType.Pinned);
				GCHandle planeVDataHandle = GCHandle.Alloc(planeV.Data, GCHandleType.Pinned);

				try
				{
					IntPtr planeYDataPointer = planeYDataHandle.AddrOfPinnedObject();
					IntPtr planeUDataPointer = planeUDataHandle.AddrOfPinnedObject();
					IntPtr planeVDataPointer = planeVDataHandle.AddrOfPinnedObject();

					//TODO: index/length
					using (var buffer = new CocoaVp8Buffer {
						PlaneY = NSData.FromBytesNoCopy(planeYDataPointer, (uint)planeY.Data.Length, false),
						PlaneU = NSData.FromBytesNoCopy(planeUDataPointer, (uint)planeU.Data.Length, false),
						PlaneV = NSData.FromBytesNoCopy(planeVDataPointer, (uint)planeV.Data.Length, false),
						StrideY = planeY.Stride,
						StrideU = planeU.Stride,
						StrideV = planeV.Stride,
						Width = frame.Width,
						Height = frame.Height
					})
					{
						using (var encodedFrame = new NSMutableData())
						{
							if (_Encoder.EncodeBuffer(buffer, encodedFrame))
							{
								return encodedFrame.ToArray();
							}
							return null;
						}
					}
				}
				finally
				{
					planeYDataHandle.Free();
					planeUDataHandle.Free();
					planeVDataHandle.Free();
				}
			}
		}

        /// <summary>
        /// Decodes an encoded frame.
        /// </summary>
        /// <param name="encodedFrame">The encoded frame.</param>
        /// <returns></returns>
        public override VideoBuffer Decode(byte[] encodedFrame)
		{
			if (_Decoder == null)
			{
				_Decoder = new CocoaVp8Decoder();
			}

			if (_Padep.SequenceNumberingViolated)
			{
				_Decoder.NeedsKeyFrame = true;
				return null;
			}

			using (var pool = new NSAutoreleasePool())
			{
				GCHandle encodedFrameHandle = GCHandle.Alloc(encodedFrame, GCHandleType.Pinned);
				try
				{
					IntPtr encodedFramePointer = encodedFrameHandle.AddrOfPinnedObject();

					using (var encodedFrameData = NSData.FromBytesNoCopy(encodedFramePointer, (uint)encodedFrame.Length, false))
					{
						using (var buffer = new CocoaVp8Buffer())
						{
							if (_Decoder.DecodeFrame(encodedFrameData, buffer))
							{
								var planeYData = new byte[buffer.PlaneY.Length];
								var planeUData = new byte[buffer.PlaneU.Length];
								var planeVData = new byte[buffer.PlaneV.Length];
								Marshal.Copy(buffer.PlaneY.Bytes, planeYData, 0, (int)buffer.PlaneY.Length);
								Marshal.Copy(buffer.PlaneU.Bytes, planeUData, 0, (int)buffer.PlaneU.Length);
								Marshal.Copy(buffer.PlaneV.Bytes, planeVData, 0, (int)buffer.PlaneV.Length);
								return new VideoBuffer(buffer.Width, buffer.Height, new[] {
									new VideoPlane(planeYData, buffer.StrideY),
									new VideoPlane(planeUData, buffer.StrideU),
									new VideoPlane(planeVData, buffer.StrideV)
								}, VideoFormat.I420);
							}
							return null;
						}
					}
				}
				finally
				{
					encodedFrameHandle.Free();
				}
			}
		}

        /// <summary>
        /// Gets whether the decoder needs a keyframe. This
        /// is checked after every failed Decode operation.
        /// </summary>
        /// <returns></returns>
        public override bool DecoderNeedsKeyFrame()
        {
            if (_Decoder == null)
            {
                return false;
            }
            return _Decoder.NeedsKeyFrame;
        }

        /// <summary>
        /// Packetizes an encoded frame.
        /// </summary>
        /// <param name="encodedFrame">The encoded frame.</param>
        /// <returns></returns>
        public override RTPPacket[] Packetize(byte[] encodedFrame)
        {
            return _Padep.Packetize(encodedFrame, ClockRate);
        }

        /// <summary>
        /// Depacketizes a packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <returns></returns>
        public override byte[] Depacketize(RTPPacket packet)
        {
            return _Padep.Depacketize(packet);
        }

        private int _LossyCount;
        private int _LosslessCount;

        /// <summary>
        /// Processes RTCP packets.
        /// </summary>
        /// <param name="packets">The packets to process.</param>
        public override void ProcessRTCP(RTCPPacket[] packets)
        {
            if (_Encoder != null)
            {
                foreach (var packet in packets)
                {
                    if (packet is RTCPPliPacket)
                    {
                        Log.Info("Received PLI for video stream.");
                        _Encoder.SendKeyframe();
                    }
                    else if (packet is RTCPReportPacket)
                    {
                        var report = (RTCPReportPacket)packet;
                        foreach (var block in report.ReportBlocks)
                        {
                            Log.DebugFormat("VP8 report: {0} packet loss ({1} cumulative packets lost)", block.PercentLost.ToString("P2"), block.CumulativeNumberOfPacketsLost.ToString());
                            if (block.PercentLost > 0)
                            {
                                _LosslessCount = 0;
                                _LossyCount++;
                                if (_LossyCount > 5 && (_Encoder.Quality > 0.0 || _Encoder.Bitrate > 64 /* || _Encoder.Scale > 0.2 */))
                                {
                                    _LossyCount = 0;
                                    if (_Encoder.Quality > 0.0)
                                    {
                                        _Encoder.Quality = MathAssistant.Max(0.0, _Encoder.Quality - 0.1);
                                        Log.InfoFormat("Decreasing VP8 encoder quality to {0}.", _Encoder.Quality.ToString("P2"));
                                    }
                                    if (_Encoder.Bitrate > 64)
                                    {
                                        _Encoder.Bitrate = MathAssistant.Max(64, _Encoder.Bitrate - 64);
                                        Log.InfoFormat("Decreasing VP8 encoder bitrate to {0}.", _Encoder.Bitrate.ToString());
                                    }
                                    /*if (_Encoder.Scale > 0.2)
                                    {
                                        _Encoder.Scale = MathAssistant.Max(0.2, _Encoder.Scale - 0.2);
                                        Log.InfoFormat("Decreasing VP8 encoder scale to {0}.", _Encoder.Scale.ToString("P2"));
                                    }*/
                                }
                            }
                            else
                            {
                                _LossyCount = 0;
                                _LosslessCount++;
                                if (_LosslessCount > 5 && (_Encoder.Quality < 1.0 || _Encoder.Bitrate < 640 /* || _Encoder.Scale < 1.0 */))
                                {
                                    _LosslessCount = 0;
                                    if (_Encoder.Quality < 1.0)
                                    {
                                        _Encoder.Quality = MathAssistant.Min(1.0, _Encoder.Quality + 0.1);
                                        Log.InfoFormat("Increasing VP8 encoder quality to {0}.", _Encoder.Quality.ToString("P2"));
                                    }
                                    if (_Encoder.Bitrate < 640)
                                    {
                                        _Encoder.Bitrate = MathAssistant.Min(640, _Encoder.Bitrate + 64);
                                        Log.InfoFormat("Increasing VP8 encoder bitrate to {0}.", _Encoder.Bitrate.ToString());
                                    }
                                    /*if (_Encoder.Scale < 1.0)
                                    {
                                        _Encoder.Scale = MathAssistant.Min(1.0, _Encoder.Scale + 0.2);
                                        Log.InfoFormat("Increasing VP8 encoder scale to {0}.", _Encoder.Scale.ToString("P2"));
                                    }*/
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Destroys the codec.
        /// </summary>
        public override void Destroy()
        {
            if (_Encoder != null)
            {
                _Encoder.Destroy();
                _Encoder.Dispose();
                _Encoder = null;
            }

            if (_Decoder != null)
            {
                _Decoder.Destroy();
                _Decoder.Dispose();
                _Decoder = null;
            }
        }
    }
}