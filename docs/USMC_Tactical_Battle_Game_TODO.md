# Always Faithful — Product and Implementation TODO

Status: planning baseline  
Target: **Always Faithful**, a standalone Unity USMC tactical game that can also be launched by, receive battle context from, and return results to **Sea of Uncertainty**.

## 1. Product direction

Build **Always Faithful**, an original, turn-based US Marine Corps tactical game with:

- the breadth and readability of Battle Academy 2's tactical systems;
- the compact, low-friction interface sensibility of Broken Front;
- original rules tuning, terminology, scenarios, UI, art, audio, data, and code;
- a versioned integration boundary so Sea of Uncertainty does not depend on tactical-game internals.

Setting: **2030 near-future USMC littoral operations**. Force composition should be a plausible evolution of publicly documented systems, organizations, and concepts rather than speculative science fiction. The opposing force and the treatment of real versus fictional equipment remain design decisions.

## 2. Design pillars

- [ ] **Readable combined arms:** a player should understand movement, LOS, likely fire effects, and unit state before committing an action.
- [ ] **Position before attrition:** terrain, concealment, observation, facing, suppression, and mutual support should matter more than simply concentrating hit points.
- [ ] **Marine task organization:** model task-organized infantry, reconnaissance, fires, aviation, logistics, engineers, and littoral capabilities without turning the game into a staff simulator.
- [ ] **Fast turns, consequential choices:** keep selection and orders contextual; reserve detailed calculations for expandable tooltips and the combat log.
- [ ] **Scenario-first architecture:** all units, terrain, objectives, missions, reinforcements, and support assets should be data-driven.
- [ ] **Standalone first, integration-ready always:** every battle must run without Sea of Uncertainty, while any valid strategic battle package can launch the same simulation.
- [ ] **Deterministic and auditable:** seeded resolution and a replayable event log must make integration bugs and campaign outcomes reproducible.

## 3. Source-game translation

| Area | Retain from BA2 functionality | Apply Broken Front sensibility | USMC adaptation |
|---|---|---|---|
| Map | tiled battlefield, terrain, elevation, weather, LOS/fog | compact board, strong hex/state overlays, direct inspection | littoral terrain, urban zones, beaches, waterways, landing zones, key maritime terrain |
| Movement | multiple movement postures, facing, transport/load/unload | clear reachable-area and AP-cost preview | tactical, rapid, and cautious movement; embark/disembark; amphibious and rotary-wing mobility |
| Combat | AP/HE distinction, range effects, suppression, reaction fire, assaults, bombardment | explicit state badges and inspectable modifier breakdown | anti-armor, small arms, precision fires, loitering/aviation effects, modern sensors and countermeasures |
| Information | concealment, spotting, ambush, uncertain occupied terrain | LOS tool and darkened unseen space | sensor-source tracks, identification quality, emissions, drones, reconnaissance |
| Unit state | morale, experience, ammunition, damage and mobility effects | compact counter values and Disrupted/Reduced-style legibility | cohesion/suppression, casualties, readiness, ammunition, fuel where operationally useful |
| Support | artillery, air, smoke, resupply, rally-style assets | contextual action controls | mortars, artillery, naval surface fire, aviation, UAS, EW, smoke, casualty evacuation/resupply |
| Scenarios | briefings, force choice, objectives, reinforcements, editor, campaigns | simple setup flow and validation | commander's intent, tasks, ROE, collateral/civilian constraints where appropriate |
| Presentation | informative map tooltips and detailed unit panels | uncluttered counter-first UI, NATO-symbol option, concise controls | Marine symbology and plain-language labels; optional doctrinal detail |

This table is a design reference, not a requirement to reproduce either game's exact formulas, content, screen layouts, names, or assets.

## 4. Decisions to lock before production

- [x] Set the time period and tone to **2030 near future**, grounded in plausible USMC force composition.
- [ ] Define the initial opposing force and whether it is fictionalized.
- [ ] Pick the tactical scale after prototyping:
  - recommended first test: hexes at roughly 200–300 meters with platoon/detachment units;
  - alternate test: smaller cells and squad/single-vehicle units for BA2-like granularity.
