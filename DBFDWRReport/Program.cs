using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel.Composition;

using DB.Extensibility.Contracts;
using DB.Api;

namespace DBApplyMeasureExample
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

        public void ReportFdwr()
        {
            Site site = ApiEnvironment.Site;
            foreach (Building building in site.Buildings)
            {
                BuildingFdwr fdwr = CalculateFdwr(building);

                StringBuilder report = new StringBuilder();
                report.AppendFormat("Building: {0}\n", building.GetAttribute("Title"));
                report.AppendFormat("    - Wall Area: {0:0.00}m2\n", fdwr.WallArea);
                report.AppendFormat("    - Door Area: {0:0.00}m2\n", fdwr.DoorArea);
                report.AppendFormat("    - Window Area: {0:0.00}m2\n", fdwr.WindowArea);
                report.AppendFormat("    - FDWR: {0:0.00}%", fdwr.GetValue());

                MessageBox.Show(report.ToString());
            }
        }
        public class BuildingFdwr
        {
            public double WallArea { get; set; }
            public double DoorArea { get; set; }
            public double WindowArea { get; set; }

            public BuildingFdwr() { }

            public BuildingFdwr(double wallArea, double doorArea, double windowArea)
            {
                WallArea = wallArea;
                DoorArea = doorArea;
                WindowArea = windowArea;
            }

            public double GetValue()
            {
                return (WindowArea + DoorArea) / WallArea * 100;
            }
        }

        public BuildingFdwr CalculateFdwr(Building building)
        {
            double totalWallArea = 0;
            double totalDoorArea = 0;
            double totalWindowArea = 0;

            foreach (BuildingBlock block in building.BuildingBlocks)
            {
                foreach (Zone zone in block.Zones)
                {
                    foreach (Surface surface in zone.Surfaces)
                    {
                        if (surface.Type == SurfaceType.Wall)
                        {
                            foreach (Adjacency adjacency in surface.Adjacencies)
                            {
                                if (adjacency.IsExternal && adjacency.AdjacencyCondition == AdjacencyCondition.NotAdjacentToGround)
                                {
                                    totalWallArea += adjacency.Area;
                                    foreach (Opening opening in adjacency.Openings)
                                    {
                                        if (opening.Type == OpeningType.Window)
                                        {
                                            totalWindowArea += opening.Area;
                                        }
                                        if (opening.Type == OpeningType.Door)
                                        {
                                            totalDoorArea += opening.Area;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return new BuildingFdwr(totalWallArea, totalDoorArea, totalWindowArea);
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
