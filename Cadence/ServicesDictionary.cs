using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cadence
{
    class Utility
    {
        public static Dictionary<String, String> serviceNamesByCode = 
                        new Dictionary<string, string>
                        {
                            ["1800"] = "Generic Access",
                            ["1801"] = "Generic Attribute",
                            ["180f"] = "Battery Service",
                            ["180a"] = "Device Information",
                            ["1816"] = "Cycling Speed and Cadence"
                        };

        public static Dictionary<String, String> characteristicNamesByCode =
                        new Dictionary<string, string>
                        {
                            ["2a19"] = "Battery Level",
                            ["2a29"] = "Manufacturer Name String",
                            ["2a27"] = "Hardware Revision String",
                            ["2a26"] = "Firmware Revision String",
                            ["2a5b"] = "Aggregate",
                            ["2a5c"] = "CSC Feature",
                            ["2a5d"] = "Sensor Location",
                            ["2a55"] = "SC Control Point",
                            ["2a05"] = "Service Changed",
                            ["2a00"] = "Device Name",
                            ["2a01"] = "Appearance",
                            ["2a04"] = "Peripheral Preferred Connection Parameters",
                        };
    }
}
