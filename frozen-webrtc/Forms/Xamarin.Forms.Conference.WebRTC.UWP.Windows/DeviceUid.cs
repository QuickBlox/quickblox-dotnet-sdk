using System;
using Windows.System.Profile;
using Windows8.Conference.WebRTC;
using Xamarin.PCL.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(DeviceUid))]

namespace Windows8.Conference.WebRTC
{

    public class DeviceUid : IDeviceIdentifier
    {
        public string GetIdentifier()
        {
            var token = HardwareIdentification.GetPackageSpecificToken(null);
            var hardwareId = token.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

            byte[] bytes = new byte[hardwareId.Length];
            dataReader.ReadBytes(bytes);

            return BitConverter.ToString(bytes).Replace("-", "").Substring(0,15);
        }
    }
}