- [ ] Test one-unit versus two-unit friendly stacking; adopt stacking only if selection and targeting remain unambiguous.
- [ ] Decide alternating side-turns versus alternating formation activations. Prototype both; favor the model that best supports reaction fire and short turns.
- [ ] Define battle duration: abstract turns, clock time, or both.
- [ ] Decide how much logistics appears tactically: ammunition by weapon, abstract supply state, or support actions only.
- [ ] Define the initial platform targets and input methods.
- [ ] Confirm whether standalone campaigns are in the first release or whether Sea of Uncertainty provides the initial campaign layer.
- [x] Adopt **Always Faithful** as the player-facing title.
- [ ] Create an original visual identity, logo, and store presentation for Always Faithful.

## 5. Unity and repository foundation

- [ ] Create a separate Unity project/repository for the tactical game.
- [ ] Pin a supported Unity LTS editor version and commit the exact version in `ProjectVersion.txt`.
- [ ] Establish assemblies with one-way dependencies:
  - `Tactical.Domain`: pure C# rules and data contracts, no Unity scene dependencies;
  - `Tactical.Application`: commands, turn flow, AI orchestration, saves, and battle import/export;
  - `Tactical.Unity`: presentation, input, animation, audio, and platform services;
  - `Tactical.Editor`: authoring and validation tools;
  - `Tactical.Tests`: edit-mode/domain tests and play-mode smoke tests.
- [ ] Keep authoritative battle state in plain serializable models, not MonoBehaviours or scene objects.
- [ ] Create stable IDs for units, formations, equipment, maps, objectives, factions, and scenarios.
- [ ] Add a seeded random service; prohibit direct random-number calls in combat rules.
- [ ] Add structured logging and a deterministic battle-event stream.
- [ ] Establish save migration and external-contract migration policies before the first playable build.
- [ ] Add CI for compilation, edit-mode tests, play-mode smoke tests, content validation, and headless battle simulations.

## 6. Sea of Uncertainty integration contract

### Boundary

- [ ] Treat Always Faithful as a separately runnable application.
- [ ] Support three entry modes:
  - standalone menu and local scenarios;
  - launch with a path to an imported battle package;
  - developer/test launch with fixed scenario ID and random seed.
- [ ] Use files as the first integration transport because they are debuggable and tolerant of either game closing; add IPC only if user experience later requires it.
- [ ] Write results atomically to a caller-provided output location, then exit or offer return-to-campaign.
- [ ] Never allow either game to read the other's private save format directly.

### Versioned battle input

- [ ] Define and JSON-schema validate a `BattleRequest` containing at minimum:
  - contract version, request ID, campaign ID, and deterministic seed;
  - tactical map/scenario template ID and strategic location/context;
  - date/time, weather, visibility, sea state if relevant, and battle duration;
  - attacker/defender and player-controlled side;
  - formations, unit identities, personnel/readiness, equipment, ammunition/supply, experience, attachments, and prior damage;
  - available support assets and any arrival windows;
  - objectives, commander's intent, withdrawal rules, and campaign-level constraints;
  - output path and a return-launch token that contains no secrets.
- [ ] Define fallbacks for strategic units that have no tactical representation.
- [ ] Reject invalid or unsupported requests with a machine-readable error and a player-readable explanation.
- [ ] Preserve imported strategic IDs throughout the tactical battle.

### Versioned battle output

- [ ] Define and JSON-schema validate a `BattleResult` containing at minimum:
  - matching request/campaign IDs, contract version, tactical build version, and seed;
  - terminal reason and victory tier;
  - objective ownership and score breakdown;
  - per-unit survivors, casualties, equipment loss/damage, ammunition/supply expenditure, cohesion/readiness, and experience change;
  - destroyed/captured/abandoned/recovered equipment distinctions;
  - support assets consumed or damaged;
  - battle duration and end positions summarized at an agreed strategic resolution;
  - notable events and optional replay/event-log path;
  - integrity hash and completion timestamp.
- [ ] Make result application idempotent in Sea of Uncertainty: applying the same request ID twice must not duplicate losses or rewards.
- [ ] Specify cancellation, retreat, crash recovery, corrupt-result, and incompatible-version behavior.

### Integration test harness

