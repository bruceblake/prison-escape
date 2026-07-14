# Roll Call & Shakedown

Morning roll call is not just a timer — a guard sweeps every cell, confiscates contraband, and the phase ends when all inmates are accounted for.

## Flow

1. Schedule enters morning line-up → `MorningRollCallTracker.BeginPhase()` (clears per-cell completion set)
2. Inmates stand at their cell's roll-call stand point ([[Locations, Zones & Cells]])
3. `MorningShakedownSweeper` (a guard with the MorningShakedown role) visits each cell:
   - waits for the occupant to stand clear (**3.5 m** clearance)
   - sweeps the cell interior sphere and **destroys illegal world pickups**
   - marks the cell shakedown-complete
4. When **all inmates' cells are cleared** → schedule advances early (`AdvanceMorningRollCallWhenComplete`); safety cap **600 s** real time
5. Cleared inmates are **released early** — they stay compliant while walking to the next phase destination

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
- `PrisonerPresence` aggregates all inmates for headcounts
- Player compliance during morning roll call: at the stand point (**3 m**) or inside the cell interior sphere

## Night counterpart

Night roll call / lights out uses **bed presence verification** by night-verifier guards instead — see [[Guard AI]] and [[Escape Routes & Mechanics]] (fake bed dummy).

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Prison/MorningRollCallTracker.cs` | Per-cell completion set + phase gate |
| `Assets/Scripts/Singleplayer/AI/MorningShakedownSweeper.cs` | The sweep coroutine |
| `Assets/Scripts/Shared/Prison/PrisonerPresence.cs` / `IPrisoner.cs` | Presence contract |

Related: [[Time & Schedule]] · [[Guard AI]] · [[Security, Heat & Alerts]] · [[Inventory & Items]]
