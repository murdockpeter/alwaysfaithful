# Always Faithful — Graphical Enhancements TODO

Status: active near-term list  
Scope: presentation-layer work only — HUD, symbology, feedback, and accessibility/animation polish for the existing tactical and operational maps. This is not a replacement for [`ALWAYS_FAITHFUL_PHASE_I_TODO.md`](ALWAYS_FAITHFUL_PHASE_I_TODO.md) (rules/function work and the release gate) — it collects the graphics-specific items called for there into one actionable list so presentation work doesn't get lost.

## Group A — Settings pass B (accessibility & display)

Second half of the Settings panel; Pass A (persistence, UI scale, remappable Cancel/Reset Camera keys) already shipped. **Complete.**

- [x] Graphics presets (Low/Medium/High): scales cover/built-up prop density and terrain contour shading strength.
- [x] Color-safe (colorblind-friendly) overlay palette option: swaps unit-status, spotted-tier, and LOS colors from a red/orange/yellow ramp to a blue/orange/magenta one.
- [x] Reduced-motion mode: removes the movement bob and snaps the reaction-fire camera pan instead of easing it.
- [x] Adjustable animation speed (Normal/Fast/Skip): scales movement steps, fire-line animation, and reaction-fire camera pans; also feeds the existing enemy-turn pacing.

## Group B — HUD and information density

- [ ] Restrained map-first HUD: selected-unit panel, turn/objective strip, contextual action bar, collapsible event log.
  - [x] Live, collapsible event log panel ("LOG" button) reusing the same chronological log the after-action screen builds — previously only visible once a battle had already ended.
  - [x] Contextual action bar already existed (the per-unit order popup); extended with the LOS-overlay toggle below rather than rebuilt.
  - [ ] The selected-unit panel and turn/objective strip are still the original always-on command card, not a restrained redesign — no layout change attempted this pass.
- [ ] Complete the selected-unit preview with posture, LOS, visible threats, and predicted exposure (reachable highlights and hover-driven path preview already exist).
  - Untouched this pass — "posture" isn't an implemented mechanic yet (see the retired product TODO's still-open movement-posture decision), and "predicted exposure" needs a real danger-zone computation, not just a badge.
- [x] Surface suppression, degradation, fired/moved, ammunition-concern, and reaction-eligibility state without requiring a panel to be opened.
  - Suppression/degradation (combat status) and fired/moved (Readiness) were already always-visible on the command card. New this pass: the AMMO badge now colors orange/red when low/empty, and a "REACTION FIRE • READY/UNAVAILABLE" badge reflects the same eligibility gate `TacticalReactionFire` itself checks.
  - "Dug-in" and "passenger" states don't exist as mechanics yet (no entrenchment or embark/transport system) — nothing to surface until those land.
- [ ] Add next-unit, next-actionable-unit, and objective-focus navigation, plus toggleable LOS and threat overlays and map labels/pins.
  - [x] Objective-focus: a "FOCUS OBJECTIVE" button snaps the camera to the objective hex.
  - [x] Toggleable LOS overlay: a tactical-menu button tints every in-range hex Clear/Obscured/Blocked from the selected unit's current position, kept mutually exclusive with the pre-existing single-target INSPECT LOS tool since both drive the same per-cell tint.
  - Next-unit/next-actionable-unit navigation is not meaningful yet at Phase I's one-controllable-unit-per-side scale; a threat overlay and map labels/pins remain undone.

## Group C — Symbology and skins

