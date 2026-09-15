# Always Faithful

Standalone 2030 near-future USMC tactical game and tactical-resolution module for Sea of Uncertainty.

## Current prototype

The prototype now presents the complete island of Taiwan as a 64×104 whole-island operational board (6,656 hexes at roughly 4 km spacing), using the same Natural Earth coastline and NOAA ETOPO 2022 pipeline as Sea of Uncertainty. One selectable USMC rifle-platoon counter exercises the order workflow. The platoon begins unselected. Opening its order menu and choosing Move reveals the area reachable with its remaining action points; hovering a legal destination previews the deterministic least-cost path, and left-clicking confirms the move. Terrain-weighted movement spends AP, the counter visibly enters a spent state at zero AP, and End Turn restores its allowance.

Platoon position, AP, readiness, and selection now live in serializable gameplay state rather than in the counter renderer. The compact command card presents the current turn, unit state, occupied terrain, and four readable AP pips at every supported window height.

Any selected land hex can now open a deterministic 25×19 local battlefield. Its 475 hexes use 250 m geographic spacing and retain a versioned battlefield ID, parent operational hex, center/origin coordinates, bounds, terrain, and elevation samples. A short command-table transition leads to a local briefing card, amplified local relief, continuous coastal classification and shoreline accents, local hex inspection, bounded camera navigation, and a Return to Island control that restores the exact prior overview camera.

The USMC rifle platoon deploys onto each local map with eight tactical AP. Local movement combines destination terrain and elevation-change costs, rejects water, slopes over 90 m per edge, occupied cells, out-of-bounds destinations, and unaffordable routes, and persists completed, rejected, and cancelled orders as serializable movement events. A continuous teal-to-gold route ribbon, destination ghost, reachable-area wash, live AP label, invalid red state, and terrain-following counter animation keep every order legible.

The tactical order menu also provides an inspect-only line-of-sight tool with a 12-hex (3 km) limit. It traces a deterministic hex line between observer and target, compares the interpolated sightline against measured elevation and rough-terrain obstruction, and reports clear, obscured, blocked, out-of-range, and map-edge results without committing an attack. Teal, amber, and red line segments and intervening-hex washes make the visibility transition readable directly on the terrain, while the inspection card lists the range and decisive modifiers.

Open `Assets/Scenes/HexAndCounterPrototype.unity` and enter Play mode.

Controls:

- Hover any map hex to inspect its coordinates, latitude/longitude, elevation or depth, terrain class, and movement cost.
- Right click the counter to open its unit-order menu.
- Choose **Move** to show legal movement destinations.
- Hover a highlighted destination to preview its least-cost path.
- Left click a highlighted destination to confirm the move.
- Left click another hex to inspect its terrain and movement cost.
- Select a land hex and choose **Open 250 m Map** to enter its local tactical battlefield.
- On the tactical map, right click the platoon and choose **Move**; hover a destination and left click to confirm.
- On the tactical map, right click the platoon and choose **Inspect LOS**, then hover hexes to inspect visibility.
- Right click away from the unit or press **Escape** to cancel tactical movement or LOS inspection.
- Use **Return to Island** to restore the operational map and its prior camera position.
- Use **End Turn** on the unit card to advance the turn and restore the platoon's AP.
- Mouse wheel zooms.
- Middle-mouse drag or WASD pans.
- R resets the camera.

## Geography strategy

The prototype carries a full-Taiwan crop from the same Natural Earth coastline and NOAA ETOPO 2022 source pipeline used by Sea of Uncertainty so the games share geographic provenance and a related command-map palette. A dedicated two-sided cartographic terrain shader renders the complete board consistently from strategic and tactical camera angles. Land elevation uses subtle 250 m contour bands, water uses measured shelf/slope/abyss depth bands, and true land-water edges receive a two-stage wet-shore/coast highlight.

The current ETOPO bake is two arc-minutes (roughly 3–4 km sample spacing) and the coastline is simplified for an operational map. The 250 m local grids presently resample and visually amplify these shared sources, which preserves geographic identity and shoreline continuity but does not invent tactical-detail accuracy. Higher-resolution DEM and vector data will later replace those samples for tactical cover and LOS.

Regenerate the checked-in Taiwan resources with `tools/build-taiwan-coastline.cjs` and `tools/build-taiwan-elevation.ps1`.

See `Assets/Resources/Geography/NOTICE.md` for attribution.
