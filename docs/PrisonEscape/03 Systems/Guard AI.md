# Guard AI

Guards patrol, detect non-compliant inmates, escort them back to their cells, verify beds at night, and run the morning shakedown.

> ✅ **Social / career hooks (on `dev`):** guard archetypes via `GuardSocialProfile` / `GuardArchetype` (By-the-Book, **Corrupt/bribable**, Rookie **75°** cone, Veteran **8 m** proximity + distraction immune); Corrupt bribes from Talk; snitch tips → `MorningShakedownSweeper.QueueTargetedShakedown`. Facility / career `DetectionRangeMult` scales detection. **Per-player guard Trust (7/17):** trust ≥ 50 → 10 s compliance grace on a fresh schedule lapse (`GuardTrustMath`, never in restricted zones); trust ≤ −25 → ×1.2 detection ranges. Design: [[Social Ecosystem & Gangs]] · [[Social & Reputation]].

## Duties & spawn roles

Configured via `GameManager.guardSpawnTable` (`GuardSpawnEntry`: name, spawn point, waypoints, role, `onDutyDuring[]` phases — empty/`null` = always on duty). Spawns **snap to NavMesh** (6 m sample) so off-mesh / sunk spawn points still place a visible, pathable guard.

| Role | Behavior |
|---|---|
| **StandardPatrol** | Waypoint patrol + detection/escort; during morning line-up switches to shakedown mode |
| **NightCellVerifier** | Same patrol/detection, plus walks every cell during night phases and verifies bed presence |
| **MorningShakedown** | FSM/detection off; runs the [[Roll Call & Shakedown|shakedown sweep]] when on duty |

**Counts need no dedicated guard role.** Midday/evening counts are mandatory in-cell phases, so standard patrol detection already enforces them; the presence check + lockdown on mismatch is handled by `FormalCountMonitor` ([[Roll Call & Shakedown]]). `onDutyDuring[]` accepts the new `WorkProgram`/`MiddayCount`/`EveningCount` phases for shift scheduling (e.g. a workshop supervision post during `WorkProgram`).

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