- [ ] Build a small launcher stub that generates requests, starts the tactical build, and reads results.
- [ ] Add golden request/result fixtures to both projects.
- [ ] Add round-trip tests for pristine units, damaged units, attachments, reinforcements, support use, retreat, annihilation, draw, cancellation, and version mismatch.
- [ ] Add an integration debug screen that displays the exact imported inputs and proposed exported changes.

## 7. Core simulation TODO

### Board, terrain, and visibility

- [x] Implement prototype hex coordinates, distance, deterministic neighbors, terrain movement costs, reachable-area calculation, least-cost paths, preview elevation, and board bounds.
- [ ] Implement terrain data with movement cost, cover, concealment, LOS blocking, elevation interaction, and unit-class restrictions.
- [ ] Implement roads, bridges/crossings, shoreline, shallow/deep water, beach, open ground, vegetation, urban, fortified, and damaged terrain.
- [ ] Implement weather, illumination, smoke, and transient terrain effects.
- [ ] Implement friendly visibility, last-known contacts, detection/identification states, and fog of war.
- [ ] Implement an LOS inspection tool whose preview uses exactly the same rules as combat validation.

### Turn and order system

- [ ] Implement the chosen turn/activation sequence as an explicit state machine.
- [x] Prototype a four-AP movement budget with open, rough, highland, and impassable-water costs.
- [ ] Implement contextual legal-action generation so the UI cannot offer illegal commands.
- [ ] Implement movement postures: cautious/hunt, normal/tactical, and rapid/dash.
- [ ] Implement facing and facing-dependent observation/reaction where it adds tactical value.
- [ ] Implement hold fire, target priorities, and selectable reaction-fire policy.
- [ ] Implement opportunity/reaction fire with a clear interrupt/resolve flow.
- [ ] Implement embark, transport, disembark, and passenger consequences.
- [ ] Implement assault/close combat and retreat/displacement rules.
- [ ] Implement digging in, prepared positions, obstacles, breaching, and route clearance.

### Direct and indirect combat

- [ ] Separate hit, effect/penetration, suppression, and damage resolution so the UI can explain each stage.
- [ ] Model target classes and appropriate ammunition/effect types without exposing needless ammunition micromanagement.
- [ ] Implement range bands, movement penalties, facing/aspect, terrain, smoke, posture, experience, and sensor-quality modifiers.
- [ ] Implement area/suppression fire against suspected positions.
- [ ] Implement mortars and artillery with spotters, delay, dispersion, ammunition, smoke, and cooldown/reload.
- [ ] Implement aviation/UAS/naval fires through the same support-mission framework rather than bespoke buttons.
- [ ] Implement collateral/restricted-fire rules as optional scenario data.
- [ ] Implement combat previews that show expected effect bands and an expandable modifier list; avoid deceptive false precision.

### Unit state and command effects

- [ ] Define a small, readable state model: effective, suppressed/disrupted, degraded, routed/withdrawn, destroyed/captured.
- [ ] Track casualties and equipment damage separately from temporary cohesion effects.
- [ ] Implement rally/recovery and leadership influence.
- [ ] Implement experience/training effects with bounded bonuses.
- [ ] Implement ammunition/readiness and resupply at the level selected in the logistics decision.
- [ ] Implement sensors, electronic warfare, communications degradation, and command radius only after the conventional loop is fun.

### Objectives and battle ending

- [ ] Implement occupy/control, hold, seize, destroy, protect, reconnoiter, extract, withdraw, and survive objective types.
- [ ] Support primary, secondary, and optional objectives with per-side visibility.
- [ ] Support turn/time limits, force elimination, voluntary withdrawal, surrender, and scripted endings.
- [ ] Generate a concise after-action report with outcome rationale, losses, expenditure, and campaign consequences.

## 8. USMC content TODO

- [ ] Define the initial Marine Air-Ground Task Force slice and keep the first roster deliberately small.
- [ ] MVP roster candidate:
  - rifle platoon and weapons attachments;
  - reconnaissance/scout element;
  - light tactical vehicles and selected armored mobility;
  - mortar and artillery fire-support representatives;
  - engineer/breaching element;
  - logistics/resupply element;
  - UAS reconnaissance and one aviation support option;
  - one littoral/anti-ship capability only if the tactical scale supports meaningful employment.
