using QbChat.Pcl.Interfaces;
using System;
using Windows.System.Profile;

namespace QbChat.UWP
{
    public class DeviceUid_Uwp : IDeviceIdentifier
    {
        public string GetIdentifier()
        {
            var token = HardwareIdentification.GetPackageSpecificToken(null);
            var hardwareId = token.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

            byte[] bytes = new byte[hardwareId.Length];
            dataReader.ReadBytes(bytes);

            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}
