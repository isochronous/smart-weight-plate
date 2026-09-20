# Smart Weight Plate

An [Oxygen Not Included](https://www.klei.com/games/oxygen-not-included) mod that adds a **Smart Weight Plate**: a weight plate with a low and a high threshold, so its signal behaves like the Smart Reservoir's instead of a single cut-off.

Status: **work in progress**. The building uses the vanilla weight plate art with a blue tint as placeholder art.

## What it does

The plate weighs whatever sits on the tile above it, exactly like the vanilla Weight Plate: a solid tile, loose items and debris, and Duplicants or critters standing on it. Gases and liquids do not count.

- Sends a **green** signal once the weight drops to the **Low Threshold**, and keeps sending it until the weight reaches the **High Threshold**.
- Sends a **red** signal once the weight reaches the High Threshold, and keeps sending it until the weight drops to the Low Threshold.
- Comparisons are inclusive, as with the Smart Reservoir. If the two thresholds are equal the plate acts as a plain "at or above" threshold.
- **Invert Signal** swaps green and red without a NOT gate.
- Thresholds run from 0 to 2000 kg in whole kilograms, using the same two-slider panel as the Smart Reservoir. The plate's own panel shows the current weight and the invert checkbox.
- The plate looks pressed while it is holding in the heavy state, and lights up with the signal it is sending.
- Supports the copy-settings tool; all settings are saved with the building.

Typical use: keep a debris pile between two limits, so an auto-sweeper or conveyor loader tops it up only after it has run low, instead of chattering around a single threshold.

Unlocked by **Advanced Automation** (the tier after the Weight Plate's Generic Sensors). Costs 75 kg Refined Metal. Found under Automation > Sensors, next to the vanilla Weight Plate.

## Publishing

Steam Workshop item **3804040668**. `publish/content` holds the upload set (DLL, `mod.yaml`, `mod_info.yaml`, `preview.png`) and `publish/workshop-description.txt` the Steam-markup description. Update it by zipping the contents of `publish/content` and running `common/tools/WorkshopUpload update 3804040668 <zip> publish/preview.png`, or with Klei's **Oxygen Not Included Uploader** (Steam Library > Tools), never with steamcmd; see the [oni-mods-common README](https://github.com/isochronous/oni-mods-common#publishing-to-the-steam-workshop) for why. The preview is composed from the game's weight plate sprite with `common/tools/MakePreview`.

## Building

Requires the .NET SDK (8+). Shared build configuration lives in the [oni-mods-common](https://github.com/isochronous/oni-mods-common) submodule, so clone with `--recurse-submodules` (or run `git submodule update --init`). The game DLLs are referenced directly from the game install; override the path if yours differs:

```
dotnet build src/SmartWeightPlate -c Release -p:GameFolder="<path-to>\OxygenNotIncluded"
```

A successful build merges [PLib](https://github.com/peterhaneve/ONIMods/tree/main/PLib) into the DLL and deploys the mod to `Documents\Klei\OxygenNotIncluded\mods\local\SmartWeightPlate` (disable with `-p:ModDeployFolder=none`).

## Implementation notes

- `SmartWeightPlate` (the building component) measures mass the same three ways as the vanilla `LogicMassSensor`, re-evaluates its latch every 200 ms, and sends on its own output port. It implements `IActivationRangeTarget`, so the vanilla `ActiveRangeSideScreen` supplies the threshold sliders with no custom UI. Note that interface's naming is inverted: `ActivateValue` is the upper slider (high threshold) and `DeactivateValue` the lower one, the same swap the Smart Reservoir makes.
- `SmartWeightPlateSideScreen` is a small PLib panel (current weight, invert checkbox) sorted above the vanilla slider panel.
- Patch points: `GeneratedBuildings.LoadGeneratedBuildings` (plan screen), `Db.Initialize` (tech), `DetailsScreen.OnPrefabInit` (side screen registration).
