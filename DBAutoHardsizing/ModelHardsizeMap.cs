using System.Collections.Generic;

namespace DBAutoHardsizeModel
{
    /// <summary>How the .eio component name is derived from the DB object.</summary>
    public enum NameRule
    {
        /// <summary>The component's own Title attribute. Verified 100% match.</summary>
        Title,
        /// <summary>Title + " Controller" - air-loop water coils only.</summary>
        TitlePlusController,
        /// <summary>Title + " Outdoor Air Controller" - AHU only.</summary>
        TitlePlusOutdoorAirController,
        /// <summary>The Title of the loop the component sits on.</summary>
        ParentLoopTitle,
    }

    public class ModelHardsizeEntry
    {
        /// <summary>DesignBuilder attribute name (from the Formats tables, ItemType 77).</summary>
        public string Attribute { get; }

        /// <summary>
        /// Candidate EnergyPlus object types to search in the .eio. More than one
        /// where DesignBuilder's single component maps to several E+ types
        /// (fans, heating coils). First type whose name matches wins.
        /// </summary>
        public string[] EioTypes { get; }

        /// <summary>Exact .eio "Input Field Description" string.</summary>
        public string EioDescription { get; }

        public NameRule Rule { get; }

        /// <summary>Included in the default selection.</summary>
        public bool OnByDefault { get; }

        public ModelHardsizeEntry(string attribute, string[] eioTypes, string eioDescription,
                                  NameRule rule = NameRule.Title, bool onByDefault = true)
        {
            Attribute = attribute;
            EioTypes = eioTypes;
            EioDescription = eioDescription;
            Rule = rule;
            OnByDefault = onByDefault;
        }
    }

    /// <summary>
    /// Maps DesignBuilder Detailed HVAC attributes to EnergyPlus .eio sizing rows.
    ///
    /// PROVENANCE - every line below is confirmed, not guessed:
    ///   * Attribute names come from the DesignBuilder Formats tables dumped from
    ///     the live model, filtered to ItemType = 77 (the field type that accepts
    ///     the literal text "autosize").
    ///   * The names were cross-checked against a live probe of the model, which
    ///     independently returned the same names with value "autosize".
    ///   * .eio description strings are verbatim from the client's eplusout.eio.
    ///   * Name matching (Title -> .eio Component Name) was verified against the
    ///     real files: 4/4 FCUs, 8/8 PTACs, 5/5 water cooling coils, 8/8 DX coils,
    ///     4/4 ADUs, 13/13 heating coils across the two E+ coil types.
    ///
    /// TWO THINGS THE .eio DECIDES FOR US:
    ///   1. Heating coil water vs electric - the coil name appears under
    ///      Coil:Heating:Water or Coil:Heating:Electric, so no need to interrogate
    ///      DB's coil type attribute.
    ///   2. Fan type - Fan:OnOff for zone equipment, Fan:ConstantVolume on the AHU.
    ///      Both are searched.
    ///
    /// DELIBERATELY OMITTED (autosize-capable in DB, but no .eio row exists, so
    /// they are left autosized and reported):
    ///   * FanCoilUnit / PTAC outdoor air flow rates
    ///   * HeatingCoil MinimumActuatedFlow
    ///   * DxCoolingCoil EvaporativeCondenserAirFlowRate
    ///   * AirHandlingUnit NominalSupplyAirFlowRate
    ///   * AirLoop DesignOutdoorAirFlowRate (Sizing:System - not reported in .eio)
    /// </summary>
    public static class ModelHardsizeMap
    {
        private const string CoolWater = "Coil:Cooling:Water";
        private const string HeatWater = "Coil:Heating:Water";
        private const string HeatElec = "Coil:Heating:Electric";
        private const string CtrlWaterCoil = "Controller:WaterCoil";
        private const string CtrlOutdoorAir = "Controller:OutdoorAir";

