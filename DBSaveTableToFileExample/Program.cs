using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel.Composition;
using DB.Extensibility.Contracts;
using DB.Api;

namespace DBSaveTableToFileExample
{
    [Export(typeof(IPlugin2))]
    public class ExamplePlugin : PluginBase2, IPlugin2
    {
        class MenuKeys
        {
            public const string Root = "root";
            public const string ListTables = "listTables";
            public const string SaveTable = "saveTable";
            public const string SaveAllTables = "saveAllTables";
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
                menu.AppendFormat("*Table Export,{0}", MenuKeys.Root);
                menu.AppendFormat("*>List Available Tables,{0}", MenuKeys.ListTables);
                menu.AppendFormat("*>Save Table To File...,{0}", MenuKeys.SaveTable);
                menu.AppendFormat("*>Save All Tables To Folder...,{0}", MenuKeys.SaveAllTables);
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
            // TableOfTables lives on ApplicationTemplates, so listing tables doesn't need a model loaded.
            mMenuItems.Add(MenuKeys.ListTables, new MenuItem(ListAvailableTables));
            mMenuItems.Add(MenuKeys.SaveTable, new MenuItem(SaveTableToFile));
            mMenuItems.Add(MenuKeys.SaveAllTables, new MenuItem(SaveAllTablesToFolder));
        }

        /// <summary>
        /// TableOfTables is the master table listing every table DesignBuilder knows about (component
        /// databases, template databases, application-only data, etc.) - see Data\TableOfTables.dat in
        /// the DesignBuilder install. Each record's "Name" field is the string you'd pass to GetTable.
        /// </summary>
        private List<KeyValuePair<string, string>> GetAvailableTableNames()
        {
            Table tableOfTables = ApiEnvironment.ApplicationTemplates.GetTable("TableOfTables");

            List<KeyValuePair<string, string>> tables = new List<KeyValuePair<string, string>>();
            foreach (Record record in tableOfTables.Records)
            {
                tables.Add(new KeyValuePair<string, string>(record["Name"], record["Title"]));
            }
            return tables;
        }

        public void ListAvailableTables()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("Available tables");
            report.AppendLine("-----------------");
            report.AppendLine();

            foreach (KeyValuePair<string, string> table in GetAvailableTableNames())
            {
                report.AppendFormat("{0,-40} {1}{2}", table.Key, table.Value, System.Environment.NewLine);
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = "AvailableTables.txt",
                InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, report.ToString());

                if (MessageBox.Show(@"List generated successfully. Would you like to open it?", @"List Complete",
                        MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start(saveFileDialog.FileName);
                }
            }
        }

        /// <summary>
        /// Lets the user pick a table name from a filterable list (rather than type one, which is
        /// easy to misspell), resolves it to a Table (trying the model's Site tables first, then the
        /// two library/template scopes that work without a model loaded), and writes it out via the
        /// DB API's own Table.SaveToFile.
        /// </summary>
        public void SaveTableToFile()
        {
            frmTablePicker picker = new frmTablePicker(GetAvailableTableNames());
            if (!picker.ShowMe(out string tableName))
            {
                return;
            }

            Table table = GetTableByName(tableName);
            if (table == null)
            {
                MessageBox.Show(string.Format(
                    "Table '{0}' was not found. It may require a model to be loaded, or the name may be misspelled.",
                    tableName));
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "DesignBuilder table files (*.dat)|*.dat|All files (*.*)|*.*",
                FileName = tableName + ".dat",
                InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                bool saved = table.SaveToFile(saveFileDialog.FileName);

                MessageBox.Show(saved
                    ? string.Format("Table '{0}' saved to {1}", tableName, saveFileDialog.FileName)
                    : string.Format("Failed to save table '{0}'.", tableName));
            }
        }

        /// <summary>
        /// Saves every table listed in TableOfTables into a chosen folder in one go, one .dat file
        /// per table (named after the table). Tables that can't be resolved in the current context
        /// (e.g. Site-scope component tables when no model is loaded) are skipped rather than failing
        /// the whole batch, and are called out in the summary so nothing silently goes missing.
        /// </summary>
        public void SaveAllTablesToFolder()
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog
            {
                Description = "Select a folder to save all tables into"
            })
            {
                if (folderDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                List<string> skipped = new List<string>();
                List<string> failed = new List<string>();
                int savedCount = 0;

                foreach (KeyValuePair<string, string> tableInfo in GetAvailableTableNames())
                {
                    string tableName = tableInfo.Key;
                    Table table = GetTableByName(tableName);

                    if (table == null)
                    {
                        skipped.Add(tableName);
                        continue;
                    }

                    string destPath = Path.Combine(folderDialog.SelectedPath, tableName + ".dat");
                    if (table.SaveToFile(destPath))
                    {
                        savedCount++;
                    }
                    else
                    {
                        failed.Add(tableName);
                    }
                }

                StringBuilder summary = new StringBuilder();
                summary.AppendFormat("Saved {0} table(s) to {1}", savedCount, folderDialog.SelectedPath);
                if (skipped.Count > 0)
                {
                    summary.AppendFormat("{0}{1} skipped (not available without a model / in this context): {2}",
                        System.Environment.NewLine, skipped.Count, string.Join(", ", skipped));
                }
                if (failed.Count > 0)
                {
                    summary.AppendFormat("{0}{1} failed to save: {2}",
                        System.Environment.NewLine, failed.Count, string.Join(", ", failed));
                }

                MessageBox.Show(summary.ToString(), "Save All Tables Complete");
            }
        }

        /// <summary>
        /// Tables live in one of three scopes depending on type (see the "Tables" section of the
        /// DesignBuilder extensibility guide): component tables on the loaded model's Site, template
        /// tables on ApplicationTemplates, and library component tables on ApplicationComponents.
        /// </summary>
        private Table GetTableByName(string tableName)
        {
            Table table = ApiEnvironment.Site.GetTable(tableName);
            if (table != null)
            {
                return table;
            }

            table = ApiEnvironment.ApplicationTemplates.GetTable(tableName);
            if (table != null)
            {
                return table;
            }

            return ApiEnvironment.ApplicationComponents.GetTable(tableName);
        }
    }
}