- [ ] Represent task organization through parent formation plus attachments rather than a separate bespoke unit for every combination.
- [ ] Define an opposing-force MVP roster of comparable tactical roles.
- [ ] Research public, unclassified sources for capabilities and doctrine; document abstractions and avoid claims of training fidelity.
- [ ] Create original, gameplay-tuned statistics with citations/notes in source data.
- [ ] Add doctrinally flavored briefings, terminology, and task organization while retaining plain-language tooltips.
- [ ] Include rules for littoral mobility, distributed forces, reconnaissance/counter-reconnaissance, and contested sustainment only where they create decisions on the tactical map.

## 9. Interface and player experience TODO

- [ ] Use a restrained map-first HUD with a selected-unit panel, turn/objective strip, contextual action bar, and collapsible event log.
- [ ] Provide illustrated markers and NATO/MIL-STD-inspired symbols as interchangeable presentation skins over the same units.
- [ ] Complete selected-unit previews with posture, LOS, visible threats, and predicted exposure.
- [x] Prototype selected-unit reachable highlights and hover-driven path preview; retain posture, LOS, threat, and exposure layers for later passes.
- [ ] On target hover, show weapon choice, legality, range band, expected outcome band, and expandable modifiers.
- [ ] Make suppression, degradation, digging in, fired/moved state, passengers, ammunition concern, and reaction eligibility visible without opening a panel.
- [ ] Use animation to communicate results, but never delay input longer than needed; support fast/skip animation settings.
- [ ] Provide next unit, next actionable unit, objective focus, LOS overlay, threat overlay, and map labels/pins.
- [ ] Include scalable UI, remappable controls, colorblind-safe palettes, reduced motion/screen shake, subtitle support, and keyboard navigation for primary flows.
- [ ] Build a guided first scenario that teaches movement, LOS, suppression, reaction fire, assault, and indirect support in that order.

## 10. AI TODO

- [ ] Build AI against the same legal-action and preview APIs used by the player.
- [ ] Separate tactical evaluation from personality/doctrine profiles.
- [ ] Implement basic behaviors: seek cover, maintain fields of fire, suppress before movement, protect support units, react to armor, contest objectives, preserve force, and withdraw.
- [ ] Add mission roles and phase plans so AI does not reduce every scenario to nearest-target attacks.
- [ ] Let AI reason with only information its side is allowed to know.
- [ ] Add headless batch simulations and telemetry for win rate, objective timing, losses, action distribution, and stalled turns.
- [ ] Add difficulty through planning depth, uncertainty tolerance, and limited command benefits—not hidden combat-stat cheating by default.

## 11. Scenario, skirmish, and modding tools

- [ ] Define data schemas for maps, terrain palettes, units, weapons/effects, formations, support missions, scenarios, and campaigns.
- [ ] Build content validators before building a polished editor.
- [ ] Build a scenario editor for map selection, deployment zones, units, objectives, reinforcement/support timing, weather, briefings, and victory rules.
- [ ] Add scenario test-play directly from the editor.
- [ ] Build skirmish setup with map, side, mission type, force budget, turn limit, force purchase, and free deployment.
- [ ] Support meeting engagement, attack, defense, and raid for the first skirmish set.
- [ ] Keep mods in separate packages with manifests, namespaces, dependency/version checks, and load-order reporting.
- [ ] Do not allow mods to overwrite base-game files in place.

## 12. Vertical slice definition

The first playable slice is complete when it contains:

- [ ] one original littoral map;
- [ ] one USMC force and one opposing force, each with infantry, mobile element, anti-armor, and indirect fire;
- [ ] movement postures, terrain costs, facing, LOS/fog, direct fire, suppression, reaction fire, assault, smoke, indirect fire, objectives, and withdrawal;
- [ ] a compact Broken Front-inspired HUD with both illustrated and symbol counters;
- [ ] one tutorial scenario and one replayable attack/defense scenario;
- [ ] one competent objective-driven AI profile;
- [ ] deterministic save/load and battle replay event log;
- [ ] import of a sample `BattleRequest` and export of a valid `BattleResult` through the launcher stub;
- [ ] an after-action screen that previews exactly what Sea of Uncertainty will receive.

