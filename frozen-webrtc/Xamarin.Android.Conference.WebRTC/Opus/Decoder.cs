using System;

using Android.Runtime;

namespace Xamarin.Android.Conference.WebRTC.Opus
{
    public class Decoder
    {
        private long State;

        public Decoder(int clockRate, int channels, int packetTime)
        {
            try
            {
                State = OpusLibrary.Java_aopus_OpusLibrary_decoderCreate(JNIEnv.Handle, IntPtr.Zero, clockRate, channels, packetTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); 
            }
        }

        public void Destroy()
        {
            try
            {
                OpusLibrary.Java_aopus_OpusLibrary_decoderDestroy(JNIEnv.Handle, IntPtr.Zero, State);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public byte[] Decode(byte[] encodedData)
        {
            IntPtr encodedDataPtr = (encodedData == null ? IntPtr.Zero : JNIEnv.NewArray(encodedData));
            try
            {
                return (byte[])JNIEnv.GetArray(OpusLibrary.Java_aopus_OpusLibrary_decoderDecode(JNIEnv.Handle, IntPtr.Zero, State, encodedDataPtr), (encodedData == null ? JniHandleOwnership.TransferLocalRef : JniHandleOwnership.DoNotTransfer), typeof(byte));
            }
            finally
            {
                JNIEnv.DeleteLocalRef(encodedDataPtr);
            }
        }

        public byte[] Decode(byte[] encodedData, bool fec)
        {
            IntPtr encodedDataPtr = (encodedData == null ? IntPtr.Zero : JNIEnv.NewArray(encodedData));
            try
            {
                return (byte[])JNIEnv.GetArray(OpusLibrary.Java_aopus_OpusLibrary_decoderDecode2(JNIEnv.Handle, IntPtr.Zero, State, encodedDataPtr, fec), (encodedData == null ? JniHandleOwnership.TransferLocalRef : JniHandleOwnership.DoNotTransfer), typeof(byte));
            }
            finally
            {
                JNIEnv.DeleteLocalRef(encodedDataPtr);
            }
        }
    }
}