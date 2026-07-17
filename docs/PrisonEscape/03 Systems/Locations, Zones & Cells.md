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

- **Open during** movement blocks: Breakfast, Lunch, Dinner, Free Time, `WorkProgram`
- **Closed during** night lock-in **and cell counts**: `LightsOut`, `NightRollCall`, `MorningRollCall` / legacy `RollCall`, `MiddayCount`, `EveningCount` — doors unlock when Breakfast begins after morning count
- **Pose** — BlenderKit facility doors keep their **authored FBX local TRS** (`RestoreAuthoredDoorPose`). Shell-center `AlignDoorToCellWall` is legacy/tests only — it drifted doors one bay and flipped yaw.
- **Closed pose** is baked by the installer/fixer and marked authored so Play Mode `Start` does **not** re-capture a left-open door as closed (that bug blocked cell exits).
- **Prison → Fix Cell Doors & Waypoints** restores authored poses, wires controllers (~1.35 m slide, capped ~1.6 m), creates missing stand points, snaps patrol waypoints, re-wires the registry, and saves.
- Slide: local `openOffset` default **~1.35 m** along the wall, `slideSpeed` **3**/s lerp
- **NavMesh doorway gating** — `CellDoorNavMeshLink` keeps a `NavMeshLink` through the doorway **only while the door is open** (installed by **Prison → Fix Collision & Camera Clipping**). Closed doors = no agent path cell↔corridor through that bay.
- Fully covered by EditMode tests for the controller (see [[Testing & QA]])

## The 16 cells

Two blocks of 8 (see [[Prison Layout — Minimum Security]]): `JailCells` (01–08 west) and `JailCells_East` (09–16 east). NPC prisoners spawn into cells by index; `GameManager` default NPC cell ID range is 101–108 (legacy numbering for NPC-only cells).

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Prison/PrisonLocationRegistry.cs` | Registry singleton |
| `Assets/Scripts/Shared/Prison/PrisonLocationZone.cs` | Zone triggers + labels |
| `Assets/Scripts/Shared/Prison/CellData.cs` | Per-cell data |
| `Assets/Scripts/Shared/Prison/CellDoorController.cs` | Schedule-driven doors |
| `Assets/Scripts/Shared/Prison/CellDoorNavMeshLink.cs` | Schedule-gated doorway NavMeshLink |
| `Assets/Scripts/Shared/Prison/PrisonNavMeshValidator.cs` | Editor check: stand points on NavMesh |

Related: [[Time & Schedule]] · [[Prisoner AI & NPCs]] · [[Roll Call & Shakedown]] · [[Prison Layout — Minimum Security]]
