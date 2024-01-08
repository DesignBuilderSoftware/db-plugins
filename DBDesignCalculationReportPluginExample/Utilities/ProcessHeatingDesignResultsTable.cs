using System;
using System.Globalization;
using DB.Api;

namespace DesignReportPlugin
{
    public static class ProcessHeatingDesignResultsTable
    {
        public static void GenerateHeatingDesignResultsTable(Building building)
        {
            String buildingName = building.GetAttribute("Title");
            if (building.GetTable("HLResultsTable") != null)
            {
                building.RemoveTable("HLResultsTable");
            }
            Table resultTable = building.AddTable("HLResultsTable", 15);
            Double sf = Double.Parse(building.GetAttribute("HLOverDesign"));
            Boolean hlUseSteadyState = Int32.Parse(building.GetAttribute("HLUseSteadyState")) == 1;
            foreach (BuildingBlock block in building.BuildingBlocks)
            {
                String blockName = block.GetAttribute("Title");
                foreach (Zone zone in block.Zones)
                {
                    if (!zone.IsLumpedOut)
                    {
                        String zoneName = zone.GetAttribute("Title");
                        Double zoneFloorArea = zone.FloorArea * Double.Parse(zone.GetAttribute("ZoneMultiplier"));
                        Table dataTable = zone.GetTable("HLTimeSteply");
                        if (dataTable != null)
                        {
                            Record dataRecord = dataTable.Records[0];
                            Boolean goodParse;
                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.GlazingGain], out Double glazingGains);
                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.RooflightsGain], out Double roofLightGains);
                            glazingGains += roofLightGains;

                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.WallsGain], out Double wallGains);
                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.PartitionsGain], out Double partitionGains);
                            wallGains += partitionGains;

                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.FloorsGain], out Double floorGains);
                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.SolidFloorsGain], out Double solidFloorGains);
                            floorGains += solidFloorGains;
                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.ExtFloorsGain], out Double extFloorGains);
                            floorGains += extFloorGains;

                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.RoofsGain], out Double roofGains);
                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.CeilingsGain], out Double ceilingGains);
                            roofGains += ceilingGains;

                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.ExtNatVentGain], out Double ventGains);
                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.ExtMechVentGain], out Double extMechVentGains);
                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.IntNatVentGain], out Double intNatVentGains);
                            ventGains += extMechVentGains;
                            ventGains += intNatVentGains;

                            goodParse = Int32.TryParse(zone.GetAttribute("HeatingMechVentSizingType"), out Int32 heatingMechVentSizingType);
                            if (heatingMechVentSizingType == 1)
                            {
                                ventGains *= Double.Parse(zone.GetAttribute("ZoneMultiplier"));
                            }

                            goodParse = Double.TryParse(dataRecord[(Int32)ItemIndex.ExtInfiltrationGain], out Double infiltrationGains);

                            Record resultRecord = resultTable.AddRecord();
                            resultRecord[0] = buildingName;
                            resultRecord[1] = blockName;
                            resultRecord[2] = zoneName;
                            resultRecord[3] = dataRecord[50];    // SS comfort temperature
                            resultRecord[4] = dataRecord[46];    // SS
                            resultRecord[5] = dataRecord[47];    // Intermittent
                            CultureInfo invC = CultureInfo.InvariantCulture;
                            if (hlUseSteadyState)
                            {
                                resultRecord[6] = (Double.Parse(dataRecord[46]) * sf).ToString(invC);
                                resultRecord[7] = (zoneFloorArea > 0) ? (Double.Parse(dataRecord[46]) * 1000 * sf / zoneFloorArea).ToString(invC) : "0.0";      //kW to W
                            }
                            else
                            {
                                resultRecord[6] = dataRecord[47];
                                resultRecord[7] = (zoneFloorArea > 0) ? (Double.Parse(dataRecord[47]) * 1000 * sf / zoneFloorArea).ToString(invC) : "0.0";      //kW to W
                            }
                            resultRecord[8] = glazingGains.ToString(invC);
                            resultRecord[9] = wallGains.ToString(invC);
                            resultRecord[10] = floorGains.ToString(invC);
                            resultRecord[11] = roofGains.ToString(invC);
                            resultRecord[12] = ventGains.ToString(invC);
                            resultRecord[13] = infiltrationGains.ToString(invC);
                        }
                    }
                }
            }
        }
    }
}

