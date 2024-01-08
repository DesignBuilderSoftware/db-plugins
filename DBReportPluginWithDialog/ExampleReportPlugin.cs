using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DB.Extensibility.Contracts;
using DB.Api;

namespace DBReportPluginWithDialog
{
    [Export(typeof(IPlugin2))]
    class ExampleReportPlugin : PluginBase2, IPlugin2
    {
        public bool ShowGains;
        public bool ShowAirflow;
        public bool ShowPolygons;
        public bool IsEnglish;
        public bool IsModelLoaded;// = true;
        public StringBuilder ReportBuilder;
        public Table WeatherTable;
        public Table LocationTable;
        public Table ConstructionTable;
        public Table GlazingTable;
        public Table ScheduleTable;
        public const string Name = "Name";
        public const string Title = "Title";
        public const string LocationTemplate = "Location Template";
        public const string HourlyWeather = "Hourly Weather";
        public const string AreaLabel = "Area (m2)";
        public const string VolumeLabel = "Volume (m3)";
        public const string ConstructionName = "Construction Name";
        public const string UValue = "U-Value";

        public class MenuKeys
        {
            public const string Root = "root";
            public const string ShowReportMenu = "showReportMenu";
        }
        public override bool HasMenu => true;

        public override string MenuLayout
        {
            get
            {
                StringBuilder menu = new StringBuilder();
                menu.AppendFormat("*Model Report,{0}", MenuKeys.Root);
                menu.AppendFormat("*>Generate Model Report,{0}", MenuKeys.ShowReportMenu);
                return menu.ToString();
            }
        }
        public override bool IsMenuItemVisible(string key)
        {
            return IsModelLoaded;
        }

        public override bool IsMenuItemEnabled(string key)
        {
            return true;
        }
        public override void OnMenuItemPressed(string key)
        {
            if (key == MenuKeys.ShowReportMenu)
            {
                if (ApiEnvironment.Site.GetTable("LocationTemplates") != null)
                {
                    MainForm repOptions = new MainForm();

                    ShowGains = false;
                    ShowAirflow = false;
                    ShowPolygons = false;

                    IsEnglish = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ".";

                    if (repOptions.ShowMe(ref ShowGains, ref ShowAirflow, ref ShowPolygons))
                    {
                        GenerateReport();
                    }
                }
                else
                {
                    MessageBox.Show(@"No model loaded");
                }
            }
        }

        public override void ModelLoaded()
        {
            IsModelLoaded = true;
            
        }

        public override void ModelUnloaded()
        {
            IsModelLoaded = false;
        }

        public void GenerateReport()
        {
            //Initialise Report
            ReportBuilder = new StringBuilder();

            //Initialise Site
            Site modelSite = ApiEnvironment.Site;

            InitTables(modelSite);
            WriteSiteData(modelSite);
            WriteBuildings(modelSite);
            SaveReport();
        }

        /// <summary>
        /// Initialize data tables
        /// </summary>
        /// <param name="site">Current Site object</param>
        private void InitTables(Site site)
        {
            WeatherTable = site.GetTable("HourlyWeather");
            LocationTable = site.GetTable("LocationTemplates");
            ConstructionTable = site.GetTable("Constructions");
            GlazingTable = site.GetTable("Glazing");
            ScheduleTable = site.GetTable("Schedules");
        }

        /// <summary>
        /// Retrieve and write out site level data
        /// </summary>
        /// <param name="site">Current Site object</param>
        private void WriteSiteData(Site site)
        {
            AddLine(0, true, "Site", site.GetAttribute(Title));
            if (int.TryParse(site.GetAttribute("LocationTemplateId"), out int locationTemplateRecordId))
            {
                Record locationRecord = LocationTable.Records.GetRecordFromHandle(locationTemplateRecordId);
                AddRecordDataLine(1, LocationTemplate, locationRecord, "LocationName", false,false);
            }
            else
            {
                AddLine(1, false, LocationTemplate, "Unknown");
            }

            AddLine(1, false, "Latitude (°)", FormatNumericValue(site.GetAttribute("Latitude")));
            AddLine(1, false, "Latitude (°)", FormatNumericValue(site.GetAttribute("Longitude")));

            if (int.TryParse(site.GetAttribute("HourlyWeatherData"), out int hourlyWeatherDataRecordId))
            {
                Record weatherRecord = WeatherTable.Records.GetRecordFromHandle(hourlyWeatherDataRecordId);
                AddRecordDataLine(1, HourlyWeather, weatherRecord, Name, false, false);
            }
            else
            {
                AddLine(1, false, HourlyWeather, "Unknown");
            }
        }

