using System;

using Foundation;
using ObjCRuntime;

namespace Xamarin.iOS.Opus
{
    [BaseType(typeof(NSObject))]
    public partial interface CocoaOpusBuffer
    {
        [Export("data", ArgumentSemantic.Retain)]
        NSData Data { get; set; }

        [Export("index")]
        int Index { get; set; }

        [Export("length")]
        int Length { get; set; }
    }

    [BaseType(typeof(NSObject))]
    public partial interface CocoaOpusDecoder
    {
        [Export("debugMode")]
        bool DebugMode { get; set; }

        [Export("initWithClockRate:channels:packetTime:")]
        IntPtr Constructor(int clockRate, int channels, int packetTime);

        [Export("decodeFrame:")]
        CocoaOpusBuffer DecodeFrame([NullAllowed] NSData frame);

        [Export("decodeFrame:fec:")]
        CocoaOpusBuffer DecodeFrame([NullAllowed] NSData frame, bool fec);

        [Export("destroy")]
        void Destroy();
    }

    [BaseType(typeof(NSObject))]
    public partial interface CocoaOpusEncoder
    {
        [Export("debugMode")]
        bool DebugMode { get; set; }

        [Export("bitrate")]
        int Bitrate { get; set; }

        [Export("quality")]
        double Quality { get; set; }

        [Export("initWithClockRate:channels:packetTime:")]
        IntPtr Constructor(int clockRate, int channels, int packetTime);

        [Export("activateFECWithPacketLossPercent:")]
        void ActivateFEC(int packetLossPercent);

        [Export("deactivateFEC")]
        void DeactivateFEC();

        [Export("encodeBuffer:")]
        NSMutableData EncodeBuffer(CocoaOpusBuffer buffer);

        [Export("destroy")]
        void Destroy();
    }
}

