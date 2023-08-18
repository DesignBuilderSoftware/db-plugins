using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Attribute_Addition_Plugin;
using DB.Extensibility.Contracts;
using DB.Api;

namespace Attribute_Addition_Plugin
{
    [Export(typeof(IPlugin2))]
    public class ExampleAddAttributePlugin : PluginBase2, IPlugin2
    {
        public bool IsModelLoaded;

        public class MenuKeys
        {
            public const string Root = "root";
            public const string Interact = "interactWithAttribute";
        }
        public override bool HasMenu
        {
            get { return true; }

        }
        public override string MenuLayout
        {
            get
            {
                StringBuilder menu = new StringBuilder();
                menu.AppendFormat(
                    "*Test Plugin,{0}", MenuKeys.Root);
                menu.AppendFormat("*>Interact with custom attribute,{0}", MenuKeys.Interact);
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
            if (key == MenuKeys.Interact)
            {
                var InteractDialog = new frmAttributeInteraction();
                InteractDialog.ShowMe(ref ModelSite, AttributeName);
            }
        }

        public override void ModelLoaded()
        {
            IsModelLoaded = true;
            //AddAttribute();
        }

        public override void ModelUnloaded()
        {
            IsModelLoaded = false;
        }

        public override void ScreenChanged(ScreenCode screenCode)
        { 
            if (screenCode==ScreenCode.Edit & IsModelLoaded == false)
            {
                AddAttribute();
            }
            //MessageBox.Show(screenCode.ToString());
        }

        public Record GetRecordByName(Table RecordTable, string RecordName)
        {
            foreach (Record thisRecord in RecordTable.Records)
            {
                if (thisRecord["Name"] == RecordName)
                {
                    return thisRecord;
                }
            }
            return null;
        }

        public Table FormatsTable;
        public Site ModelSite;
        public string AttributeName = "DBExampleAttribute";

        public void AddAttribute()
        {
            ModelSite = ApiEnvironment.Site;
            FormatsTable = ApiEnvironment.ApplicationTemplates.GetTable("EditFormats");
            
            Record newRecord;
            if (GetRecordByName(FormatsTable, AttributeName) == null)
            {
                newRecord = FormatsTable.AddRecord();
                newRecord["Name"] = AttributeName;
                newRecord["Caption"] = "21";
                newRecord["IsData"] = "1";
                newRecord["Default"] = "New Record Default";
                newRecord["Indent"] = "1";
                newRecord["OptionGroup"] = "0";
                newRecord["ItemType"] = "7";
                newRecord["Labels"] = "";
                newRecord["ListType"] = "";
                newRecord["Enabled"] = "True";
                newRecord["DataType"] = "0";
                newRecord["LowestDecompositionLevel"] = "0";
                newRecord["Format"] = "0.00";
                newRecord["Min"] = "0";
                newRecord["Max"] = "110";
                newRecord["Locked"] = "False";
                newRecord["SiteTab"] = "-1";
                newRecord["BuildingTab"] = "-1";
                newRecord["PBTab"] = "-1";
                newRecord["ZoneTab"] = "-1";
                newRecord["SurfaceTab"] = "-1";
                newRecord["OpeningTab"] = "-1";
                newRecord["ObjectIndex"] = "0";
                newRecord["ValueLabel"] = "";
                newRecord["Visible"] = "True";
                newRecord["ParametricList"] = "";
                newRecord["UniqueCaption"] = "";
                newRecord["IconName"] = "";
                newRecord["ChangeUpdatesRenderedView"] = "0";
                newRecord["ChangeUpdatesShading"] = "0";
                newRecord["HideWhenMerged"] = "0";
                newRecord["ChangeUpdatesHG"] = "0";
                newRecord["ChangeUpdatesSS"] = "0";
                newRecord["ChangeUpdatesGeometry"] = "0";
                newRecord["MyTab"] = "-1";
                newRecord["Id"] = "0";
                newRecord["Dirty"] = "False";
                newRecord["TemplateIdName"] = "";
                newRecord["Prefix"] = "";
                newRecord["Units"] = "99";
                newRecord["ShowNCM"] = "0";
                newRecord["ShowEnergyPlus"] = "1";
                newRecord["ShowKLIMA"] = "1";
                newRecord["ShowPT"] = "1";
                newRecord["ShowFR"] = "1";
                newRecord["ShowES"] = "1";
                newRecord["ShowDE"] = "1";
                newRecord["ShowDBSim"] = "1";
                newRecord["ShowL5"] = "1";
                ModelSite.AddAttribute(AttributeName, "Test");
            }
        }

    }
}
