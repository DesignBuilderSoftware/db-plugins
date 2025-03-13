using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Composition;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using DB.Extensibility.Contracts;
using DB.Api;
using Environment = System.Environment;


namespace DBReportBridgingLengths
{
    [Export(typeof(IPlugin2))]
    public class ExamplePlugin : PluginBase2, IPlugin2
    {
        class MenuKeys
        {
            public const string Root = "root";
            public const string ReportBridgingLengths = "report bridging";
        }

        class MenuItem
        {
            public Action Action { get; set; }
            public bool IsEnabled { get; set; }
            public bool IsVisible { get; set; }

            public MenuItem(
                Action action = null,
                bool enabled = true,
                bool visible = true)
            {
                Action = action ?? delegate { };
                IsEnabled = enabled;
                IsVisible = visible;
            }
        }

        private readonly Dictionary<string, MenuItem> mMenuItems = new Dictionary<string, MenuItem>();

        public override bool HasMenu
        {
            get { return true; }
        }

        public override string MenuLayout
        {
            get
            {
                StringBuilder menu = new StringBuilder();
                menu.AppendFormat("*Bridging,{0}", MenuKeys.Root);
                menu.AppendFormat("*>Report bridging,{0}", MenuKeys.ReportBridgingLengths);
                return menu.ToString();
            }
        }

        public override bool IsMenuItemVisible(string key)
        {
            return mMenuItems[key].IsVisible;
        }

        public override bool IsMenuItemEnabled(string key)
        {
            return mMenuItems[key].IsEnabled;
        }

        public override void OnMenuItemPressed(string key)
        {
            mMenuItems[key].Action();
        }

        public override void Create()
        {
            mMenuItems.Add(MenuKeys.Root, new MenuItem());
            mMenuItems.Add(MenuKeys.ReportBridgingLengths, new MenuItem(ReportBridgingLengths, false));
        }

        public void ReportBridgingLengths()
        {
            string report = GetBridgingLengthsReport();

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.Title = "Save report to text file";
            saveFileDialog.DefaultExt = "txt";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, report);
                if (MessageBox.Show(@"Report generated successfully. Would you like to open it?", @"Report Complete",
                        MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start(saveFileDialog.FileName);
                }
            }
        }

        public string GetBridgingLengthsReport()
        {
            Site site = ApiEnvironment.Site;
            Building building = site.Buildings[ApiEnvironment.CurrentBuildingIndex];
            building.CreateThermalBridgingSurfaceEdgeList();

            StringBuilder report = new StringBuilder();
            report.AppendLine("Linear bridging report");
            report.AppendLine("----------------------");
            report.AppendLine();
            report.AppendFormat("Building: {0}{1}", building.GetAttribute("Title"), Environment.NewLine);
            report.AppendLine();

            foreach (BuildingBlock block in building.BuildingBlocks)
            {
                foreach (Zone zone in block.Zones)
                {
                    if (zone.IsChildZone) continue;
                    string title = block.GetAttribute("Title") + ":" + zone.GetAttribute("Title");
                    ZoneBridging bridging = new ZoneBridging(title, zone);
                    report.Append(bridging.GetReport(includeZeroLength: false));
                    report.AppendLine();
                }
            }

            return report.ToString();
        }


        public class ZoneBridging
        {
            private string zoneName;
            private Dictionary<string, double> metalCladBridgingLengths;
            private Dictionary<string, double> noCladBridgingLengths;
            private Dictionary<string, double> metalCladBridgingValues;
            private Dictionary<string, double> noCladBridgingValues;
            private double exposedGroundFloorBriging;

            private Dictionary<string, string> metalCladAttributes = new Dictionary<string, string>
            {
                { "Wall-External Floor", "L5JunctWallExtFlrMC" },
                { "Wall-Internal Floor", "L5JunctWallFlrMC" },
                { "Roof-Wall", "L5JunctRoofWallMC" },
                { "Wall-Wall", "L5JunctWallWallMC" },
                { "Wall-Ground Floor", "JunctWallGrndMC" },
                { "Lintel above window or door", "L5JunctLintelMC" },
                { "Sill below window", "L5JunctSillMC" },
                { "Jamb at window or door", "L5JunctJambMC" },
            };

            private Dictionary<string, string> noCladAttributes = new Dictionary<string, string>
            {
                { "Wall-External Floor", "L5JunctWallExtFlr" },
                { "Wall-Internal Floor", "L5JunctWallFlr" },
                { "Roof-Wall", "L5JunctRoofWall" },
                { "Wall-Wall", "L5JunctWallWall" },
                { "Wall-Ground Floor", "L5JunctWallGrnd" },
                { "Lintel above window or door", "L5JunctLintel" },
                { "Sill below window", "L5JunctSill" },
                { "Jamb at window or door", "L5JunctJamb" }
            };


