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
- [ ] Settings/accessibility pass B (graphics presets, color-safe overlays, reduced motion, animation speed) — tracked in [`GRAPHICAL_ENHANCEMENTS_TODO.md`](GRAPHICAL_ENHANCEMENTS_TODO.md); Pass A (persistence, UI scaling, remappable keys) already shipped.
- [x] Save and restore an in-progress standalone battle.
- [x] Ensure every important action has visible feedback and an event-log explanation.
- [x] Add randomized scenario generation for standalone play, so no two battles at a hex play the same.
- [x] Add new mission types (Raid, Reconnaissance-in-Force, Withdrawal) and a persistent standalone Battalion status/campaign layer.
- [x] Add an order-of-battle support-card resource pool (ISR, Fire Support, Reserve) with a pre-battle commitment modal (Pass A); an in-battle Call for Fire order spending the same pool is still to come (Pass B).
- [x] Give the PLA an active sensor-tasking capability (Recon order) and real turn-to-turn contact memory, add a persistent "enemy tracking you" readout for the player, and close the event-log/status gaps left in the support-card system (ISR/Reserve plays were previously invisible once committed).
- [x] Replace the disconnected overview sandbox with a full first-launch operational scenario: two tracked USMC battalions versus three tracked PLA battalions, a complete operations-order/OOB briefing, persistent both-side position and strength, maneuver-level fog/contact memory, active operational Recon, hidden PLA movement, and a selected-parent-battalion handoff into tactical resolution.
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
