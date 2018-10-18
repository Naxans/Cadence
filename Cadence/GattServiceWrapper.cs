using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Cadence
{
    class GattServiceWrapper
    {
        public GattDeviceService Service { get; set; }

        public GattServiceWrapper(GattDeviceService service)
        {
            Service = service;
        }

        public override string ToString()
        {
            String serviceCode = Service.Uuid.ToString().Substring(4, 4);
            if (Utility.serviceNamesByCode.ContainsKey(serviceCode))
            {
                return Utility.serviceNamesByCode[serviceCode];
            } else
            {
                return $"Unknown Service ({Service.Uuid.ToString()})";
            }
        }
    }
}