        /// <summary>
        /// Write out the data of each building
        /// </summary>
        /// <param name="site">Current Site object</param>
        private void WriteBuildings(Site site)
        {
            foreach (Building building in site.Buildings)
            {
                AddLine(1, true, "Building", building.GetAttribute(Title));
                AddLine(4, false, "Are Openings Lumped", int.Parse(building.GetAttribute("LumpOpenings")) != 0);

                WriteBuildingBlocks(building);
            }
        }

        /// <summary>
        /// Write out each building block of the current building
        /// </summary>
        /// <param name="building">Current Building object</param>
        private void WriteBuildingBlocks(Building building)
        {
            foreach (BuildingBlock buildingBlock in building.BuildingBlocks)
            {
                AddLine(2, true, "Block", buildingBlock.GetAttribute(Title));

                WriteZones(buildingBlock, building);
            }
        }

        /// <summary>
        /// Write out each zone of the current building block
        /// </summary>
        /// <param name="buildingBlock">Current BuildingBlock object</param>
        /// <param name="building">Current Building object</param>
        private void WriteZones(BuildingBlock buildingBlock, Building building)
        {
            foreach (Zone zone in buildingBlock.Zones)
            {
                AddLine(3, true, "Zone", zone.GetAttribute(Title));
                AddLine(4, false, "Floor " + AreaLabel, Math.Round(zone.FloorArea, 3));
                AddLine(4,false, "Zone " + VolumeLabel, Math.Round(zone.Volume, 3));
                AddLine(4, false, "Is Merged", zone.IsChildZone);

                if (zone.IsChildZone)
                {
                    AddLine(4, false, "Parent Zone", GetParentZoneName(building, zone));
                }

                //For parent zones, write out lumped data and a list of child zones
                else if (zone.GetChildZones(building).Any())
                {
                    AddLine(4, true, "Is Parent Zone","True");
                    AddLine(5, false, "Lumped Zone " + AreaLabel, Math.Round(zone.LumpedFloorArea, 3));
                    AddLine(5, false, "Lumped Zone " + VolumeLabel, Math.Round(zone.LumpedVolume, 3));
                    AddLine(5,true,"Child Zones","");

                    int childNumber = 0;

                    foreach (Zone childZone in zone.GetChildZones(building))
                    {
                        childNumber += 1;
                        AddLine(6,false, "Child " + childNumber, childZone.GetAttribute(Title));
                    }
                }

                WriteGainsAndLightingData(zone);
                WriteAirFlowData(zone);
                WriteSurfaces(zone, building);
            }
        }
        /// <summary>
        /// Write out each surface of the current zone
        /// </summary>
        /// <param name="zone">Current Zone object</param>
        /// <param name="building">Current Building object</param>
        private void WriteSurfaces(Zone zone, Building building)
        {
            foreach (Surface surface in zone.Surfaces)
            {
                AddLine(4, true, "Surface", surface.GetAttribute(Title));

                switch (surface.Type)
                {
                    case SurfaceType.FlatRoof: case SurfaceType.PitchedRoof:
                        AddLine(5, false, "Type", "Roof/Ceiling");
                        break;
                    case SurfaceType.Wall: case SurfaceType.InternalPartition:
                        AddLine(5, false, "Type", "Wall");
                        break;
                    default:
                        AddLine(5, false, "Type", surface.Type);
                        break;
                }

                AddLine(5, false, AreaLabel, Math.Round(surface.Area, 3));
                AddLine(5, false, "Tilt", Math.Round(surface.Tilt));

                WriteAdjacencies(surface, building);
            }
        }

