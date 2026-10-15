using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace DBHvacGridImport
{
    /// <summary>
    /// Confirmation dialog. Lists every value that will change - current and new
    /// value in DesignBuilder's current units, plus the value as written in the file
    /// when it had to be converted - with one tick box per table column, so a whole
    /// column (e.g. every PressureRise) can be left out.
    ///
    /// Built in code rather than the designer, so the plugin has no .resx file to
    /// keep in sync.
    /// </summary>
    public class frmImportConfirm : Form
    {
        private readonly List<PendingChange> mAll;
        private readonly List<string> mProblems;
        private readonly int mUnchanged;
        private readonly UnitSystem mDisplayUnits;
        private readonly CheckedListBox mGroups;
        private readonly TextBox mDetail;
        private readonly Label mSummary;
        private readonly Button mOk;

        public List<PendingChange> Accepted { get; private set; }

        public frmImportConfirm(List<PendingChange> changes, List<string> problems, int unchanged,
                                string fileNames, UnitSystem displayUnits)
        {
            mAll = changes;
            mProblems = problems ?? new List<string>();
            mUnchanged = unchanged;
            mDisplayUnits = displayUnits;
            Accepted = new List<PendingChange>();

            Text = "HVAC Data Import - confirm   [" + fileNames + "]";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1060, 600);
            MinimumSize = new Size(680, 420);
            MinimizeBox = false;
            MaximizeBox = true;

            mSummary = new Label { Dock = DockStyle.Top, Height = 46, Padding = new Padding(10, 8, 10, 0) };

            mGroups = new CheckedListBox { Dock = DockStyle.Left, Width = 300, CheckOnClick = true,
                                           IntegralHeight = false };

            mDetail = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
                                    ScrollBars = ScrollBars.Both, WordWrap = false,
                                    Font = new Font(FontFamily.GenericMonospace, 8.5f) };

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46,
                FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10, 8, 10, 8) };
            var cancel = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel };
            mOk = new Button { Text = "Import", Width = 110, DialogResult = DialogResult.OK };
            mOk.Click += (s, e) => Accepted = Selected();
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(mOk);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 0) };
            body.Controls.Add(mDetail);
            body.Controls.Add(mGroups);

            Controls.Add(body);
            Controls.Add(buttons);
            Controls.Add(mSummary);
            AcceptButton = mOk;
            CancelButton = cancel;

            var groups = new List<string>();
            foreach (PendingChange change in mAll)
            {
                if (!groups.Contains(change.Group)) groups.Add(change.Group);
            }
            groups.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string group in groups)
            {
                mGroups.Items.Add(group, true);
            }

            // Wired only after the list is filled: adding checked items fires ItemCheck,
            // and BeginInvoke needs the window handle, which does not exist until the
            // dialog is shown. BeginInvoke itself is needed because ItemCheck fires
            // before the new check state is committed.
            mGroups.ItemCheck += (s, e) =>
            {
                if (!IsHandleCreated) return;
                BeginInvoke((Action)RefreshDetail);
            };

            RefreshDetail();
        }

        private List<PendingChange> Selected()
        {
            var checkedGroups = new List<string>();
            foreach (object item in mGroups.CheckedItems)
            {
                checkedGroups.Add((string)item);
            }
            return mAll.FindAll(c => checkedGroups.Contains(c.Group));
        }

        private void RefreshDetail()
        {
            List<PendingChange> selected = Selected();

            var components = new List<string>();
            foreach (PendingChange change in selected)
            {
                if (!components.Contains(change.Title)) components.Add(change.Title);
            }

            mSummary.Text = string.Format(CultureInfo.InvariantCulture,
                "{0} value(s) on {1} component(s) will change. Values are shown in {2} units, as currently " +
                "set in DesignBuilder.\n{3} value(s) already match the model and are left alone.",
                selected.Count, components.Count, mDisplayUnits, mUnchanged);

            // Group the lines by component, keeping the file order within each one.
            var sb = new StringBuilder();
            foreach (string title in components)
            {
                List<PendingChange> lines = selected.FindAll(c => c.Title == title);
                sb.AppendLine();
                sb.AppendLine(title + "   [" + lines[0].Container + "]");
                foreach (PendingChange change in lines)
                {
                    // The file value is shown only when it was converted to DesignBuilder's units;
                    // the note says which fan a unit's flow rate came from.
                    string converted = change.FileDisplay.Length > 0 ? "(file: " + change.FileDisplay + ")  " : "";
                    string source = change.Note.Length > 0 ? "[" + change.Note + "]" : "";
                    sb.AppendLine("    " + change.Attribute.PadRight(40) +
                                  change.CurrentDisplay.PadLeft(18) + "  ->  " +
                                  change.NewDisplay.PadRight(20) + converted + source);
                }
            }

            if (mProblems.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(new string('-', 100));
                sb.AppendLine("NOT IMPORTED (" + mProblems.Count.ToString(CultureInfo.InvariantCulture) + "):");
                sb.AppendLine();
                foreach (string problem in mProblems)
                {
                    sb.AppendLine("    " + problem);
                }
            }

            mDetail.Text = sb.ToString();
            mOk.Enabled = selected.Count > 0;
        }
    }
}
