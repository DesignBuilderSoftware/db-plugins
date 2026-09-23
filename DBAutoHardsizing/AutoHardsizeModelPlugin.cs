using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

using DB.Api;
using DB.Extensibility.Contracts;

using DbEnvironment = DB.Api.Environment;

namespace DBAutoHardsizeModel
{
    /// <summary>A single pending model-level change.</summary>
    public class PendingChange
    {
        public string ZoneOrLoop;
        public string ComponentType;
        public string Title;
        public string Attribute;
        public string CurrentValue;
        public string NewValue;
        public string EioType;
        public bool IsZero;
        public HvacComponent Component;   // null when the target is a loop
        public HvacLoop Loop;             // null when the target is a component
    }

    /// <summary>
    /// DesignBuilder plugin: Auto hardsize (model level).
    ///
    /// Writes EnergyPlus sizing results from the last autosized run back into the
    /// DesignBuilder model itself, so the values appear in the interface, persist
    /// in the .dsb, and survive IDF regeneration - unlike editing the generated
    /// IDF, which DesignBuilder overwrites on every run.
    ///
    /// WORKFLOW
    ///   1. Run an autosized simulation (produces eplusout.eio).
    ///   2. Hardsizing -> Auto hardsize (model).
    ///   3. Review the confirmation dialog, apply.
    ///   4. The HVAC dialogs now show fixed numbers instead of "autosize".
    ///
    /// THE RULE FOR WHAT GETS CHANGED
    /// Only attributes whose current value is literally "autosize" are touched.
    /// Everything else is left exactly as it is. The autosize-capable set is
    /// defined by DesignBuilder itself (Formats tables, ItemType 77) and is
    /// encoded in ModelHardsizeMap.
    ///
    /// Note the marker's capitalisation is inconsistent in DesignBuilder -
    /// plant components use "Autosize", zone and air components use "autosize",
    /// and heating coils use both - so the comparison is case-insensitive.
    ///
    /// DISCLAIMER: provided as-is. This one DOES modify your model. Take a
    /// backup of the .dsb before running it.
    /// </summary>
    [Export(typeof(IPlugin2))]
    public class AutoHardsizeModelPlugin : PluginBase2, IPlugin2
    {
        private static class MenuKeys
        {
            public const string Root = "hardsizeRoot";
            public const string Run = "hardsizeModel";
        }

        private readonly Dictionary<string, string> _unmapped =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Count of attributes that are autosize-capable in DesignBuilder but do
        /// not apply to the component's current configuration - a water-coil field
        /// on an electric coil, a controller field on a zone coil, and so on.
        /// Counted rather than listed: these are normal and listing them buries
        /// the anomalies that do matter.
        /// </summary>
        private int _notApplicable;

        /// <summary>
        /// Notes a component type that has no mapping. Only reported to the user
        /// if it turns out to hold autosized values, since most unmapped types
        /// (splitters, mixers, nodes) legitimately have nothing to size.
        /// </summary>
        private void NoteUnmapped(string componentType, string title)
        {
            if (_unmapped.ContainsKey(componentType)) return;
            _unmapped[componentType] = title ?? "";
        }

        /// <summary>
        /// Probes an unmapped component for values of "autosize" using the union
        /// of every attribute name in the map. A hit means the component holds
        /// sizing data we are not currently handling - worth telling the user
        /// about, unlike a splitter or a node which has nothing to size.
        /// </summary>
        private static bool HoldsAutosizedValue(HvacComponent component)
        {
            foreach (var entries in ModelHardsizeMap.ByComponentType.Values)
            {
                foreach (var entry in entries)
                {
                    string v = Attr(component, entry.Attribute);
                    if (!IsUnknown(v) && IsAutosize(v)) return true;
                }
            }
            return false;
        }

        private bool _modelLoaded;

        public override bool HasMenu { get { return true; } }

        public override string MenuLayout
        {
            get
            {
                var menu = new StringBuilder();
                menu.AppendFormat("*Hardsizing,{0}", MenuKeys.Root);
                menu.AppendFormat("*>Auto hardsize (model),{0}", MenuKeys.Run);
                return menu.ToString();
            }
        }

        public override bool IsMenuItemVisible(string key) { return true; }
        public override bool IsMenuItemEnabled(string key) { return _modelLoaded; }
        public override void ModelLoaded() { _modelLoaded = true; }
        public override void ModelUnloaded() { _modelLoaded = false; }

