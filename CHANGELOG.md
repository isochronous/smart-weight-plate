# Changelog

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
