# Roadmap & Priorities

Living note — reorder as the game evolves. Log completed items in [[Prison Escape Devlog Dashboard]].

## Now (highest priority)

1. **Escape completion system** — 🚧 **in progress** on `feat/escape-completion`. Fully specced: [[Escape Completion System]] (escape boundary win, end screen + stats, restricted zones, solitary confinement block, mental health/strength stats, suspicion window).

## Next

2. **Vent network geometry** — the plumbing/electrical corridors behind the cell rows exist in design ([[Prison Layout — Minimum Security]]) but need to be built into the level and connected to the vent-cover mechanic
3. **Courtyard fence + cut-through escape route** — barbed-wire fence, wire cutters as the tool gate
4. **Front-entrance escape route** — Main Security bypass (disguise? keycard? timing?) — needs design in this vault first
5. **Escape route prerequisites → loot placement pass** — make sure the items each route needs actually spawn ([[Loot & Economy]])

## Later

- Yard roll-call zone + full zone wiring for all 16 cells
- Kitchen (back-of-house behind the cafeteria serving line)
- Laundry room
- Medium-security prison (second facility)
- PlayMode test infrastructure (`.asmdef` migration)

## Test debt (from the coverage matrix)

Pure-logic seams worth extracting for EditMode tests: `PrisonTimeManager` progress math, location-registry occupancy, roll-call tracker set logic, `GuardDetection.IsInSight` geometry, `GuardShiftController.IsOnDutyFor`, prisoner zone-compliance matrix, wallet clamp, unscrew math, UI formatting helpers.

See [[Testing & QA]] for the full matrix.