        /// <summary>
        /// Write out each adjacency of the current surface
        /// </summary>
        /// <param name="surface">Current Surface object</param>
        /// <param name="building">Current Building object</param>
        private void WriteAdjacencies(Surface surface, Building building)
        {
            foreach (Adjacency adjacency in surface.Adjacencies)
            {
                Zone otherZone = building.GetZoneFromHandle(adjacency.ZoneHandle);
                string attribute = string.Empty;

                switch (surface.Type)
                {
                    //get adjacency construction material based on surface type
                    case SurfaceType.Floor:
                        if (adjacency.IsExternal)
                        {
                            switch (adjacency.AdjacencyCondition)
                            {
                                case AdjacencyCondition.AdjacentToGround:
                                    attribute = "CombinedGroundFloorConstr";
                                    break;
                                case AdjacencyCondition.Adiabatic:
                                    attribute = "CombinedInternalFloorConstr";
                                    break;
                                default:
                                    attribute = "CombinedExternalFloorConstr";
                                    break;
                            }
                        }
                        else
                        {
                            attribute = "CombinedInternalFloorConstr";
                        }
                        break;

                    case SurfaceType.Wall:
                        switch (adjacency.AdjacencyCondition)
                        {
                            case AdjacencyCondition.AdjacentToGround:
                                attribute = "WallBelowGradeConstr";
                                break;
                            case AdjacencyCondition.Adiabatic:
                                attribute = "InternalWallConstr";
                                break;
                            default:
                                attribute = "WallConstr";
                                break;
                        }
                        break;

                    case SurfaceType.FlatRoof:
                        if (adjacency.IsExternal)
                        {
                            if (adjacency.AdjacencyCondition == AdjacencyCondition.Adiabatic || adjacency.AdjacencyCondition== AdjacencyCondition.AdjacentToGround)
                            {
                                attribute = "CombinedInternalFloorConstr";
                            }
                            else
                            {
                                attribute = "CombinedFlatRoofConstr";
                            }
                        }
                        else
                        {
                            attribute = "CombinedInternalFloorConstr";
                        }
                        break;

                    case SurfaceType.PitchedRoof:
                        attribute = "PitchedRoofConstr";
                        break;

                    case SurfaceType.InternalPartition:
                        attribute = "InternalWallConstr";
                        break;
                }

                string adjacencyCondition;

                if (adjacency.AdjacencyCondition == AdjacencyCondition.AdjacentToGround)
                {
                    adjacencyCondition = "Ground";
                }
                else if (adjacency.AdjacencyCondition == AdjacencyCondition.Adiabatic)
                {
                    adjacencyCondition = "Adiabatic";
                }
                else if (adjacency.IsExternal)
                {
                    adjacencyCondition = "External";
                }
                else
                {
                    BuildingBlock otherBlock = building.BuildingBlocks[otherZone.ParentBuildingBlockIndex];
                    adjacencyCondition = "Internal => " + otherBlock.GetAttribute(Title) + ", " + otherZone.GetAttribute(Title);
                }

                if (int.TryParse(adjacency.GetAttribute(attribute), out int constructionId) && constructionId != 0)
                {
                    Record constructionRecord = ConstructionTable.Records.GetRecordFromHandle(constructionId);

                    AddLine(5, true, "Adjacency", adjacencyCondition);
                    AddLine(6, false, AreaLabel, Math.Round(adjacency.Area, 3));

                    if (surface.Type == SurfaceType.FlatRoof & !adjacency.IsExternal) //This surface is a ceiling
                    {
                        AddRecordDataLine(6, ConstructionName, constructionRecord, Name, false, true);
                    }
                    else
                    {
                        AddRecordDataLine(6, ConstructionName, constructionRecord, Name, false, false);
                    }

                    AddRecordDataLine(6, UValue + " (W/m2-K)", constructionRecord, UValue, true, false);
                }

                //Loop through all polygons in adjacency and write out the polygons
                foreach (Polygon adjacencyPolygon in adjacency.SimplePolygons)
                {
                    WriteVertices(adjacencyPolygon, 6);
                }

                WriteOpening(adjacency, surface);
            }
        }

