using System.ComponentModel.Composition;
using System.Text;
using System.Windows.Forms;
using DB.Extensibility.Contracts;
using DB.Api;
using Environment = DB.Api.Environment;

namespace DBUIAdditionPlugin
{
    [Export(typeof(IPlugin2))]
    public class ExampleAddUIPlugin : PluginBase2, IPlugin2
    {
        public bool IsElementsAdded;
        public class MenuKeys
        {
            public const string Root = "root";
        }
        public override bool HasMenu
        {
            get { return false; }

        }
        public override string MenuLayout
        {
            get
            {
                StringBuilder menu = new StringBuilder();
                return menu.ToString();
            }
        }
        public override bool IsMenuItemVisible(string key)
        {
            return false;
        }
        public override bool IsMenuItemEnabled(string key)
        {
            return true;
        }
        public override void OnMenuItemPressed(string key)
        {
            
        }

        public override void ModelLoaded()
        {
            AddAttributesToModel();
        }
        
        public override Environment ApiEnvironment
        {
            get => base.ApiEnvironment;
            set
            {
                base.ApiEnvironment = value;
                if (!IsElementsAdded)
                {
                    AddUIElements();
                    IsElementsAdded = true;
                }
            }
        }

        public override void Create()
        {
            base.Create();
            IsElementsAdded = false;
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

        public void AddUIElements()
        {
            FormatsTable = ApiEnvironment.ApplicationTemplates.GetTable("EditFormats");

            Record newRecord;
            //Header record
            string attributeName = "ExampleUIHeader";
            newRecord = GetRecordByName(FormatsTable, attributeName);
            if (newRecord == null)
            {
                newRecord = FormatsTable.AddRecord();
                newRecord["Name"] = attributeName;
                newRecord["Caption"] = "Header";
                newRecord["IsData"] = "0";
                newRecord["Default"] = "1";
                newRecord["Indent"] = "0";
                newRecord["OptionGroup"] = "0";
                newRecord["ItemType"] = "0";
                newRecord["Labels"] = "";
                newRecord["ListType"] = "";
                newRecord["Enabled"] = "True";
                newRecord["DataType"] = "0";
                newRecord["LowestDecompositionLevel"] = "5";
                newRecord["Format"] = "";
                newRecord["Min"] = "";
                newRecord["Max"] = "";
                newRecord["Locked"] = "False";
                newRecord["SiteTab"] = "-1";
                newRecord["BuildingTab"] = "1";
                newRecord["PBTab"] = "1";
                newRecord["ZoneTab"] = "1";
                newRecord["SurfaceTab"] = "1";
                newRecord["OpeningTab"] = "1";
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
                newRecord["MyTab"] = "1";
                newRecord["Id"] = "0";
                newRecord["Dirty"] = "False";
                newRecord["TemplateIdName"] = "";
                newRecord["Prefix"] = "";
                newRecord["Units"] = "UNKNOWN";
                newRecord["ShowNCM"] = "0";
                newRecord["ShowEnergyPlus"] = "1";
                newRecord["ShowKLIMA"] = "1";
                newRecord["ShowPT"] = "1";
                newRecord["ShowFR"] = "1";
                newRecord["ShowES"] = "1";
                newRecord["ShowDE"] = "1";
                newRecord["ShowDBSim"] = "1";
                newRecord["ShowL5"] = "1";
            }

            attributeName = "ExampleUITextEdit";
            newRecord = GetRecordByName(FormatsTable, attributeName);
            if (newRecord == null)
            {
                newRecord = FormatsTable.AddRecord();
                newRecord["Name"] = attributeName;
                newRecord["Caption"] = "Text Edit";
                newRecord["IsData"] = "1";
                newRecord["Default"] = "Example Text";
                newRecord["Indent"] = "1";
                newRecord["OptionGroup"] = "0";
                newRecord["ItemType"] = "7";
                newRecord["Labels"] = "";
                newRecord["ListType"] = "";
                newRecord["Enabled"] = "True";
                newRecord["DataType"] = "0";
                newRecord["LowestDecompositionLevel"] = "5";
                newRecord["Format"] = "";
                newRecord["Min"] = "0";
                newRecord["Max"] = "0";
                newRecord["Locked"] = "False";
                newRecord["SiteTab"] = "-1";
                newRecord["BuildingTab"] = "1";
                newRecord["PBTab"] = "1";
                newRecord["ZoneTab"] = "1";
                newRecord["SurfaceTab"] = "1";
                newRecord["OpeningTab"] = "1";
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
                newRecord["MyTab"] = "1";
                newRecord["Id"] = "0";
                newRecord["Dirty"] = "False";
                newRecord["TemplateIdName"] = "";
                newRecord["Prefix"] = "";
                newRecord["Units"] = "UNKNOWN";
                newRecord["ShowNCM"] = "0";
                newRecord["ShowEnergyPlus"] = "1";
                newRecord["ShowKLIMA"] = "1";
                newRecord["ShowPT"] = "1";
                newRecord["ShowFR"] = "1";
                newRecord["ShowES"] = "1";
                newRecord["ShowDE"] = "1";
                newRecord["ShowDBSim"] = "1";
                newRecord["ShowL5"] = "1";
            }

            attributeName = "ExampleUICheckbox";
            newRecord = GetRecordByName(FormatsTable, attributeName);
            if (newRecord == null)
            {
                newRecord = FormatsTable.AddRecord();
                newRecord["Name"] = attributeName;
                newRecord["Caption"] = "Checkbox";
                newRecord["IsData"] = "1";
                newRecord["Default"] = "Test";
                newRecord["Indent"] = "1";
                newRecord["OptionGroup"] = "0";
                newRecord["ItemType"] = "1";
                newRecord["Labels"] = "";
                newRecord["ListType"] = "";
                newRecord["Enabled"] = "True";
                newRecord["DataType"] = "0";
                newRecord["LowestDecompositionLevel"] = "5";
                newRecord["Format"] = "0.00";
                newRecord["Min"] = "0";
                newRecord["Max"] = "0";
                newRecord["Locked"] = "False";
                newRecord["SiteTab"] = "-1";
                newRecord["BuildingTab"] = "1";
                newRecord["PBTab"] = "1";
                newRecord["ZoneTab"] = "1";
                newRecord["SurfaceTab"] = "1";
                newRecord["OpeningTab"] = "1";
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
                newRecord["MyTab"] = "1";
                newRecord["Id"] = "0";
                newRecord["Dirty"] = "False";
                newRecord["TemplateIdName"] = "";
                newRecord["Prefix"] = "";
                newRecord["Units"] = "UNKNOWN";
                newRecord["ShowNCM"] = "0";
                newRecord["ShowEnergyPlus"] = "1";
                newRecord["ShowKLIMA"] = "1";
                newRecord["ShowPT"] = "1";
                newRecord["ShowFR"] = "1";
                newRecord["ShowES"] = "1";
                newRecord["ShowDE"] = "1";
                newRecord["ShowDBSim"] = "1";
                newRecord["ShowL5"] = "1";
            }

            attributeName = "ExampleUIDistanceEdit";
            newRecord = GetRecordByName(FormatsTable, attributeName);
            if (newRecord == null)
            {
                newRecord = FormatsTable.AddRecord();
                newRecord["Name"] = attributeName;
                newRecord["Caption"] = "Numeric Edit";
                newRecord["IsData"] = "1";
                newRecord["Default"] = "5";
                newRecord["Indent"] = "2";
                newRecord["OptionGroup"] = "0";
                newRecord["ItemType"] = "7";
                newRecord["Labels"] = "";
                newRecord["ListType"] = "";
                newRecord["Enabled"] = "True";
                newRecord["DataType"] = "0";
                newRecord["LowestDecompositionLevel"] = "5";
                newRecord["Format"] = "0.00";
                newRecord["Min"] = "0";
                newRecord["Max"] = "50";
                newRecord["Locked"] = "False";
                newRecord["SiteTab"] = "-1";
                newRecord["BuildingTab"] = "1";
                newRecord["PBTab"] = "1";
                newRecord["ZoneTab"] = "1";
                newRecord["SurfaceTab"] = "1";
                newRecord["OpeningTab"] = "1";
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
                newRecord["MyTab"] = "1";
                newRecord["Id"] = "0";
                newRecord["Dirty"] = "False";
                newRecord["TemplateIdName"] = "";
                newRecord["Prefix"] = "";
                newRecord["Units"] = "43";
                newRecord["ShowNCM"] = "0";
                newRecord["ShowEnergyPlus"] = "1";
                newRecord["ShowKLIMA"] = "1";
                newRecord["ShowPT"] = "1";
                newRecord["ShowFR"] = "1";
                newRecord["ShowES"] = "1";
                newRecord["ShowDE"] = "1";
                newRecord["ShowDBSim"] = "1";
                newRecord["ShowL5"] = "1";
            }
        }

        public void AddAttributesToModel()
        {
            ModelSite = ApiEnvironment.Site;
            AddAttribute("ExampleUIHeader");
            AddAttribute("ExampleUITextEdit");
            AddAttribute("ExampleUICheckbox");
            AddAttribute("ExampleUIDistanceEdit");
        }

        private void AddAttribute(string attributeName)
        {
            Record userRecord = GetRecordByName(FormatsTable, attributeName);
            if (userRecord != null)
            {
                if (ModelSite.GetAttribute(attributeName) == "UNKNOWN")
                {
                    ModelSite.AddAttribute(attributeName, userRecord["Default"]);
                }
            }
            else
            {
                MessageBox.Show(attributeName + " record does not exist");
            }
        }
    }
}