        public override void OnMenuItemPressed(string key)
        {
            if (key != MenuKeys.Run) return;
            try { Run(); }
            catch (Exception ex)
            {
                MessageBox.Show("Auto hardsize failed.\n\n" + ex.GetType().Name + ": " +
                    ex.Message + "\n\n" + ex.StackTrace, "Auto hardsize",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ------------------------------------------------------------------
        private void Run()
        {
            _unmapped.Clear();
            _notApplicable = 0;

            string eioPath = EioSizingReader.ResolveEioPath(ApiEnvironment.EnergyPlusInputIdfPath);
            if (eioPath == null)
            {
                MessageBox.Show(
                    "No .eio file found from a previous autosized run.\n\n" +
                    "Run a simulation first, then try Auto hardsize again.",
                    "Auto hardsize", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var eio = new EioSizingReader(eioPath);
            var changes = new List<PendingChange>();
            var skipped = new List<string>();

            Collect(eio, changes, skipped);

            // Surface any component type that holds autosized data but has no
            // mapping. Normally empty; a populated list means either a component
            // family we have not covered, or an enum member renamed by a
            // DesignBuilder update.
            foreach (var kv in _unmapped)
            {
                skipped.Add("UNMAPPED component type \"" + kv.Key + "\" (e.g. \"" +
                            kv.Value + "\") holds autosized values but is not in " +
                            "the mapping - please report this.");
            }

            if (changes.Count == 0)
            {
                MessageBox.Show(
                    "Nothing to hardsize.\n\n" +
                    "Either no in-scope components are currently autosized, or the " +
                    ".eio has no matching sizing results.\n\n" +
                    "Sizing rows read: " + eio.RowCount.ToString(CultureInfo.InvariantCulture) +
                    (skipped.Count > 0
                        ? "\nSkipped: " + skipped.Count.ToString(CultureInfo.InvariantCulture)
                        : ""),
                    "Auto hardsize", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new frmConfirm(changes, skipped, Path.GetFileName(eioPath),
                                            _notApplicable))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                Apply(dlg.Accepted);
            }
        }

        // ------------------------------------------------------------------
        // Walk the network and match against the .eio
        // ------------------------------------------------------------------
        private void Collect(EioSizingReader eio, List<PendingChange> changes,
                             List<string> skipped)
        {
            Site site = ApiEnvironment.Site;
            if (site == null) return;

            for (int b = 0; b < site.Buildings.Count; b++)
            {
                HvacNetwork network;
                try { network = site.Buildings[b].HvacNetwork; }
                catch { continue; }
                if (network == null) continue;

                foreach (HvacLoop loop in network.Loops)
                {
                    string loopTitle = Attr(loop, "Title");

                    CollectSubLoop(eio, changes, skipped, loop, loopTitle,
                        Try(() => loop.SupplySubLoop));
                    CollectSubLoop(eio, changes, skipped, loop, loopTitle,
                        Try(() => loop.DemandSubLoop));
                }

                foreach (HvacZoneGroup group in network.ZoneGroups)
                {
                    foreach (HvacZone zone in group.Zones)
                    {
                        string zoneName = ZoneName(zone);
                        foreach (HvacComponent c in zone.Components)
                            CollectComponent(eio, changes, skipped, c, zoneName, null);
                    }
                }
            }
        }

        private void CollectSubLoop(EioSizingReader eio, List<PendingChange> changes,
                                    List<string> skipped, HvacLoop loop, string loopTitle,
                                    HvacSubLoop subLoop)
        {
            if (subLoop == null) return;
            foreach (HvacComponent c in subLoop.Components)
                CollectComponent(eio, changes, skipped, c, loopTitle, loopTitle);
        }

        private void CollectComponent(EioSizingReader eio, List<PendingChange> changes,
                                      List<string> skipped, HvacComponent component,
                                      string container, string parentLoopTitle)
        {
            if (component == null) return;

            string componentType = Try(() => component.ComponentType.ToString()) ?? "";
            string title = Attr(component, "Title");

            var entries = ModelHardsizeMap.For(componentType);
            if (entries == null && !string.IsNullOrEmpty(componentType))
            {
                // No mapping for this component type. That is expected for
                // splitters, mixers, nodes and so on, but it would also be the
                // symptom of an enum value being renamed in a future release
                // (the air distribution unit currently arrives as the bare
                // integer "95" because its enum member is undefined).
                // Record it so a silent regression cannot hide here - but only if
                // the component actually holds autosized data, otherwise every
                // splitter, mixer and node would be reported as noise.
                if (HoldsAutosizedValue(component))
                    NoteUnmapped(componentType, title);
            }

            if (entries != null && !string.IsNullOrEmpty(title))
            {
                foreach (var entry in entries)
                {
                    string current = Attr(component, entry.Attribute);

                    // "UNKNOWN" means the attribute does not apply to this
                    // component's sub-type. Anything not literally "autosize"
                    // is a value the user has already set - leave it alone.
                    if (IsUnknown(current)) continue;
                    if (!IsAutosize(current)) continue;

                    string lookupName = DeriveName(entry.Rule, title, parentLoopTitle);
                    string value, matchedType;

                    if (!eio.TryGetValue(entry.EioTypes, lookupName,
                                         entry.EioDescription, out value, out matchedType))
                    {
                        // Distinguish two very different situations.
                        //
                        // (a) The object does not exist in the .eio under any of
                        //     this entry's EnergyPlus types. The attribute is
                        //     dormant for this component's configuration -
                        //     DesignBuilder stores the full superset of coil
                        //     attributes regardless of the Type dropdown, so an
                        //     electric heating coil still reports RatedCapacity as
                        //     "autosize" even though it is never written to the
                        //     IDF. Likewise zone coils have no Controller:WaterCoil
                        //     object at all. Nothing is wrong; do not alarm the user.
                        //
                        // (b) The object IS in the .eio but this particular sizing
                        //     row is absent. That is a real anomaly worth reporting.
                        if (!eio.HasObject(entry.EioTypes, lookupName))
                            _notApplicable++;
                        else
                            skipped.Add(ModelHardsizeMap.DisplayName(componentType) +
                                        " :: " + title + " :: " + entry.Attribute +
                                        "  (object found in .eio but no \"" +
                                        entry.EioDescription + "\" row)");
                        continue;
                    }

                    changes.Add(new PendingChange
                    {
                        ZoneOrLoop = container,
                        ComponentType = componentType,
                        Title = title,
                        Attribute = entry.Attribute,
                        CurrentValue = current,
                        NewValue = value,
                        EioType = matchedType,
                        IsZero = IsZero(value),
                        Component = component
                    });
                }
            }

            // Composite units: FCU -> fan + coils, PTAC -> DX coil + coil + fan.
            try
            {
                foreach (HvacComponent sub in component.SubComponents)
                    CollectComponent(eio, changes, skipped, sub, container, parentLoopTitle);
            }
            catch { }
        }

        private static string DeriveName(NameRule rule, string title, string parentLoopTitle)
        {
            switch (rule)
            {
                case NameRule.TitlePlusController:
                    return title + " Controller";
                case NameRule.TitlePlusOutdoorAirController:
                    return title + " Outdoor Air Controller";
                case NameRule.ParentLoopTitle:
                    return parentLoopTitle ?? title;
                default:
                    return title;
            }
        }

        // ------------------------------------------------------------------
        private void Apply(List<PendingChange> accepted)
        {
            int applied = 0;
            var failures = new List<string>();

            // Components touched, so UpdateAttributeData is called once each
            // rather than once per attribute.
            var touched = new List<HvacComponent>();

            foreach (var change in accepted)
            {
                try
                {
                    if (change.Component != null)
                    {
                        change.Component.SetAttribute(change.Attribute, change.NewValue);
                        if (!touched.Contains(change.Component)) touched.Add(change.Component);
                    }
                    else if (change.Loop != null)
                    {
                        change.Loop.SetAttribute(change.Attribute, change.NewValue);
                        change.Loop.UpdateAttributeData();
                    }
                    applied++;
                }
                catch (Exception ex)
                {
                    failures.Add(change.Title + " :: " + change.Attribute + " - " + ex.Message);
                }
            }

            // REQUIRED: without this the HVAC network is not synchronised with the
            // attribute changes and the writes may not stick.
            foreach (var component in touched)
            {
                try { component.UpdateAttributeData(); }
                catch (Exception ex) { failures.Add("UpdateAttributeData: " + ex.Message); }
            }

            var msg = new StringBuilder();
            msg.AppendLine("Hardsized " + applied.ToString(CultureInfo.InvariantCulture) +
                           " parameter(s) in the model.");
            msg.AppendLine();
            msg.AppendLine("The HVAC dialogs will now show fixed values instead of " +
                           "\"autosize\". Save the model to keep them.");
            if (failures.Count > 0)
            {
                msg.AppendLine();
                msg.AppendLine("Failures (" + failures.Count.ToString(CultureInfo.InvariantCulture) + "):");
                foreach (string f in failures) msg.AppendLine("   " + f);
            }

            MessageBox.Show(msg.ToString(), "Auto hardsize",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ------------------------------------------------------------------
        private static string ZoneName(HvacZone zone)
        {
            try
            {
                Zone building = zone.BuildingZone;
                if (building != null)
                {
                    string t = building.GetAttribute("Title");
                    if (!string.IsNullOrEmpty(t) && !IsUnknown(t)) return t;
                }
            }
            catch { }
            return "(zone)";
        }

        private static string Attr(HvacComponent c, string name)
        {
            try { return c.GetAttribute(name) ?? ""; } catch { return ""; }
        }

        private static string Attr(HvacLoop l, string name)
        {
            try { return l.GetAttribute(name) ?? ""; } catch { return ""; }
        }

        private static T Try<T>(Func<T> f) where T : class
        {
            try { return f(); } catch { return null; }
        }

        /// <summary>GetAttribute returns the literal "UNKNOWN" when an attribute
        /// does not exist on the object, rather than null or empty.</summary>
        private static bool IsUnknown(string v)
        {
            return v == null || v.Trim().Length == 0 ||
                   v.Trim().Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAutosize(string v)
        {
            if (v == null) return false;
            string t = v.Trim();
            // Capitalisation is inconsistent across component families.
            return t.Equals("autosize", StringComparison.OrdinalIgnoreCase)
                || t.Equals("autocalculate", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsZero(string v)
        {
            double d;
            return double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out d)
                   && Math.Abs(d) < 1e-12;
        }
    }

    // ======================================================================
    /// <summary>
    /// Confirmation dialog. Lists every object and parameter that will change,
    /// grouped by component type, with per-type tick boxes.
    ///
    /// Zero-valued results are excluded by default: writing 0 into a capacity or
    /// flow fixes that component at zero permanently, which is a very different
    /// outcome from leaving it autosized.
    /// </summary>
    public class frmConfirm : Form
    {
        private readonly List<PendingChange> _all;
        private readonly List<string> _skipped;
        private CheckedListBox _types;
        private CheckBox _includeZeros;
        private TextBox _detail;
        private Label _summary;
        private Button _ok;

        public List<PendingChange> Accepted { get; private set; }

        private readonly int _notApplicable;

        public frmConfirm(List<PendingChange> changes, List<string> skipped, string eioName,
                          int notApplicable)
        {
            _all = changes;
            _skipped = skipped ?? new List<string>();
            _notApplicable = notApplicable;
            Accepted = new List<PendingChange>();

            Text = "Auto hardsize (model) - confirm   [source: " + eioName + "]";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(820, 580);
            MinimumSize = new Size(640, 420);
            MinimizeBox = false;
            MaximizeBox = false;

            _summary = new Label { Dock = DockStyle.Top, Height = 46,
                                   Padding = new Padding(10, 8, 10, 0) };

            _types = new CheckedListBox { Dock = DockStyle.Left, Width = 260,
                                          CheckOnClick = true, IntegralHeight = false };
            // ItemCheck is wired AFTER the list is populated - see below. Adding
            // items with a check state fires ItemCheck, and the handler needs the
            // form's window handle, which does not exist until the dialog is shown.

            _detail = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
                                    ScrollBars = ScrollBars.Both, WordWrap = false,
                                    Font = new Font(FontFamily.GenericMonospace, 8.5f) };

            _includeZeros = new CheckBox { Dock = DockStyle.Bottom, Height = 28,
                Padding = new Padding(10, 4, 10, 0), Checked = false,
                Text = "Also apply zero-valued results (fixes that component at zero - not recommended)" };
            _includeZeros.CheckedChanged += (s, e) => Refresh2();

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46,
                FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10, 8, 10, 8) };
            var cancel = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel };
            _ok = new Button { Text = "Hardsize", Width = 110, DialogResult = DialogResult.OK };
            _ok.Click += (s, e) => Accepted = Selected();
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(_ok);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 0) };
            body.Controls.Add(_detail);
            body.Controls.Add(_types);

            Controls.Add(body);
            Controls.Add(_includeZeros);
            Controls.Add(buttons);
            Controls.Add(_summary);
            AcceptButton = _ok;
            CancelButton = cancel;

            // The list shows friendly labels rather than raw enum text, so the
            // air distribution unit reads "Air terminal / ADU" instead of "95".
            var seen = new List<string>();
            foreach (var c in _all)
            {
                string label = ModelHardsizeMap.DisplayName(c.ComponentType);
                if (!seen.Contains(label)) seen.Add(label);
            }
            seen.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string t in seen) _types.Items.Add(t, true);

            // Wire ItemCheck only now that the list is populated.
            // BeginInvoke is needed because ItemCheck fires BEFORE the check state
            // is committed, so GetItemChecked would still return the old value.
            // The IsHandleCreated guard covers any other pre-show state change.
            _types.ItemCheck += (s, e) =>
            {
                if (!IsHandleCreated) return;
                BeginInvoke((Action)Refresh2);
            };

            Refresh2();
        }

        private bool Checked(string componentType)
        {
            string label = ModelHardsizeMap.DisplayName(componentType);
            for (int i = 0; i < _types.Items.Count; i++)
                if (string.Equals((string)_types.Items[i], label,
                        StringComparison.OrdinalIgnoreCase))
                    return _types.GetItemChecked(i);
            return false;
        }

        private List<PendingChange> Selected()
        {
            var result = new List<PendingChange>();
            foreach (var c in _all)
            {
                if (!Checked(c.ComponentType)) continue;
                if (c.IsZero && !_includeZeros.Checked) continue;
                result.Add(c);
            }
            return result;
        }

        private void Refresh2()
        {
            var sel = Selected();

            var objects = new List<string>();
            foreach (var c in sel)
                if (!objects.Contains(c.Title)) objects.Add(c.Title);

            _summary.Text = string.Format(
                "{0} parameter(s) across {1} object(s) will change from \"autosize\" " +
                "to a fixed value.\nEverything else is left untouched.",
                sel.Count, objects.Count);

            var sb = new StringBuilder();
            string last = null;
            foreach (var c in sel)
            {
                if (c.Title != last)
                {
                    sb.AppendLine();
                    sb.AppendLine(ModelHardsizeMap.DisplayName(c.ComponentType) +
                                  " :: " + c.Title + "   [" + c.ZoneOrLoop + "]");
                    last = c.Title;
                }
                sb.AppendLine("    " + c.Attribute.PadRight(48) + " autosize -> " + c.NewValue);
            }

            var zeros = new List<PendingChange>();
            foreach (var c in _all) if (c.IsZero) zeros.Add(c);
            if (zeros.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(new string('-', 84));
                sb.AppendLine("WARNING - the sizing run returned zero for these:");
                sb.AppendLine("Writing 0 fixes the component at zero capacity or flow for every");
                sb.AppendLine("later run, including your ECMs. Left as autosize unless ticked below.");
                sb.AppendLine();
                foreach (var c in zeros)
                    sb.AppendLine("    " + c.Title + " :: " + c.Attribute + " = " + c.NewValue);
            }

            if (_notApplicable > 0)
            {
                sb.AppendLine();
                sb.AppendLine(new string('-', 84));
                sb.AppendLine(_notApplicable.ToString(CultureInfo.InvariantCulture) +
                              " attribute(s) left as autosize because they do not apply");
                sb.AppendLine("to the component's configuration - for example a water-coil");
                sb.AppendLine("capacity on an electric coil, or a controller setting on a zone");
                sb.AppendLine("coil that has no controller. DesignBuilder stores these fields");
                sb.AppendLine("regardless of coil type, but never writes them to the IDF, so");
                sb.AppendLine("EnergyPlus never sizes them. This is normal.");
            }

            if (_skipped.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(new string('-', 84));
                sb.AppendLine("NEEDS ATTENTION - the object exists in the .eio but the expected");
                sb.AppendLine("sizing row is missing (" +
                              _skipped.Count.ToString(CultureInfo.InvariantCulture) + "):");
                sb.AppendLine();
                foreach (string s in _skipped) sb.AppendLine("    " + s);
            }

            _detail.Text = sb.ToString();
            _ok.Enabled = sel.Count > 0;
        }
    }
}
