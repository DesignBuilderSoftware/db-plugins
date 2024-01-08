using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using DB.Extensibility.Contracts;
using DB.Api;
using DB.Api.Extensions;
using System.Collections.Generic;
using System.Linq;
using DBDesignCalculationReportPluginExample.Resources;

namespace DesignReportPlugin
{
    [Export(typeof(IPlugin))]
    class DesignCalcReportPlugin : PluginBase, IPlugin
    {
        public Boolean ShowNonCoincidentBuilding;
        public Boolean ShowNonCoincidentCoolingSystem;
        public Boolean ShowCoincidentDefBuilding;
        public Boolean ShowCoincidentDefCoolingSystem;
        public Boolean ShowCoincidentTotalCalcBuilding;
        public Boolean ShowCoincidentTotalCalcCoolingSystem;
        public StringBuilder ReportBuilder;
        public UnitsTypes ModelUnitsType;
        public Table UnitsTable;
        public Boolean IsEnglish;
        public Boolean IsOutputCSV;

        private readonly List<ReportItem> coolingReportItem = new List<ReportItem>
        {
            { "Building or system name", "", "", 0 },
            { "Block name", "", "", 0 },
            { "Zone name", "", "", 0 },
            { "Design capacity", "kW", "kBtu/h", 24 },
            { "Design flow rate", "m3/s", "ft3/min", 3 },
            { "Total cooling load", "kW", "kBtu/h", 24 },
            { "Sensible", "kW", "kBtu/h", 24 },
            { "Latent", "kW", "kBtu/h", 24 },
            { "Air temperature", "°C", "°F", 4 },
            { "Humidity", "%", "%", 122 },
            { "Time of max cooling", "", "", 0 },
            { "Max Op Temp in Day", "°C", "°F", 4 },
            { "Floor area", "m2", "ft2", 9 },
            { "Volume", "m3", "ft3", 16 },
            { "Flow/floor area", "l/s-m2", "ft3/min-ft2", 60 },
            { "Design cooling load per floor area", "W/m2", "W/ft2", 23 },
            { "Outside dry-bulb temperature at time of peak cooling load", "°C", "°F", 4 },
            { "Glazing gains", "kW", "kBtu/h", 24 },
            { "Wall gains", "kW", "kBtu/h", 24 },
            { "Floor gains", "kW", "kBtu/h", 24 },
            { "Roof and ceiling gains", "kW", "kBtu/h", 24 },
            { "Ventilation gains", "kW", "kBtu/h", 24 },
            { "Infiltration gains", "kW", "kBtu/h", 24 },
            { "Electric equipment gains", "kW", "kBtu/h", 24 },
            { "Lighting gains", "kW", "kBtu/h", 24 },
            { "People gains", "kW", "kBtu/h", 24 },
            { "Solar gains", "kW", "kBtu/h", 24 },
            { "Mechanical ventilation fresh air rate", "m3/s", "ft3/min", 3 },
            { "Fresh air % of supply air", "%", "%", 122 }
        };