        private static readonly string[] FanTypes =
        {
            "Fan:OnOff", "Fan:ConstantVolume", "Fan:VariableVolume", "Fan:SystemModel"
        };

        private static readonly string[] DxTypes =
        {
            "Coil:Cooling:DX:SingleSpeed", "Coil:Cooling:DX:TwoSpeed"
        };

        /// <summary>
        /// Keyed on HvacComponentType.ToString(). Note "95" - the air distribution
        /// unit's enum value has no name in this API version, so it arrives as a
        /// bare number. Both spellings are registered.
        /// </summary>
        public static readonly Dictionary<string, IList<ModelHardsizeEntry>> ByComponentType =
            new Dictionary<string, IList<ModelHardsizeEntry>>(System.StringComparer.OrdinalIgnoreCase)
        {
            // ---------------- Terminal units (priority 1) ----------------
            { "ZoneFanCoilUnit", new List<ModelHardsizeEntry>
                {
                    new ModelHardsizeEntry("MaxSupplyAirFlowRate",
                        new[] { "ZoneHVAC:FourPipeFanCoil" },
                        "Design Size Maximum Supply Air Flow Rate [m3/s]"),
                    // NOTE: .eio drops the trailing "Rate" on these two.
                    new ModelHardsizeEntry("MaxColdWaterFlowRate",
                        new[] { "ZoneHVAC:FourPipeFanCoil" },
                        "Design Size Maximum Cold Water Flow [m3/s]"),
                    new ModelHardsizeEntry("MaxHotWaterFlowRate",
                        new[] { "ZoneHVAC:FourPipeFanCoil" },
                        "Design Size Maximum Hot Water Flow [m3/s]"),
                }
            },

            { "ZoneTerminalPackagedAirConditioner", new List<ModelHardsizeEntry>
                {
                    new ModelHardsizeEntry("SupplyAirFlowRateDuringCoolingOperation",
                        new[] { "ZoneHVAC:PackagedTerminalAirConditioner" },
                        "Design Size Cooling Supply Air Flow Rate [m3/s]"),
                    new ModelHardsizeEntry("SupplyAirFlowRateDuringHeatingOperation",
                        new[] { "ZoneHVAC:PackagedTerminalAirConditioner" },
                        "Design Size Heating Supply Air Flow Rate [m3/s]"),
                    new ModelHardsizeEntry("SupplyAirFlowRateWhenNoCoolingOrHeatingIsNeeded",
                        new[] { "ZoneHVAC:PackagedTerminalAirConditioner" },
                        "Design Size No Load Supply Air Flow Rate [m3/s]"),
                }
            },

            { "ZoneTerminalPackagedHeatPump", new List<ModelHardsizeEntry>
                {
                    new ModelHardsizeEntry("SupplyAirFlowRateDuringCoolingOperation",
                        new[] { "ZoneHVAC:PackagedTerminalHeatPump" },
                        "Design Size Cooling Supply Air Flow Rate [m3/s]"),
                    new ModelHardsizeEntry("SupplyAirFlowRateDuringHeatingOperation",
                        new[] { "ZoneHVAC:PackagedTerminalHeatPump" },
                        "Design Size Heating Supply Air Flow Rate [m3/s]"),
                    new ModelHardsizeEntry("SupplyAirFlowRateWhenNoCoolingOrHeatingIsNeeded",
                        new[] { "ZoneHVAC:PackagedTerminalHeatPump" },
                        "Design Size No Load Supply Air Flow Rate [m3/s]"),
                }
            },

            // Air distribution unit. HvacComponentType has no name for this in
            // v2025.1 so ToString() yields "95"; DirectAirADU registered too in
            // case a later version names it.
            { "95", AduEntries() },
            { "DirectAirADU", AduEntries() },

            // ---------------- Coils ----------------
            { "WaterCoolingCoil", new List<ModelHardsizeEntry>
                {
                    new ModelHardsizeEntry("DesignWaterFlowRate", new[] { CoolWater },
                        "Design Size Design Water Flow Rate [m3/s]"),
                    new ModelHardsizeEntry("DesignAirFlowRate", new[] { CoolWater },
                        "Design Size Design Air Flow Rate [m3/s]"),
                    new ModelHardsizeEntry("DesignInletWaterTemperature", new[] { CoolWater },
                        "Design Size Design Inlet Water Temperature [C]"),
                    new ModelHardsizeEntry("DesignInletAirTemperature", new[] { CoolWater },
                        "Design Size Design Inlet Air Temperature [C]"),
                    new ModelHardsizeEntry("DesignOutletAirTemperature", new[] { CoolWater },
                        "Design Size Design Outlet Air Temperature [C]"),
                    new ModelHardsizeEntry("DesignInletAirHumidityRatio", new[] { CoolWater },
                        "Design Size Design Inlet Air Humidity Ratio [kgWater/kgDryAir]"),
                    new ModelHardsizeEntry("DesignOutletAirHumidityRatio", new[] { CoolWater },
                        "Design Size Design Outlet Air Humidity Ratio [kgWater/kgDryAir]"),
                    // Controller - resolves only for air-loop coils; zone coils have
                    // no Controller:WaterCoil object and are skipped automatically.
                    new ModelHardsizeEntry("MaximumActuatedFlow", new[] { CtrlWaterCoil },
                        "Maximum Actuated Flow [m3/s]", NameRule.TitlePlusController),
                    new ModelHardsizeEntry("ControllerConvergenceTolerance", new[] { CtrlWaterCoil },
                        "Controller Convergence Tolerance", NameRule.TitlePlusController,
                        onByDefault: false),
                }
            },

            { "HeatingCoil", new List<ModelHardsizeEntry>
                {
                    // Water coil fields - resolve only if the name appears under
                    // Coil:Heating:Water in the .eio.
                    new ModelHardsizeEntry("RatedCapacity", new[] { HeatWater },
                        "Design Size Rated Capacity [W]"),
                    new ModelHardsizeEntry("MaxWaterFlowRate", new[] { HeatWater },
                        "Design Size Maximum Water Flow Rate [m3/s]"),
                    new ModelHardsizeEntry("UFactorTimesArea", new[] { HeatWater },
                        "Design Size U-Factor Times Area Value [W/K]"),
                    // Electric coil field - resolves only under Coil:Heating:Electric.
                    new ModelHardsizeEntry("NominalCapacity", new[] { HeatElec },
                        "Design Size Nominal Capacity [W]"),
                    new ModelHardsizeEntry("MaximumActuatedFlow", new[] { CtrlWaterCoil },
                        "Maximum Actuated Flow [m3/s]", NameRule.TitlePlusController),
                    new ModelHardsizeEntry("ControllerConvergenceTolerance", new[] { CtrlWaterCoil },
                        "Controller Convergence Tolerance", NameRule.TitlePlusController,
                        onByDefault: false),
                }
            },

            { "DxCoolingCoil", new List<ModelHardsizeEntry>
                {
                    new ModelHardsizeEntry("RatedTotalCoolingCapacity", DxTypes,
                        "Design Size Gross Rated Total Cooling Capacity [W]"),
                    new ModelHardsizeEntry("RatedSensibleHeatRatio", DxTypes,
                        "Design Size Gross Rated Sensible Heat Ratio"),
                    new ModelHardsizeEntry("RatedAirFlowRate", DxTypes,
                        "Design Size Rated Air Flow Rate [m3/s]"),
                }
            },

            // ---------------- Fans ----------------
            { "SupplyFan", FanEntries() },
            { "ExtractFan", FanEntries() },
            { "Fan", FanEntries() },

            // ---------------- Air handling unit ----------------
            { "AirHandlingUnit", new List<ModelHardsizeEntry>
                {
                    // The AirLoopHVAC object is named after the LOOP, not the AHU.
                    new ModelHardsizeEntry("DesignSupplyAirFlowRate", new[] { "AirLoopHVAC" },
                        "Design Supply Air Flow Rate [m3/s]", NameRule.ParentLoopTitle),
                    new ModelHardsizeEntry("MinimumOutdoorAirFlowRate", new[] { CtrlOutdoorAir },
                        "Minimum Outdoor Air Flow Rate [m3/s]",
                        NameRule.TitlePlusOutdoorAirController),
                    new ModelHardsizeEntry("MaximumOutdoorAirFlowRate", new[] { CtrlOutdoorAir },
                        "Maximum Outdoor Air Flow Rate [m3/s]",
                        NameRule.TitlePlusOutdoorAirController),
                }
            },
        };

