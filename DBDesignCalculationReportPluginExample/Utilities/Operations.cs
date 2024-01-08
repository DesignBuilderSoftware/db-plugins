using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DB.Api;

namespace DesignReportPlugin
{
    public static class Operations
    {
        public static String ConvertToIpUnits(Double inputValue, Int32 unitsId, Double conversionFactor, Double conversionAdd, Double gbpConversionRate = 1.0)
        {
            // Convert Double data in native format to display format (SI/IP units depending on program setting)
            // for example, Convert degree centigrade to Centigrade/Fahrenheit (depending on program setting)

            if (IsUnitMonetary(unitsId))
                switch (unitsId)
                {
                    case 118:
                        return (inputValue / gbpConversionRate).ToString();
                    case 82:
                    case 83:
                    case 107:
                    case 108:
                        return (inputValue / (gbpConversionRate * conversionFactor)).ToString();
                    default:
                        return inputValue.ToString();
                }
            else
            {
                return (inputValue * conversionFactor + conversionAdd).ToString();
            }
        }

        private static Boolean IsUnitMonetary(Int32 unitsId)
        {
            switch (unitsId)
            {
                case 82:
                case 83:
                case 107:
                case 108:
                case 118:
                    return true;
                default:
                    return false;
            }
        }
    }

}
