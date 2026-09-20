# Always Faithful — Phase I Paired-Pass TODO

Target window: approximately 4–6 weeks  
Phase goal: a short, understandable, and repeatable 2030 USMC attack-or-defend engagement on Taiwan that runs standalone and can complete a stub round trip with Sea of Uncertainty.

## Working rule for every development pass

Every normal pass must deliver all four items below. An urgent regression fix may be narrower, but the following pass returns to this balance.

- [ ] **Function:** add or deepen one player decision, rule, tool, or integration capability.
- [ ] **Presentation:** add an equally meaningful improvement to graphics, animation, sound, or information readability.
- [ ] **Proof:** add or extend deterministic/domain, interaction, and visual regression checks appropriate to the change.
- [ ] **Playable build:** rebuild the Windows player, inspect a current capture, update documentation, and push one focused commit.

Do not count invisible refactoring as the functional half unless it directly unlocks the paired playable feature. Do not count decoration as the presentation half unless it communicates game state or materially improves the battlefield.

## Phase I completion gate

A new player can launch a scenario, understand the terrain and objective, issue movement and fire orders, experience enemy activity and reaction fire, finish the battle, and understand the result without consulting a manual. The same battle can be launched from a versioned Sea of Uncertainty request and return one deterministic result.

Required slice:

- one localized Taiwan battlefield derived from a selected whole-island hex;
- one USMC rifle platoon, one attached support element, and at least two opposing units;
- movement, AP expenditure, terrain cost, LOS, spotting, direct fire, suppression/cohesion, and reaction fire;
- one attack objective and one defense setup;
- a short turn limit, victory conditions, event log, and after-action result;
- standalone launch plus file-based `BattleRequest`/`BattleResult` developer flow.

## Baseline already complete

- [x] Unity 6000.2 project and Windows build pipeline.
- [x] Full-Taiwan 64×104 operational board with Natural Earth coastline and NOAA ETOPO elevation/bathymetry.
- [x] Shared-mesh 6,656-cell rendering and overview/tactical camera zoom.
- [x] Neutral unit state and contextual `RMB unit → Move → LMB destination` workflow.
- [x] Deterministic reachable-area and least-cost path calculation.
- [x] Stable elevation-aware screen picking, route preview, and completed-movement regression.

## Paired implementation passes

Complete these in order unless a discovered dependency requires a documented reorder.

### Pass 1 — Taiwan navigation and terrain legibility

- [x] **Function:** add cursor latitude/longitude, hex coordinates, elevation, broad terrain, movement cost, and land/water inspection without selecting a unit.
- [x] **Presentation:** add subtle elevation contours, improved coastal treatment, clearer shallow/deep water bands, and restrained zoom-aware geographic labels for Taiwan, adjacent seas, and key cities.
- [x] **Proof:** validate coordinate round trips, land/water classification, elevation sampling, map-edge picking, movement behavior, and an overview reference frame.

### Pass 2 — Authoritative unit/AP state

- [x] **Function:** move the platoon, AP, readiness, and selection state out of presentation objects into serializable domain state; spend AP on movement and add End Turn.
- [x] **Presentation:** replace prototype text with a compact Broken Front-inspired unit card, AP pips, terrain readout, and clear selected/available/spent counter states.
- [x] **Proof:** test AP spending, illegal orders, turn reset, state/view agreement, and selection/menu transitions.

### Pass 3 — Strategic-to-tactical battlefield extraction

- [x] **Function:** select a Taiwan operational hex and generate/load a bounded local 250 m tactical board with stable geographic origin and parent-hex identity.
- [x] **Presentation:** add an overview-to-tactical camera transition, location briefing card, local relief, shoreline continuity, and a visible return-to-island control.
- [x] **Proof:** test deterministic extraction, coordinate containment, parent/local ID preservation, and repeated entry/exit without drift.

### Pass 4 — Tactical movement quality

- [x] **Function:** apply terrain and slope costs, impassable edges, occupancy, destination validation, cancellation, and movement event records.
- [x] **Presentation:** replace waypoint beads with a polished directional route ribbon, destination ghost, AP cost label, invalid-route feedback, and purposeful counter movement animation.
- [x] **Proof:** test cheapest paths, slope/terrain modifiers, occupied destinations, cancel/reissue behavior, and route visibility throughout animation.

### Pass 5 — LOS and observation tool

- [x] **Function:** implement deterministic hex LOS using elevation and blocking terrain, plus an inspect-only LOS command available before firing.
- [x] **Presentation:** draw clear/open, obscured, and blocked LOS segments; highlight intervening terrain and show a concise modifier breakdown.
- [x] **Proof:** cover ridge, reverse-slope, same-height, adjacent, maximum-range, and map-edge cases with fixed fixtures.

### Pass 6 — Spotting and fog of war

- [x] **Function:** add hidden, contact, identified, and currently observed states with deterministic observer checks.
- [x] **Presentation:** add terrain-aware fog shading, uncertain contact markers, reveal/loss transitions, and observer-source feedback.
- [x] **Proof:** test state transitions, stale contacts, save/reload visibility, and prohibition of attacks on illegal information states.

### Pass 7 — Direct fire

- [x] **Function:** add one deterministic small-arms fire action with range, terrain, LOS, target state, ammunition abstraction, and seeded outcome events.
- [x] **Presentation:** add target preview, expected-effect panel, fire line, restrained muzzle/impact effects, and readable hit/miss/suppression feedback.
- [x] **Proof:** test identical-seed replay, modifier accounting, illegal targets, ammunition expenditure, and event/view synchronization.

### Pass 8 — Suppression, cohesion, and recovery

