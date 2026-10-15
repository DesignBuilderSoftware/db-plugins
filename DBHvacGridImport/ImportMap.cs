using System;
using System.Collections.Generic;

namespace DBHvacGridImport
{
    /// <summary>Unit system selected in DesignBuilder.</summary>
    public enum UnitSystem
    {
        SI,
        IP
    }

    /// <summary>Physical quantity of a column, used to pick the unit conversion.</summary>
    public enum Quantity
    {
        /// <summary>Dimensionless (COP, SHR, fractions). Imported as written.</summary>
        None,
        /// <summary>Capacity. Stored in the model in W.</summary>
        Power,
        /// <summary>Air volume flow. Stored in the model in m3/s.</summary>
        VolumeFlow,
        /// <summary>Pressure. Stored in the model in Pa.</summary>
        Pressure
    }

    /// <summary>One importable column of a model data grid table.</summary>
    public class ImportColumn
    {
        /// <summary>DesignBuilder attribute name - exactly as in row 4 of the export.</summary>
        public string Attribute { get; private set; }
        public Quantity Quantity { get; private set; }
        /// <summary>True if the cell may contain "autosize".</summary>
        public bool Autosizable { get; private set; }
        /// <summary>Lowest accepted value, in SI units.</summary>
        public double Min { get; private set; }
        /// <summary>Highest accepted value, in SI units.</summary>
        public double Max { get; private set; }
        /// <summary>True if the value must be strictly greater than Min.</summary>
        public bool MinExclusive { get; private set; }

        /// <summary>
        /// For components inside a unit (e.g. the fan of a PTHP): the unit's component
        /// type and the unit attributes that receive the value instead of the component.
        /// Null when the value is always written to the component itself.
        /// </summary>
        public IDictionary<string, string[]> ParentAttributes { get; private set; }

        public ImportColumn(string attribute, Quantity quantity, bool autosizable,
                            double min = double.NegativeInfinity,
                            double max = double.PositiveInfinity,
                            bool minExclusive = false,
                            IDictionary<string, string[]> parentAttributes = null)
        {
            Attribute = attribute;
            Quantity = quantity;
            Autosizable = autosizable;
            Min = min;
            Max = max;
            MinExclusive = minExclusive;
            ParentAttributes = parentAttributes;
        }
    }

    /// <summary>A supported model data grid table and the columns imported from it.</summary>
    public class ImportTable
    {
        /// <summary>Friendly name shown in the confirmation dialog.</summary>
        public string DisplayName { get; private set; }
        public IList<ImportColumn> Columns { get; private set; }

        public ImportTable(string displayName, params ImportColumn[] columns)
        {
            DisplayName = displayName;
            Columns = columns;
        }
    }

    public static class ImportMap
    {
        // ==================================================================
        // USER CONFIGURATION SECTION
        //
        // Which model data grid tables can be imported, and which of their
        // columns are written to the model. Columns not listed here are never
        // touched, even if they were edited in the file.
        //
        // To support another table or column:
        //   1. Export that table from the model data grid.
        //   2. The table name is in row 4, column 2 after "HVAC" and the
        //      separator (e.g. FanComponent); the attribute names are the
        //      rest of row 4.
        //   3. Add an entry below with the attribute name, its quantity
        //      (which sets the unit conversion) and its valid range.
        //
        // Ranges are in SI units and follow the EnergyPlus input limits for
        // the objects DesignBuilder writes from these components.
        // ==================================================================

