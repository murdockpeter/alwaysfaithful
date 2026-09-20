# Always Faithful — Graphical Enhancements TODO

Status: active near-term list  
Scope: presentation-layer work only — HUD, symbology, feedback, and accessibility/animation polish for the existing tactical and operational maps. This is not a replacement for [`ALWAYS_FAITHFUL_PHASE_I_TODO.md`](ALWAYS_FAITHFUL_PHASE_I_TODO.md) (rules/function work and the release gate) or [`USMC_Tactical_Battle_Game_TODO.md`](USMC_Tactical_Battle_Game_TODO.md) (long-range product design) — it pulls the graphics-specific items already called for in those documents into one actionable list so presentation work doesn't get lost between them.

## Group A — Settings pass B (accessibility & display)

Second half of the Settings panel; Pass A (persistence, UI scale, remappable Cancel/Reset Camera keys) already shipped.

- [ ] Graphics presets (e.g., low/medium/high visual fidelity tiers).
- [ ] Color-safe (colorblind-friendly) overlay palette option.
- [ ] Reduced-motion / reduced-screen-shake mode.
- [ ] Adjustable animation speed, with a fast/skip setting for enemy-turn and movement animation.

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
