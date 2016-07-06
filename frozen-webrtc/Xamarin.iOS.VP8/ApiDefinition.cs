using Foundation;
using ObjCRuntime;

namespace Xamarin.iOS.VP8
{
    [BaseType(typeof(NSObject))]
    public partial interface CocoaVp8Buffer
    {
        [Export("planeY", ArgumentSemantic.Retain)]
        NSData PlaneY { get; set; }

        [Export("planeU", ArgumentSemantic.Retain)]
        NSData PlaneU { get; set; }

        [Export("planeV", ArgumentSemantic.Retain)]
        NSData PlaneV { get; set; }

        [Export("strideY")]
        int StrideY { get; set; }

        [Export("strideU")]
        int StrideU { get; set; }

        [Export("strideV")]
        int StrideV { get; set; }

        [Export("width")]
        int Width { get; set; }

        [Export("height")]
        int Height { get; set; }
    }

    [BaseType(typeof(NSObject))]
    public partial interface CocoaVp8Decoder
    {
        [Export("debugMode")]
        bool DebugMode { get; set; }

        [Export("needsKeyFrame")]
        bool NeedsKeyFrame { get; set; }

        [Export("decodeFrame:toBuffer:")]
        bool DecodeFrame(NSData frame, CocoaVp8Buffer buffer);

        [Export("destroy")]
        void Destroy();
    }

    [BaseType(typeof(NSObject))]
    public partial interface CocoaVp8Encoder
    {
        [Export("debugMode")]
        bool DebugMode { get; set; }

        [Export("quality")]
        double Quality { get; set; }

        [Export("bitrate")]
        int Bitrate { get; set; }

        [Export("encodeBuffer:toFrame:")]
        bool EncodeBuffer(CocoaVp8Buffer buffer, NSMutableData frame);

        [Export("sendKeyframe")]
        void SendKeyframe();

        [Export("destroy")]
        void Destroy();
    }
}