        // Fan flow rates set by the unit containing the fan. A fan inside an air
        // handling unit or a zone unit takes its flow rate from the unit, and its
        // own "Maximum flow rate" is greyed out in DesignBuilder. For these fans the
        // imported MaximumFlowRate is written to the unit inputs listed here.
        //   Key:   the unit's component type
        //   Value: the unit's supply air flow rate attribute(s)
        // Extract fans are never redirected, since their flow follows the unit.
        // A fan inside a unit type not listed here is reported and not imported.
        // Declared before Tables, which uses it.
        public static readonly Dictionary<string, string[]> SupplyFanFlowOnUnit =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "AirHandlingUnit", new[] { "DesignSupplyAirFlowRate" } },
            { "ZoneTerminalPackagedHeatPump", new[] { "SupplyAirFlowRateDuringCoolingOperation",
                                                      "SupplyAirFlowRateDuringHeatingOperation" } },
            { "ZoneTerminalPackagedAirConditioner", new[] { "SupplyAirFlowRateDuringCoolingOperation",
                                                            "SupplyAirFlowRateDuringHeatingOperation" } },
            { "ZoneFanCoilUnit", new[] { "MaxSupplyAirFlowRate" } },
        };

        public static readonly Dictionary<string, ImportTable> Tables =
            new Dictionary<string, ImportTable>(StringComparer.OrdinalIgnoreCase)
        {
            // Coil:Cooling:DX:SingleSpeed (and the general fields of the other DX coil types)
            { "DXCoolingCoil", new ImportTable("DX cooling coil",
                new ImportColumn("RatedTotalCoolingCapacity", Quantity.Power, autosizable: true,
                                 min: 0.0, minExclusive: true),
                new ImportColumn("RatedSensibleHeatRatio", Quantity.None, autosizable: true,
                                 min: 0.5, max: 1.0),
                new ImportColumn("RatedCOP", Quantity.None, autosizable: false,
                                 min: 0.0, minExclusive: true)) },

            // Coil:Heating:DX:SingleSpeed
            { "DXHeatingCoil", new ImportTable("DX heating coil",
                new ImportColumn("RatedTotalHeatingCapacity", Quantity.Power, autosizable: true,
                                 min: 0.0, minExclusive: true),
                new ImportColumn("RatedCOP", Quantity.None, autosizable: false,
                                 min: 0.0, minExclusive: true)) },

            // Supply and extract fans (Fan:ConstantVolume, Fan:VariableVolume, Fan:OnOff).
            // A zero flow rate is rejected: it would fix the fan at no flow on every run.
            // The flow rate of a fan inside a unit is written to the unit (see above).
            { "FanComponent", new ImportTable("Fan",
                new ImportColumn("MaximumFlowRate", Quantity.VolumeFlow, autosizable: true,
                                 min: 0.0, minExclusive: true,
                                 parentAttributes: SupplyFanFlowOnUnit),
                new ImportColumn("PressureRise", Quantity.Pressure, autosizable: false,
                                 min: 0.0)) },
        };

        /// <summary>
        /// Display units found in the column captions (row 5 of the export, in
        /// brackets) and the factor that converts them to the SI unit stored in
        /// the model. Keys are lower case without spaces.
        /// </summary>
        public static readonly Dictionary<string, double> ToSiFactors =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            // Power -> W
            { "w", 1.0 },
            { "kw", 1000.0 },
            { "btu/h", 0.29307107 },
            { "kbtu/h", 293.07107 },
            // Volume flow -> m3/s
            { "m3/s", 1.0 },
            { "l/s", 0.001 },
            { "m3/h", 1.0 / 3600.0 },
            { "ft3/min", 0.00047194745 },
            { "cfm", 0.00047194745 },
            // Pressure -> Pa (inH2O at 4 C, the value EnergyPlus uses)
            { "pa", 1.0 },
            { "kpa", 1000.0 },
            { "inh2o", 249.08891 },
            { "inwg", 249.08891 },
        };

        /// <summary>
        /// Units DesignBuilder displays for each quantity, in SI and in IP. When a
        /// file's units differ from DesignBuilder's, values are converted to these
        /// for the confirmation dialog. Each unit must be listed in ToSiFactors.
        /// </summary>
        public static readonly Dictionary<Quantity, string> SiDisplayUnits = new Dictionary<Quantity, string>
        {
            { Quantity.Power, "W" },
            { Quantity.VolumeFlow, "m3/s" },
            { Quantity.Pressure, "Pa" },
        };

        public static readonly Dictionary<Quantity, string> IpDisplayUnits = new Dictionary<Quantity, string>
        {
            { Quantity.Power, "Btu/h" },
            { Quantity.VolumeFlow, "ft3/min" },
            { Quantity.Pressure, "inH2O" },
        };
        // ==================================================================
        // END OF USER CONFIGURATION SECTION
        // ==================================================================

        /// <summary>Unit DesignBuilder displays for a quantity; empty if dimensionless.</summary>
        public static string DisplayUnit(Quantity quantity, UnitSystem system)
        {
            string unit;
            var units = system == UnitSystem.IP ? IpDisplayUnits : SiDisplayUnits;
            return units.TryGetValue(quantity, out unit) ? unit : "";
        }

        /// <summary>Factor converting a unit to SI. A dimensionless (empty) unit gives 1.</summary>
        public static bool TryGetToSiFactor(string unit, out double factor)
        {
            factor = 1.0;
            string key = (unit ?? "").Replace(" ", "");
            return key.Length == 0 || ToSiFactors.TryGetValue(key, out factor);
        }

        public static ImportTable For(string tableName)
        {
            ImportTable table;
            return Tables.TryGetValue(tableName ?? "", out table) ? table : null;
        }
    }
}
