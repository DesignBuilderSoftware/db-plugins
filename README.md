# DB Plugins

Example C# plugins for [DesignBuilder](https://www.designbuilder.co.uk/)'s extensibility platform. Each project builds to a standalone DLL that DesignBuilder loads at startup via [MEF](https://learn.microsoft.com/en-us/dotnet/framework/mef/) and demonstrates a different corner of the plugin API — menus, custom attributes, model-hierarchy walks, report generation, and UI extension.

This repo is the upstream source referenced by DesignBuilder's own Extensibility documentation and by the `db-extensibility` skill's `examples/upstream/` folder.

## Plugin catalogue

| Project | Interface | Demonstrates |
|---|---|---|
| [DBStaticMenuPluginExample](DBStaticMenuPluginExample/Program.cs) | `IPlugin` | Simplest possible plugin: one static top-level menu item that shows a message box. Good smoke test for a new build/deploy setup. |
| [DBDynamicMenuPluginExample](DBDynamicMenuPluginExample/Program.cs) | `IPlugin` | A menu tree whose items toggle each other's enabled/visible state at runtime (`IsMenuItemEnabled`/`IsMenuItemVisible` are re-queried on every press). |
| [DBAttributeAdditionPluginExample](DBAttributeAdditionPluginExample/ExampleAddAttributePlugin.cs) | `IPlugin2` | Registers a brand-new custom attribute (`DBExampleAttribute`) in the `EditFormats` table at Site level, then shows a dialog to read/write it. Gated on `ModelLoaded`/`ModelUnloaded` and `ScreenChanged`. |
| [DBUIAdditionPluginExample](DBUIAdditionPluginExample/ExampleAddUIPlugin.cs) | `IPlugin2` | Adds a custom UI header with child widgets (text edit, checkbox, numeric edit) to the model-data screens by writing new `EditFormats` records. |
| [DBDesignCalculationReportPluginExample](DBDesignCalculationReportPluginExample/Programs/DesignReportPlugin.cs) | `IPlugin` | Exports heating/cooling design-calculation results (per building/zone) to a `.csv` file, with a dialog for choosing which coincident/non-coincident summaries to include and SI/IP unit conversion. |
| [DBReportPluginWithDialogExample](DBReportPluginWithDialogExample/ExampleReportPlugin.cs) | `IPlugin2` | Walks the full model hierarchy (Site → Building → BuildingBlock → Zone → Surface → Adjacency → Opening) and writes a nested-text `.txt` report of geometry, constructions, gains and airflow, with dialog-selectable detail level. |
| [DBFDWRReport](DBFDWRReport/Program.cs) | `IPlugin2` | Computes Fenestration-and-Door-to-Wall Ratio (FDWR) per building and per orientation (N/S/E/W) from external wall/opening areas, shown in a message box. |
| [DBReportBridgingLengths](DBReportBridgingLengths/Program.cs) | `IPlugin2` | Reports linear thermal-bridging junction lengths and Psi-values per zone (wall–floor, wall–roof, lintels, sills, jambs, etc.), saved to a `.txt` file. |
| [DBApplyMeasureExample](DBApplyMeasureExample/program.cs) | `IPlugin2` | Applies two ECM-style measures to the current building via menu: force the air-handling-unit economiser on (ECM 1), tighten zone infiltration to a fixed rate (ECM 2). |

`DBPlugins.sln` includes all nine projects; open it in Visual Studio to build any or all of them.

## How the plugins work (reference)

### Loading model

DesignBuilder discovers plugins at startup using MEF (`System.ComponentModel.Composition`). Every plugin exports its class as `IPlugin` or `IPlugin2`:

```csharp
[Export(typeof(IPlugin2))]
public class ExamplePlugin : PluginBase2, IPlugin2 { ... }
```

If the `[Export(...)]` attribute is missing, the assembly builds fine but DesignBuilder silently skips it — this is the most common "why isn't my plugin showing up" mistake.

- **`IPlugin` / `PluginBase`** — simulation, optimisation, CFD, daylighting, cost/carbon hooks (e.g. `BeforeEnergySimulation`, `AfterEnergySimulation`) plus the menu API.
- **`IPlugin2` / `PluginBase2`** — everything `IPlugin` has, plus `ModelLoaded()`, `ModelUnloaded()`, and `ScreenChanged(ScreenCode)`. Prefer `IPlugin2` for new plugins; six of the nine examples in this repo use it.

All projects here build with `<OutputType>Library</OutputType>` — they are DLLs, not executables. A few of the older examples (e.g. `DBAttributeAdditionPluginExample`) still carry a `Program.cs` with a WinForms `static void Main()`; that's inert leftover boilerplate from the Visual Studio project template and never runs, since a Library has no entry point.

### API entry point

