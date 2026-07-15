# Roadmap & Priorities

Living note — reorder as the game evolves. Log completed items in [[Prison Escape Devlog Dashboard]].

## Now (highest priority)

1. **Escape completion system** — 🚧 **in progress** on `feat/escape-completion`. Fully specced: [[Escape Completion System]] (escape boundary win, end screen + stats, restricted zones, solitary confinement block, mental health/strength stats, suspicion window).
2. **Social ecosystem & gangs overhaul** — 📐 **specced**: [[Social Ecosystem & Gangs]]. Full v1 teardown; NPC identities + personality traits (prisoners **and** guards), Respect/Trust relationships, NPC memory + gossip, 2 gangs with territory + membership ladder, Talk Menu (chat/intel, gifts, trading, two-way favors, intimidation), snitching → guard tips, corrupt-guard bribes, wallet goes live. Build in milestones M1–M6 on `feat/social-ecosystem`.

## Next

3. **Vent network geometry** — the plumbing/electrical corridors behind the cell rows exist in design ([[Prison Layout — Minimum Security]]) but need to be built into the level and connected to the vent-cover mechanic
4. **Courtyard fence + cut-through escape route** — barbed-wire fence, wire cutters as the tool gate
5. **Front-entrance escape route** — Main Security bypass (disguise? keycard? timing?) — needs design in this vault first
6. **Escape route prerequisites → loot placement pass** — make sure the items each route needs actually spawn ([[Loot & Economy]])

## Later

- Yard roll-call zone + full zone wiring for all 16 cells
- Kitchen (back-of-house behind the cafeteria serving line)
- Laundry room
- Medium-security prison (second facility)
- PlayMode test infrastructure (`.asmdef` migration)

## Test debt (from the coverage matrix)

Pure-logic seams worth extracting for EditMode tests: `PrisonTimeManager` progress math, location-registry occupancy, roll-call tracker set logic, `GuardDetection.IsInSight` geometry, `GuardShiftController.IsOnDutyFor`, prisoner zone-compliance matrix, wallet clamp, unscrew math, UI formatting helpers.

See [[Testing & QA]] for the full matrix.