        private readonly List<ReportItem> heatingReportItem = new List<ReportItem>
        {
            { "Building name", "", "", 0 },
            { "Block name", "", "", 0 },
            { "Zone name", "", "", 0 },
            { "Comfort Temperature","°C", "°F", 4 },
            { "Steady-State Heat Loss", "kW", "kBtu/h", 24 },
            { "Intermittent Heat Loss", "kW", "kBtu/h", 24 },
            { "Design Capacity", "kW", "kBtu/h", 24 },
            { "Design Capacity Per Area", "W/m2", "Btu/h-ft2", 103 },
            { "Glazing Gains", "kW", "kBtu/h", 24 },
            { "Wall Gains", "kW", "kBtu/h", 24 },
            { "Floor Gains", "kW", "kBtu/h", 24 },
            { "Roof and Ceiling Gains", "kW", "kBtu/h", 24 },
            { "Ventilation Gains", "kW", "kBtu/h", 24 },
            { "Infiltration Gains", "kW", "kBtu/h", 24 },
        };
        public class MenuKeys
        {
            public const String Root = "root";
            public const String ShowReportMenuCooling = "ShowReportMenuCooling";
            public const String ShowReportMenuHeating = "ShowReportMenuHeating";
        }
        public override Boolean HasMenu
        {
            get { return true; }

        }
        public override String MenuLayout
        {
            get
            {
                StringBuilder menu = new StringBuilder();
                menu.AppendFormat(
                    "*Design Calculation Report,{0}", MenuKeys.Root);
                menu.AppendFormat(
                    "*>Generate Cooling Design Report,{0}", MenuKeys.ShowReportMenuCooling);
                menu.AppendFormat(
                    "*>Generate Heating Design Report,{0}", MenuKeys.ShowReportMenuHeating);
                return menu.ToString();
            }
        }
        public override Boolean IsMenuItemVisible(String key)
        {
            return ApiEnvironment.Site != null;
        }
        public override Boolean IsMenuItemEnabled(String key)
        {
            return true;
        }
        public override void OnMenuItemPressed(String key)
        {
            if (key == MenuKeys.ShowReportMenuCooling || key == MenuKeys.ShowReportMenuHeating)
            {
                if (ApiEnvironment.Site.GetTable("LocationTemplates") != null)
                {
                    IsEnglish = String.Equals(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".");
                    IsOutputCSV = true;
                    if (key == MenuKeys.ShowReportMenuCooling)
                    {
                        var repOptions = new ExportOptions();
                        ShowNonCoincidentBuilding = false;
                        ShowNonCoincidentCoolingSystem = false;
                        ShowCoincidentDefBuilding = false;
                        ShowCoincidentDefCoolingSystem = false;
                        ShowCoincidentTotalCalcBuilding = false;
                        ShowCoincidentTotalCalcCoolingSystem = false;
                        if (repOptions.ShowMe(ref ShowNonCoincidentBuilding, ref ShowNonCoincidentCoolingSystem, ref ShowCoincidentDefBuilding,
                            ref ShowCoincidentDefCoolingSystem, ref ShowCoincidentTotalCalcBuilding, ref ShowCoincidentTotalCalcCoolingSystem))
                        {
                            GenerateDesignReport(ReportTypes.Cooling);
                        }
                    }
                    else if (key == MenuKeys.ShowReportMenuHeating)
                    {
                        GenerateDesignReport(ReportTypes.Heating);
                    }
                }
                else
                {
                    MessageBox.Show("No model loaded");
                }
            }
        }

        private void InitTables(Site thisSite)
        {
            UnitsTable = thisSite.GetTable("Units");
        }

        public void GenerateDesignReport(ReportTypes reportType)
        {
            ReportBuilder = new StringBuilder();
            Site modelSite = ApiEnvironment.Site;
            InitTables(modelSite);

            ApplicationTemplates applicationTemplates = ApiEnvironment.ApplicationTemplates;
            Table programOptionTemplatesTable = applicationTemplates.GetTable("ProgramOptionTemplates");
            Record record = programOptionTemplatesTable.Records.GetRecordFromHandle(2);
            Boolean goodParse = Int32.TryParse(record["Units"].Substring(0, 1), out Int32 tempUnitsType);
            ModelUnitsType = (UnitsTypes)tempUnitsType;

            WriteData(modelSite, reportType);
            SaveReport();
        }


        /// <summary>
        /// Retrieve and write out site level data
        /// </summary>
        /// <param name="thisSite">Current Site object</param>
        private void WriteData(Site site, ReportTypes reportType)
        {
            switch (reportType)
            {
                case ReportTypes.Cooling:
                    foreach (Building building in site.Buildings)
                    {
                        WriteCoolingDesignData(building);
                    }
                    break;
                case ReportTypes.Heating:
                    foreach (Building building in site.Buildings)
                    {
                        WriteHeatingDesignData(building);
                    }
                    break;
            }
        }

