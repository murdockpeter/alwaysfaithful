# Always Faithful

**Always Faithful** is a standalone Unity tactical game about plausible 2030 US Marine Corps littoral operations. It is also designed to serve as the tactical-resolution module for **Sea of Uncertainty** through a versioned battle-request and battle-result contract.

## Current state

The initial prototype provides:

- a 14×10 flat-top tactical hex board at 250 metres per hex;
- one selectable USMC rifle-platoon counter;
- deterministic terrain movement costs and least-cost pathfinding;
- legal-movement and hover-path previews;
- click-to-move animation;
- a visual-reference integration with the same Natural Earth and NOAA ETOPO source family used by Sea of Uncertainty.

The inherited operational geography is presentation-only at this tactical scale. Authoritative movement and future line-of-sight rules remain hex data and will use higher-resolution tactical terrain sources.

## Open the project

The Unity project is in [`AlwaysFaithful/`](AlwaysFaithful/README.md) and currently targets Unity `6000.2.12f1`.

Open `AlwaysFaithful/Assets/Scenes/HexAndCounterPrototype.unity` and enter Play mode.

## Planning

See [`docs/USMC_Tactical_Battle_Game_TODO.md`](docs/USMC_Tactical_Battle_Game_TODO.md) for the product and implementation roadmap.