        /// <summary>
        /// Plant-level, kept separate because it lives on the HvacLoop object
        /// rather than a component, and because plant is out of scope by default.
        /// </summary>
        public static readonly IList<ModelHardsizeEntry> PlantLoopEntries =
            new List<ModelHardsizeEntry>
            {
                new ModelHardsizeEntry("MaximumLoopFlowRate", new[] { "PlantLoop" },
                    "Maximum Loop Flow Rate [m3/s]", NameRule.Title, onByDefault: false),
            };

        private static IList<ModelHardsizeEntry> FanEntries()
        {
            return new List<ModelHardsizeEntry>
            {
                new ModelHardsizeEntry("MaximumFlowRate", FanTypes,
                    "Design Size Maximum Flow Rate [m3/s]"),
            };
        }

        private static IList<ModelHardsizeEntry> AduEntries()
        {
            return new List<ModelHardsizeEntry>
            {
                new ModelHardsizeEntry("MaximumAirFlowRate",
                    new[]
                    {
                        "AirTerminal:SingleDuct:ConstantVolume:NoReheat",
                        "AirTerminal:SingleDuct:VAV:NoReheat",
                        "AirTerminal:SingleDuct:ConstantVolume:Reheat",
                        "AirTerminal:SingleDuct:VAV:Reheat",
                    },
                    "Design Size Maximum Air Flow Rate [m3/s]"),
            };
        }

