using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel.Composition;
using DB.Extensibility.Contracts;
using DB.Api;


namespace DBFDWRReport
{
    [Export(typeof(IPlugin2))]
    public class ExamplePlugin : PluginBase2, IPlugin2
    {
        class MenuKeys
        {
            public const string Root = "root";
            public const string ReportFDWR = "reportFDWR";
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
                menu.AppendFormat("*>Report FDWR,{0}", MenuKeys.ReportFDWR);
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
            mMenuItems.Add(MenuKeys.ReportFDWR, new MenuItem(ReportFdwr, false));
        }

        public enum Orientation
        {
            North,
            South,
            East,
            West
        }

        public Orientation GetOrientation(double azimuth)
        {
            if (azimuth >= 315 || azimuth < 45)
                return Orientation.North;
            if (azimuth >= 45 && azimuth < 135)
                return Orientation.East;
            if (azimuth >= 135 && azimuth < 225)
                return Orientation.South;
            return Orientation.West;
        }

        public void ReportFdwr()
        {
            Site site = ApiEnvironment.Site;
            foreach (Building building in site.Buildings)
            {
                BuildingFdwr fdwr = CalculateFdwr(building);

                string buildingName = building.GetAttribute("Title");
                string report = fdwr.GetReport(buildingName);

                MessageBox.Show(report, "FDWR Report");
            }
        }

        public class BuildingFdwr
        {
            private Dictionary<Orientation, OrientationFdwr> orientationFdwrs;

            public BuildingFdwr()
            {
                orientationFdwrs = new Dictionary<Orientation, OrientationFdwr>
                {
                    { Orientation.North, new OrientationFdwr() },
                    { Orientation.South, new OrientationFdwr() },
                    { Orientation.East, new OrientationFdwr() },
                    { Orientation.West, new OrientationFdwr() },
                };
            }

            public void AddWallArea(Orientation orientation, double wallArea)
            {
                orientationFdwrs[orientation].WallArea += wallArea;
            }

            public void AddDoorArea(Orientation orientation, double doorArea)
            {
                orientationFdwrs[orientation].DoorArea += doorArea;
            }

            public void AddWindowArea(Orientation orientation, double windowArea)
            {
                orientationFdwrs[orientation].WindowArea += windowArea;
            }

            public double GetTotalWallArea()
            {
                double wallArea = 0;
                foreach (KeyValuePair<Orientation, OrientationFdwr> orientationFdwr in orientationFdwrs)
                {
                    wallArea += orientationFdwr.Value.WallArea;
                }
                return wallArea;
            }

            public double GetTotalDoorArea()
            {
                double doorArea = 0;
                foreach (KeyValuePair<Orientation, OrientationFdwr> orientationFdwr in orientationFdwrs)
                {
                    doorArea += orientationFdwr.Value.DoorArea;
                }
                return doorArea;
            }

            public double GetTotalWindowArea()
            {
                double windowArea = 0;
                foreach (KeyValuePair<Orientation, OrientationFdwr> orientationFdwr in orientationFdwrs)
                {
                    windowArea += orientationFdwr.Value.WindowArea;
                }
                return windowArea;
            }

            public double GetTotalFdwr()
            {
                return (GetTotalWindowArea() + GetTotalDoorArea()) / GetTotalWallArea() * 100;
            }

            public string GetReport(string buildingName)
            {
                StringBuilder report = new StringBuilder();
                report.AppendFormat("Building: {0}\n", buildingName);
                report.AppendFormat("    - Wall Area: {0:0.00}m2\n", GetTotalWallArea());
                report.AppendFormat("    - Door Area: {0:0.00}m2\n", GetTotalDoorArea());
                report.AppendFormat("    - Window Area: {0:0.00}m2\n", GetTotalWindowArea());
                report.AppendFormat("    - FDWR: {0:0.00}%\n\n", GetTotalFdwr());

                foreach (KeyValuePair<Orientation, OrientationFdwr> orientationFdwr in orientationFdwrs)
                {
                    report.AppendFormat("{0}\n", orientationFdwr.Key);
                    report.AppendFormat("{0}\n",orientationFdwr.Value.GetReport());
                }

                return report.ToString();
            }
        }

        public class OrientationFdwr
        {
            public double WallArea { get; set; }
            public double DoorArea { get; set; }
            public double WindowArea { get; set; }

            public OrientationFdwr()
            {
                WallArea = 0;
                DoorArea = 0;
                WindowArea = 0;
            }

            public double GetValue()
            {
                return (WindowArea + DoorArea) / WallArea * 100;
            }

            public string GetReport()
            {
                StringBuilder report = new StringBuilder();
                report.AppendFormat("    - Wall Area: {0:0.00}m2\n", WallArea);
                report.AppendFormat("    - Door Area: {0:0.00}m2\n", DoorArea);
                report.AppendFormat("    - Window Area: {0:0.00}m2\n", WindowArea);
                report.AppendFormat("    - FDWR: {0:0.00}%\n", GetValue());
                return report.ToString();
            }
        }

        public BuildingFdwr CalculateFdwr(Building building)
        {
            BuildingFdwr buildingFdwr = new BuildingFdwr();

            foreach (BuildingBlock block in building.BuildingBlocks)
            {
                foreach (Zone zone in block.Zones)
                {
                    foreach (Surface surface in zone.Surfaces)
                    {
                        double azimuth = surface.Azimuth;
                        if (surface.Type == SurfaceType.Wall)
                        {
                            Orientation orientation = GetOrientation(azimuth);
                            foreach (Adjacency adjacency in surface.Adjacencies)
                            {
                                if (adjacency.IsExternal && adjacency.AdjacencyCondition ==
                                    AdjacencyCondition.NotAdjacentToGround)
                                {
                                    buildingFdwr.AddWallArea(orientation, adjacency.Area);
                                    foreach (Opening opening in adjacency.Openings)
                                    {
                                        if (opening.Type == OpeningType.Window)
                                        {
                                            buildingFdwr.AddWindowArea(orientation, opening.Area);
                                        }

                                        if (opening.Type == OpeningType.Door)
                                        {
                                            buildingFdwr.AddDoorArea(orientation, opening.Area);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return buildingFdwr;
        }

        public override void ModelLoaded()
        {
            mMenuItems[MenuKeys.ReportFDWR].IsEnabled = true;
        }

        public override void ModelUnloaded()
        {
            mMenuItems[MenuKeys.ReportFDWR].IsEnabled = false;
        }
    }
}