        private void WriteCoolingDesignData(Building building)
        {
            if (ShowNonCoincidentBuilding)
            {
                OutputCoolingDesignData(building, "HGNonCoincidentZonesSummary", "COOLING DESIGN NON COINCIDENT ZONES SUMMARY");
            }
            if (ShowNonCoincidentCoolingSystem)
            {
                OutputCoolingDesignData(building, "HGNonCoincidentSystemsSummary", "COOLING DESIGN NON COINCIDENT SYSTEMS SUMMARY");
            }
            if (ShowCoincidentDefBuilding && ShowCoincidentTotalCalcBuilding)
            {
                OutputCoolingDesignData(building, "HGCoincidentBuildingZonesSummary", "COOLING DESIGN COINCIDENT BUILDING ZONES SUMMARY");
            }
            if (ShowCoincidentDefBuilding && ShowCoincidentTotalCalcCoolingSystem)
            {
                OutputCoolingDesignData(building, "HGCoincidentBuildingSystemsSummary", "COOLING DESIGN COINCIDENT COOLING SECTOR ZONES SUMMARY");
            }
            if (ShowCoincidentDefCoolingSystem && ShowCoincidentTotalCalcBuilding)
            {
                OutputCoolingDesignData(building, "HGCoincidentCoolingSectorZonesSummary", "COOLING DESIGN COINCIDENT BUILDING SYSTEMS SUMMARY");
            }
            if (ShowCoincidentDefCoolingSystem && ShowCoincidentTotalCalcCoolingSystem)
            {
                OutputCoolingDesignData(building, "HGCoincidentCoolingSectorSystemsSummary", "COOLING DESIGN COINCIDENT COOLING SECTOR SYSTEMS SUMMARY");
            }
        }

        private void OutputCoolingDesignData(Building building, String tableNameStem, String title)
        {
            Table dataTable = building.GetTable(tableNameStem);
            AddLine(0, true, System.Environment.NewLine + "===== " + title + " ====", "");
            WriteTableData(dataTable, ReportTypes.Cooling);
        }

        private void WriteTableData(Table table, ReportTypes reportType)
        {
            List<ReportItem> reportingItem = new List<ReportItem> { { String.Empty, String.Empty, String.Empty, 0 } };
            foreach (Record record in table.Records)
            {
                if (null != record)
                {
                    AddLine(0, true, "Results for " + record[0] + ": " + record[1] + ": " + record[2], "");
                    switch (reportType)
                    {
                        case ReportTypes.Cooling:
                            reportingItem = coolingReportItem;
                            break;
                        case ReportTypes.Heating:
                            reportingItem = heatingReportItem;
                            break;
                    }
                    for (Int32 itemIndex = 0; itemIndex < reportingItem.Count; itemIndex++)
                    {
                        Boolean isNumeric = reportingItem[itemIndex].UnitsId != 0;
                        AddRecordDataLineByIndex(1, reportingItem[itemIndex].ItemName, record[itemIndex], isNumeric, ModelUnitsType,
                                reportingItem[itemIndex].UnitsStringSi, reportingItem[itemIndex].UnitsStringIp, reportingItem[itemIndex].UnitsId);
                    }
                }
            }
        }

