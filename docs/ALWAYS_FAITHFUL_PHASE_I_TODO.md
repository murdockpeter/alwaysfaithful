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

**Baseline and all 14 paired implementation passes are complete** (Unity project through the Sea of Uncertainty round-trip stub) — see git history for what each pass delivered. Remaining Phase I work is the polish and release gate below.

## Phase I polish and release gate

- [ ] Profile overview and tactical maps on the target Windows hardware; establish frame-time, memory, load-time, and draw-call budgets.
- [x] Settings/accessibility pass B (graphics presets, color-safe overlays, reduced motion, animation speed) — see [`GRAPHICAL_ENHANCEMENTS_TODO.md`](GRAPHICAL_ENHANCEMENTS_TODO.md) Group A.
- [x] Save and restore an in-progress standalone battle.
- [x] Ensure every important action has visible feedback and an event-log explanation.
- [x] Add randomized scenario generation for standalone play, so no two battles at a hex play the same.
- [x] Add new mission types (Raid, Reconnaissance-in-Force, Withdrawal) and a persistent standalone Battalion status/campaign layer.
- [x] Add an order-of-battle support-card resource pool (ISR, Fire Support, Reserve) with a pre-battle commitment modal (Pass A); an in-battle Call for Fire order spending the same pool is still to come (Pass B).
- [x] Give the PLA an active sensor-tasking capability (Recon order) and real turn-to-turn contact memory, add a persistent "enemy tracking you" readout for the player, and close the event-log/status gaps left in the support-card system (ISR/Reserve plays were previously invisible once committed).
- [x] Replace the disconnected overview sandbox with a full first-launch operational scenario: two tracked USMC battalions versus three tracked PLA battalions, a complete operations-order/OOB briefing, persistent both-side position and strength, maneuver-level fog/contact memory, active operational Recon, hidden PLA movement, and a selected-parent-battalion handoff into tactical resolution.
- [x] Fix: entering a tactical hex no longer manufactures an enemy encounter on every "OPEN LOCAL MAP" click regardless of the operational layer's fog-of-war state. A "RESOLVE CONTACT" click (a hex with a known operational contact) still rolls the usual hash-driven roster; an ordinary local-map entry with no detected contact now resolves as an enemy-free battle instead. Proven by a dedicated `--no-contact-regression` check alongside the full regression sweep.
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

## Standalone Package 1 — Company Command

- [x] Normalize fresh standalone tactical counters to platoon scale: three USMC rifle platoons plus one attachment versus four to six PLANMC rifle/weapons platoons.
- [x] Add pre-battle force organization with Balanced (Weapons), Recon, and Assault (Combat Engineer) company presets.
- [x] Add a bounded three-hex deployment phase with direct platoon selection and click-to-place repositioning.
- [x] Add direct platoon selection, **N** next-unit, **Tab** next-actionable-unit, and a live clickable company list.
- [x] Generalize movement, fire, rally, recon, rendering, objective control, defeat checks, event naming, and turn refresh around the active platoon and the complete friendly force.
- [x] Combine friendly observation into a company sensor picture and make the PLA select the best detected friendly target while advancing on the actual scenario objective.
- [x] Persist the full friendly roster, weapons, active selection, and company preset in tactical-save schema 3 while retaining schema-2 single-platoon migration.
- [x] Extend standalone battles to 8–12 turns for the larger force and retain the legacy `BattleRequest` roster/contract unchanged.
- [x] Add a dedicated `--company-regression` and pass the existing tactical regression suite against the company model.

## Standalone Package 2 — Fire and Maneuver

- [x] Add Quick, Tactical, and Bounding platoon movement postures with scaled route reach, AP cost, fire penalties, and reaction-reserve consequences.
- [x] Add a two-point reaction economy with Weapons Hold, Return Fire, and Weapons Free policies for both sides.
- [x] Add six broad facing sectors and forward/flank/rear reaction modifiers appropriate to 250 m platoon hexes.
- [x] Add area suppression against terrain hexes with explicit AP/ammunition cost and deterministic effects.
- [x] Add organic smoke with 500 m placement range, two-turn persistence, a map marker, and LOS obscuration.
- [x] Add adjacent close assault resolved from attacker/defender suppression and target cover, including advance/displacement on success.
- [x] Persist Package 2 unit state and smoke/suppression/assault event history in battlefield schema 14 and surface it in the event log.
- [x] Add a dedicated `--fire-maneuver-regression` covering the linked posture, facing, reaction, smoke, suppression, assault, turn-reset, and serialization rules.

## Standalone Package 3 — Positions and Combat Power

- [x] Separate persistent 0–100 platoon strength and supply from recoverable suppression/cohesion.
- [x] Add two-level fighting positions whose protection is lost on movement.
- [x] Add Hasty, Standard, Deliberate, and Rapid direct-fire profiles with distinct AP, ammunition, accuracy, suppression, and damage values.
- [x] Add limited in-battle company fire missions.
- [x] Make the Reserve support card deploy a real fifth rifle platoon.
- [x] Add Weapons-Platoon resupply and supply-consuming reorganization.
- [x] Persist strength, positions, logistics, and fire missions in battlefield schema 15 and cover their rules with `--combat-power-regression`.

## Standalone Package 4 — Standalone Longevity

- [x] Add a Quick Battle configurator for mission, attachment, difficulty, weather, visibility, turn limit, and tutorial mode.
- [x] Make weather and light affect detection and fire effectiveness; scale enemy combat power with difficulty.
- [x] Add named-platoon career carryover with battles, victories, experience, strength recovery, and veteran AP progression.
- [x] Add mission-aware secondary objectives.
- [x] Expand the AAR with remaining combat power, ammunition expenditure, fire missions, highlights, and secondary-objective completion.
- [x] Add JSON-driven quick-battle definitions through `--quick-battle=<path>` as the scenario/editor data foundation.
- [x] Add an optional six-step interactive tutorial overlay tied to real player actions.
- [x] Persist Package 4 state in battlefield schema 16 and add `--longevity-regression`.
- [ ] A graphical scenario editor remains a later authoring-tool project; the runtime schema and loader are now in place.

These are not rejected features. They are deferred so Phase I can prove the core decision loop, presentation language, deterministic architecture, and Sea of Uncertainty handoff first.
