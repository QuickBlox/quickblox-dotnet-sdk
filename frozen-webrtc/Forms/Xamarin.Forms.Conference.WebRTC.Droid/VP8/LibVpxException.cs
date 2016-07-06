using System;
using System.Runtime.InteropServices;

using Android.Runtime;

namespace Xamarin.Forms.Conference.WebRTC.Droid.VP8
{
    class LibVpxException : Exception
    {
        public LibVpxException(string msg)
            : base(msg)
        { }
    }
}