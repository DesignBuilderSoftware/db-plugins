# HVAC Data Import - DesignBuilder plugin

Imports HVAC component inputs that you have edited in a spreadsheet back into your model. Export a
table from the model data grid, change the capacities, efficiencies, flow rates or pressure rises in
Excel, save it as CSV, and the plugin writes the new values into every matching component in one step.

> **This plugin modifies your model.** Back up the `.dsb` before using it.

## How it works

1. You select one or more CSV files exported from the model data grid.
2. The plugin finds each row's component in the Detailed HVAC network of the current building by its
   name (for example `BLOCK1:ZONE1 PTHP DX Cooling Coil`).
3. It reads the unit system currently selected in DesignBuilder (SI or IP) and compares each value
   with the model.
4. A confirmation dialog lists every value that will change, with the current and new values in
   DesignBuilder's units. You can untick a whole column, for example every pressure rise, before
   importing.
5. The accepted values are written to the model and then read back to confirm they were kept.

The values then appear in the HVAC dialogs as normal inputs, are saved in the `.dsb` and are used on
every subsequent simulation.

### Units

The plugin checks DesignBuilder's unit setting every time you start an import, so changing between SI
and IP while a model is open is picked up.

Each column's units are read from its caption in the file. If they are the same as DesignBuilder's,
the values are shown exactly as written in the file. If they differ (for example an IP export imported
while DesignBuilder is set to SI), they are converted for display and the original file value is shown
next to them. You can therefore import a file exported in either unit system.

DesignBuilder holds model data in SI internally and converts it for display, so the plugin always
writes SI values to the model. The values you see in the HVAC dialogs afterwards follow your unit
setting as usual.

### What is imported

| Model data grid table | Columns imported |
|---|---|
| DX cooling coil | Rated total cooling capacity, rated sensible heat ratio, gross rated COP |
| DX heating coil | Gross rated heating capacity, gross rated heating COP |
| Fan | Maximum flow rate (see below), pressure rise |

**Fan flow rates.** A fan inside a unit takes its flow rate from the unit, and its own maximum flow
rate is greyed out in DesignBuilder. For these fans the plugin writes the flow rate to the unit
instead:

| Fan inside | Flow rate written to |
|---|---|
| Air handling unit | Design supply air flow rate |
| PTHP or PTAC | Supply air flow rates for cooling operation and heating operation |
| Fan coil unit | Maximum supply air flow rate |

The no-load supply air flow rate of a PTHP or PTAC is not changed. Extract fan flow rates follow
their unit and are not imported. A fan inside any other type of unit is listed as not imported; the
unit types and their inputs are in `SupplyFanFlowOnUnit` in `ImportMap.cs`. Fans that are not inside
a unit have their own maximum flow rate set directly.

All other columns are ignored, even if you edited them. Values already matching the model are left
alone, and blank cells are skipped. `autosize` can be entered for capacities, sensible heat ratio and
flow rate.

Unedited cells do not cause small changes: a value is treated as unedited when the model value, shown
in the file's units and to the number of decimal places written in the file, reads the same. For
example, a fan at 100 Pa that the export shows as `0.4` inH2O is left at 100 Pa.

Values outside the EnergyPlus input limits (for example a sensible heat ratio outside 0.5 to 1.0, or a
zero capacity, COP or flow rate) are rejected and listed in the dialog.

## Requirements

- DesignBuilder 2026.1 (EnergyPlus 25.1) or later.
- A model using Detailed HVAC.
- CSV files exported from the model data grid. Excel's "CSV (Comma delimited)" and "CSV UTF-8" formats
  both work, as does a semicolon separator.

## Installation

1. Close DesignBuilder.
2. Build `DBHvacGridImport.csproj` in Visual Studio (Release) to produce `DBHvacGridImport.dll`.
   If the build cannot find `DB.Api.dll` or `DB.Extensibility.Contracts.dll`, update the two
   `HintPath` entries in the `.csproj` to your DesignBuilder installation.
3. Create the folder `%LOCALAPPDATA%\DesignBuilder\User Plugins\DBHvacGridImport\`.
4. Copy into it:
   - `DBHvacGridImport.dll`
   - `DB.Api.dll` and `DB.Extensibility.Contracts.dll` from your own DesignBuilder installation, so
     the versions match.
5. Start DesignBuilder. A **HVAC Data Import** menu appears in the menu bar.

To disable the plugin, delete its folder (or move it out of `User Plugins`).

## Using the plugin

1. Open the model and export the HVAC table you want to edit from the model data grid to CSV.
2. Open the CSV in Excel and change the values you need. Keep the first five rows and the columns
   holding the component, zone and loop names unchanged, and do not rename components in the model
   between exporting and importing.
3. Save the file as CSV.
4. In DesignBuilder, go to the building to update and choose
   **HVAC Data Import > Import model data grid CSV...**. Select one or more files (hold Ctrl to
   select several).
5. Review the confirmation dialog. Untick any column you do not want to import, then click **Import**.
6. Read the summary message, then save the model.

## Adding tables or columns

The imported tables and columns are listed in the USER CONFIGURATION SECTION of `ImportMap.cs`. The
attribute names are in row 4 of the export, so a new column needs only one line: the attribute name,
its quantity (power, volume flow, pressure or none, which sets the unit conversion) and its valid
range. Rebuild the plugin after editing.

## Limitations

- CSV only. Save `.xlsx` workbooks as CSV before importing.
- Only the current building is updated. In a single-building model the Site level also works.
- DX cooling coils: only the general (single-speed) fields are imported, not the per-stage fields of
  multi-stage or dehumidification-mode coils.
- Values are not checked against each other. Fixing a coil capacity or fan flow while related inputs
  stay autosized (for example the coil rated air flow rate, or the PTHP supply air flow rates) can
  produce EnergyPlus warnings about inconsistent sizes. Check `eplusout.err` after the first run.
- Components within the same zone group: if the summary reports values that were "written but not
  kept", those components may share their data and cannot hold different values.

## Disclaimer

Provided as-is without warranty. DesignBuilder takes no responsibility for simulation results,
accuracy, or any issues arising from its use. Validate all outputs for your project.
