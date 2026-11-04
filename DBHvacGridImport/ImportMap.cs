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
        /// <summary>Dimensionless (COP, SHR, humidity ratio). Imported as written.</summary>
        None,
        /// <summary>Capacity. Stored in the model in W.</summary>
        Power,
        /// <summary>Air volume flow. Stored in the model in m3/s.</summary>
        AirFlow,
        /// <summary>Water volume flow. Stored in the model in m3/s.</summary>
        WaterFlow,
        /// <summary>Pressure. Stored in the model in Pa.</summary>
        Pressure,
        /// <summary>Temperature. Stored in the model in C.</summary>
        Temperature
    }

    /// <summary>Converts a unit to SI: si = value * Factor + Offset. The offset is
    /// only needed for temperatures.</summary>
    public class UnitConversion
    {
        public double Factor { get; private set; }
        public double Offset { get; private set; }

        public UnitConversion(double factor, double offset = 0.0)
        {
            Factor = factor;
            Offset = offset;
        }

        public double ToSi(double value)
        {
            return value * Factor + Offset;
        }

        public double FromSi(double si)
        {
            return (si - Offset) / Factor;
        }

        public bool SameAs(UnitConversion other)
        {
            return Math.Abs(Factor - other.Factor) <= 1e-12 * Math.Abs(other.Factor) &&
                   Math.Abs(Offset - other.Offset) <= 1e-9;
        }
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

        /// <summary>
        /// Conditions on the component, as "Attribute=word,word". The column is only
        /// imported when, for every condition, the component's attribute contains one of
        /// the words - e.g. "Type=Electric,Fuel" matches a coil whose Type is "3-Fuel".
        /// Null when the column applies to every component in the table.
        /// </summary>
        public string[] OnlyWhen { get; private set; }

        public ImportColumn(string attribute, Quantity quantity, bool autosizable,
                            double min = double.NegativeInfinity,
                            double max = double.PositiveInfinity,
                            bool minExclusive = false,
                            IDictionary<string, string[]> parentAttributes = null,
                            string[] onlyWhen = null)
        {
            Attribute = attribute;
            Quantity = quantity;
            Autosizable = autosizable;
            Min = min;
            Max = max;
            MinExclusive = minExclusive;
            ParentAttributes = parentAttributes;
            OnlyWhen = onlyWhen;
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
        // the objects DesignBuilder writes from these components. Zero flow
        // rates and capacities are rejected: they would switch the component off.
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

            // VRF terminal unit coils (Coil:Cooling:DX:VariableRefrigerantFlow and
            // Coil:Heating:DX:VariableRefrigerantFlow). VRF efficiency is set on the
            // outdoor unit, so these coils have no COP.
            { "VRFDXCoolingCoil", new ImportTable("VRF DX cooling coil",
                new ImportColumn("GrossRatedTotalCoolingCapacity", Quantity.Power, autosizable: true,
                                 min: 0.0, minExclusive: true),
                new ImportColumn("GrossRatedSensibleHeatRatio", Quantity.None, autosizable: true,
                                 min: 0.0, minExclusive: true)) },

            { "VRFDXHeatingCoil", new ImportTable("VRF DX heating coil",
                new ImportColumn("GrossRatedTotalHeatingCapacity", Quantity.Power, autosizable: true,
                                 min: 0.0, minExclusive: true)) },

            // Water-to-air heat pump coils, Equation Fit type
            // (Coil:Cooling:WaterToAirHeatPump:EquationFit and :Heating:...:EquationFit).
            // Parameter estimation coils hold their capacity in other inputs and have no
            // COP, so a changed value for one of them is reported, not written.
            { "WaterToAirHPDXCoolingCoil", new ImportTable("WSHP DX cooling coil",
                new ImportColumn("GrossRatedTotalCoolingCapacity", Quantity.Power, autosizable: true,
                                 min: 0.0, minExclusive: true,
                                 onlyWhen: new[] { "Type=Equation Fit" }),
                new ImportColumn("GrossRatedSensibleCoolingCapacity", Quantity.Power, autosizable: true,
                                 min: 0.0, minExclusive: true,
                                 onlyWhen: new[] { "Type=Equation Fit" }),
                new ImportColumn("RatedCoolingCoefficientOfPerformance", Quantity.None, autosizable: false,
                                 min: 0.0, minExclusive: true,
                                 onlyWhen: new[] { "Type=Equation Fit" })) },

            { "WaterToAirHPDXHeatingCoil", new ImportTable("WSHP DX heating coil",
                new ImportColumn("EquationFitGrossRatedHeatingCapacity", Quantity.Power, autosizable: true,
                                 min: 0.0, minExclusive: true,
                                 onlyWhen: new[] { "Type=Equation Fit" }),
                new ImportColumn("RatedHeatingCoefficientOfPerformance", Quantity.None, autosizable: false,
                                 min: 0.0, minExclusive: true,
                                 onlyWhen: new[] { "Type=Equation Fit" })) },

            // Water, electric and fuel heating coils (Coil:Heating:Water, :Electric, :Fuel).
            // Every coil holds both capacity inputs, but each coil type uses only one:
            //   electric and fuel coils - NominalCapacity ("Nominal capacity")
            //   water coils             - RatedCapacity ("Heating design capacity"), used
            //                             only with the "Nominal capacity" input method
            // A value entered in the input the coil does not use is reported, not written.
            { "HeatingCoil", new ImportTable("Heating coil",
                new ImportColumn("NominalCapacity", Quantity.Power, autosizable: true,
                                 min: 0.0, minExclusive: true,
                                 onlyWhen: new[] { "Type=Electric,Fuel" }),
                new ImportColumn("RatedCapacity", Quantity.Power, autosizable: true,
                                 min: 0.0, minExclusive: true,
                                 onlyWhen: new[] { "Type=Water", "InputMethod=Nominal capacity" })) },

            // Chilled water cooling coils (Coil:Cooling:Water - design conditions).
            { "CoolingCoil", new ImportTable("Water cooling coil",
                new ImportColumn("DesignWaterFlowRate", Quantity.WaterFlow, autosizable: true,
                                 min: 0.0, minExclusive: true),
                new ImportColumn("DesignAirFlowRate", Quantity.AirFlow, autosizable: true,
                                 min: 0.0, minExclusive: true),
                new ImportColumn("DesignInletWaterTemperature", Quantity.Temperature, autosizable: true,
                                 min: 0.0, minExclusive: true),
                new ImportColumn("DesignInletAirTemperature", Quantity.Temperature, autosizable: true,
                                 min: 0.0, minExclusive: true),
                new ImportColumn("DesignOutletAirTemperature", Quantity.Temperature, autosizable: true,
                                 min: 0.0, minExclusive: true),
                new ImportColumn("DesignInletAirHumidityRatio", Quantity.None, autosizable: true,
                                 min: 0.0),
                new ImportColumn("DesignOutletAirHumidityRatio", Quantity.None, autosizable: true,
                                 min: 0.0)) },

            // Supply and extract fans (Fan:ConstantVolume, Fan:VariableVolume, Fan:OnOff).
            // The flow rate of a fan inside a unit is written to the unit (see above).
            { "FanComponent", new ImportTable("Fan",
                new ImportColumn("MaximumFlowRate", Quantity.AirFlow, autosizable: true,
                                 min: 0.0, minExclusive: true,
                                 parentAttributes: SupplyFanFlowOnUnit),
                new ImportColumn("PressureRise", Quantity.Pressure, autosizable: false,
                                 min: 0.0)) },
        };

        /// <summary>
        /// Units found in the column captions (row 5 of the export, in brackets) and
        /// their conversion to the SI unit stored in the model. Keys are compared
        /// without spaces or case, with the degree sign written as "deg" (so the
        /// caption unit for degrees Fahrenheit matches "degF").
        /// </summary>
        public static readonly Dictionary<string, UnitConversion> Conversions =
            new Dictionary<string, UnitConversion>(StringComparer.OrdinalIgnoreCase)
        {
            // Power -> W
            { "W", new UnitConversion(1.0) },
            { "kW", new UnitConversion(1000.0) },
            { "Btu/h", new UnitConversion(0.29307107) },
            { "kBtu/h", new UnitConversion(293.07107) },
            // Air and water volume flow -> m3/s
            { "m3/s", new UnitConversion(1.0) },
            { "l/s", new UnitConversion(0.001) },
            { "m3/h", new UnitConversion(1.0 / 3600.0) },
            { "ft3/min", new UnitConversion(0.00047194745) },
            { "cfm", new UnitConversion(0.00047194745) },
            { "gal/min", new UnitConversion(0.0000630901964) },
            { "gpm", new UnitConversion(0.0000630901964) },
            // Pressure -> Pa (inH2O at 4 C, the value EnergyPlus uses)
            { "Pa", new UnitConversion(1.0) },
            { "kPa", new UnitConversion(1000.0) },
            { "inH2O", new UnitConversion(249.08891) },
            { "inwg", new UnitConversion(249.08891) },
            // Temperature -> C
            { "degC", new UnitConversion(1.0) },
            { "degF", new UnitConversion(5.0 / 9.0, -160.0 / 9.0) },
        };

        /// <summary>
        /// Units DesignBuilder displays for each quantity, in SI and in IP. When a
        /// file's units differ from DesignBuilder's, values are converted to these
        /// for the confirmation dialog. Each unit must be listed in Conversions.
        /// </summary>
        public static readonly Dictionary<Quantity, string> SiDisplayUnits = new Dictionary<Quantity, string>
        {
            { Quantity.Power, "W" },
            { Quantity.AirFlow, "m3/s" },
            { Quantity.WaterFlow, "m3/s" },
            { Quantity.Pressure, "Pa" },
            { Quantity.Temperature, "\u00B0C" },
        };

        public static readonly Dictionary<Quantity, string> IpDisplayUnits = new Dictionary<Quantity, string>
        {
            { Quantity.Power, "Btu/h" },
            { Quantity.AirFlow, "ft3/min" },
            { Quantity.WaterFlow, "gal/min" },
            { Quantity.Pressure, "inH2O" },
            { Quantity.Temperature, "\u00B0F" },
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

        /// <summary>Conversion of a unit to SI. A dimensionless (empty) unit converts as is.</summary>
        public static bool TryGetConversion(string unit, out UnitConversion conversion)
        {
            // The degree sign becomes "deg". A stray "A-circumflex" is what remains of a
            // degree sign when a UTF-8 file without a byte order mark is read as ANSI.
            string key = (unit ?? "").Replace(" ", "").Replace("\u00C2", "").Replace("\u00B0", "deg");
            if (key.Length == 0)
            {
                conversion = new UnitConversion(1.0);
                return true;
            }
            return Conversions.TryGetValue(key, out conversion);
        }

        public static ImportTable For(string tableName)
        {
            ImportTable table;
            return Tables.TryGetValue(tableName ?? "", out table) ? table : null;
        }
    }
}
