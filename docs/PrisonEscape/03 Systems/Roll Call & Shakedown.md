# Roll Call & Shakedown

Everything stops for a headcount. The prison day is anchored by **4 formal counts** — if the numbers don't match, the whole facility locks down until the discrepancy is cleared.

## The Count (design)

| Count | When | What happens | Status |
|---|---|---|---|
| Morning Count | 05:00–06:00 (`MorningRollCall`) | Stand at cell roll-call point; full **cell shakedown sweep**; no travel grace | ✅ implemented |
| Midday Count | 11:30–12:00 (`MiddayCount`) | Return to your cell; presence-only check (no sweep) | ✅ implemented |
| Evening Count | 16:00–16:30 (`EveningCount`) | Return to your cell; presence-only check | ✅ implemented |
| Night Count | 21:00–22:00 (`NightRollCall`) | Final lockdown; **bed presence verification** by night-verifier guards | ✅ implemented |

- **Count mismatch → Lockdown.** An unaccounted-for inmate triggers a facility-wide Lockdown alert. Night bed check: verifier guard raises it per empty bed. Midday/evening: **`FormalCountMonitor`** (auto-attached beside the tracker) checks `PrisonerPresence` when the count phase ends and raises `PrisonSecurityAlerts.RaiseLockdown("Count mismatch — …")` ([[Security, Heat & Alerts]]). Counts are mandatory phases, so standard guard detection also enforces them — no dedicated count-officer role needed.
- Random informal checks between counts are a future hook.
- On weekends *(planned)*, counts remain but the work blocks between them become visitation, religious services, and extended recreation ([[Time & Schedule]]).

## Morning count flow (implemented)

1. Schedule enters morning line-up → `MorningRollCallTracker.BeginPhase()` (clears per-cell completion set)
2. Inmates stand at their cell's roll-call stand point ([[Locations, Zones & Cells]])
3. `MorningShakedownSweeper` (a guard with the MorningShakedown role) visits each cell:
   - waits for the occupant to stand clear (**3.5 m** clearance)
   - sweeps the cell interior sphere and **destroys illegal world pickups**
   - marks the cell shakedown-complete
4. When **all inmates' cells are cleared** → schedule advances early (`AdvanceMorningRollCallWhenComplete`); safety cap **600 s** real time
5. Each cleared cell's **door opens immediately** (`CellDoorRegistry` + forced open on `CellDoorController`) so inmates can leave without waiting for Breakfast
6. Cleared inmates are **released early** — they stay compliant while walking to the next phase destination

## Confiscation rules

| Category | Confiscated? |
|---|---|
| Contraband | ✅ yes |
| Tool | ✅ yes |
| Weapon | ✅ yes |
| CraftingPart | ❌ kept |
| Consumable | ❌ kept |

**Important:** the sweep only destroys **world pickups** (`PickupItem` in an overlap sphere). Items in the player's inventory or hidden in the [[Escape Routes & Mechanics|pillow stash]] are NOT confiscated — hiding contraband works.

## Sweeper tuning (defaults)

| Parameter | Value |
|---|---|
| Move speed | 7 |
| Delay before first cell | 2 s |
| Stand-point visit time | 1.75 s |
| Arrive distance | 1.25 m |
| Pause between cells | 0.5 s |
| Pause between laps | 2 s |
| Max travel wait | 14 s |
| Occupant clearance | 3.5 m |

## Presence & compliance contract

- `IPrisoner` interface (implemented by `PrisonerController` and `PrisonerAI`): `IsCompliant`, `IsAtRequiredLocation`, `IsRollCallShakedownComplete`, `MovementBlocked`, `CellIndex`, `SendToCell`
- `PrisonerPresence` aggregates all inmates for headcounts — the midday/evening counts reuse this same contract (presence-only, no sweep) via `FormalCountMonitor`
- Player compliance during morning count: at the stand point (**3 m**) or inside the cell interior sphere

## Night count (implemented)

Night count / lights out uses **bed presence verification** by night-verifier guards instead of a line-up — see [[Guard AI]] and [[Escape Routes & Mechanics]] (fake bed dummy). An empty bed raises Lockdown.

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Prison/MorningRollCallTracker.cs` | Per-cell completion set + phase gate |
| `Assets/Scripts/Shared/Prison/CellDoorRegistry.cs` | Maps cell index → door; opens on shakedown complete |
| `Assets/Scripts/Shared/Prison/MorningRollCallSweeperDirector.cs` | Kickoff + fail-soft if guard never arrives |
| `Assets/Scripts/Singleplayer/AI/MorningShakedownSweeper.cs` | The sweep coroutine |
| `Assets/Scripts/Shared/Prison/PrisonerPresence.cs` / `IPrisoner.cs` | Presence contract |
| `Assets/Scripts/Shared/Prison/FormalCountMonitor.cs` | Midday/evening count → lockdown on mismatch |

Related: [[Time & Schedule]] · [[Guard AI]] · [[Security, Heat & Alerts]] · [[Inventory & Items]]