        /// <summary>
        /// Write out each opening in the current adjacency of the current surface
        /// </summary>
        /// <param name="adjacency">Current Adjacency object</param>
        /// <param name="surface">Current Surface object</param>
        private void WriteOpening(Adjacency adjacency, Surface surface)
        {
            foreach (Opening opening in adjacency.Openings)
            {
                Record openingRecord = null;

                //get opening construction material based on opening type
                switch (opening.Type)
                {
                    case OpeningType.Window:
                        if (surface.IsInternalPartition)
                        {
                            openingRecord =
                                GlazingTable.Records.GetRecordFromHandle(opening.GetAttributeAsInt("InternalGlazingType"));
                        }
                        else if (surface.Type == SurfaceType.FlatRoof || surface.Type == SurfaceType.PitchedRoof)
                        {
                            openingRecord =
                                GlazingTable.Records.GetRecordFromHandle(opening.GetAttributeAsInt("RoofGlazingType"));
                        }
                        else
                        {
                            openingRecord =
                                GlazingTable.Records.GetRecordFromHandle(opening.GetAttributeAsInt("GlazingType"));
                        }
                        break;

                    case OpeningType.Door:
                        if (surface.IsInternalPartition)
                        {
                            openingRecord =
                                ConstructionTable.Records.GetRecordFromHandle(
                                    opening.GetAttributeAsInt("InternalDoorConstr"));
                        }
                        else
                        {
                            openingRecord =
                                ConstructionTable.Records.GetRecordFromHandle(
                                    opening.GetAttributeAsInt("ExternalDoorConstr"));
                        }
                        break;

                    case OpeningType.Hole:
                        break;

                    case OpeningType.Vent:
                        if (surface.IsInternalPartition)
                        {
                            openingRecord =
                                GlazingTable.Records.GetRecordFromHandle(opening.GetAttributeAsInt("InternalVentType"));
                        }
                        else if (surface.Type == SurfaceType.FlatRoof || surface.Type == SurfaceType.PitchedRoof)
                        {
                            openingRecord =
                                GlazingTable.Records.GetRecordFromHandle(opening.GetAttributeAsInt("RoofVentType"));
                        }
                        else
                        {
                            openingRecord = GlazingTable.Records.GetRecordFromHandle(opening.GetAttributeAsInt("VentType"));
                        }
                        break;

                    case OpeningType.Surface:
                        if (surface.IsInternalPartition)
                        {
                            openingRecord =
                                ConstructionTable.Records.GetRecordFromHandle(
                                    opening.GetAttributeAsInt("InternalSubSurfaceConstr"));
                        }
                        else if (surface.Type == SurfaceType.FlatRoof || surface.Type == SurfaceType.PitchedRoof)
                        {
                            openingRecord =
                                ConstructionTable.Records.GetRecordFromHandle(
                                    opening.GetAttributeAsInt("RoofSubSurfaceConstr"));
                        }
                        else
                        {
                            openingRecord =
                                ConstructionTable.Records.GetRecordFromHandle(
                                    opening.GetAttributeAsInt("SubSurfaceConstr"));
                        }
                        break;
                }

                AddLine(6, true, "Opening", opening.Type);
                AddLine(7, false, "Width (m)", Math.Round(opening.Width,3));
                AddLine(7, false, "Height (m)", Math.Round(opening.Height,3));
                AddLine(7, false, AreaLabel, Math.Round(opening.Area, 3));

                if (opening.Type != OpeningType.Vent)
                {
                    AddRecordDataLine(7, ConstructionName, openingRecord, Name, false, false);
                }

                AddRecordDataLine(7, UValue + " (W/m2-K)", openingRecord, UValue, true, false);

                //Opening doesn't have an adjacency list
                WriteVertices(opening.Polygon, 7);
            }
        }

