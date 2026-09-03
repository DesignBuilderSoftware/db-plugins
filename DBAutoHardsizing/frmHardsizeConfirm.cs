using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DBAutoHardsize
{
    /// <summary>
    /// One pending change: an autosized IDF field with a sizing value from .eio.
    /// </summary>
    public class PendingChange
    {
        public string Category;
        public string ObjectType;
        public string ObjectName;
        public string FieldName;
        public string NewValue;
        public bool IsZero;
        public IdfDocument.Field Field;
    }

    /// <summary>
    /// Confirmation dialog for Auto hardsize.
    ///
    /// Built in code rather than the designer so the plugin stays a drop-in set
    /// of .cs files with no .resx to keep in sync.
    ///
    /// Shows every object that will be changed, grouped by category, with
    /// per-category tick boxes. Zero-valued sizing results are called out
    /// separately and excluded by default, because writing 0 into a capacity or
    /// flow field permanently disables that component - see the AHU heating coil
    /// in the sample model, which autosized to 0 W.
    /// </summary>
    public class frmHardsizeConfirm : Form
    {
        private readonly List<PendingChange> _changes;
        private readonly List<string> _unmatched;

        private CheckedListBox _clbCategories;
        private CheckBox _chkIncludeZeros;
        private TextBox _txtDetail;
        private Label _lblSummary;
        private Button _btnOk;
        private Button _btnCancel;

        public List<PendingChange> Accepted { get; private set; }

        public frmHardsizeConfirm(List<PendingChange> changes, List<string> unmatched)
        {
            _changes = changes ?? new List<PendingChange>();
            _unmatched = unmatched ?? new List<string>();
            Accepted = new List<PendingChange>();
            BuildUi();
            PopulateCategories();
            RefreshDetail();
        }

        private void BuildUi()
        {
            Text = "Auto hardsize - confirm";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(780, 560);
            MinimumSize = new Size(640, 420);

            _lblSummary = new Label
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(10, 8, 10, 0),
                Text = ""
            };

            _clbCategories = new CheckedListBox
            {
                Dock = DockStyle.Left,
                Width = 240,
                CheckOnClick = true,
                IntegralHeight = false
            };
            _clbCategories.ItemCheck += (s, e) =>
            {
                // ItemCheck fires while items are being added, before the form's
                // window handle exists, and BeginInvoke throws in that state.
                if (!IsHandleCreated) return;
                BeginInvoke((Action)RefreshDetail);
            };

            _txtDetail = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font(FontFamily.GenericMonospace, 8.5f)
            };

            _chkIncludeZeros = new CheckBox
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Padding = new Padding(10, 4, 10, 0),
                Text = "Also apply zero-valued sizing results (not recommended - see warnings)",
                Checked = false
            };
            _chkIncludeZeros.CheckedChanged += (s, e) => RefreshDetail();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10, 8, 10, 8)
            };

            _btnCancel = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel };
            _btnOk = new Button { Text = "Hardsize", Width = 110, DialogResult = DialogResult.OK };
            _btnOk.Click += (s, e) => { Accepted = Selected(); };

            buttons.Controls.Add(_btnCancel);
            buttons.Controls.Add(_btnOk);

            var split = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 0) };
            split.Controls.Add(_txtDetail);
            split.Controls.Add(_clbCategories);

            Controls.Add(split);
            Controls.Add(_chkIncludeZeros);
            Controls.Add(buttons);
            Controls.Add(_lblSummary);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void PopulateCategories()
        {
            var seen = new List<string>();
            foreach (var c in _changes)
            {
                string label = c.Category + "  -  " + c.ObjectType;
                if (!seen.Contains(label)) seen.Add(label);
            }
            seen.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (var label in seen)
                _clbCategories.Items.Add(label, true);
        }

        private bool IsCategoryChecked(PendingChange c)
        {
            string label = c.Category + "  -  " + c.ObjectType;
            for (int i = 0; i < _clbCategories.Items.Count; i++)
            {
                if (string.Equals((string)_clbCategories.Items[i], label,
                        StringComparison.OrdinalIgnoreCase))
                    return _clbCategories.GetItemChecked(i);
            }
            return false;
        }

        private List<PendingChange> Selected()
        {
            var result = new List<PendingChange>();
            foreach (var c in _changes)
            {
                if (!IsCategoryChecked(c)) continue;
                if (c.IsZero && !_chkIncludeZeros.Checked) continue;
                result.Add(c);
            }
            return result;
        }

        private void RefreshDetail()
        {
            var selected = Selected();

            int objectCount = 0;
            var seenObjects = new List<string>();
            foreach (var c in selected)
            {
                string k = c.ObjectType + "|" + c.ObjectName;
                if (!seenObjects.Contains(k)) { seenObjects.Add(k); objectCount++; }
            }

            _lblSummary.Text = string.Format(
                "The following {0} parameter(s) across {1} object(s) will be changed from " +
                "Autosize to a fixed value taken from the last autosized run.",
                selected.Count, objectCount);

            var sb = new System.Text.StringBuilder();

            string lastKey = null;
            foreach (var c in selected)
            {
                string key = c.ObjectType + "|" + c.ObjectName;
                if (key != lastKey)
                {
                    sb.AppendLine();
                    sb.AppendLine(c.ObjectType + " :: " + c.ObjectName);
                    lastKey = key;
                }
                sb.AppendLine("    " + c.FieldName.PadRight(42) + " = " + c.NewValue);
            }

            var zeros = new List<PendingChange>();
            foreach (var c in _changes) if (c.IsZero) zeros.Add(c);

            if (zeros.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(new string('-', 78));
                sb.AppendLine("WARNING - sizing run returned zero for these parameters:");
                sb.AppendLine("Writing 0 fixes the component at zero capacity/flow, which disables it");
                sb.AppendLine("for every subsequent run including your ECMs. Left as Autosize unless");
                sb.AppendLine("you tick the box below.");
                sb.AppendLine();
                foreach (var c in zeros)
                    sb.AppendLine("    " + c.ObjectType + " :: " + c.ObjectName +
                                  " :: " + c.FieldName + " = " + c.NewValue);
            }

            if (_unmatched.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(new string('-', 78));
                sb.AppendLine("Left as Autosize (no matching sizing result in the .eio):");
                sb.AppendLine();
                foreach (var u in _unmatched) sb.AppendLine("    " + u);
            }

            _txtDetail.Text = sb.ToString();
            _btnOk.Enabled = selected.Count > 0;
        }
    }
}
