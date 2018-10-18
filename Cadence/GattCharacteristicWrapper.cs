using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Cadence
{
    class GattCharacteristicWrapper
    {
        public GattCharacteristic Characteristic { get; set; }

        public GattCharacteristicWrapper(GattCharacteristic characteristic)
        {
            this.Characteristic = characteristic;
        }

        public override string ToString()
        {
            String characteristicCode = Characteristic.Uuid.ToString().Substring(4, 4);
            if (Utility.characteristicNamesByCode.ContainsKey(characteristicCode))
            {
                return Utility.characteristicNamesByCode[characteristicCode];
            } else
            {
                return $"Unknown Characteristic ({Characteristic.Uuid.ToString()})";
            }
        }
    }
}
