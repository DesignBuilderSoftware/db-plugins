using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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
            public const string Ecm1 = "ecm1";
            public const string Ecm2 = "ecm2";
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
        private readonly Dictionary<string, MenuItem> _mMenuItems = new Dictionary<string, MenuItem>();
        public override bool HasMenu
        {
            get { return true; }
        }
        public override string MenuLayout
        {
            get
            {
                StringBuilder menu = new StringBuilder();
                menu.AppendFormat("*Apply ECM,{0}", MenuKeys.Root);
                menu.AppendFormat("*>ECM 1 (Economiser),{0}", MenuKeys.Ecm1);
                menu.AppendFormat("*>ECM 2 (Infiltration),{0}", MenuKeys.Ecm2);
                return menu.ToString();
            }
        }
        public override bool IsMenuItemVisible(string key)
        {
            return _mMenuItems[key].IsVisible;
        }
        public override bool IsMenuItemEnabled(string key)
        {
            return _mMenuItems[key].IsEnabled;
        }
        public override void OnMenuItemPressed(string key)
        {
            _mMenuItems[key].Action();
        }
        public override void Create()
        {
            _mMenuItems.Add(MenuKeys.Root, new MenuItem());
            _mMenuItems.Add(MenuKeys.Ecm1, new MenuItem(SetEconomizer));
            _mMenuItems.Add(MenuKeys.Ecm2, new MenuItem(SetInfiltration));
        }

        public Building GetCurrentBuilding()
        {
            Site site = ApiEnvironment.Site;
            int buildingIndex = ApiEnvironment.CurrentBuildingIndex;
            if (buildingIndex != -1)
            {
                return site.Buildings[buildingIndex];
            }
            return null;

        }

        public void SetEconomizer()
        {
            Building building = GetCurrentBuilding();
            if (building != null)
            {
                const string economiserAttribute = "EconomiserControlType";
                const string economiserType = "2-Fixed Drybulb";

                List<string> airLoopNames = new List<string>();

                var airHandlingUnits = building.HvacNetwork.Loops
                    .Where(loop => loop.LoopType == HvacLoopType.Air)
                    .SelectMany(loop => loop.SupplySubLoop.Components)
                    .Where(component => component.ComponentType == HvacComponentType.AirHandlingUnit);
                foreach (var unit in airHandlingUnits)
                {
                    airLoopNames.Add(unit.GetAttribute("Title"));
                    unit.SetAttribute(economiserAttribute, economiserType);
                }

                MessageBox.Show(String.Format("Building {0}: set attribute '{1}' to '{2}' for loops [ {3} ]",
                    building.GetAttribute("Title"), economiserAttribute,
                    economiserType, String.Join(",", airLoopNames)));
            }
        }

        public void SetInfiltration()
        {
            Building building = GetCurrentBuilding();
            if (building != null)
            {
                const string infiltrationAttribute = "InfiltrationValue";
                const string infiltrationValue = "0.15";

                building.SetAttribute(infiltrationAttribute, infiltrationValue);

                MessageBox.Show(String.Format("Building {0}: set attribute '{1}' to '{2}'.",
                    building.GetAttribute("Title"), infiltrationAttribute, infiltrationValue));
            }
        }

        public override void ModelLoaded()
        {
            _mMenuItems[MenuKeys.Root].IsEnabled = true;
        }
        public override void ModelUnloaded()
        {
            _mMenuItems[MenuKeys.Root].IsEnabled = false;
        }
    }
}
