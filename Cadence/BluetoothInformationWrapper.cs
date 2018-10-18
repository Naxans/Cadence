using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace Cadence
{
    class BluetoothInformationWrapper
    {
        public DeviceInformation DeviceInformation { get; set; }

        public BluetoothInformationWrapper(DeviceInformation deviceInformation)
        {
            DeviceInformation = deviceInformation;
        }

        public override string ToString()
        {
            return DeviceInformation.Name == "" ? "Onbekend toestel" : DeviceInformation.Name;
        }
    }
}
