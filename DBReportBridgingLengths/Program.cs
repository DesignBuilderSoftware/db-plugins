using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Composition;
using System.IO;
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
            public const string ReportBridgingLengths = "report bridging lengths";
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
                menu.AppendFormat("*Plugins,{0}", MenuKeys.Root);
                menu.AppendFormat("*>Report bridging lengths,{0}", MenuKeys.ReportBridgingLengths);
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
                string fileName = saveFileDialog.FileName;
                File.WriteAllText(fileName, report);
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
            report.AppendFormat("Building {0}{1}", building.GetAttribute("Title"), Environment.NewLine);
            foreach (BuildingBlock block in building.BuildingBlocks)
            {
                foreach (Zone zone in block.Zones)
                {
                    ZoneBridging bridging = new ZoneBridging(zone);
                    report.Append(bridging.GetReport());
                    report.AppendLine();
                }
            }

            return report.ToString();
        }


        public class ZoneBridging
        {
            private string zoneName;
            private Dictionary<string, double> metalCladBridging;
            private Dictionary<string, double> noCladBridging;
            private double exposedGroundFloorBriging;

            public ZoneBridging(Zone zone)
            {
                ZoneBridgingLengths bridgingLengths = zone.ThermalBridgingLengths;
                zoneName = zone.IdfName;
                noCladBridging = new Dictionary<string, double>
                {
                    { "WallExternalFloorLength", bridgingLengths.WallExternalFloorLength },
                    { "WallInternalFloorLength", bridgingLengths.WallInternalFloorLength },
                    { "WallRoofLength", bridgingLengths.WallRoofLength },
                    { "WallWallLength", bridgingLengths.WallWallLength },
                    { "WallGroundFloorLength", bridgingLengths.WallGroundFloorLength },
                    { "WallLintelLength", bridgingLengths.WallLintelLength },
                    { "WallCillLength", bridgingLengths.WallCillLength },
                    { "WallJambLength", bridgingLengths.WallJambLength }
                };
                metalCladBridging = new Dictionary<string, double>
                {
                    { "MetalWallExternalFloorLength", bridgingLengths.MetalWallExternalFloorLength },
                    { "MetalWallInternalFloorLength", bridgingLengths.MetalWallInternalFloorLength },
                    { "MetalWallRoofLength", bridgingLengths.MetalWallRoofLength },
                    { "MetalWallWallLength", bridgingLengths.MetalWallWallLength },
                    { "MetalWallGroundFloorLength", bridgingLengths.MetalWallGroundFloorLength },
                    { "MetalWallLintelLength", bridgingLengths.MetalWallLintelLength },
                    { "MetalWallCillLength", bridgingLengths.MetalWallCillLength },
                    { "MetalWallJambLength", bridgingLengths.MetalWallJambLength },
                };
                exposedGroundFloorBriging = bridgingLengths.ExposedWallGroundFloorLength;
            }

            public string GetReport()
            {
                StringBuilder report = new StringBuilder();
                report.AppendFormat("Zone: {0}{1}", zoneName, Environment.NewLine);
                report.AppendLine("  Psi values NOT involving metal cladding:");
                foreach (KeyValuePair<string, double> kvp in noCladBridging)
                {
                    report.AppendFormat("    - {0}: {1:0.00}m{2}", kvp.Key, kvp.Value, Environment.NewLine);
                }

                report.AppendLine("");
                report.AppendLine("  Psi values involving metal cladding:");
                foreach (KeyValuePair<string, double> kvp in metalCladBridging)
                {
                    report.AppendFormat("    - {0}: {1:0.00}m{2}", kvp.Key, kvp.Value, Environment.NewLine);
                }

                report.AppendLine("");
                report.AppendLine("  Exposed ground floor Psi values:");
                report.AppendFormat("    - ExposedWallGroundFloorLength: {0:0.00}m{1}", exposedGroundFloorBriging,
                    Environment.NewLine);
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