# Roadmap & Priorities

Living note — reorder as the game evolves. Log completed items in [[Prison Escape Devlog Dashboard]].

## Now (highest priority)

1. **Vent network geometry** — plumbing/electrical corridors behind the cell rows exist in design ([[Prison Layout — Minimum Security]]) but need to be built into the level and connected to the vent-cover mechanic so the escape boundary is reachable in play
2. **Courtyard fence + cut-through escape route** — barbed-wire fence, wire cutters as the tool gate
3. **Vault sync P3** — process note cleanup (P0–P2 hub/engineering/surface notes done)
4. **Social polish** — install `Resources/Social/` assets; richer Talk chrome; fuller per-guard Trust→detection ([[Social & Reputation]])

## Next

5. **Front-entrance escape route** — Main Security bypass (disguise? keycard? timing?) — needs design in this vault first
6. **Escape route prerequisites → loot placement pass** — make sure the items each route needs actually spawn ([[Loot & Economy]]); **Prison → Setup Items & World Loot** (`PrisonLootSetupRunner`) is on `dev` for one-click placement
7. **Career M6+ facility scenes** — State ×3 → Federal ×5 (County stub `CountyJail` + Dev Sandbox already playable)

## Done on `dev`

- **Social ecosystem v3** — [[Social Ecosystem & Gangs]] / [[Social & Reputation]] (Respect/Trust, gangs, Talk, dossier, trade, favors, snitch) — micro-stack #58–#72
- **Prison career ladder M1–M5** — [[Prison Career Ladder]] (+ [[World Saves & Start Screen]] · [[Facility Transfer & Graduation]]): worlds, MainMenu hub, facility defs, transfer ceremony, County sentence clock; County stub scene
- **Escape completion** — [[Escape Completion System]] (boundary end, solitary, suspicion, stats, restricted zones + career transfer framing)
- **Realistic 13-phase schedule** — count-driven prison day ([[Time & Schedule]])
- **BlenderKit prison build** — facility install, polish pass, door/waypoint fixer ([[Blender Asset Kit]] · [[Editor Tooling]])
- **ProBuilder rebuild pipeline** — alternative layout path ([[Editor Tooling]])
- **Character visuals** — rigged BlenderKit characters + procedural fallback ([[Character Visuals]])

## Later

- Yard roll-call zone + full zone wiring for all 16 cells
- Kitchen (back-of-house behind the cafeteria serving line)
- Laundry room
- Career facility scenes polish beyond County stub ([[Prison Career Ladder]] M6+)
- PlayMode test infrastructure (`.asmdef` migration)

## Test debt (from the coverage matrix)

Pure-logic seams worth extracting for EditMode tests: `PrisonTimeManager` progress math, location-registry occupancy, roll-call tracker set logic, `GuardDetection.IsInSight` geometry, `GuardShiftController.IsOnDutyFor`, prisoner zone-compliance matrix, wallet clamp, unscrew math, UI formatting helpers.

See [[Testing & QA]] for the full matrix.
