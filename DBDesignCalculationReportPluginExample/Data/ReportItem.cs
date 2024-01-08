using System;

namespace DesignReportPlugin
{
    public class ReportItem
    {
        public String ItemName;
        public String UnitsStringSi;
        public String UnitsStringIp;
        public Int32 UnitsId;

        public ReportItem(String itemName, String unitsStringSi, String unitsStringIp, Int32 unitsId)
        {
            ItemName = itemName;
            UnitsStringSi = unitsStringSi;
            UnitsStringIp = unitsStringIp;
            UnitsId = unitsId;
        }
    }
}