- [x] Add an illustrated-counter and NATO/MIL-STD-symbol option, switchable as an interchangeable presentation skin over the same underlying units. **Complete.**
  - [x] Counters, cover clumps, and buildings now render with real directional shading (new `MapSolid` shader) instead of completely flat/unlit color, so spheres and cubes read as actual 3D shapes rather than flat blobs — confirmed by side-by-side capture at close tactical zoom.
  - [x] New `CounterSkinTier` setting (Illustrated/Symbol) in the Settings panel, persisted like every other display setting. Symbol is a `NatoSymbolView` alternate skin — a doctrinal blue-friendly/red-hostile frame with an infantry-cross or support-dot icon — switchable live for the player's platoon and every PLA contact marker without losing detection-tier or combat-status coloring. Confirmed by side-by-side capture (player counter blue-framed with cross, Identified PLA contact red-framed with cross, both labeled).
  - [x] PLA rifle squads vs. support teams are now visually distinct in both skins: the illustrated model gets a protruding barrel prop over its center element, the symbol skin swaps the infantry cross for a solid dot — derived from the same "SUPPORT" role string in both places. Now confirmed by capture (a later, unrelated camera-rotation test happened to catch an Identified "PLA SUPPORT" contact showing the dot icon correctly).
  - Not full MIL-STD-2525 fidelity: no size/echelon glyph, no mobility/equipment modifiers — a simplified stand-in, documented as such in `NatoSymbolView`.
  - [x] Follow-up: the Symbol skin now billboards — a fixed tilt (`Quaternion.FromToRotation(Vector3.up, cameraOffsetDirection)`, since the tactical camera's viewing angle never orbits, only pans/zooms) carries the whole frame/icon/label/badge group to face the camera squarely, like a real paper counter, instead of lying flat on the hex as a foreshortened decal. A small separate ground-anchor dot stays flat so hex occupancy is still obvious. Confirmed by capture: the frame now reads as an undistorted square.

## Group G — Miniature counter skin (real sculpted figures)

Added after the user asked for USMC/PLANMC counters closer to painted tabletop-miniature quality (Axis & Allies-style) than either existing skin could reach with primitives alone. This is this codebase's first use of imported 3D assets, a deliberate, explicitly-approved exception to the "no imported assets, flat-colored primitives" idiom every other piece in this game follows.

- [x] Third `CounterSkinTier` option, **Miniature**: sculpted USMC/PLANMC OBJ figures under `Assets/Resources/Models/OneStar` (USMC Rifleman, PLANMC Rifleman, PLANMC Mortar Team), reused from the sibling `down_range_campaign` project's "OneStar" Down Range miniature pack. Lit by the scene's existing directional lights via Unity's Standard shader — the one piece in this game that isn't flat/unlit — with glossiness/metallic zeroed out for a matte painted-miniature look, and a brightness tint since the default response read too dark against this scene's lights.
- [x] **License: CC BY-NC-SA 4.0** (© Nicholas Royer / Down Range, sourced from Printables.com prints) — NonCommercial and ShareAlike, both confirmed acceptable for this project. Full terms and attribution in `Assets/Resources/Models/OneStar/NOTICE.md` and the retained `LICENSE-Down-Range-Models.txt`. **If Always Faithful ever moves toward a commercial release, these specific files need to be replaced or separately licensed first.**
- [x] Wired through the same `ApplySkin`/`ApplyCounterSkin` machinery Group C already built: a third `MiniatureCounterView` sibling per counter (player platoon and every PLA contact marker), toggled the same way as Illustrated/Symbol. The player's counter self-drives via a `BindReactive` reactive Update() (same pattern as `NatoSymbolView`); PLA contacts go through explicit `ContactMarkerView` calls. Combat status reads as a darkening tint over the painted texture rather than a re-tint into a tier palette, to keep the miniature's own paint scheme legible.
- [x] Model scale and material settings tuned empirically against captures — the native models are ~2 world units tall (a standing human figure); the first attempt (one figure at 0.62x) read as a small standing figure but too large/lone for a squad-level counter.
- [x] Follow-up: rifle-role figures now render as a small cluster of four (0.38x each, scattered/yaw-jittered like `TacticalFormationView`'s own three-element wedge) instead of one dominant soldier — reads as a squad, not a clone-stamped giant, and reuses the single rifleman sculpt the way a real tabletop stand mounts several identical minis. The support-role Mortar Team keeps its single instance since that sculpt is already a multi-figure crew. Confirmed by capture.
- Not done: PLANMC Mortar Team's distinct silhouette wasn't screenshot-verified (same gap as Group C's illustrated/symbol barrel-prop distinction); the cluster figures' facing direction is an untested guess, not checked against movement direction; USMC Officer/Corpsman/M249 Gunner/MAAWS Gunner/EW Operator and the PLANMC/USMC vehicles and drones already sitting in the source pack are not yet imported — only the three roles Always Faithful currently fields.

## Group H — Free camera (pan, zoom, yaw orbit, pitch tilt)

Added after the user asked for six-degrees-of-freedom camera control. Scoped down to pan + zoom + yaw + pitch (no roll, so hexes/labels/counters never tip sideways), with pitch clamped to stay off the horizon and short of straight-down — both confirmed with the user first, since a truly unrestricted free-fly camera was the other option on the table.

- [x] Both maps now share one spherical direction formula (`CameraDirectionFromYawPitch`) instead of each having a different fixed/zoom-curved offset. Operational mode's old "auto-tilt steeper as you zoom out" curve is gone, replaced by the same manual pitch control tactical mode now also has — a deliberate behavior change, not an oversight.
- [x] **Q/E** orbits yaw; **Page Up/Down** tilts pitch (clamped 15°-85°). Chosen specifically to avoid every existing binding — not RMB-drag (orders), not MMB-drag (pan) — in both modes. On-screen control hints and the Reset Camera key updated to match (reset now also zeroes yaw/pitch back to each mode's original default angle).
- [x] Pan is now yaw-relative (rotated by the current `cameraYaw` before being applied) so WASD/middle-drag still feels like screen-relative up/down/left/right once the camera has been rotated, instead of always sliding along absolute world north/south.
- [x] Fixed a real dependency this surfaced: the NATO Symbol skin's billboard tilt was a one-time-computed constant assuming the camera's angle never changed. It's now `NatoSymbolView.FaceCamera()`, recomputed every frame against a shared `ActiveCamera` reference — and, while touching this, also fixed it to use world rotation rather than local rotation, since the player's own counter root already gets rotated to face its last movement direction and the card must face the camera regardless of that.
- [x] Confirmed by capture at a rotated yaw/pitch (90°/45°): the map genuinely renders from the new angle, and the NATO frame still reads as an undistorted square rather than breaking now that it faces the camera dynamically.
- Not done: no on-screen visual indicator of current yaw/pitch (e.g., a compass); the illustrated skin's flat "lying on the ground" labels/plates were left as-is (correctly) rather than also billboarded — they're meant to be a physical chip viewed from whatever angle, not a face-on card.

## Group F — Terrain fidelity at close zoom

Added after a close-zoom (low camera distance) capture showed the tactical ground as one perfectly flat, unlit color per hex with near-black hex-wall edges.

- [x] Procedural per-hex grain in `MapTerrain.shader` (world-position hash noise, no texture asset) so the ground isn't a single flat swatch up close; verified it still reads as plain flat color at whole-island overview scale.
- [x] Softened the hex side-wall shading floor so raised-hex edges read as a subtle rim rather than a near-black outline.
- Not attempted: reshaping the vegetation-clump/building geometry itself (the new lighting alone made them read as real 3D bumps rather than flat smears, which was the main problem) or blending color across adjacent hex edges.

## Group D — Combat preview and feedback

- [ ] On target hover, show weapon choice, legality, range band, expected-outcome band, and an expandable modifier breakdown.
- [ ] Use animation to communicate results without delaying input longer than needed (ties to Group A's animation-speed setting).

## Group E — Accessibility polish

- [ ] Subtitle support for any voiced or audio-cued feedback.
- [ ] Keyboard navigation for primary flows (selection, orders, end turn, menus).

## Working notes

- Treat each group as independently shippable — a group doesn't need its neighbors finished to land.
- When a group closes out a checkbox that's also tracked in the Phase I release gate (Group A), check it off there too so the two lists don't drift.
- Pull any newly discovered graphics/UI item here first; only promote it to the Phase I release gate if it turns out to be release-blocking.
