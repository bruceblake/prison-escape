# Roadmap & Priorities

Living note — reorder as the game evolves. Log completed items in [[Prison Escape Devlog Dashboard]].

## Now (highest priority)

1. **Social ecosystem & gangs overhaul** — 📐 **specced**: [[Social Ecosystem & Gangs]]. Full v1 teardown; NPC identities + personality traits (prisoners **and** guards), Respect/Trust relationships, NPC memory + gossip, 2 gangs with territory + membership ladder, Talk Menu (chat/intel, gifts, trading, two-way favors, intimidation), snitching → guard tips, corrupt-guard bribes, wallet goes live. Build in milestones M1–M6 on `feat/social-ecosystem`.

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
- Medium-security prison (second facility)
- PlayMode test infrastructure (`.asmdef` migration)

## Test debt (from the coverage matrix)

Pure-logic seams worth extracting for EditMode tests: `PrisonTimeManager` progress math, location-registry occupancy, roll-call tracker set logic, `GuardDetection.IsInSight` geometry, `GuardShiftController.IsOnDutyFor`, prisoner zone-compliance matrix, wallet clamp, unscrew math, UI formatting helpers.

See [[Testing & QA]] for the full matrix.
