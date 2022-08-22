using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using System.Text;
using DB.Extensibility.Contracts;

namespace DBDynamicMenuPluginExample
{
    [Export(typeof(IPlugin))]
    public class ExamplePlugin : PluginBase, IPlugin
    {
        class MenuKeys
        {
            public const string Root = "root";
            public const string State = "state";
            public const string Visibility = "visibility";
            public const string EnableAll = "enableAll";
            public const string DisableAll = "disableAll";
            public const string VisibleAll = "visibleAll";
            public const string InvisibleAll = "invisibleAll";
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
                menu.AppendFormat("*Example Plugin,{0}", MenuKeys.Root);
                menu.AppendFormat("*>State,{0}", MenuKeys.State);
                menu.AppendFormat("*>>Enable All,{0}", MenuKeys.EnableAll);
                menu.AppendFormat("*>>Disable All,{0}", MenuKeys.DisableAll);
                menu.AppendFormat("*>Visibility,{0}", MenuKeys.Visibility);
                menu.AppendFormat("*>>Make All Visible,{0}", MenuKeys.VisibleAll);
                menu.AppendFormat("*>>Make All Invisible,{0}", MenuKeys.InvisibleAll);
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
            MessageBox.Show(String.Format("Plugin '{0}' initialized!", GetType().Namespace));
            mMenuItems.Add(MenuKeys.Root, new MenuItem());
            mMenuItems.Add(MenuKeys.State, new MenuItem());
            mMenuItems.Add(MenuKeys.Visibility, new MenuItem()); 
            mMenuItems.Add(MenuKeys.EnableAll, new MenuItem(OnEnableAll));
            mMenuItems.Add(MenuKeys.DisableAll, new MenuItem(OnDisableAll));
            mMenuItems.Add(MenuKeys.VisibleAll, new MenuItem(OnVisibleAll));
            mMenuItems.Add(MenuKeys.InvisibleAll, new MenuItem(OnInvisibleAll));

        }
        private void OnEnableAll()
        {
            mMenuItems[MenuKeys.DisableAll].IsEnabled = true;
            mMenuItems[MenuKeys.Visibility].IsEnabled = true;
        }
        private void OnDisableAll()
        {
            mMenuItems[MenuKeys.DisableAll].IsEnabled = false;
            mMenuItems[MenuKeys.Visibility].IsEnabled = false;
        }
        private void OnVisibleAll()
        {
            mMenuItems[MenuKeys.InvisibleAll].IsVisible = true;
            mMenuItems[MenuKeys.State].IsVisible = true;
        }
        private void OnInvisibleAll()
        {
            mMenuItems[MenuKeys.InvisibleAll].IsVisible = false;
            mMenuItems[MenuKeys.State].IsVisible = false;
        }
    }
}