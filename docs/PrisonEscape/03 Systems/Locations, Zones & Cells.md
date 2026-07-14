# Locations, Zones & Cells

Defines *where inmates must be* for each schedule phase, plus cell identity (spawn, bed, shakedown points) and trigger volumes for compliance.

## Zones (`PrisonLocationZone`)

Zone types: **Cell**, **Cafeteria**, **Yard**, **RollCallArea**. Each zone has stand points, an optional custom HUD name, and a trigger collider that registers the player entering/leaving. Default HUD labels: `CELL {n}`, `CAFETERIA`, `YARD`, `ROLL CALL`.

## Registry (`PrisonLocationRegistry`, singleton)

- Holds `cells[]` (index **0 = the player's cell**) plus cafeteria/yard/roll-call zone references (auto-found if unassigned)
- Tracks cell occupancy (`TryRegisterCellOccupant`) for spawn exclusivity
- `GetStandPointForEvent(evt, cellIndex)` answers "where should this inmate be right now":

| Phase | Stand point |
|---|---|
| Morning Roll Call / Roll Call | Cell roll-call stand point (fallback: spawn, then random roll-call area) |
| Night Roll Call / Lights Out | Cell spawn point |
| Breakfast / Lunch / Dinner | Random cafeteria stand point |
| Free Time | Random yard stand point (fallback: cafeteria) |

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

- **Open during:** Roll Call, Breakfast, Lunch, Dinner, Free Time, Morning Roll Call
- **Closed during:** Lights Out, Night Roll Call
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
