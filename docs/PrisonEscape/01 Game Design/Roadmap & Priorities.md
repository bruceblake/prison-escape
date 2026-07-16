# Roadmap & Priorities

Living note — reorder as the game evolves. Log completed items in [[Prison Escape Devlog Dashboard]].

## Now (highest priority)

1. **Social ecosystem & gangs overhaul** — 📐 **specced (v3)**: [[Social Ecosystem & Gangs]]. Research-backed (real prison governance + Escapists/TE2 + Back to Dawn); NPC identities + personality traits (prisoners **and** guards); Respect/Trust + Standing bands (Enemy→Confidant); NPC memory + gossip; 2 exclusive gangs with territory + Outsider→Trusted ladder; [[Talk Menu & NPC Profile]]; notebook [[Social Dossier — Relationships & Gangs]]; trading + live wallet; two-way favors; snitching → guard tips; corrupt-guard bribes. Build M1–M6 on `feat/social-ecosystem`.
2. **Prison career ladder** — 📐 **specced**: [[Prison Career Ladder]] (+ [[World Saves & Start Screen]] · [[Facility Transfer & Graduation]]). County → State ×3 → Federal ×5; escape = caught & transferred; named world saves, prison-select hub with locked silhouettes, global carry vs local reset, County sentence clock. Build M1–M5 (`feat/career-world-saves` → `feat/county-sentence-clock`); facility scenes are M6+ content epics.

## Next

2. **Vent network geometry** — the plumbing/electrical corridors behind the cell rows exist in design ([[Prison Layout — Minimum Security]]) but need to be built into the level and connected to the vent-cover mechanic
3. **Courtyard fence + cut-through escape route** — barbed-wire fence, wire cutters as the tool gate
4. **Front-entrance escape route** — Main Security bypass (disguise? keycard? timing?) — needs design in this vault first
5. **Escape route prerequisites → loot placement pass** — make sure the items each route needs actually spawn ([[Loot & Economy]]); **Prison → Setup Items & World Loot** (`PrisonLootSetupRunner`) is on `dev` for one-click placement

## Done on `dev` (7/15/2026 integration)

- **Escape completion v1** — [[Escape Completion System]] merged (boundary win, solitary, suspicion, stats, restricted zones)
- **Realistic 13-phase schedule** — count-driven prison day ([[Time & Schedule]])
- **BlenderKit prison build** — facility install, polish pass, door/waypoint fixer ([[Blender Asset Kit]] · [[Editor Tooling]])
- **ProBuilder rebuild pipeline** — alternative layout path from `feat/prison-probuilder-rebuild` ([[Editor Tooling]])
- **Character visuals** — rigged BlenderKit characters + procedural fallback ([[Character Visuals]])

## Later

- Yard roll-call zone + full zone wiring for all 16 cells
- Kitchen (back-of-house behind the cafeteria serving line)
- Laundry room
- Career facility scenes (County greybox first, then State ×3, Federal ×5 — [[Prison Career Ladder]] M6+; replaces the old "medium-security second facility" item)
- PlayMode test infrastructure (`.asmdef` migration)

## Test debt (from the coverage matrix)

Pure-logic seams worth extracting for EditMode tests: `PrisonTimeManager` progress math, location-registry occupancy, roll-call tracker set logic, `GuardDetection.IsInSight` geometry, `GuardShiftController.IsOnDutyFor`, prisoner zone-compliance matrix, wallet clamp, unscrew math, UI formatting helpers.

See [[Testing & QA]] for the full matrix.
