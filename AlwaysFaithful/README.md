# Always Faithful

Standalone 2030 near-future USMC tactical game and tactical-resolution module for Sea of Uncertainty.

## Current prototype

The prototype now presents the complete island of Taiwan as a 64×104 whole-island operational board (6,656 hexes at roughly 4 km spacing), using the same Natural Earth coastline and NOAA ETOPO 2022 pipeline as Sea of Uncertainty. One selectable USMC rifle-platoon counter exercises the order workflow. The platoon begins unselected. Opening its order menu and choosing Move reveals the area reachable with its remaining action points; hovering a legal destination previews the deterministic least-cost path, and left-clicking confirms the move. Terrain-weighted movement spends AP, the counter visibly enters a spent state at zero AP, and End Turn restores its allowance.

Platoon position, AP, readiness, and selection now live in serializable gameplay state rather than in the counter renderer. The compact command card presents the current turn, unit state, occupied terrain, and four readable AP pips at every supported window height.

Any selected land hex can now open a deterministic 25×19 local battlefield. Its 475 hexes use 250 m geographic spacing and retain a versioned battlefield ID, parent operational hex, center/origin coordinates, bounds, terrain, and elevation samples. A short command-table transition leads to a local briefing card, amplified local relief, continuous coastal classification and shoreline accents, local hex inspection, bounded camera navigation, and a Return to Island control that restores the exact prior overview camera.

The USMC rifle platoon deploys onto each local map with eight tactical AP. Local movement combines destination terrain and elevation-change costs, rejects water, slopes over 90 m per edge, occupied cells, out-of-bounds destinations, and unaffordable routes, and persists completed, rejected, and cancelled orders as serializable movement events. A continuous teal-to-gold route ribbon, destination ghost, reachable-area wash, live AP label, invalid red state, and terrain-following counter animation keep every order legible.

The tactical order menu also provides an inspect-only line-of-sight tool with a 12-hex (3 km) limit. It traces a deterministic hex line between observer and target, compares the interpolated sightline against measured elevation and rough-terrain obstruction, and reports clear, obscured, blocked, out-of-range, and map-edge results without committing an attack. Teal, amber, and red line segments and intervening-hex washes make the visibility transition readable directly on the terrain, while the inspection card lists the range and decisive modifiers.

Pass 6 adds an authoritative tactical observation picture. Every local hex receives terrain- and LOS-aware fog shading, while two opposing formations are represented according to what the USMC observer actually knows: hidden, uncertain contact, identified unit, or currently observed unit. Lost tracks persist for one turn at their last-known position before expiring. Contact records retain observer, range, LOS, turn, and stale-state data through JSON save/reload, and only a non-stale currently observed target passes the combat-information gate reserved for direct fire in Pass 7.

Pass 7 activates that combat-information gate with a deterministic M27 small-arms action. Direct fire costs 2 AP and one of six abstract ammunition units. A pre-fire panel exposes the hit chance, expected effect, range, terrain, and LOS modifiers before confirmation; seeded resolution produces a miss, suppression, or hit event. The fire line, target reticle, restrained muzzle/impact cue, counter response, ammunition display, and persistent versioned event record all present the same authoritative result.

Tactical units now use compact procedural formation models inspired by Sea of Uncertainty's counter language instead of flat blocks. The USMC rifle platoon and identified PLA formations combine three maneuver elements, a command node, affiliation-specific recognition stripe, designation plate, soft shadow, and selection halo. Uncertain enemy information remains a diamond contact rather than revealing formation geometry. The whole-island operational counter is intentionally unchanged in this first tactical-only art pass.

Pass 8 gives direct fire lasting consequences. Every Suppressed or Hit outcome now adds cumulative suppression points to the target, moving it through Ready, Suppressed, Disrupted, and Reduced cohesion states with their own movement and fire restrictions: Disrupted units cannot return fire, and a Reduced unit cannot move or fire at all until it recovers. A Rally order spends one AP to shake off a fixed amount of suppression, cohesion also decays passively at the start of every turn, and every application is a persistent, serializable event rather than a one-off die roll. Suppressed formations carry a pulsing status badge and a desaturated body color on the tactical counter and on any identified or observed enemy marker, the command card reports the unit's own status and point total once it leaves Ready, the direct-fire target tooltip previews the enemy's current cohesion, and the tactical intelligence panel appends a known enemy's status next to its contact state.

Every player action now has a rudimentary audio cue. Short tones are synthesized at runtime (no imported sound assets) for selecting a hex, opening an order menu, entering a planning mode, confirming or rejecting a move, each direct-fire outcome, rallying, ending a turn, transitioning to or from the tactical map, and a contact being gained or lost.

