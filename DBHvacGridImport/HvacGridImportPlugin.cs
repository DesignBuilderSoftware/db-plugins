/*
HVAC Data Import - DesignBuilder plugin: imports HVAC component inputs edited in a spreadsheet
exported from the model data grid.

Purpose:
- Updates many Detailed HVAC components in one step - for example the capacity and COP of every
  PTHP coil and the flow rate and pressure rise of every fan - instead of opening each dialog.
- The values are written to the model itself, so they appear in the HVAC dialogs, are saved in
  the .dsb and are used on every subsequent simulation.

Main Steps:
1. Reads the unit system currently selected in DesignBuilder (SI or IP). This is done every time
   the import is started, because the setting can be changed while a model is open.
2. Reads one or more CSV files exported from the model data grid (HVAC component tables).
3. Finds each row's component in the HVAC network of the current building by its name, using the
   zone or loop name only when several components share a name.
4. Compares each supported column with the model. Values are converted only when the file's units
   differ from DesignBuilder's, and the confirmation dialog shows everything in DesignBuilder's
   units.
5. Writes the accepted values, then reads them back to check that the model stored them.

How to Use:
- Configuration: the tables and columns that are imported, with their valid ranges, are in the
  USER CONFIGURATION SECTION of ImportMap.cs. By default:
    DX cooling coil  - RatedTotalCoolingCapacity, RatedSensibleHeatRatio, RatedCOP
    DX heating coil  - RatedTotalHeatingCapacity, RatedCOP
    Fan              - MaximumFlowRate, PressureRise
  A fan inside a unit takes its flow rate from the unit, so for those fans MaximumFlowRate is
  written to the unit instead: the design supply air flow rate of an air handling unit, the
  cooling and heating supply air flow rates of a PTHP or PTAC, or the maximum supply air flow rate
  of a fan coil unit (see SupplyFanFlowOnUnit in ImportMap.cs). Extract fan flow rates follow
  their unit and are not imported.
- Prerequisites: the model uses Detailed HVAC. In the model data grid, export the HVAC table to
  CSV, edit the values in Excel and save as CSV. Keep the first five rows and the component, zone
  and loop name columns unchanged.
- In DesignBuilder, go to the building to update and choose
  HVAC Data Import > Import model data grid CSV... then select one or more files.

Notes:
- This plugin modifies the model. Back up the .dsb before using it and save the model afterwards.
- Only the columns listed in ImportMap.cs are written; other edited columns are ignored.
- Units: each column's units are taken from its caption in the file, so files exported in SI or IP
  both work whatever unit system DesignBuilder is set to. DesignBuilder holds model data in SI
  internally and converts it for display, so values are always written to the model in SI.
- A value is left alone when the current model value, shown in the file's units and to the
  number of decimal places written in the file, already reads the same. This stops the rounding
  in the export from making small unintended changes to values that were not edited.
- Blank cells are ignored. "autosize" is accepted for inputs that can be autosized.
- Only the current building is updated (in a single-building model the Site level also works).

DISCLAIMER: This plugin is provided as-is without warranty. DesignBuilder takes no responsibility for
simulation results, accuracy, or any issues arising from its use. Users are responsible for validating
all outputs and ensuring the plugin meets their specific modeling requirements.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

using DB.Api;
using DB.Extensibility.Contracts;

namespace DBHvacGridImport
{
    /// <summary>A single proposed change to one attribute of one component.</summary>
    public class PendingChange
    {
        /// <summary>"Table :: Attribute" - one tick box per group in the dialog.</summary>
        public string Group;
        public string Title;
        public string Container;
        public string Attribute;
        /// <summary>Values as shown in the dialog, in DesignBuilder's current units.</summary>
        public string CurrentDisplay;
        public string NewDisplay;
        /// <summary>Value as written in the file; empty when no conversion was needed.</summary>
        public string FileDisplay;
        /// <summary>Text written to the model, in SI.</summary>
        public string NewValue;
        /// <summary>DesignBuilder display unit and its factor to SI, for messages.</summary>
        public string DisplayUnit;
        public double DisplayFactor;
        /// <summary>Where the value came from when it is written to a unit rather than
        /// to the row's own component, e.g. "from BLOCK1:ZONE1 PTHP Supply Fan".</summary>
        public string Note = "";
        public HvacComponent Component;
    }

    /// <summary>An HVAC component of the current building and where it sits.</summary>
    internal class ModelComponent
    {
        public HvacComponent Component;
        public string Title;
        /// <summary>HvacComponentType name, e.g. SupplyFan or ZoneTerminalPackagedHeatPump.</summary>
        public string Type;
        /// <summary>The unit containing this component (e.g. the PTHP of a fan); null if none.</summary>
        public ModelComponent Parent;
        /// <summary>Names of the zone or loop holding the component.</summary>
        public List<string> Containers;
    }

    /// <summary>Units of one column in a file and in DesignBuilder.</summary>
    internal class ColumnUnits
    {
        public string FileUnit = "";
        public double FileFactor = 1.0;
        public string DisplayUnit = "";
        public double DisplayFactor = 1.0;

        /// <summary>True when the file is already in DesignBuilder's units, so its
        /// values are shown without conversion.</summary>
        public bool SameUnits
        {
            get { return Math.Abs(FileFactor - DisplayFactor) <= 1e-12 * Math.Abs(DisplayFactor); }
        }
    }

    // [Export] is what makes DesignBuilder load the plugin. Without it the DLL is silently ignored.
    [Export(typeof(IPlugin2))]
    public class HvacGridImportPlugin : PluginBase2, IPlugin2
    {
        private static class MenuKeys
        {
            public const string Root = "hvacImportRoot";
            public const string Import = "hvacImportCsv";
        }

        private const string Caption = "HVAC Data Import";

        /// <summary>Longest list shown in a message box before it is shortened.</summary>
        private const int MaxListedItems = 25;

        private bool mModelLoaded;

        public override bool HasMenu
        {
            get { return true; }
        }

        public override string MenuLayout
        {
            get
            {
                var menu = new StringBuilder();
                menu.AppendFormat("*HVAC Data Import,{0}", MenuKeys.Root);
                menu.AppendFormat("*>Import model data grid CSV...,{0}", MenuKeys.Import);
                return menu.ToString();
            }
        }

        public override bool IsMenuItemVisible(string key)
        {
            return true;
        }

        // The root menu is always enabled; the import needs a loaded model.
        public override bool IsMenuItemEnabled(string key)
        {
            return key == MenuKeys.Root || mModelLoaded;
        }

        public override void ModelLoaded()
        {
            mModelLoaded = true;
        }

        public override void ModelUnloaded()
        {
            mModelLoaded = false;
        }

        public override void OnMenuItemPressed(string key)
        {
            if (key != MenuKeys.Import)
            {
                return;
            }
            try
            {
                Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show("The import could not complete.\n\n" + ex.GetType().Name + ": " +
                    ex.Message + "\n\n" + ex.StackTrace, Caption,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ------------------------------------------------------------------
        private void Run()
        {
            // Read on every run, not at start-up or model load: the unit system can be
            // changed while a model is open.
            UnitSystem displayUnits;
            if (!TryReadDisplayUnits(out displayUnits))
            {
                MessageBox.Show("Could not read the unit system (SI or IP) selected in DesignBuilder, " +
                    "so nothing was imported.", Caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Building building = GetCurrentBuilding();
            if (building == null)
            {
                MessageBox.Show("Go to the building you want to update in the navigator, then try again.",
                    Caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            HvacNetwork network = Try(() => building.HvacNetwork);
            if (network == null)
            {
                MessageBox.Show("This building has no Detailed HVAC network to update.",
                    Caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string[] paths = AskForFiles();
            if (paths == null)
            {
                return;
            }

            Dictionary<string, List<ModelComponent>> index = BuildModelIndex(network);
            var changes = new List<PendingChange>();
            var problems = new List<string>();
            int unchanged = 0;
            var fileNames = new List<string>();

            foreach (string path in paths)
            {
                fileNames.Add(Path.GetFileName(path));
                CollectFile(path, index, displayUnits, changes, problems, ref unchanged);
            }

            if (changes.Count == 0)
            {
                var msg = new StringBuilder();
                msg.AppendLine("Nothing to import.");
                msg.AppendLine();
                msg.AppendLine(unchanged.ToString(CultureInfo.InvariantCulture) +
                               " value(s) in the file(s) already match the model.");
                AppendList(msg, "Not imported", problems);
                MessageBox.Show(msg.ToString(), Caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new frmImportConfirm(changes, problems, unchanged,
                                                     string.Join(", ", fileNames), displayUnits))
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                Apply(dialog.Accepted, Attr(building, "Title"));
            }
        }

        private static string[] AskForFiles()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select model data grid CSV file(s)";
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.Multiselect = true;
                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileNames : null;
            }
        }

        // ------------------------------------------------------------------
        // Index the HVAC network by component name
        // ------------------------------------------------------------------
        private static Dictionary<string, List<ModelComponent>> BuildModelIndex(HvacNetwork network)
        {
            var index = new Dictionary<string, List<ModelComponent>>(StringComparer.OrdinalIgnoreCase);

            foreach (HvacLoop loop in network.Loops)
            {
                List<string> containers = Names(Attr(loop, "Title"));
                AddSubLoop(index, Try(() => loop.SupplySubLoop), containers);
                AddSubLoop(index, Try(() => loop.DemandSubLoop), containers);
            }

            // Zone equipment (PTHPs, fan coils...) lives under the zone groups. The zone is
            // recorded under both its HVAC and its building name, whichever the export uses.
            foreach (HvacZoneGroup group in network.ZoneGroups)
            {
                foreach (HvacZone zone in group.Zones)
                {
                    List<string> containers = Names(Attr(zone, "Title"), BuildingZoneTitle(zone));
                    foreach (HvacComponent component in zone.Components)
                    {
                        Add(index, component, containers, null);
                    }
                }
            }
            return index;
        }

        private static void AddSubLoop(Dictionary<string, List<ModelComponent>> index,
                                       HvacSubLoop subLoop, List<string> containers)
        {
            if (subLoop == null)
            {
                return;
            }
            foreach (HvacComponent component in subLoop.Components)
            {
                Add(index, component, containers, null);
            }
        }

        private static void Add(Dictionary<string, List<ModelComponent>> index,
                                HvacComponent component, List<string> containers,
                                ModelComponent parent)
        {
            if (component == null)
            {
                return;
            }

            var entry = new ModelComponent
            {
                Component = component,
                Title = Attr(component, "Title"),
                Type = Try(() => component.ComponentType.ToString()) ?? "",
                Parent = parent,
                Containers = containers
            };

            if (!IsUnknown(entry.Title))
            {
                List<ModelComponent> sameName;
                if (!index.TryGetValue(entry.Title, out sameName))
                {
                    sameName = new List<ModelComponent>();
                    index.Add(entry.Title, sameName);
                }
                // A component reached twice through the network is recorded once.
                if (!sameName.Exists(m => ShareAName(m.Containers, containers)))
                {
                    sameName.Add(entry);
                }
            }

            // Composite units: the coils and fan of a PTHP or fan coil unit, the fans of an AHU.
            try
            {
                foreach (HvacComponent sub in component.SubComponents)
                {
                    Add(index, sub, containers, entry);
                }
            }
            catch
            {
                // Components that are not composite may not expose sub-components.
            }
        }

        /// <summary>Finds the model component for a file row: by name, then by zone or
        /// loop if the name is not unique. Returns null with a reason otherwise.</summary>
        private static ModelComponent Resolve(Dictionary<string, List<ModelComponent>> index,
                                              string title, string container, out string reason)
        {
            reason = null;
            List<ModelComponent> candidates;
            if (!index.TryGetValue(title, out candidates))
            {
                reason = "no HVAC component with this name in the current building.";
                return null;
            }
            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            List<ModelComponent> inContainer = candidates.FindAll(
                m => ShareAName(m.Containers, Names(container)));
            if (inContainer.Count == 1)
            {
                return inContainer[0];
            }

            reason = candidates.Count.ToString(CultureInfo.InvariantCulture) +
                     " components have this name and the zone or loop '" + container +
                     "' does not identify one of them.";
            return null;
        }

        // ------------------------------------------------------------------
        // Compare one file with the model
        // ------------------------------------------------------------------
        private static void CollectFile(string path, Dictionary<string, List<ModelComponent>> index,
                                        UnitSystem displayUnits, List<PendingChange> changes,
                                        List<string> problems, ref int unchanged)
        {
            string fileName = Path.GetFileName(path);

            GridFile file;
            try
            {
                file = GridFile.Load(path);
            }
            catch (Exception ex)
            {
                problems.Add(fileName + ": " + ex.Message);
                return;
            }

            ImportTable table = ImportMap.For(file.TableName);
            if (table == null)
            {
                problems.Add(fileName + ": the table '" + file.TableName +
                             "' is not in the import list (see ImportMap.cs).");
                return;
            }

            // Columns of this table present in the file. Each column's units come from its
            // caption in the file, and are compared with the units DesignBuilder displays.
            var columns = new List<ImportColumn>();
            var units = new List<ColumnUnits>();
            foreach (ImportColumn column in table.Columns)
            {
                if (!file.HasColumn(column.Attribute))
                {
                    continue;
                }
                var columnUnits = new ColumnUnits();
                if (column.Quantity != Quantity.None)
                {
                    columnUnits.FileUnit = file.CaptionUnit(column.Attribute);
                    columnUnits.DisplayUnit = ImportMap.DisplayUnit(column.Quantity, displayUnits);
                }
                if (!ImportMap.TryGetToSiFactor(columnUnits.FileUnit, out columnUnits.FileFactor))
                {
                    problems.Add(fileName + ": " + column.Attribute + " - the unit '" + columnUnits.FileUnit +
                                 "' is not recognised, so the column was skipped (see ImportMap.cs).");
                    continue;
                }
                if (!ImportMap.TryGetToSiFactor(columnUnits.DisplayUnit, out columnUnits.DisplayFactor))
                {
                    problems.Add(fileName + ": " + column.Attribute + " - the display unit '" +
                                 columnUnits.DisplayUnit + "' has no conversion in ImportMap.cs.");
                    continue;
                }
                columns.Add(column);
                units.Add(columnUnits);
            }
            if (columns.Count == 0)
            {
                problems.Add(fileName + ": none of the importable " + table.DisplayName +
                             " columns were found.");
                return;
            }

            foreach (string[] row in file.Rows)
            {
                // The deepest name filled in is the component itself, e.g. the coil
                // rather than the PTHP that contains it.
                string title = FirstNonEmpty(
                    file.Cell(row, GridFile.ColSubLoopSubComponent),
                    file.Cell(row, GridFile.ColZoneSubComponent),
                    file.Cell(row, GridFile.ColSubLoopComponent),
                    file.Cell(row, GridFile.ColZoneComponent));
                string container = FirstNonEmpty(
                    file.Cell(row, GridFile.ColZone),
                    file.Cell(row, GridFile.ColLoop));

                if (title.Length == 0)
                {
                    problems.Add(fileName + ": a row has no component name and was skipped.");
                    continue;
                }

                string reason;
                ModelComponent target = Resolve(index, title, container, out reason);
                if (target == null)
                {
                    problems.Add(fileName + ": " + title + " - " + reason);
                    continue;
                }

                for (int i = 0; i < columns.Count; i++)
                {
                    Evaluate(file, row, table, columns[i], units[i], container, target,
                             changes, problems, ref unchanged);
                }
            }
        }

        /// <summary>
        /// Checks one cell and adds a change for each model input it updates. Nothing is
        /// added when the cell is blank, already matches the model, or is invalid (then a
        /// problem is recorded). Usually the input is on the row's own component; for a
        /// fan inside a unit, the flow rate goes to the unit (ImportColumn.ParentAttributes).
        /// The model holds SI values; values shown to the user are in DesignBuilder's
        /// current units, converted from the file only when its units differ.
        /// </summary>
        private static void Evaluate(GridFile file, string[] row, ImportTable table,
                                     ImportColumn column, ColumnUnits units, string container,
                                     ModelComponent target, List<PendingChange> changes,
                                     List<string> problems, ref int unchanged)
        {
            string text = file.Cell(row, column.Attribute);
            if (text.Length == 0)
            {
                return;
            }
            string where = Path.GetFileName(file.FilePath) + ": " + target.Title + " :: " + column.Attribute;

            // 1. Check the file value.
            bool toAutosize = IsAutosize(text);
            double fileValue = 0.0;
            double si = 0.0;
            int decimals = -1;
            if (toAutosize)
            {
                if (!column.Autosizable)
                {
                    problems.Add(where + " - this input cannot be set to autosize.");
                    return;
                }
            }
            else
            {
                if (!file.TryParseNumber(text, out fileValue, out decimals))
                {
                    problems.Add(where + " - '" + text + "' is not a number.");
                    return;
                }
                si = fileValue * units.FileFactor;
                if (!InRange(si, column))
                {
                    problems.Add(where + " - " + JoinUnit(text, units.FileUnit) +
                                 " is outside the valid range (" + RangeText(column, units) + ").");
                    return;
                }
            }

            // 2. Decide which component and inputs receive it.
            ModelComponent owner = target;
            string[] attributes = { column.Attribute };
            string note = "";
            if (column.ParentAttributes != null && target.Parent != null)
            {
                string[] onUnit;
                if (target.Type == "ExtractFan")
                {
                    problems.Add(where + " - set by its unit '" + target.Parent.Title + "', so not imported.");
                    return;
                }
                if (!column.ParentAttributes.TryGetValue(target.Parent.Type, out onUnit))
                {
                    problems.Add(where + " - set by its unit '" + target.Parent.Title + "' (type " +
                                 target.Parent.Type + "), which is not listed in ImportMap.cs, so not imported.");
                    return;
                }
                owner = target.Parent;
                attributes = onUnit;
                note = "from " + target.Title;
            }

            // 3. One change per input that differs from the file value.
            foreach (string attribute in attributes)
            {
                // GetAttribute returns "UNKNOWN" when the component has no such input,
                // e.g. when the row matched a component of another type.
                string current = Attr(owner.Component, attribute);
                if (IsUnknown(current))
                {
                    problems.Add(where + " - '" + owner.Title + "' has no input " + attribute + ".");
                    continue;
                }

                double currentSi;
                bool currentIsNumber = TryParseInvariant(current, out currentSi);
                string newValue;
                string newDisplay;
                if (toAutosize)
                {
                    if (IsAutosize(current))
                    {
                        unchanged++;
                        continue;
                    }
                    newValue = text;
                    newDisplay = text;
                }
                else
                {
                    if (currentIsNumber && ReadsTheSame(currentSi / units.FileFactor, fileValue, decimals))
                    {
                        unchanged++;
                        continue;
                    }
                    newValue = si.ToString("0.##########", CultureInfo.InvariantCulture);
                    newDisplay = units.SameUnits
                        ? JoinUnit(text, units.DisplayUnit)
                        : Display(si / units.DisplayFactor, units.DisplayUnit);
                }

                changes.Add(new PendingChange
                {
                    Group = table.DisplayName + " :: " + column.Attribute,
                    Title = owner.Title,
                    Container = container,
                    Attribute = attribute,
                    CurrentDisplay = currentIsNumber
                        ? Display(currentSi / units.DisplayFactor, units.DisplayUnit)
                        : current,
                    NewDisplay = newDisplay,
                    FileDisplay = units.SameUnits ? "" : JoinUnit(text, units.FileUnit),
                    NewValue = newValue,
                    DisplayUnit = units.DisplayUnit,
                    DisplayFactor = units.DisplayFactor,
                    Note = note,
                    Component = owner.Component
                });
            }
        }

        /// <summary>True when the model value, written to the decimal places used in the
        /// file, gives the file value - i.e. the cell was not edited.</summary>
        private static bool ReadsTheSame(double currentInFileUnits, double fileValue, int decimals)
        {
            double difference = Math.Abs(currentInFileUnits - fileValue);
            if (decimals < 0)
            {
                return difference <= 1e-9 * Math.Max(1.0, Math.Abs(fileValue));
            }
            double halfStep = 0.5 * Math.Pow(10.0, -decimals);
            return difference <= halfStep * (1.0 + 1e-9);
        }

        private static bool InRange(double value, ImportColumn column)
        {
            bool aboveMin = column.MinExclusive ? value > column.Min : value >= column.Min;
            return aboveMin && value <= column.Max;
        }

        /// <summary>Valid range in DesignBuilder's current units.</summary>
        private static string RangeText(ImportColumn column, ColumnUnits units)
        {
            string min = (column.Min / units.DisplayFactor).ToString("G6", CultureInfo.InvariantCulture);
            string max = (column.Max / units.DisplayFactor).ToString("G6", CultureInfo.InvariantCulture);
            if (!double.IsInfinity(column.Max))
            {
                return JoinUnit(min + " to " + max, units.DisplayUnit);
            }
            return JoinUnit((column.MinExclusive ? "greater than " : "at least ") + min, units.DisplayUnit);
        }

        // ------------------------------------------------------------------
        // Write the accepted changes and read them back
        // ------------------------------------------------------------------
        private void Apply(List<PendingChange> accepted, string buildingTitle)
        {
            var written = new List<PendingChange>();
            var failures = new List<string>();

            // Components touched, so UpdateAttributeData is called once each.
            var touched = new List<HvacComponent>();

            foreach (PendingChange change in accepted)
            {
                try
                {
                    change.Component.SetAttribute(change.Attribute, change.NewValue);
                    written.Add(change);
                    if (!touched.Contains(change.Component))
                    {
                        touched.Add(change.Component);
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(change.Title + " :: " + change.Attribute + " - " + ex.Message);
                }
            }

            // Required: synchronises the HVAC network with the new attribute values.
            // Without it the changes may not be kept.
            foreach (HvacComponent component in touched)
            {
                try
                {
                    component.UpdateAttributeData();
                }
                catch (Exception ex)
                {
                    failures.Add(Attr(component, "Title") + " - UpdateAttributeData: " + ex.Message);
                }
            }

            // Read everything back only after all writes, so a value overwritten by a
            // later write to another component is caught too.
            var mismatches = new List<string>();
            foreach (PendingChange change in written)
            {
                string stored = Attr(change.Component, change.Attribute);
                if (!StoredAsWritten(stored, change.NewValue))
                {
                    double storedSi;
                    string storedDisplay = TryParseInvariant(stored, out storedSi)
                        ? Display(storedSi / change.DisplayFactor, change.DisplayUnit)
                        : stored;
                    mismatches.Add(change.Title + " :: " + change.Attribute + " - expected " +
                                   change.NewDisplay + ", model now reads " + storedDisplay);
                }
            }

            var msg = new StringBuilder();
            msg.AppendLine("Imported " + (written.Count - mismatches.Count).ToString(CultureInfo.InvariantCulture) +
                           " value(s) into " + touched.Count.ToString(CultureInfo.InvariantCulture) +
                           " component(s) of building '" + buildingTitle + "'.");
            msg.AppendLine();
            msg.AppendLine("Save the model to keep the changes.");
            AppendList(msg, "Could not be written", failures);
            if (mismatches.Count > 0)
            {
                AppendList(msg, "Written but not kept by the model", mismatches);
                msg.AppendLine();
                msg.AppendLine("Check these components in the HVAC dialogs. If several components of the " +
                               "same zone group are listed, they may share their data, in which case " +
                               "they cannot hold different values.");
            }

            MessageBox.Show(msg.ToString(), Caption, MessageBoxButtons.OK,
                failures.Count + mismatches.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        /// <summary>DesignBuilder may reformat a number when storing it, so numbers are
        /// compared to within 0.1 % rather than as text.</summary>
        private static bool StoredAsWritten(string stored, string written)
        {
            if (IsAutosize(written))
            {
                return IsAutosize(stored);
            }
            double a;
            double b;
            if (TryParseInvariant(stored, out a) && TryParseInvariant(written, out b))
            {
                return Math.Abs(a - b) <= 1e-3 * Math.Max(Math.Abs(b), 1e-9);
            }
            return string.Equals((stored ?? "").Trim(), written, StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Unit system currently selected in DesignBuilder, from the program options
        /// (ProgramOptionTemplates, record 2). The Units field starts with 1 for SI and
        /// 2 for IP. This only affects how values are displayed: the model holds SI.
        /// </summary>
        private bool TryReadDisplayUnits(out UnitSystem units)
        {
            units = UnitSystem.SI;
            try
            {
                Table options = ApiEnvironment.ApplicationTemplates.GetTable("ProgramOptionTemplates");
                Record record = options.Records.GetRecordFromHandle(2);
                string value = (record["Units"] ?? "").Trim();
                if (value.StartsWith("1", StringComparison.Ordinal))
                {
                    return true;
                }
                if (value.StartsWith("2", StringComparison.Ordinal))
                {
                    units = UnitSystem.IP;
                    return true;
                }
            }
            catch (Exception)
            {
                // Reported by the caller.
            }
            return false;
        }

        /// <summary>Building shown in the interface. At Site level, the only building
        /// of a single-building model; otherwise null.</summary>
        private Building GetCurrentBuilding()
        {
            Site site = ApiEnvironment.Site;
            if (site == null)
            {
                return null;
            }
            int index = ApiEnvironment.CurrentBuildingIndex;
            if (index < 0 && site.Buildings.Count == 1)
            {
                index = 0;
            }
            return index < 0 ? null : site.Buildings[index];
        }

        private static string BuildingZoneTitle(HvacZone zone)
        {
            try
            {
                Zone buildingZone = zone.BuildingZone;
                return buildingZone == null ? "" : (buildingZone.GetAttribute("Title") ?? "");
            }
            catch
            {
                return "";
            }
        }

        private static List<string> Names(params string[] names)
        {
            var result = new List<string>();
            foreach (string name in names)
            {
                if (!IsUnknown(name))
                {
                    result.Add(name.Trim());
                }
            }
            return result;
        }

        private static bool ShareAName(List<string> a, List<string> b)
        {
            if (a.Count == 0 && b.Count == 0)
            {
                return true;
            }
            foreach (string name in a)
            {
                if (b.Exists(other => string.Equals(other, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            return false;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            return "";
        }

        private static void AppendList(StringBuilder msg, string heading, List<string> items)
        {
            if (items.Count == 0)
            {
                return;
            }
            msg.AppendLine();
            msg.AppendLine(heading + " (" + items.Count.ToString(CultureInfo.InvariantCulture) + "):");
            for (int i = 0; i < items.Count && i < MaxListedItems; i++)
            {
                msg.AppendLine("   " + items[i]);
            }
            if (items.Count > MaxListedItems)
            {
                msg.AppendLine("   ... and " +
                    (items.Count - MaxListedItems).ToString(CultureInfo.InvariantCulture) + " more.");
            }
        }

        private static string Display(double value, string unit)
        {
            return JoinUnit(value.ToString("G6", CultureInfo.InvariantCulture), unit);
        }

        private static string JoinUnit(string value, string unit)
        {
            return string.IsNullOrEmpty(unit) ? value : value + " " + unit;
        }

        private static bool TryParseInvariant(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string Attr(HvacComponent c, string name)
        {
            try { return c.GetAttribute(name) ?? ""; } catch { return ""; }
        }

        private static string Attr(HvacLoop l, string name)
        {
            try { return l.GetAttribute(name) ?? ""; } catch { return ""; }
        }

        private static string Attr(HvacZone z, string name)
        {
            try { return z.GetAttribute(name) ?? ""; } catch { return ""; }
        }

        private static string Attr(Building b, string name)
        {
            try { return b.GetAttribute(name) ?? ""; } catch { return ""; }
        }

        private static T Try<T>(Func<T> f) where T : class
        {
            try { return f(); } catch { return null; }
        }

        /// <summary>GetAttribute returns the text "UNKNOWN" for an attribute the object
        /// does not have, rather than null or empty.</summary>
        private static bool IsUnknown(string value)
        {
            return value == null || value.Trim().Length == 0 ||
                   value.Trim().Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The autosize marker's capitalisation varies between component types.</summary>
        private static bool IsAutosize(string value)
        {
            return value != null && value.Trim().Equals("autosize", StringComparison.OrdinalIgnoreCase);
        }
    }
}
