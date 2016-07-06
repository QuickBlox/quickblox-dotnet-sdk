using FM;
using FM.IceLink;
using FM.IceLink.WebRTC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;

namespace WindowsPhone.Conference.WebRTC
{
    public class Vp8Codec : VideoCodec
    {
        private Vp8Padep _Padep;
        private Win8_VP8.Decoder _Decoder;
        private Win8_VP8.Encoder _Encoder;

        public Vp8Codec()
        {
            _Padep = new Vp8Padep();
        }

        public override byte[] Encode(VideoBuffer frame)
        {
            if (_Encoder == null)
            {
                _Encoder = new Win8_VP8.Encoder();
            }

            if (frame.ResetKeyFrame)
            {
                _Encoder.ForceKeyframe();
            }

            // buffer -> bitmap
            var bitmap = new Win8_VP8.Nv12Bitmap(frame.Width, frame.Height)
            {
                Buffer = frame.Plane.Data
            };

            // bitmap -> vp8
            return _Encoder.Encode(bitmap);
        }

        public override VideoBuffer Decode(byte[] encodedFrame)
        {
            if (_Decoder == null)
            {
                _Decoder = new Win8_VP8.Decoder();
            }

            if (_Padep.SequenceNumberingViolated)
            {
                _Decoder.NeedsKeyFrame = true;
                return null;
            }

            // vp8 -> bitmap
            var bitmap = _Decoder.Decode(encodedFrame);

            // bitmap -> buffer
            if (bitmap == null)
            {
                return null;
            }
            return new VideoBuffer(bitmap.Width, bitmap.Height, new VideoPlane(bitmap.Buffer));
        }

        public override bool DecoderNeedsKeyFrame()
        {
            if (_Decoder == null)
            {
                return false;
            }
            return _Decoder.NeedsKeyFrame;
        }

        public override RTPPacket[] Packetize(byte[] encodedFrame)
        {
            return _Padep.Packetize(encodedFrame, ClockRate);
        }

        public override byte[] Depacketize(RTPPacket packet)
        {
            return _Padep.Depacketize(packet);
        }

        public override void Destroy()
        {
            if (_Encoder != null)
            {
                _Encoder.Destroy();
                _Encoder = null;
            }

            if (_Decoder != null)
            {
                _Decoder.Destroy();
                _Decoder = null;
            }
        }

        public override void ProcessRTCP(RTCPPacket[] packets)
        {
            for (int i = 0; i < packets.Length; i++)
            {
                var packet = packets[i];
                if (packet is RTCPPliPacket)
                {
                    if (_Encoder != null)
                    {
                        _Encoder.ForceKeyframe();
                    }
                }
                else if (packet is RTCPReportPacket)
                {
                    var report = (RTCPReportPacket)packet;
                    foreach (var block in report.ReportBlocks)
                    {
                        Log.DebugFormat("VP8 report: {0}% packet loss ({1} cumulative packets lost)", ((int)(block.PercentLost * 100)).ToString(), block.CumulativeNumberOfPacketsLost.ToString());
                    }
                }
            }
        }
    }
}