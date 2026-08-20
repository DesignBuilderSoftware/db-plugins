using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBSaveTableToFileExample
{
    /// <summary>
    /// Simple filterable picker over a list of table names, used instead of asking the user to type
    /// an exact (and easy to misspell) table name. Built entirely in code rather than the WinForms
    /// designer, since the layout is straightforward: a filter box on top, a listbox filling the rest.
    /// </summary>
    public class frmTablePicker : Form
    {
        private readonly List<TableInfo> mAllTables;
        private readonly TextBox mFilterBox;
        private readonly ListBox mTableList;
        private readonly Button mOkButton;

        public string SelectedTableName { get; private set; }

        private class TableInfo
        {
            public string Name;
            public string Title;

            public override string ToString()
            {
                return string.IsNullOrEmpty(Title) || Title == Name
                    ? Name
                    : string.Format("{0}  ({1})", Name, Title);
            }
        }

        public frmTablePicker(IEnumerable<KeyValuePair<string, string>> tableNamesAndTitles)
        {
            mAllTables = tableNamesAndTitles
                .Select(kvp => new TableInfo { Name = kvp.Key, Title = kvp.Value })
                .OrderBy(t => t.Name)
                .ToList();

            Text = "Select Table";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(420, 440);

            Label filterLabel = new Label
            {
                Text = "Filter:",
                Location = new Point(12, 15),
                AutoSize = true
            };

            mFilterBox = new TextBox
            {
                Location = new Point(60, 12),
                Width = ClientSize.Width - 72,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            mFilterBox.TextChanged += (sender, args) => ApplyFilter();

            mTableList = new ListBox
            {
                Location = new Point(12, 42),
                Size = new Size(ClientSize.Width - 24, ClientSize.Height - 90),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            mTableList.DoubleClick += (sender, args) => Accept();
            mTableList.SelectedIndexChanged += (sender, args) => mOkButton.Enabled = mTableList.SelectedItem != null;

            mOkButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(ClientSize.Width - 170, ClientSize.Height - 36),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Enabled = false
            };
            mOkButton.Click += (sender, args) => Accept();

            Button cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(ClientSize.Width - 88, ClientSize.Height - 36),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            Controls.Add(filterLabel);
            Controls.Add(mFilterBox);
            Controls.Add(mTableList);
            Controls.Add(mOkButton);
            Controls.Add(cancelButton);

            AcceptButton = mOkButton;
            CancelButton = cancelButton;

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string filter = mFilterBox.Text.Trim();

            IEnumerable<TableInfo> matches = string.IsNullOrEmpty(filter)
                ? mAllTables
                : mAllTables.Where(t =>
                    t.Name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (t.Title != null && t.Title.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0));

            mTableList.BeginUpdate();
            mTableList.Items.Clear();
            foreach (TableInfo table in matches)
            {
                mTableList.Items.Add(table);
            }
            mTableList.EndUpdate();

            mOkButton.Enabled = false;
        }

        private void Accept()
        {
            if (mTableList.SelectedItem is TableInfo selected)
            {
                SelectedTableName = selected.Name;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        /// <summary>
        /// Shows the picker modally. Returns true and sets tableName when the user picks one and
        /// presses OK (or double-clicks); returns false on Cancel.
        /// </summary>
        public bool ShowMe(out string tableName)
        {
            bool accepted = ShowDialog() == DialogResult.OK && SelectedTableName != null;
            tableName = accepted ? SelectedTableName : null;
            return accepted;
        }
    }
}