Every plugin receives a singleton `DB.Api.Environment` (set by DesignBuilder) through the `ApiEnvironment` property inherited from `PluginBase`/`PluginBase2`:

```
ApiEnvironment
├── .Site                     → Building (1..n) → BuildingBlock (1..n) → Zone (1..n) → Surface (1..n)
│                                                                                        ├── Adjacency
│                                                                                        └── Opening
├── .ApplicationTemplates     → library/template tables (e.g. "EditFormats")
├── .CurrentBuildingIndex     → index of the building currently shown in the UI
└── .HvacOperations, .CfdOperations, .Events, undo/redo, navigation, ...
```

Cache `ApiEnvironment`/`Site` in `Create()` or `ModelLoaded()` if you need it outside a hook method — see `DBAttributeAdditionPluginExample` and `DBUIAdditionPluginExample` for the pattern.

Most decomposition-level objects (Site, Building, Zone, Surface, Adjacency, Opening, ...) store data as string key/value **attributes**:

```csharp
string raw = zone.GetAttribute("OccupancyValue");
zone.SetAttribute("OccupancyValue", "0.1");
```

Attribute keys are discoverable in the running application via **Tools → Program options → Interface → Interface Style → Show attribute names in tooltips**.

### Menus

`MenuLayout` is a tiny DSL returned as a string; each line is `*<indentation-as-'>' repeats><label>,<key>`. `HasMenu`/`MenuLayout` are read once at load; `IsMenuItemVisible`/`IsMenuItemEnabled` are re-queried on load **and** on every menu press, which is how dynamic menus (see `DBDynamicMenuPluginExample`, `DBFDWRReport`, `DBReportBridgingLengths`, `DBApplyMeasureExample`) toggle sibling items' state from inside `OnMenuItemPressed`.

### Hook points

`IPlugin` methods are called at fixed points in the simulation/optimisation/CFD/daylighting/cost-carbon pipeline (`BeforeEnergySimulation`, `AfterEnergySimulation`, ...); `IPlugin2` adds `ModelLoaded`, `ModelUnloaded`, `ScreenChanged`. Execution order across all active scripts and plugins is: scripts (Script Manager order) → plugins (alphabetical by assembly name). Don't assume state carries between hook calls — another script or plugin may have mutated it in between.

For the full canonical hook list, table catalogue, and attribute reference, see the `db-extensibility` skill (`reference/hooks.md`, `reference/tables.md`, `reference/attributes.md`), or the official PDF linked below.

### Deploying a built plugin

Copy the built DLL plus `DB.Api.dll` and `DB.Extensibility.Contracts.dll` into their own subdirectory under:

```
%LOCALAPPDATA%\DesignBuilder\User Plugins\<YourPluginName>\
```

Restart DesignBuilder to pick it up. To disable a plugin without deleting it, remove the `[Export(...)]` attribute and rebuild, or just delete its subdirectory.

## Building

**Prerequisite**: a local DesignBuilder installation (default `C:\Program Files (x86)\DesignBuilder\`). All nine projects reference `DB.Api.dll` and `DB.Extensibility.Contracts.dll` straight from that install's `Lib` folder via `$(MSBuildProgramFiles32)\DesignBuilder\Lib\*.dll` — there is no bundled copy of these DLLs in the repo, so building requires DesignBuilder to be installed on the build machine.

1. Open `DBPlugins.sln` in Visual Studio 2019+ (or `msbuild DBPlugins.sln`).
2. Build. `DB.Api.dll` / `DB.Extensibility.Contracts.dll` copy-local into each project's output folder alongside the plugin DLL (default `CopyLocal` behaviour), so the output folder is ready to deploy as-is.
3. Build output lands in each project's own `bin\Debug` / `bin\Release`.

All nine projects target **.NET Framework 4.8** (`TargetFrameworkVersion` and each `App.config`'s `supportedRuntime` are kept in sync).

## Adding a new plugin project

1. Create a new **Class Library (.NET Framework 4.8)** project alongside the existing ones.
2. Reference `DB.Api.dll` and `DB.Extensibility.Contracts.dll` from the local DesignBuilder install (`$(MSBuildProgramFiles32)\DesignBuilder\Lib\*.dll`) plus `System.ComponentModel.Composition` — see any existing `.csproj` for the full reference list.
3. Implement `IPlugin` or `IPlugin2` (prefer `IPlugin2`) on a class deriving from `PluginBase`/`PluginBase2`, and mark it `[Export(typeof(IPlugin))]` / `[Export(typeof(IPlugin2))]`.
4. Add the project to `DBPlugins.sln`.
5. Add a row to the [plugin catalogue](#plugin-catalogue) table above.

## Documentation

Detailed DB API documentation can be accessed at:\
https://www.designbuilder.co.uk/downloads/help/DesignBuilder_Extensibility_User_Guide_v7.3.pdf
