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

using Xamarin.iOS.Opus;

namespace Xamarin.iOS.Conference.WebRTC.Opus
{
    /// <summary>
    /// Opus codec wrapper for Xamarin.iOS.
    /// </summary>
    class OpusCodec : AudioCodec
    {
        private CocoaOpusEncoder _Encoder;
        private CocoaOpusDecoder _Decoder;
        private BasicAudioPadep _Padep;

        /// <summary>
        /// Gets or sets the loss percentage (0-100)
        /// before forward error correction (FEC) is
        /// activated (only if supported by the remote peer).
        /// Affects encoded data only.
        /// Defaults to 5.
        /// </summary>
        public int PercentLossToTriggerFEC { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to
        /// disable forward error correction (FEC) completely.
        /// If set to true, FEC will never activate.
        /// Affects encoded data only.
        /// Defaults to false.
        /// </summary>
        public bool DisableFEC { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether forward
        /// error correction (FEC) is currently active.
        /// </summary>
        public bool FecActive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpusCodec"/> class.
        /// </summary>
        public OpusCodec()
            : base(20)
		{
            DisableFEC = false;
            PercentLossToTriggerFEC = 5;

            _Padep = new BasicAudioPadep();
        }

        /// <summary>
        /// Encodes a frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <returns></returns>
        public override byte[] Encode(AudioBuffer frame)
		{
			if (_Encoder == null)
			{
				_Encoder = new CocoaOpusEncoder(ClockRate, Channels, PacketTime);
                _Encoder.Quality = 1.0;
                _Encoder.Bitrate = 125;
			}

			using (var pool = new NSAutoreleasePool())
			{
				GCHandle dataHandle = GCHandle.Alloc(frame.Data, GCHandleType.Pinned);
				try
				{
					IntPtr dataPointer = dataHandle.AddrOfPinnedObject();

					using (var buffer = new CocoaOpusBuffer {
						Data = NSData.FromBytesNoCopy(dataPointer, (uint)frame.Data.Length, false),
						Index = frame.Index,
						Length = frame.Length
					})
					{
						using (var encodedFrameData = _Encoder.EncodeBuffer(buffer))
						{
							return encodedFrameData.ToArray();
						}
					}
				}
				finally
				{
					dataHandle.Free();
				}
			}
        }

        private int CurrentRTPSequenceNumber = -1;
        private int LastRTPSequenceNumber = -1;

        /// <summary>
        /// Decodes an encoded frame.
        /// </summary>
        /// <param name="encodedFrame">The encoded frame.</param>
        /// <returns></returns>
        public override AudioBuffer Decode(byte[] encodedFrame)
		{
			if (_Decoder == null)
			{
                _Decoder = new CocoaOpusDecoder(ClockRate, Channels, PacketTime);
                Link.GetRemoteStream().DisablePLC = true;
			}

            if (LastRTPSequenceNumber == -1)
            {
                LastRTPSequenceNumber = CurrentRTPSequenceNumber;
                return DecodeNormal(encodedFrame);
            }
            else
            {
                var sequenceNumberDelta = RTPPacket.GetSequenceNumberDelta(CurrentRTPSequenceNumber, LastRTPSequenceNumber);
                LastRTPSequenceNumber = CurrentRTPSequenceNumber;

                var missingPacketCount = sequenceNumberDelta - 1;
                var previousFrames = new AudioBuffer[missingPacketCount];

                var plcFrameCount = (missingPacketCount > 1) ? missingPacketCount - 1 : 0;
                if (plcFrameCount > 0)
                {
                    Log.InfoFormat("Adding {0} frames of loss concealment to incoming audio stream. Packet sequence violated.", plcFrameCount.ToString());
                    for (var i = 0; i < plcFrameCount; i++)
                    {
                        previousFrames[i] = DecodePLC();
                    }
                }

                var fecFrameCount = (missingPacketCount > 0) ? 1 : 0;
                if (fecFrameCount > 0)
                {
                    var fecFrame = DecodeFEC(encodedFrame);
                    var fecFrameIndex = missingPacketCount - 1;
                    if (fecFrame == null)
                    {
                        previousFrames[fecFrameIndex] = DecodePLC();
                    }
                    else
                    {
                        previousFrames[fecFrameIndex] = fecFrame;
                    }
                }

                var frame = DecodeNormal(encodedFrame);
                frame.PreviousBuffers = previousFrames;
                return frame;
            }

        }

        private AudioBuffer DecodePLC()
        {
            return Decode(null, false);
        }

        private AudioBuffer DecodeFEC(byte[] encodedFrame)
        {
            return Decode(encodedFrame, true);
        }

        private AudioBuffer DecodeNormal(byte[] encodedFrame)
        {
            return Decode(encodedFrame, false);
        }

        private AudioBuffer Decode(byte[] encodedFrame, bool fec)
        {
            using (var pool = new NSAutoreleasePool())
            {
                GCHandle encodedFrameHandle = GCHandle.Alloc(encodedFrame, GCHandleType.Pinned);
                try
                {
                    IntPtr encodedFramePointer = encodedFrame == null ? IntPtr.Zero : encodedFrameHandle.AddrOfPinnedObject();

                    using (var encodedFrameData = encodedFrame == null ? null : NSData.FromBytesNoCopy(encodedFramePointer, (uint)encodedFrame.Length, false))
                    {
                        using (var buffer = _Decoder.DecodeFrame(encodedFrameData, fec))
                        {
                            if (buffer == null)
                            {
                                return null;
                            }

                            var frame = new byte[buffer.Length];
                            Marshal.Copy(buffer.Data.Bytes, frame, buffer.Index, buffer.Length);
                            return new AudioBuffer(frame, 0, frame.Length);
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
        /// Packetizes an encoded frame.
        /// </summary>
        /// <param name="encodedFrame">The encoded frame.</param>
        /// <returns></returns>
        public override RTPPacket[] Packetize(byte[] encodedFrame)
        {
            return _Padep.Packetize(encodedFrame, ClockRate, PacketTime, ResetTimestamp);
        }

        /// <summary>
        /// Depacketizes a packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <returns></returns>
        public override byte[] Depacketize(RTPPacket packet)
        {
            CurrentRTPSequenceNumber = packet.SequenceNumber;

            return _Padep.Depacketize(packet);
        }

        private int _LossyCount = 0;
        private int _LosslessCount = 0;

        private int _MinimumReportsBeforeFEC = 1;
        private long _ReportsReceived = 0;

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
                    if (packet is RTCPReportPacket)
                    {
                        _ReportsReceived++;

                        var report = (RTCPReportPacket)packet;
                        foreach (var block in report.ReportBlocks)
                        {
                            Log.DebugFormat("Opus report: {0} packet loss ({1} cumulative packets lost)", block.PercentLost.ToString("P2"), block.CumulativeNumberOfPacketsLost.ToString());
                            if (block.PercentLost > 0)
                            {
                                _LosslessCount = 0;
                                _LossyCount++;
                                if (_LossyCount > 5 && _Encoder.Quality > 0.0)
                                {
                                    _LossyCount = 0;
                                    _Encoder.Quality = MathAssistant.Max(0.0, _Encoder.Quality - 0.1);
                                    Log.InfoFormat("Decreasing Opus encoder quality to {0}.", _Encoder.Quality.ToString("P2"));
                                }
                            }
                            else
                            {
                                _LossyCount = 0;
                                _LosslessCount++;
                                if (_LosslessCount > 5 && _Encoder.Quality < 1.0)
                                {
                                    _LosslessCount = 0;
                                    _Encoder.Quality = MathAssistant.Min(1.0, _Encoder.Quality + 0.1);
                                    Log.InfoFormat("Increasing Opus encoder quality to {0}.", _Encoder.Quality.ToString("P2"));
                                }
                            }

                            if (!DisableFEC && !FecActive && _ReportsReceived > _MinimumReportsBeforeFEC)
                            {
                                if ((block.PercentLost * 100) > PercentLossToTriggerFEC)
                                {
                                    Log.InfoFormat("Activating FEC for Opus audio stream.");
                                    _Encoder.ActivateFEC(PercentLossToTriggerFEC);
                                    FecActive = true;
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