        /// <summary>
        /// If user selects "Show Gains", write out gain data
        /// </summary>
        /// <param name="zone">Current Zone object</param>
        private void WriteGainsAndLightingData(Zone zone)
        {
            if (ShowGains)
            {
                //Write out gain types
                AddLine(4, true, "Gains and Lighting", "");
                WriteGain(zone, ScheduleTable, "Computers");
                WriteGain(zone, ScheduleTable, "Equipment");
                WriteGain(zone, ScheduleTable, "Miscellaneous");
                WriteGain(zone, ScheduleTable, "Catering");
                WriteGain(zone, ScheduleTable, "Process");
                WriteGeneralLighting(zone);
                WriteTaskDisplayLighing(zone);
            }
        }

        /// <summary>
        /// Write out Task and Display Lighting
        /// </summary>
        /// <param name="zone">Current Zone object</param>
        private void WriteTaskDisplayLighing(Zone zone)
        {
            if (zone.GetAttribute("TungstenLightingOn") == "1")
            {
                AddLine(5, true, "Lighting Type", "Task and Display");

                //Power density value depends on unit type.
                if (zone.GetAttribute("TungstenLightingUnits").Contains("1"))
                {
                    AddLine(6, false, "Power Density (W/m2)",
                        FormatNumericValue(zone.GetAttribute("TungstenLightingValue")));
                }
                else
                {
                    AddLine(6, false, "Absolute Power (W)",
                        FormatNumericValue(zone.GetAttribute("TungstenLightingAbsoluteValue")));
                }

                //Write out task and display lighting schedule name
                Record gainsScheduleRecord =
                    ScheduleTable.Records.GetRecordFromHandle(zone.GetAttributeAsInt("TungstenLightingSchedule"));

                AddRecordDataLine(6, "Operation Schedule", gainsScheduleRecord, Name, false, false);
            }
        }

        /// <summary>
        /// Write out General Lighting
        /// </summary>
        /// <param name="zone">Current Zone object</param>
        private void WriteGeneralLighting(Zone zone)
        {
            if (zone.GetAttribute("FluorescentLightingOn") == "1")
            {
                AddLine(5, true, "Lighting Type", "General");

                //Power density value depends on unit type.
                if (zone.GetAttribute("FluorescentLightingUnits").Contains("1"))
                {
                    AddLine(6, false, "Power Density (W/m2)",
                        FormatNumericValue(zone.GetAttribute("FluorescentLightingValue")));
                }
                else if (zone.GetAttribute("FluorescentLightingUnits").Contains("2"))
                {
                    AddLine(6, false, "Normalised Power Density (W/m2-100 lux)",
                        FormatNumericValue(zone.GetAttribute("FluorescentLightingWattsPerM2Per100Lux")));
                }
                else
                {
                    AddLine(6, false, "Absolute Power (W)",
                        FormatNumericValue(zone.GetAttribute("FluorescentLightingAbsoluteWatts")));
                }

                //Write out general lighting schedule name
                Record gainsScheduleRecord =
                    ScheduleTable.Records.GetRecordFromHandle(zone.GetAttributeAsInt("FluorescentLightingSchedule"));

                AddRecordDataLine(6, "Operation Schedule", gainsScheduleRecord, Name, false, false);

                //Write out other general lighting data
                AddLine(6, false, "Return Air Fraction",
                    FormatNumericValue(zone.GetAttribute("LightingReturnAirFraction")));
                AddLine(6, false, "Radiant Fraction", FormatNumericValue(zone.GetAttribute("LightingRadiantFraction")));
                AddLine(6, false, "Visible Fraction", FormatNumericValue(zone.GetAttribute("LightingVisibleFraction")));
                AddLine(6, false, "Convective Fraction",
                    FormatNumericValue(zone.GetAttribute("LightingConvectiveFraction")));
            }
        }

        /// <summary>
        /// If user selects "Show Airflow", write out the zone's mechanical ventilation, natural ventilation and infiltration.
        /// </summary>
        /// <param name="zone">Current Zone object</param>
        private void WriteAirFlowData(Zone zone)
        {
            if (ShowAirflow)
            {
                AddLine(4, true, "Air Flow", "");
                WriteMechVent(zone);
                WriteNatVent(zone);
                WriteInfiltration(zone);
            }
        }

