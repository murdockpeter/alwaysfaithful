# Always Faithful

Standalone 2030 near-future USMC tactical game and tactical-resolution module for Sea of Uncertainty.

## Current prototype

The first Unity spike contains a compact 250 m flat-top hex board and one selectable USMC rifle-platoon counter. Selecting the platoon reveals its legal four-AP movement area; hovering a legal destination previews the deterministic least-cost path, and clicking moves the counter.

Open `Assets/Scenes/HexAndCounterPrototype.unity` and enter Play mode.

Controls:

- Left click the counter to show legal movement destinations.
- Hover a highlighted destination to preview its least-cost path.
- Left click a highlighted destination to move.
- Left click another hex to inspect its terrain and movement cost.
- Mouse wheel zooms.
- Middle-mouse drag or WASD pans.
- R resets the camera.

## Geography strategy

The prototype carries the same cropped Natural Earth coastline and NOAA ETOPO 2022 source assets used by Sea of Uncertainty so the games can share geographic provenance and a related command-map palette.

The current ETOPO bake is two arc-minutes (roughly 3–4 km sample spacing) and the coastline is simplified for an operational map. These resources are visual-reference data only at Always Faithful's initial 250 m tactical scale. They must not determine authoritative tactical cover, movement, or LOS. A later importer will use higher-resolution DEM and vector data behind the same geography interface.

See `Assets/Resources/Geography/NOTICE.md` for attribution.
