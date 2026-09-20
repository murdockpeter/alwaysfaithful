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
- [ ] Complete the selected-unit preview with posture, LOS, visible threats, and predicted exposure (reachable highlights and hover-driven path preview already exist).
- [ ] Surface suppression, degradation, dug-in, fired/moved, passenger, ammunition-concern, and reaction-eligibility state without requiring a panel to be opened.
- [ ] Add next-unit, next-actionable-unit, and objective-focus navigation, plus toggleable LOS and threat overlays and map labels/pins.

## Group C — Symbology and skins

- [ ] Add an illustrated-counter and NATO/MIL-STD-symbol option, switchable as an interchangeable presentation skin over the same underlying units.

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
