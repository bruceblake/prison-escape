# Time & Schedule

The heartbeat of the single-player game: an in-game clock advances through timed phases that every other system reacts to.

## How it works

- `PrisonTimeManager` (singleton) advances `currentTimeMinutes` by `minutesPerRealSecond × deltaTime`, wrapping at **1440** (24 h clock).
- Phases come from a `PrisonSchedule` ScriptableObject: an ordered list of `ScheduleEntry { eventType, startTimeMinutes, durationMinutes }`.
- **Duration drives progression** — after session start, `startTimeMinutes` is display-only; each phase runs for its `durationMinutes` then advances (wrapping to the first entry).
- On every phase change the manager fires `OnEventChanged`, which nearly everything subscribes to: prisoners, guards, shakedown sweeper, social manager, cell doors, fake bed dummies, all HUDs.

## The daily schedule (runtime asset)

`Assets/ScriptableObjects/PrisonSchedule.asset`, `minutesPerRealSecond = 1` (1 game minute per real second):

| # | Phase | Clock | Duration (game min) |
|---|---|---|---|
| 0 | Morning Roll Call | 01:00 | 60 |
| 1 | Breakfast | 06:15 | 30 |
| 2 | Free Time | 06:45 | 60 |
| 3 | Lunch | 07:45 | 30 |
| 4 | Free Time | 08:15 | 90 |
| 5 | Dinner | 09:45 | 30 |
| 6 | Free Time | 10:15 | 120 |
| 7 | Lights Out | 12:15 | 360 |
| 8 | Night Roll Call | 18:15 | 15 |

Full loop ≈ **13.25 real minutes** at this time scale.

> ⚠️ The code defaults inside `PrisonSchedule.cs` differ from the asset (starts with legacy `RollCall`, `minutesPerRealSecond = 0.1`). The **asset is authoritative** when assigned.

## Phase types (`PrisonEventType`)

`RollCall` (legacy), `Breakfast`, `Lunch`, `Dinner`, `FreeTime`, `LightsOut`, `MorningRollCall`, `NightRollCall`

## Rules

- **Mandatory vs flexible:** every phase is mandatory **except `FreeTime`** (`PrisonEventRules`)
- **Morning line-up** = `RollCall` or `MorningRollCall`; **night bed phase** = `NightRollCall` or `LightsOut` (`PrisonEventExtensions`)
- **High-stakes warning:** flexible now + mandatory next → HUD warning state
- **Travel grace:** entering a mandatory phase grants **50 real seconds** (`complianceGraceRealSeconds`) where prisoners count as compliant while traveling. **Not granted** for morning line-up or initial setup.
- **Morning roll-call gate:** the phase can end **early** when every inmate's cell passes shakedown (`endMorningRollCallWhenAllAccounted = true`), with a **600 s** real-time safety cap (`morningRollCallMaxRealSeconds`). See [[Roll Call & Shakedown]].

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Prison/PrisonTimeManager.cs` | Singleton clock + phase machine |
| `Assets/Scripts/Shared/Prison/PrisonSchedule.cs` | ScriptableObject schedule data |
| `Assets/Scripts/Shared/Prison/PrisonEventType.cs` / `PrisonEventRules.cs` / `PrisonEventExtensions.cs` | Enum + rules |
| `Assets/ScriptableObjects/PrisonSchedule.asset` | The live schedule |

## Tuning

Change the day by editing the schedule asset (phases, durations, time scale). Grace seconds and the roll-call gate live on `PrisonTimeManager`.

Related: [[World Rules]] · [[Roll Call & Shakedown]] · [[UI & HUD]] · [[Locations, Zones & Cells]]
