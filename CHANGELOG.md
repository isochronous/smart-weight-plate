# Changelog

## Unreleased

### Changed
- The signal now matches the vanilla Weight Plate by default: green once the weight reaches the High Threshold, red once it drops to the Low Threshold. Plates built with earlier versions keep the behaviour they had: their Invert setting is flipped once, the first time the save is loaded with this version.
- New art by 3GuB: two knobs on the plate slide across when it is pressed and back when it is released, and the button sits behind the plate.
- The knobs move only when the pressure state changes; a signal change alone, such as toggling Invert, only switches the light.

## 0.1.4 - 2026-10-03

- Internal cleanup; the plate behaves as before.

## 0.1.3 - 2026-09-23

### Fixed

- With Invert Signal on, the output port's hover text and details described the non-inverted plate (Red "at or above the High Threshold" while the plate was light). The port now reads Green at or above the High Threshold, Red at or below the Low Threshold, when inverted.

## 0.1.2 - 2026-09-23

### Fixed

- A placed but unbuilt plate now shows the usual white construction outline instead of looking already built.
- The Low and High Threshold text boxes accept the full 0-2000 kg range; typed values were being capped at 100.

## 0.1.1 - 2026-09-20

- Custom art: the plate shows its signal colour and sinks when pressed, like the vanilla Weight Plate.
- The plate is drawn behind automation wires in the Automation overlay.
- With Better Automation Overlay installed, the overlay label shows the thresholds in kg rather than percentages.
- Building description wording.

## 0.1.0 - 2026-09-20

- First release: a weight plate with a low and a high threshold, so its signal behaves like a Liquid or Gas Reservoir's, plus an Invert Signal option.