Pass 9 lets the enemy shoot back while the platoon is moving. Every eligible PLA formation now carries its own small-arms weapon, and each move checks the hexes the platoon actually steps into for an enemy that is still able to fire and has an unblocked, in-range line of sight; the nearest eligible reactor (ties broken by unit ID) takes exactly one snap-shot reaction per move, resolved with the same deterministic small-arms model as deliberate fire but at a shorter range and an accuracy penalty. A miss lets the platoon complete its route; a suppressing or damaging hit halts it at the exposed hex and applies the same cumulative suppression as Pass 8. The interruption pauses the route animation, pans and tightens the camera on the firer, raises an on-screen reaction-fire banner, flashes the reactor's marker and the platoon's own counter, and logs a persistent, replayable reaction event alongside the movement record it cut short.

Pass 10 closes the turn loop with a deterministic, objective-aware PLA turn. Ending the tactical turn hands control to every opposing formation in a fixed order; each one plans through the same legal-command interface the player uses, prioritizing recovery when disrupted, deliberate fire when a target is currently observed, objective-seeking movement along the cheapest legal route when neither applies, and an inspect-only observation update otherwise, with every candidate order re-validated against current board state before it executes. Visible enemy actions pan and tighten the camera on the acting unit and animate at normal speed; actions the platoon cannot currently observe resolve instantly behind a "hidden activity" summary, and a cinematic/fast toggle lets a player skip ahead once the pattern is familiar. A turn banner tracks phase and side, every AI action is logged as a persistent, replayable event alongside the existing movement, fire, suppression, and reaction records, and a fixed-seed planner replay, illegal-order rejection, and a hard planning-time budget are all covered by headless regression.

Pass 11 gives every 250 m tactical map real terrain texture. Each land hex now deterministically rolls a Light, Medium, or Heavy cover density — weighted by its Open/Rough/Highland terrain type, and reproducible from the battlefield's own ID so the same operational hex always regenerates the same layout — plus a chance of joining a small or medium built-up cluster of two to seven hexes wherever it falls within three hexes of shore. Cover is entirely independent of movement cost: it instead adds obstruction height and obscures line of sight, and meaningfully reduces hit and suppression chance for a defender in both deliberate and reaction fire, up to a 32-point hit-chance penalty for Heavy cover. Cover-bearing hexes carry a subtle color tint plus procedural vegetation clumps or, for built-up cells, small boxy structures with a contrasting roofline, and the LOS/fire inspection panels report the target's cover alongside every other modifier.

Pass 12 lets a tactical battle actually end. Every local map now deterministically assigns an Attack or Defend posture and a single objective hex — Attack places it as far from the platoon's own landing zone as the terrain allows without sitting next to a PLA start position, Defend simply asks the platoon to hold the ground it lands on — plus a short turn limit. A gold ring marks the objective and turns green the instant the platoon physically occupies it, a teal ring marks the platoon's own deployment zone when the two differ, and the command card reports the battle's turn count against its limit alongside a seize/hold status line. The battle concludes the moment the platoon or every observed PLA formation is fought to a standstill, or the turn limit expires, whichever comes first: a full-screen after-action screen reports victory, defeat, a mutual-destruction draw, or an inconclusive stalemate, alongside turns taken, casualties on both sides, and a chronological log merged from every movement, fire, suppression, reaction, and enemy-action record of the battle.

Pass 13 adds a Recon order without adding a new unit — ISR drones stay off the table for now. The platoon can task a focused sensor sweep on any hex up to 12 hexes away for 3 AP, and unlike every other spotting tool this one needs no ground line of sight, representing an indirect or overhead request rather than the platoon's own eyes. A successful tasking grants a one-tier detection bonus — Hidden to Contact, Contact to Identified, Identified to Observed — on that exact hex for two turns, marked by a violet ring distinct from the objective and deployment-zone rings; because the bonus is tied to the hex rather than the formation on it, a PLA element that moves off the tasked ground stops benefiting from it, a deliberate limit rather than a gap. The order, its AP cost, its expiry, and the resulting contact-state change are all persistent, replayable events alongside the rest of the battle log.

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
- On the tactical map, right click the platoon and choose **Direct Fire**, hover a currently observed enemy, and left click to fire.
- On the tactical map, right click the platoon and choose **Rally** to spend 1 AP recovering suppression once the unit is Suppressed, Disrupted, or Reduced.
- On the tactical map, right click the platoon and choose **Recon** to spend 3 AP tasking a sensor sweep on any hex within 12 hexes, no line of sight required; watch for the violet ring and the "Recon active" line in hex inspection.
- Right click away from the unit or press **Escape** to cancel tactical movement, LOS inspection, fire targeting, or recon tasking.
- Moving within an alert enemy's range and line of sight can trigger a reaction shot that pauses and may halt the move partway.
- On the tactical map, use **End Turn** to hand control to the PLA; watch the phase banner and camera focus for each visible action, or toggle **Enemy Speed** between cinematic and fast pacing.
- Watch the gold objective ring and the turn counter on the command card; the battle ends in victory, defeat, a draw, or a stalemate once the platoon or the PLA is fought to a standstill or the turn limit runs out, and the after-action screen's **Return to Island** button ends the battle and restores the operational map.
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
