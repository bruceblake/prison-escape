# Guard AI

Guards patrol, detect non-compliant inmates, escort them back to their cells, verify beds at night, and run the morning shakedown.

> 🔭 **Planned (specced):** guards join the social ecosystem — per-guard archetypes (By-the-Book, **Corrupt/bribable**, Rookie 75° cone, Veteran 8 m proximity spot), per-player guard trust (≥ 50 → +10 s compliance tolerance; ≤ −25 → +2 m detection vs you), and snitch **tips** that queue targeted shakedowns. Design: [[Social Ecosystem & Gangs]] (§ guard archetypes, bribes, snitching).

## Duties & spawn roles

Configured via `GameManager.guardSpawnTable` (`GuardSpawnEntry`: name, spawn point, waypoints, role, `onDutyDuring[]` phases — empty = always on duty).

| Role | Behavior |
|---|---|
| **StandardPatrol** | Waypoint patrol + detection/escort; during morning line-up switches to shakedown mode |
| **NightCellVerifier** | Same patrol/detection, plus walks every cell during night phases and verifies bed presence |
| **MorningShakedown** | FSM/detection off; runs the [[Roll Call & Shakedown|shakedown sweep]] when on duty |
| **CountOfficer** *(planned)* | Runs the presence-only midday (11:30) and evening (16:00) counts under the new schedule ([[Time & Schedule]]); a mismatch raises Lockdown |

With the new schedule, `onDutyDuring[]` gains the `WorkProgram`, `MiddayCount`, and `EveningCount` phases — e.g. work-zone supervision posts during `WorkProgram`, count officers during the two new counts.

`GuardShiftController` subscribes to schedule changes and enables/disables NavMeshAgent, FSM, detection, and sweeper per role and phase.

## State machine (`GuardFSM`)

States: **Patrol** → **Escort** (→ back to Patrol). 
(`Enforce` exists in the enum but is never entered by the current update path.)

- **Patrol:** cycle waypoints, arrive distance 1.5 m
- **→ Escort:** when detection finds a non-compliant prisoner
- **Escort:** chase until within **2 m** (arrest → prisoner movement blocked) → lead prisoner (snapped 1.5 m behind guard) to their cell spawn → `SendToCell` → back to Patrol. Force-completes after **35 s** stall.

## Detection (`GuardDetection`)

| Parameter | Default |
|---|---|
| Detection range | **10 m** |
| Vision cone | **90°** (45° half-angle) |
| Proximity spot (bypasses cone) | **6 m** |

**Spot rule:** prisoner is non-compliant AND (in cone at range OR within 6 m proximity). Already-arrested targets are skipped. The same geometry drives the heat eye UI via `IsPositionInAttentionZone` ([[Security, Heat & Alerts]]).

**Night bed check:** `OverlapSphere` at the cell's bed presence center (radius = cell interior radius). Passes if it finds the cell's prisoner **or a matching `FakeBedDummy`**. Failure → `PrisonSecurityAlerts.RaiseLockdown`.

## Movement numbers

| Parameter | Value |
|---|---|
| Patrol speed | 8 |
| Escort speed | 24 |
| Turn speed / escort turn | 720 / 960 °/s |
| Night approach arrive distance | 1.75 m |
| Post-arrest prisoner release | 1 s after cell delivery |

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Singleplayer/AI/GuardFSM.cs` | State machine |
| `Assets/Scripts/Singleplayer/AI/GuardDetection.cs` | Vision + bed checks |
| `Assets/Scripts/Singleplayer/AI/GuardShiftController.cs` | Duty scheduling |
| `Assets/Scripts/Singleplayer/AI/MorningShakedownSweeper.cs` | Shakedown |
| `Assets/Prefabs/AIPrefabs/Guard.prefab` | Guard prefab (navy + gold visuals) |

Related: [[Time & Schedule]] · [[Prisoner AI & NPCs]] · [[Security, Heat & Alerts]] · [[Escape Routes & Mechanics]]
