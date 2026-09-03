# Auto Hardsize (model level) — DesignBuilder plugin

Writes EnergyPlus sizing results from the last autosized run **into the
DesignBuilder model itself**. The values then appear in the HVAC dialogs, persist
in the `.dsb`, and survive IDF regeneration — which the earlier IDF-level plugin
could not do.

**This one modifies your model. Back up the `.dsb` first.**

## The rule

Only attributes whose current value is literally `autosize` are changed.
Everything else is left exactly as it is.

The autosize-capable set is not something I guessed — it comes from
DesignBuilder's own Formats tables, filtered to **`ItemType = 77`**, the field
type that accepts the text `autosize`. Fields with `ItemType = 7` are plain
numeric and are never touched.

`Default` and `ItemType` are independent, which the PTHP table proves: its
outdoor air flows are ItemType 77 with a blank default — autosize-capable, but
not autosized by default. So capability and current state are checked separately.

## Verified against your files

Simulating the plugin against `hvac_attributes.csv` + `eplusout.eio`:

| | |
|---|---|
| Parameters matched to a sizing result | **118** |
| Left as `autosize` (no matching `.eio` row) | 49 — all expected, see below |
| Zero-valued, excluded by default | 4 |

Matched by component: WaterCoolingCoil 37, DxCoolingCoil 24, HeatingCoil 24,
SupplyFan 13, ZoneFanCoilUnit 12, ADU 4, AirHandlingUnit 3, ExtractFan 1.

**Plus 24 PTAC parameters** (8 PTACs × 3 flow rates) — see the caveat below.

### Provenance of every mapping

- **Attribute names** — DesignBuilder Formats tables, ItemType 77.
- **Cross-checked** — an independent live probe of your model returned the same
  names with value `autosize`.
- **`.eio` descriptions** — verbatim from your `eplusout.eio`.
- **Name matching** — `Title` ↔ `.eio` Component Name, verified 4/4 FCUs,
  8/8 PTACs, 5/5 water cooling coils, 8/8 DX coils, 4/4 ADUs, 13/13 heating coils.

### Two things the `.eio` decides, so the plugin doesn't have to

1. **Water vs electric heating coils.** All 13 coils expose the same six
   attributes in DesignBuilder, so the model can't tell them apart. The `.eio`
   can: 9 names appear under `Coil:Heating:Water`, 4 under
   `Coil:Heating:Electric`. Whichever type holds the name wins.
2. **Fan type.** 12 fans are `Fan:OnOff`, the AHU supply fan is
   `Fan:ConstantVolume`. Both are searched.

## The 49 left as autosize are all correct

- `NominalCapacity` on **water** heating coils — no electric row exists. DB keeps
  an unused field; harmless, and it isn't written to the IDF.
- `RatedCapacity` / `MaxWaterFlowRate` on the four **electric** PTAC coils — the
  mirror image.
- Controller attributes on **zone** coils — only air-loop water coils get a
  `Controller:WaterCoil` object, so there's nothing to read. Correct behaviour,
  and it matches the IDF, which has exactly two controllers.

All of them are listed in the confirmation dialog rather than hidden.

## Zero values are excluded by default

Your AHU heating coil sizes to zero:

```
Air Loop AHU Heating Coil   RatedCapacity        = 0
Air Loop AHU Heating Coil   MaxWaterFlowRate     = 0
Air Loop AHU Heating Coil   MaximumActuatedFlow  = 0
Air Loop AHU                MinimumOutdoorAirFlowRate = 0
```

Hardsizing a zero is not the same as leaving it autosized. Autosize would
re-size the coil if an ECM raised the heating load; a hard `0` fixes it at zero
capacity permanently and the coil silently does nothing. They're listed under a
warning and need an explicit tick.

Worth understanding why that coil sizes to zero before this becomes your
baseline — it may point at the air loop setup rather than at the plugin.

## Caveat: the PTAC path is unverified

The discovery probe never tested the PTAC attribute names — I'd shortened them
when rebuilding the map from the `.eio` wording, so it probed
`CoolingSupplyAirFlowRate` when DesignBuilder uses
`SupplyAirFlowRateDuringCoolingOperation`.

The corrected names are confirmed from `PackagedTerminalAirConditionerFormats.txt`
(ItemType 77, default `autosize`), all 8 PTAC titles match `.eio` rows, and all
24 lookups resolve. But there is no live read confirming the attributes return
`autosize` on your actual components, because that read was never taken.

**So check the PTACs first after running.** If they don't change, the attribute
names are the place to look, and the fix is one line each in `ModelHardsizeMap`.

## Install

Target **.NET Framework 4.8**. Copy the built DLL to:

```
%LOCALAPPDATA%\DesignBuilder\User Plugins\AutoHardsizeModel\
```

with `DB.Api.dll` and `DB.Extensibility.Contracts.dll`. Restart DesignBuilder.

## Usage

1. Run an autosized simulation.
2. **Hardsizing → Auto hardsize (model)**.
3. Review the dialog — objects grouped by type, tick boxes per type, zero-value
   warnings, and everything left autosized.
4. Apply, then **save the model**.

## Implementation notes

- **`UpdateAttributeData()` is called after the writes**, once per component. The
  API docs say it synchronises the HVAC network with attribute changes; without
  it the writes may appear to succeed and not stick.
- **`GetAttribute` returns the literal string `"UNKNOWN"`** for attributes that
  don't apply to a component's sub-type, not null. That's used as the
  applicability filter, and it's why the earlier discovery run reported 9585
  "hits" when the real number was 323.
- **The autosize marker's capitalisation is inconsistent** — plant components use
  `Autosize`, zone and air components use `autosize`, heating coils use both in
  the same model. Comparison is case-insensitive throughout.
- **Plant is out of scope** by agreement. `MaximumLoopFlowRate` is mapped and
  ready in `ModelHardsizeMap.PlantLoopEntries` but not wired into the walk.

## Known limitations

- **Uncompiled and untested.** No .NET toolchain or DesignBuilder here. The API
  calls are verified against the v2025.1 reference and the logic is verified
  against your data, but expect to shake out build issues.
- The model-side and IDF-side autosize states **can disagree**. Your FCUs and
  PTACs report `MaxOutdoorAirFlowRate = autosize` in the model while the IDF
  writes `0.0`, presumably because outdoor air supply is off. Those have no
  `.eio` rows so they're skipped anyway, but don't assume the two views match.
- Field coverage is bounded by what EnergyPlus reports. `Sizing:System`,
  `Sizing:Zone` and the DX evaporative condenser flow have no `.eio` rows and
  cannot be hardsized this way.
