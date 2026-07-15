# Time & Schedule

The heartbeat of the single-player game: an in-game clock advances through timed phases that every other system reacts to. The prison day is built entirely around **predictability, control, and headcount** — every day is micro-managed to the minute so the facility can keep a large population moving without friction.

## How it works

- `PrisonTimeManager` (singleton) advances `currentTimeMinutes` by `minutesPerRealSecond × deltaTime`, wrapping at **1440** (24 h clock).
- Phases come from a `PrisonSchedule` ScriptableObject: an ordered list of `ScheduleEntry { eventType, startTimeMinutes, durationMinutes }`.
- **Duration drives progression** — after session start, `startTimeMinutes` is display-only; each phase runs for its `durationMinutes` then advances (wrapping to the first entry).
- On every phase change the manager fires `OnEventChanged`, which nearly everything subscribes to: prisoners, guards, shakedown sweeper, social manager, cell doors, fake bed dummies, all HUDs.

## The weekday schedule (canonical design)

| # | Clock | Block | Phase | Mandatory |
|---|---|---|---|---|
| 0 | 05:00–06:00 | Morning Count & Wake-Up | `MorningRollCall` (count + cell shakedown) | Yes |
| 1 | 06:00–07:00 | Breakfast | `Breakfast` | Yes |
| 2 | 07:00–08:00 | Morning Movement | `FreeTime` | No |
| 3 | 08:00–11:30 | Work / Education / Programs | `WorkProgram` *(new)* | Yes |
| 4 | 11:30–12:00 | Midday Count | `MiddayCount` *(new)* — return to cell/housing | Yes |
| 5 | 12:00–12:30 | Lunch | `Lunch` | Yes |
| 6 | 12:30–13:00 | Midday Movement | `FreeTime` | No |
| 7 | 13:00–16:00 | Afternoon Work Shift | `WorkProgram` *(new)* | Yes |
| 8 | 16:00–16:30 | Evening Count | `EveningCount` *(new)* — return to cell/housing | Yes |
| 9 | 16:30–17:00 | Dinner | `Dinner` | Yes |
| 10 | 17:00–21:00 | Yard Time & Recreation | `FreeTime` (yard, dayroom, commissary, weights) | No |
| 11 | 21:00–22:00 | Final Lockdown & Night Count | `NightRollCall` (bed check) | Yes |
| 12 | 22:00–05:00 | Lights Out | `LightsOut` (doors locked) | Yes |

Durations equal the clock gaps, so the clock display stays truthful. At `minutesPerRealSecond = 1` a full day = **24 real minutes**.

**Block notes:**
- **Morning Count & Wake-Up** — the day starts with a formal, mandatory headcount. Inmates must be awake, standing, or visible to guards; the loudspeaker/bell signals the start of the day. The cell shakedown sweep runs during this phase.
- **Meals** — eating is highly managed: inmates move to the chow hall by housing block and have a strict **15–20 game-minute** window to get food, eat, and clear out.
- **Work / Education / Programs** — the prison becomes a small city. Inmates head to assigned locations: **jobs** (kitchen crew, laundry, facility maintenance, workshop), **education** (GED classes, vocational training), or **programs** (substance abuse groups, anger management).
- **Yard Time & Recreation** — the most flexible part of the day: outdoor yard, dayroom, TV, cards, weights, letters, or the commissary.
- **Final Lockdown & Lights Out** — inmates are ordered back to their cells for the final count. Cell doors mechanically lock, common areas close, and lights dim.

### Tuning the day length

- Raise `minutesPerRealSecond` to ~1.8 for a ~13-minute real-time day.
- Or shorten the `LightsOut` duration (the clock display will drift, which the system tolerates since duration drives progression).

## The Count

Everything stops for a headcount. **4 formal counts per day**: morning (05:00), midday (11:30), evening (16:00), and the night bed check (21:00) — plus a hook for random informal checks (future).

- A **count mismatch** (an inmate missing or unaccounted for) triggers a facility-wide **Lockdown** — everyone is confined where they stand until the discrepancy clears. This generalizes the existing night-verifier lockdown ([[Security, Heat & Alerts]]).
- Morning count includes the cell shakedown sweep; midday/evening counts are lighter presence-only checks. See [[Roll Call & Shakedown]].

## Weekends *(planned — needs day-of-week support)*

Schedules shift dramatically on Saturdays and Sundays: **no `WorkProgram` blocks**. The work slots become **Visitation** hours, **religious services**, and **extended recreation** in the dayroom or yard.

## Security levels *(design direction for later prison tiers)*

The schedule above reflects the current game's **low/medium security** facility.

- **Minimum Security** — inmates get much more autonomy, potentially off-site work (road crews) during the day.
- **Maximum Security** — movement is severely restricted: up to **23 hours a day in-cell**, with one hour of isolated recreation or shower time under direct escort.

## Phase types (`PrisonEventType`)

Implemented: `RollCall` (legacy), `Breakfast`, `Lunch`, `Dinner`, `FreeTime`, `LightsOut`, `MorningRollCall`, `NightRollCall`
Needed for this design: `WorkProgram`, `MiddayCount`, `EveningCount`, `Visitation` (weekend, later)

## Rules

- **Mandatory vs flexible:** every phase is mandatory **except `FreeTime`** (`PrisonEventRules`)
- **Morning line-up** = `RollCall` or `MorningRollCall`; **night bed phase** = `NightRollCall` or `LightsOut` (`PrisonEventExtensions`)
- **High-stakes warning:** flexible now + mandatory next → HUD warning state
- **Travel grace:** entering a mandatory phase grants **50 real seconds** (`complianceGraceRealSeconds`) where prisoners count as compliant while traveling. **Not granted** for morning line-up or initial setup.
- **Morning roll-call gate:** the phase can end **early** when every inmate's cell passes shakedown (`endMorningRollCallWhenAllAccounted = true`), with a **600 s** real-time safety cap (`morningRollCallMaxRealSeconds`). See [[Roll Call & Shakedown]].

## Implementation status

This schedule is the approved design; the Unity project still runs the old 9-phase loop. Gaps to close:

| Gap | What's needed |
|---|---|
| `WorkProgram`, `MiddayCount`, `EveningCount` phases | Extend `PrisonEventType` + `PrisonEventRules`/`PrisonEventExtensions` |
| Schedule data | Re-author `Assets/ScriptableObjects/PrisonSchedule.asset` with the 13-entry table above |
| Work zones | Kitchen / Laundry / Workshop / Classroom zones + stand points in `PrisonLocationRegistry` ([[Locations, Zones & Cells]]) |
| Midday/evening counts | Presence-only count logic (reuse `PrisonerPresence`), guard count duties ([[Guard AI]]) |
| Count-mismatch lockdown | Generalize night-verifier `RaiseLockdown` to all formal counts |
| Weekends / Visitation | Day-of-week tracking in `PrisonTimeManager`, alternate weekend schedule asset |

> ⚠️ The code defaults inside `PrisonSchedule.cs` differ from the asset (starts with legacy `RollCall`, `minutesPerRealSecond = 0.1`). The **asset is authoritative** when assigned.

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