        /// <summary>
        /// Add data from a record as a line in the report. Calls AddLine.
        /// </summary>
        /// <param name="indent">The indent level of the line</param>
        /// <param name="caption">Descriptive text of the value</param>
        /// <param name="dataRecord">Record containing the data</param>
        /// <param name="itemIndex">Data from its index</param>
        /// <param name="isNumeric">Is the data to be written out a number</param>
        private void AddRecordDataLineByIndex(Int32 indent, String caption, String value, Boolean isNumeric, UnitsTypes unitType, String siUnitsString, String ipUnitsString, Int32 unitsId)
        {
            Record unitsRecord = UnitsTable.Records.GetRecordFromHandle(unitsId);
            if (unitType == UnitsTypes.UnitsIp)
            {
                if (unitsId != 0)
                {
                    value = (value == "N/A") ? "0" : value;
                    CultureInfo invC = CultureInfo.InvariantCulture;
                    value = Operations.ConvertToIpUnits(Double.Parse(String.IsNullOrWhiteSpace(value) ? "0" : value, invC), unitsId,
                                    Double.Parse(String.IsNullOrWhiteSpace(unitsRecord["ConversionFactor"]) ? "0" : unitsRecord["ConversionFactor"], invC),
                                    Double.Parse(String.IsNullOrWhiteSpace(unitsRecord["ConversionAdd"]) ? "0" : unitsRecord["ConversionAdd"], invC));
                }
            }
            String unitsString = String.Empty;
            switch (unitType)
            {
                case UnitsTypes.UnitsSi:
                    unitsString = siUnitsString;
                    break;
                case UnitsTypes.UnitsIp:
                    unitsString = ipUnitsString;
                    break;
            }
            value = (isNumeric && value == "N/A") ? "0.00" : value;
            value = (unitsId == 60) ? (Double.Parse(value) * 1000).ToString(CultureInfo.CurrentCulture.NumberFormat) : value;
            AddLine(indent, false, unitsString.IsNullOrEmpty() ? caption : caption + " (" + unitsString + ")", isNumeric ? (object)FormatNumericValue(value) : value);
        }

        private Double FormatNumericValue(String dataString)
        {
            if (!IsEnglish)
            {
                if (dataString.Contains("."))
                {
                    dataString = dataString.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                }
            }
            return dataString.IsNullOrEmpty() ? 0.000 : Math.Round(Double.Parse(dataString), 3);
        }

        private void WriteHeatingDesignData(Building building)
        {
            ProcessHeatingDesignResultsTable.GenerateHeatingDesignResultsTable(building);
            Table heatingDesignDataTable = building.GetTable("HLResultsTable");
            WriteTableData(heatingDesignDataTable, ReportTypes.Heating);
        }

        private void SaveReport()
        {
            SaveFileDialog saveReportDialog = new SaveFileDialog();
            if (IsOutputCSV)
            {
                saveReportDialog.Filter = StrResource.csv_file_type_prompt;
                saveReportDialog.FileName = "report.csv";
            }
            else
            {
                saveReportDialog.Filter = StrResource.txt_files_type_prompt;
                saveReportDialog.FileName = "report.txt";
            }
            saveReportDialog.InitialDirectory =
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            if (saveReportDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveReportDialog.FileName, ReportBuilder.ToString(), new UTF8Encoding(true));
                if (MessageBox.Show( StrResource.Report_generated_successfully_Would_you_like_to_open_it, StrResource.Report_Complete,
                        MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start(saveReportDialog.FileName);
                }
            }
        }

        /// <summary>
        /// Add a line to the report
        /// </summary>
        /// <param name="indent">The indent level of the line</param>
        /// <param name="header">Is the line a header</param>
        /// <param name="caption">Descriptive text of the value</param>
        /// <param name="value"></param>
        private void AddLine(Int32 indent, Boolean header, String caption, object value)
        {
            StringBuilder line = new StringBuilder();
            const String IndentStr = "  ";
            line.Append(String.Concat(Enumerable.Repeat(IndentStr, indent)));

            if (IsOutputCSV)
            {
                line.Append("," + caption + ",{0}" + System.Environment.NewLine);
                ReportBuilder.AppendFormat(line.ToString(), value);
            }
            else
            {
                line.Append(header ? "+ " : "- ");
                line.Append(caption);
                line.Append(": {0}" + System.Environment.NewLine);
                ReportBuilder.AppendFormat(line.ToString(), value);
            }
        }
    }
    public static class Extensions
    {
        public static void Add(this ICollection<ReportItem> target, String itemName, String unitsStringSi, String unitsStringIp, Int32 unitsId)
        {
            target.Add(new ReportItem(itemName, unitsStringSi, unitsStringIp, unitsId));
        }
    }
}