        /// <summary>
        /// Write out Infiltration
        /// </summary>
        /// <param name="zone">Current Zone object</param>
        private void WriteInfiltration(Zone zone)
        {
            if (zone.GetAttribute("InfiltrationOn") == "1")
            {
                AddLine(5, true, "Airflow Type", "Infiltration");
                AddLine(6, false, "Max Airflow (ac/h)", FormatNumericValue(zone.GetAttribute("InfiltrationValue")));

                //Write out infiltration schedule name
                Record airFlowScheduleRecord =
                    ScheduleTable.Records.GetRecordFromHandle(zone.GetAttributeAsInt("InfiltrationSchedule"));

                AddRecordDataLine(6, "Operation Schedule", airFlowScheduleRecord, Name, false, false);
            }
        }


        /// <summary>
        /// Write out Natural Ventilation
        /// </summary>
        /// <param name="zone">Current Zone object</param>
        private void WriteNatVent(Zone zone)
        {
            
            if (zone.GetAttribute("NaturalVentilationOn") == "1")
            {
                AddLine(5, true, "Airflow Type", "Natural Ventilation");
                AddLine(6, false, "Outside Air Definition Method",
                    zone.GetAttribute("NaturalVentilationRateType")
                        .Substring(2)); //This will cut off the number and dash at the start of the list item

                //Airflow value depends on unit type.
                if (zone.GetAttribute("NaturalVentilationRateType").Contains("1"))
                {
                    AddLine(6, false, "Max Airflow (ac/h)",
                        FormatNumericValue(zone.GetAttribute("NaturalVentilationValue")));
                }
                else if (zone.GetAttribute("NaturalVentilationRateType").Contains("3"))
                {
                    AddLine(6, false, "Design Flow Rate (m3/s)",
                        FormatNumericValue(zone.GetAttribute("NaturalVentilationDesignFlowRate")));
                }

                //Write out natural ventilation schedule name
                Record airFlowScheduleRecord =
                    ScheduleTable.Records.GetRecordFromHandle(zone.GetAttributeAsInt("NaturalVentilationSchedule"));

                AddRecordDataLine(6, "Operation Schedule", airFlowScheduleRecord, Name, false, false);
            }
        }

        /// <summary>
        /// Write out Mechanical Ventilation
        /// </summary>
        /// <param name="zone">Current Zone object</param>
        private void WriteMechVent(Zone zone)
        {
            if (zone.GetAttribute("MechanicalVentilationOn") == "1")
            {
                AddLine(5, true, "Airflow Type", "Mechanical Ventilation");
                AddLine(6, false, "Outside Air Definition Method",
                    zone.GetAttribute("MechanicalVentilationRateType")
                        .Substring(2)); //This will cut off the number and dash at the start of the list item

                //Airflow value depends on unit type.
                if (zone.GetAttribute("MechanicalVentilationRateType").Contains("1"))
                {
                    AddLine(6, false, "Max Airflow (ac/h)",
                        FormatNumericValue(zone.GetAttribute("MechanicalVentilationValue")));
                }
                else if (zone.GetAttribute("MechanicalVentilationRateType").Contains("6"))
                {
                    AddLine(6, false, "Design Flow Rate (m3/s)",
                        FormatNumericValue(zone.GetAttribute("MechanicalVentilationDesignFlowRate")));
                }

                //Write out mechanical ventilation schedule name
                Record airFlowScheduleRecord =
                    ScheduleTable.Records.GetRecordFromHandle(zone.GetAttributeAsInt("MechanicalVentilationSchedule"));

                AddRecordDataLine(6, "Operation Schedule", airFlowScheduleRecord, Name, false, false);
            }
        }

        private void SaveReport()
        {
            SaveFileDialog saveReportDialog = new SaveFileDialog
                                                  {
                                                      Filter = @"txt files (*.txt)|*.txt|All files (*.*)|*.*",
                                                      FileName = "report.txt",
                                                      InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
                                                  };

            if (saveReportDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveReportDialog.FileName, ReportBuilder.ToString());

                if (MessageBox.Show(@"Report generated successfully. Would you like to open it?", @"Report Complete",
                        MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start(saveReportDialog.FileName);
                }
            }
        }

