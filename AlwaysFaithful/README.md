# Always Faithful

Standalone 2030 near-future USMC tactical game and tactical-resolution module for Sea of Uncertainty.

## Current prototype

The prototype now presents the complete island of Taiwan as a 64×104 whole-island operational board (6,656 hexes at roughly 4 km spacing), using the same Natural Earth coastline and NOAA ETOPO 2022 pipeline as Sea of Uncertainty. One selectable USMC rifle-platoon counter exercises the order workflow. The platoon begins unselected. Opening its order menu and choosing Move reveals its legal four-AP movement area; hovering a legal destination previews the deterministic least-cost path, and left-clicking confirms the move.

Open `Assets/Scenes/HexAndCounterPrototype.unity` and enter Play mode.

Controls:

- Hover any map hex to inspect its coordinates, latitude/longitude, elevation or depth, terrain class, and movement cost.
- Right click the counter to open its unit-order menu.
- Choose **Move** to show legal movement destinations.
- Hover a highlighted destination to preview its least-cost path.
- Left click a highlighted destination to confirm the move.
- Left click another hex to inspect its terrain and movement cost.
- Mouse wheel zooms.
- Middle-mouse drag or WASD pans.
- R resets the camera.

## Geography strategy

The prototype carries a full-Taiwan crop from the same Natural Earth coastline and NOAA ETOPO 2022 source pipeline used by Sea of Uncertainty so the games share geographic provenance and a related command-map palette. A dedicated two-sided cartographic terrain shader renders the complete board consistently from strategic and tactical camera angles. Land elevation uses subtle 250 m contour bands, water uses measured shelf/slope/abyss depth bands, and true land-water edges receive a two-stage wet-shore/coast highlight.

The current ETOPO bake is two arc-minutes (roughly 3–4 km sample spacing) and the coastline is simplified for an operational map. These resources drive presentation and broad terrain classes on the whole-island layer; they are not authoritative sources for tactical cover or LOS. Sea of Uncertainty engagements will later open localized 250 m tactical boards backed by higher-resolution DEM and vector data.

Regenerate the checked-in Taiwan resources with `tools/build-taiwan-coastline.cjs` and `tools/build-taiwan-elevation.ps1`.

See `Assets/Resources/Geography/NOTICE.md` for attribution.
