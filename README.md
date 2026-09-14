# Always Faithful

**Always Faithful** is a standalone Unity tactical game about plausible 2030 US Marine Corps littoral operations. It is also designed to serve as the tactical-resolution module for **Sea of Uncertainty** through a versioned battle-request and battle-result contract.

## Current state

The initial prototype provides:

- a 64×104 full-Taiwan operational board with 6,656 flat-top hexes at roughly 4 km spacing;
- one selectable USMC rifle-platoon counter;
- deterministic terrain movement costs and least-cost pathfinding;
- legal-movement and hover-path previews;
- click-to-move animation;
- a full-island terrain integration using the same Natural Earth and NOAA ETOPO source pipeline as Sea of Uncertainty.

The whole-island geography supports operational presentation and broad terrain classes. Authoritative tactical movement and future line-of-sight rules will use higher-resolution local terrain sources when Sea of Uncertainty opens a 250 m engagement map.

## Open the project

The Unity project is in [`AlwaysFaithful/`](AlwaysFaithful/README.md) and currently targets Unity `6000.2.12f1`.

Open `AlwaysFaithful/Assets/Scenes/HexAndCounterPrototype.unity` and enter Play mode.

## Planning

See [`docs/USMC_Tactical_Battle_Game_TODO.md`](docs/USMC_Tactical_Battle_Game_TODO.md) for the product and implementation roadmap.