        /// <summary>
        /// Write out data for specified gain of specified zone
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="scheduleTable"></param>
        /// <param name="gainType"></param>
        private void WriteGain(Zone zone, Table scheduleTable, string gainType)
        {
            //Check the gain is included in this zone
            //Checkbox data is stored as 1 (checked) or 0 (unchecked)
            if (zone.GetAttribute(gainType + "On") == "1")
            {
                //Write out gain type
                AddLine(5, true, "Gain Type", gainType.Equals("Equipment") ? "Office equipment" : gainType);

                //Power depends on the gain unit type
                string powerUnitName = gainType.Equals("Equipment") ? "OfficeEquipmentUnit" : gainType + "Units";

                if (zone.GetAttribute(powerUnitName).Contains("1")) //This will cut off the number and dash at the start of the list item
                {
                    AddLine(6, false, "Power Density (W/m2)", FormatNumericValue(zone.GetAttribute(gainType + "Value")));
                }
                else
                {
                    AddLine(6, false, "Absolute Zone Power (W)", FormatNumericValue(zone.GetAttribute(gainType + "AbsoluteValue")));
                }

                //Write out operation schedule name
                Record gainsScheduleRecord = scheduleTable.Records.GetRecordFromHandle(zone.GetAttributeAsInt(gainType + "Schedule"));
                AddRecordDataLine(6, "Operation Schedule", gainsScheduleRecord, Name, false, false);

                //Write out gain radiant fraction
                AddLine(6, false, "Radiant Fraction (W)", FormatNumericValue(zone.GetAttribute(gainType + "RadiantFraction")));
            }
        }

        /// <summary>
        /// Add the polygon's list of vertecies to the report. Calls AddLine.
        /// </summary>
        /// <param name="polygon">The polygon to be written out</param>
        /// <param name="polygonIndent">The indent level of the line</param>
        private void WriteVertices(Polygon polygon, int polygonIndent)
        {
            if (ShowPolygons)
            {
                int vertInt = 0;

                AddLine(polygonIndent, true, "Polygon", "");

                foreach (Point3d vertex in polygon.Vertices)
                {
                    vertInt++;
                    AddLine(polygonIndent + 1, false, "Vertex " + vertInt, vertex.X + ", " + vertex.Y + ", " + vertex.Z);
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
        private void AddLine(int indent, bool header, string caption, object value)
        {
            StringBuilder line = new StringBuilder();

            line.Insert(0, "  ", indent);

            line.Append(header ? "+ " : "- ");
            line.Append(caption);
            line.Append(": {0}" + System.Environment.NewLine);

            ReportBuilder.AppendFormat(line.ToString(), value);
        }

        /// <summary>
        /// Add data from a record as a line in the report. Calls AddLine.
        /// </summary>
        /// <param name="indent">The indent level of the line</param>
        /// <param name="caption">Descriptive text of the value</param>
        /// <param name="dataRecord">Record containing the data</param>
        /// <param name="dataName">Field containing the data</param>
        /// <param name="isNumeric">Is the data to be written out a number</param>
        private void AddRecordDataLine(int indent, string caption, Record dataRecord, string dataName, bool isNumeric, bool isReversed)
        {
            if (dataRecord != null)
            {
                string dataString = dataRecord[dataName];

                if (isReversed & !isNumeric)
                {
                    dataString = dataString + " (reversed)";
                }

                AddLine(indent, false, caption, isNumeric ? (object) FormatNumericValue(dataString) : dataString);
            }
        }

        private double FormatNumericValue(string dataString)
        {
            if (!IsEnglish)
            {
                if (dataString.Contains("."))
                {
                    dataString = dataString.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                }
            }
            return dataString != "" ?  Math.Round(double.Parse(dataString), 3) : 0;
        }

        private string GetParentZoneName(Building parentBuilding, Zone childZone)
        {
            Zone parentZone = parentBuilding.GetZoneFromHandle(childZone.LumpedHandle);
            return parentZone.GetAttribute(Title);
        }
    }
}