            public ZoneBridging(string title, Zone zone)
            {
                ZoneBridgingLengths bridgingLengths = zone.ThermalBridgingLengths;
                zoneName = title;
                noCladBridgingLengths = new Dictionary<string, double>
                {
                    { "Wall-External Floor", bridgingLengths.WallExternalFloorLength },
                    { "Wall-Internal Floor", bridgingLengths.WallInternalFloorLength },
                    { "Roof-Wall", bridgingLengths.WallRoofLength },
                    { "Wall-Wall", bridgingLengths.WallWallLength },
                    { "Wall-Ground Floor", bridgingLengths.WallGroundFloorLength },
                    { "Lintel above window or door", bridgingLengths.WallLintelLength },
                    { "Sill below window", bridgingLengths.WallCillLength },
                    { "Jamb at window or door", bridgingLengths.WallJambLength }
                };
                noCladBridgingValues = new Dictionary<string, double>();
                foreach (KeyValuePair<string, string> kvp in noCladAttributes)
                {
                    noCladBridgingValues[kvp.Key] = zone.GetAttributeAsDouble(kvp.Value);
                }

                metalCladBridgingLengths = new Dictionary<string, double>
                {
                    { "Wall-External Floor", bridgingLengths.MetalWallExternalFloorLength },
                    { "Wall-Internal Floor", bridgingLengths.MetalWallInternalFloorLength },
                    { "Roof-Wall", bridgingLengths.MetalWallRoofLength },
                    { "Wall-Wall", bridgingLengths.MetalWallWallLength },
                    { "Wall-Ground Floor", bridgingLengths.MetalWallGroundFloorLength },
                    { "Lintel above window or door", bridgingLengths.MetalWallLintelLength },
                    { "Sill below window", bridgingLengths.MetalWallCillLength },
                    { "Jamb at window or door", bridgingLengths.MetalWallJambLength },
                };
                metalCladBridgingValues = new Dictionary<string, double>();
                foreach (KeyValuePair<string, string> kvp in metalCladAttributes)
                {
                    metalCladBridgingValues[kvp.Key] = zone.GetAttributeAsDouble(kvp.Value);
                }

                exposedGroundFloorBriging = bridgingLengths.ExposedWallGroundFloorLength;
            } 

            private string GetGroupReport(string groupTitle, Dictionary<string, double> lengths,
                Dictionary<string, double> values, bool includeZeroLength)
            {
                StringBuilder groupReport = new StringBuilder();
                groupReport.AppendFormat("  {0}:{1}", groupTitle, Environment.NewLine);
                bool nonZeroIncluded = false;
                foreach (KeyValuePair<string, double> kvp in lengths)
                {
                    if (includeZeroLength || kvp.Value > 0)
                    {
                        string delimitedKey = kvp.Key + ":";
                        string paddedKey = delimitedKey.PadRight(30);
                        groupReport.AppendFormat("    - {0} {1,5:0.00}m    {2,6:0.000}W/(m.K){3}", paddedKey, kvp.Value, values[kvp.Key],
                            Environment.NewLine);
                        nonZeroIncluded = true;
                    }
                }
                if (!nonZeroIncluded)
                {
                    groupReport.AppendLine("    - None");
                }
                return groupReport.ToString();
            }

            public string GetReport(bool includeZeroLength = true)
            {
                StringBuilder report = new StringBuilder();
                report.AppendFormat("Zone: {0}{1}", zoneName, Environment.NewLine);
                string noCladReport =  GetGroupReport( "Psi values NOT involving metal cladding", noCladBridgingLengths, noCladBridgingValues, includeZeroLength);
                report.Append(noCladReport);
                report.AppendLine();

                string metalCladReport = GetGroupReport("Psi values involving metal cladding", metalCladBridgingLengths, metalCladBridgingValues, includeZeroLength);
                report.Append(metalCladReport);
                report.AppendLine();

                report.AppendFormat("  Exposed ground floor length:{0,10:0.00}m{1}", exposedGroundFloorBriging, Environment.NewLine);
                return report.ToString();
            }
        }

        public override void ModelLoaded()
        {
            mMenuItems[MenuKeys.ReportBridgingLengths].IsEnabled = true;
        }

        public override void ModelUnloaded()
        {
            mMenuItems[MenuKeys.ReportBridgingLengths].IsEnabled = false;
        }
    }
}