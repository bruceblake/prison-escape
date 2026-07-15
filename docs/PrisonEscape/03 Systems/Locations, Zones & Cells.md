# Locations, Zones & Cells

Defines *where inmates must be* for each schedule phase, plus cell identity (spawn, bed, shakedown points) and trigger volumes for compliance.

## Zones (`PrisonLocationZone`)

Zone types: **Cell**, **Cafeteria**, **Yard**, **RollCallArea**. Each zone has stand points, an optional custom HUD name, and a trigger collider that registers the player entering/leaving. Default HUD labels: `CELL {n}`, `CAFETERIA`, `YARD`, `ROLL CALL`.

The **Workshop** zone is wired into the registry for the `WorkProgram` blocks ([[Time & Schedule]]). Additional work zones (**Kitchen**, **Laundry**, **Classroom**) with per-inmate assignments are a follow-up.

## Registry (`PrisonLocationRegistry`, singleton)

- Holds `cells[]` (index **0 = the player's cell**) plus cafeteria/yard/roll-call zone references (auto-found if unassigned)
- Tracks cell occupancy (`TryRegisterCellOccupant`) for spawn exclusivity
- `GetStandPointForEvent(evt, cellIndex)` answers "where should this inmate be right now":

| Phase | Stand point |
|---|---|
| Morning Count (`MorningRollCall` / legacy `RollCall`) | Cell roll-call stand point (fallback: spawn, then random roll-call area) |
| Night Count (`NightRollCall`) / Lights Out | Cell spawn point |
| Breakfast / Lunch / Dinner | Random cafeteria stand point |
| Free Time (movement + yard & recreation) | Random yard stand point (fallback: cafeteria) |
| Work / Education / Programs (`WorkProgram`) | Random Workshop stand point (fallback: cafeteria, then yard) |
| Midday / Evening Count | Cell roll-call stand point (presence-only, no sweep) |

## Cell data (`CellData`)

Per-cell serializable transforms + radius:

| Field | Default | Use |
|---|---|---|
| `spawnPoint` | — | Spawn / night destination |
| `rollCallStandPoint` | — | Morning line-up position |
| `nightCheckApproachPoint` | falls back to roll-call → spawn | Guard door approach at night |
| `bedPresenceCenter` | falls back to spawn | Night bed-check overlap center |
| `shakedownSweepCenter` | falls back to bed → spawn | Morning sweep center |
| `interiorCheckRadius` | **2.5 m** (layout tooling has used ~10.3 for big cells) | Bed presence + shakedown sphere |

## Cell doors (`CellDoorController`)

Barred doors slide with the schedule:

- **Open during** all day phases (05:00–21:00): Morning Count, Breakfast, Lunch, Dinner, Free Time, `WorkProgram`, `MiddayCount`, `EveningCount`, legacy Roll Call
- **Closed during** Final Lockdown & Lights Out (21:00–05:00): `NightRollCall`, `LightsOut`
- **Alignment** is now a single canonical path (`PrisonFacilityInstaller.AlignDoorToCellWall` + 6 m `ComputeDoorOpenOffsetLocal`) used by both the facility installer and the modular kit; **Prison → Fix Cell Doors & Waypoints** realigns every door, creates missing cell stand-point children, snaps patrol waypoints to the NavMesh, re-wires the registry, and saves the scene
- Slide: local `openOffset` default **(0, 0, 6)**, `slideSpeed` **3**/s lerp
- Fully covered by EditMode tests (see [[Testing & QA]])

## The 16 cells

Two blocks of 8 (see [[Prison Layout — Minimum Security]]): `JailCells` (01–08 west) and `JailCells_East` (09–16 east). NPC prisoners spawn into cells by index; `GameManager` default NPC cell ID range is 101–108 (legacy numbering for NPC-only cells).

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Prison/PrisonLocationRegistry.cs` | Registry singleton |
| `Assets/Scripts/Shared/Prison/PrisonLocationZone.cs` | Zone triggers + labels |
| `Assets/Scripts/Shared/Prison/CellData.cs` | Per-cell data |
| `Assets/Scripts/Shared/Prison/CellDoorController.cs` | Schedule-driven doors |
| `Assets/Scripts/Shared/Prison/PrisonNavMeshValidator.cs` | Editor check: stand points on NavMesh |

Related: [[Time & Schedule]] · [[Prisoner AI & NPCs]] · [[Roll Call & Shakedown]] · [[Prison Layout — Minimum Security]]
