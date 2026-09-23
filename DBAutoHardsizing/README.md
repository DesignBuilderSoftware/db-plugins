# Auto Hardsize HVAC settings — DesignBuilder plugin

Replaces autosized HVAC inputs in your model with the values EnergyPlus calculated during sizing, so that subsequent 
runs use fixed sizes instead of re-sizing each time.

> **This plugin modifies your model.** Back up the `.dsb` before using it.

## How it Works

When you run a simulation with components set to Autosize, EnergyPlus calculates the sizes it needs and records them in an output file called `eplusout.eio`.
Normally those values are discarded and the next run simply calculates them again.

The plugin reads this file and writes the calculated values back into your model:

1. It goes through the HVAC components in your model and finds every input currently set to **Autosize**.
2. For each one, it looks up the matching sizing result in `eplusout.eio`, matching components by name.
3. It shows you exactly what it proposes to change, and you confirm.
4. On confirmation, each Autosize input is replaced with its calculated value.

The values then appear in the HVAC dialogs as ordinary numbers. 
You can edit them like any other input, they are saved in the `.dsb`, and they stay fixed on every subsequent run.

**Inputs you have already set yourself are never touched.** 
The plugin only changes fields whose current value is Autosize, and only where EnergyPlus actually reported a size for them.

---

## Requirements

The plugin works on the Detailed HVAC network. 
If a model uses Simple HVAC there is no component network to read, and the plugin will report that it found nothing to change.

A prior completed simulation that produced `eplusout.eio` is required.

### What it covers

- Fan coil units, PTACs, PTHPs and air terminals — supply air flow rates
- Heating coils (water and electric) — capacity, water flow, UA value
- Cooling coils (water and DX) — capacity, air and water flow, design conditions
- Supply and extract fans — maximum flow rate
- Air handling units — design supply air flow, outdoor air flow rates
- Water coil controllers — maximum actuated flow

### What it does not cover

- **Plant equipment** — boilers, chillers, pumps and plant loops are excluded by design. 
  These are reported in the dialog so you can see what was left alone.
- **System and zone sizing parameters** — EnergyPlus does not report these in `eplusout.eio`, so they cannot be read back.

---

## Installation

1. Close DesignBuilder.
2. Copy the two DesignBuilder assemblies from the plugin references:
   - `DB.Api.dll`
   - `DB.Extensibility.Contracts.dll`
3. Build the plugin:
   - Build DBAutoHardsizeModel.csproj in Visual Studio to produce DBAutoHardsizeModel.dll
4. Create the plugin folder:
   ```
   %LOCALAPPDATA%\DesignBuilder\User Plugins\AutoHardsizeModel\
   ```
5. Copy three files into it:
   - `DBAutoHardsizeModel.dll`
   - `DB.Api.dll`
   - `DB.Extensibility.Contracts.dll`

   The two `DB.*` files come from your DesignBuilder installation, under
   `Components\DB.Api\` and `Components\DB.Extensibility\`. Use the copies from
   your own installation so the versions match.
6. Start DesignBuilder. A **Hardsizing** menu appears in the menu bar.

---

## Using the plugin

1. **Open the model** you want to hardsize.
2. **Run a simulation.** Any run that completes sizing will do. This produces
   the `eplusout.eio` file the plugin reads.
3. Select **Hardsizing → Auto hardsize (model)**.
4. **Review the confirmation dialog** (see below), then click **Hardsize**.
5. **Save the model.** The plugin writes the values but does not save for you —
   if you close without saving, the changes are lost.
6. **Re-run** to confirm the model still simulates and gives comparable results.

## Installation

1. Close DesignBuilder.
2. Create this folder:
   ```
   %LOCALAPPDATA%\DesignBuilder\User Plugins\AutoHardsizeModel\
   ```
3. Copy three files into it:
   - `DBAutoHardsizeModel.dll`
   - `DB.Api.dll`
   - `DB.Extensibility.Contracts.dll`

   The two `DB.*` files come from your DesignBuilder installation, under
   `Components\DB.Api\` and `Components\DB.Extensibility\`. Use the copies from
   your own installation so the versions match.
4. Start DesignBuilder. A **Hardsizing** menu appears in the menu bar.

---

## Usage

1. Run an autosized simulation. This produces the `eplusout.eio` file the plugin reads.
   **If you are using the Simulation Manager, load the results into DesignBuilder.**
2. Select **Hardsizing → Auto hardsize (model)**.
3. **Review the confirmation dialog** (see below), then click **Hardsize**.
4. Apply, then **save the model**.
5. **Re-run** to confirm the model still simulates and gives comparable results.

### The confirmation dialog

Before anything is written, the dialog lists every object and input that will change, with its new value. 
Components are grouped by type with a tick box each, so you can exclude a whole group.

It also reports, separately:

- **Zero values** — inputs that sized to zero. These are excluded by default and need an explicit tick.
- **Left as Autosize** — inputs with no corresponding sizing result.
- **Out of scope** — plant equipment found in the model and deliberately skipped.

*Why some inputs stay on Autosize*

DesignBuilder stores the same set of inputs for every variant of a component.
An electric heating coil, for example, still holds the water-coil inputs (rated capacity, water flow rate, UA value) even though they are not used. 
They show as Autosize, but EnergyPlus never calculates a size for them because they are never written to the simulation file.

---

## Troubleshooting

**The Hardsizing menu doesn't appear.**
Check that `DB.Api.dll` and `DB.Extensibility.Contracts.dll` are in the plugin
folder alongside the plugin, and that DesignBuilder was restarted. There is no
error message when a plugin fails to load.

**"No .eio file found from a previous autosized run."**
Run a simulation first. If you have run one, check that it completed sizing
rather than failing early.
*If Simulation Manager was used to run the model, you will need to load the results into DesignBuilder first.*

**"Nothing to hardsize."**
Either no HVAC components are currently set to Autosize, or the model is using
Simple HVAC rather than Detailed HVAC.

**Results change noticeably after hardsizing.**
Some change is expected, since sizing is no longer recalculated. A large change
suggests a component was fixed at an inappropriate value — check the zero-value
list first.

---