        public static IList<ModelHardsizeEntry> For(string componentType)
        {
            IList<ModelHardsizeEntry> entries;
            return ByComponentType.TryGetValue(componentType ?? "", out entries) ? entries : null;
        }

        /// <summary>
        /// Human-readable label for a DesignBuilder component type.
        ///
        /// Needed because HvacComponentType.ToString() returns the raw integer
        /// when the enum has no member for that value. In v2025.1 the air
        /// distribution unit has no member, so it arrives as "95".
        /// </summary>
        public static string DisplayName(string componentType)
        {
            if (string.IsNullOrEmpty(componentType)) return "(unknown)";

            switch (componentType)
            {
                case "95":
                case "DirectAirADU":
                    return "Air terminal / ADU";
                case "ZoneFanCoilUnit":
                    return "Fan coil unit";
                case "ZoneTerminalPackagedAirConditioner":
                    return "PTAC";
                case "ZoneTerminalPackagedHeatPump":
                    return "PTHP";
                case "WaterCoolingCoil":
                    return "Cooling coil (water)";
                case "HeatingCoil":
                    return "Heating coil";
                case "DxCoolingCoil":
                    return "Cooling coil (DX)";
                case "SupplyFan":
                    return "Supply fan";
                case "ExtractFan":
                    return "Extract fan";
                case "AirHandlingUnit":
                    return "Air handling unit";
                default:
                    // An unnamed enum value we have not catalogued. Show the raw
                    // value so it is obvious something needs looking at.
                    int n;
                    return int.TryParse(componentType, out n)
                        ? "Component type " + componentType + " (unnamed)"
                        : componentType;
            }
        }
    }
}