- [x] **Function:** add Ready, Suppressed, Disrupted, and Reduced effects plus a Rally/Recover action and movement/fire restrictions.
- [x] **Presentation:** add counter badges, desaturation/pulse language, compact status tooltips, and recovery feedback without excessive screen effects.
- [x] **Proof:** test thresholds, cumulative effects, restrictions, recovery, and state persistence.

### Pass 9 — Reaction fire and interruption

- [x] **Function:** detect eligible movement exposure, pause movement, resolve one reaction shot, and deterministically resume, halt, or suppress the mover.
- [x] **Presentation:** add an interruption banner, reaction-source indication, paused route state, camera cue, and event timing that remains readable at faster speeds.
- [x] **Proof:** test eligibility arcs/range/LOS, multiple reactors with deterministic ordering, interrupted paths, and no duplicate reactions.

### Pass 10 — Enemy turn and scenario loop

- [x] **Function:** add a deterministic objective-aware opponent capable of movement, observation, fire, and recovery through the same legal-command interface as the player.
- [x] **Presentation:** add enemy-activity pacing, visible-action focus, hidden-action summaries, turn transition treatment, and optional fast animation.
- [x] **Proof:** run headless battles, reject illegal AI orders, verify fixed-seed replay, and enforce a maximum turn-processing time.

### Pass 11 — Tactical terrain richness: cover and built-up areas

- [x] **Function:** add a per-hex Light/Medium/Heavy cover attribute (probabilistic by terrain type) that is movement-cost-neutral but meaningfully reduces hit/suppression chance and adds LOS obscuration/obstruction, plus deterministic small/medium built-up clusters near shorelines.
- [x] **Presentation:** add procedural vegetation-clump and built-up structure props on the tactical map, a cover color tint, and cover/built-up reporting in hex inspection and fire/LOS breakdowns.
- [x] **Proof:** verify deterministic cover/built-up generation, shore-proximity enforcement for built-up clusters, and cover's effect on direct fire, reaction fire, and line of sight.

### Pass 12 — Objectives, victory, and after-action review

- [x] **Function:** add attack/defend setup, objective control, turn limit, losses, victory calculation, and a structured battle-event history.
- [x] **Presentation:** add objective markers, setup boundaries, turn/side banner, victory progress, final result screen, and an inspectable chronological action log.
- [x] **Proof:** test every victory branch, ties, timeout, objective ownership, casualty totals, and result reconstruction from events.

### Pass 13 — Recon and ISR tasking

- [x] **Function:** add a platoon-level recon order that tasks a focused sensor sweep on a hex up to 12 hexes away without requiring ground line of sight, granting a temporary one-tier detection bonus (Hidden→Contact→Identified→Observed) for a short duration; no new unit, deliberately distinct from the still-deferred ISR drone concept.
- [x] **Presentation:** add a distinct violet marker ring on the tasked hex, a RECON order-menu option and AP cost, hover/target feedback, and recon status/duration reporting in hex inspection and the chronological event log.
- [x] **Proof:** verify the detection-bonus state progression and its cap, exact-hex targeting, and an in-game order/decay round trip (AP spent, event recorded, marker and marker expiry after its duration).

### Pass 14 — Sea of Uncertainty round trip

- [x] **Function:** define versioned `BattleRequest` and `BattleResult` schemas (Phase I stub scope); import theater location, forces, posture, seed, and objective; atomically export outcome exactly once.
- [x] **Presentation:** add campaign handoff/loading treatment, strategic-context briefing, imported-force provenance, return-to-campaign confirmation, and graceful validation errors.
- [x] **Proof:** add schema fixtures, malformed/unsupported-version cases, deterministic request replay, ID preservation, atomic-write checks, and an automated launch/result round trip.

**Paired implementation passes are now complete.** Remaining Phase I work is the polish and release gate below.

## Phase I polish and release gate

- [ ] Profile overview and tactical maps on the target Windows hardware; establish frame-time, memory, load-time, and draw-call budgets.
- [ ] Add graphics presets, UI scaling, color-safe overlays, reduced motion, animation speed, and remappable essential controls.
- [x] Save and restore an in-progress standalone battle.
- [x] Ensure every important action has visible feedback and an event-log explanation.
- [x] Add randomized scenario generation for standalone play, so no two battles at a hex play the same.
- [x] Add new mission types (Raid, Reconnaissance-in-Force, Withdrawal) and a persistent standalone Battalion status/campaign layer.
- [x] Add an order-of-battle support-card resource pool (ISR, Fire Support, Reserve) with a pre-battle commitment modal (Pass A); an in-battle Call for Fire order spending the same pool is still to come (Pass B).
- [x] Give the PLA an active sensor-tasking capability (Recon order) and real turn-to-turn contact memory, add a persistent "enemy tracking you" readout for the player, and close the event-log/status gaps left in the support-card system (ISR/Reserve plays were previously invisible once committed).
- [ ] Run a clean-machine Windows build test and archive the exact executable plus test logs.
- [ ] Conduct at least three no-instruction playtests and record confusion, misclicks, and unreadable states.
- [ ] Resolve all blocking and high-severity findings before calling Phase I complete.

## Explicitly deferred beyond Phase I

- multiple tactical maps and a public map editor;
- full contemporary USMC and opposing-force rosters;
- vehicles, transport, aviation, naval gunfire, detailed logistics, EW, drones, smoke, engineering, and amphibious movement;
- multiplayer;
- authoritative 250 m terrain coverage for the entire island at once.

Campaign progression inside Always Faithful, previously listed here, is now implemented in a lightweight form for standalone play (persistent Battalion status carrying attrition across generated scenarios) — removed from this deferred list accordingly.

These are not rejected features. They are deferred so Phase I can prove the core decision loop, presentation language, deterministic architecture, and Sea of Uncertainty handoff first.