Explicitly outside the vertical slice:

- [ ] online multiplayer;
- [ ] a full standalone campaign layer;
- [ ] a polished public scenario editor;
- [ ] every contemporary USMC platform;
- [ ] complex EW, civilian simulation, naval maneuver, strategic logistics, and procedural map generation.

## 13. Milestones and gates

### M0 — Design lock and technical spike

- [ ] Complete the decisions in Section 4.
- [x] Start a Unity 6000.2.12f1 project with a 250 m hex-map and single USMC unit-counter interaction spike.
- [x] Add a 64×104 full-Taiwan operational overview using the shared Sea of Uncertainty Natural Earth/NOAA ETOPO pipeline as the strategic-to-tactical staging layer.
- [ ] Extend the prototype through LOS, pathfinding, counter readability testing, stacking, and reaction interruption.
- [ ] Replace the inherited operational-resolution geography preview with a tactical-resolution DEM/vector import while retaining the shared Sea of Uncertainty visual language and attribution pipeline.
- [ ] Approve the input/output contract draft with the Sea of Uncertainty side.
- [ ] Gate: a player can inspect and execute a move and attack without reading a manual.

### M1 — Deterministic rules sandbox

- [ ] Complete the pure-C# board, turn, movement, visibility, combat, morale/cohesion, and objective rules.
- [ ] Add automated tests and headless simulations.
- [ ] Gate: identical request plus seed produces the same ordered battle events and result.

### M2 — Playable tactical loop

- [ ] Add Unity map presentation, unit selection, contextual actions, feedback, AI, save/load, and after-action reporting.
- [ ] Gate: the attack/defense scenario is finishable by either side and all outcomes are explainable in the log.

### M3 — Sea of Uncertainty round trip

- [ ] Complete launcher stub, request import, result export, error recovery, and idempotency tests.
- [ ] Gate: a strategic test battle launches externally and returns losses/objectives/expenditure exactly once.

### M4 — Vertical-slice quality

- [ ] Complete tutorial, accessibility baseline, audio/visual feedback, balance passes, performance profiling, and packaging.
- [ ] Gate: new-player tests demonstrate comprehension of LOS, suppression, reaction fire, and victory conditions.

### M5 — Expansion

- [ ] Add more maps, missions, units, supports, AI profiles, editor capability, mod packaging, campaign features, and optional asynchronous multiplayer in evidence-driven order.

## 14. Verification checklist for every feature

- [ ] Domain rule has deterministic tests.
- [ ] AI can use the feature through the same legal-action interface.
- [ ] UI previews legality, cost, and likely consequence.
- [ ] Event log explains resolution and modifiers.
- [ ] Save/load preserves the state.
- [ ] Replay/event stream reproduces it.
- [ ] Scenario validator catches invalid content.
- [ ] Imported strategic IDs survive the feature.
- [ ] Result export reports any lasting campaign consequence.
- [ ] Accessibility and fast-animation modes remain functional.

## 15. Immediate next actions

- [ ] Write a one-page 2030 setting brief that locks theater, opposing force, geopolitical framing, and realism level.
- [ ] Draft `BattleRequest` and `BattleResult` JSON Schemas jointly with Sea of Uncertainty.
- [ ] Create a paper rules prototype for one rifle platoon, one mobile unit, one anti-armor unit, and one mortar/support asset per side.
- [ ] Build a Unity interaction spike for hex selection, movement preview, LOS preview, counter states, and target preview.
- [ ] Playtest side-turns versus formation activation and one-unit versus two-unit stacking.
- [ ] Convert the chosen rules into deterministic pure-C# tests before producing final art or a large unit roster.

## 16. Reference baseline

- `BA 2 manual EBOOK.pdf`: campaign/scenario flow, map/UI functions, movement modes and facing, transport, direct and suppression fire, reaction fire, assaults, bombardment, morale, experience, terrain/fortification, editor, and modding.
- `Broken_Front_Game_Manual.pdf` (Alpha 0.9.8): platoon/hex scale, AP and condition clarity, LOS inspection, opportunity fire, assaults, smoke/spotting, concise objectives/skirmish/editor flow, NATO-symbol option, and data-driven modding.
