# Game Vision & Core Loop

## What the game is

A **prison-escape simulation** built in **Unity 6 (URP)**. Single-player core with a secondary **multiplayer FPS** layer sharing the same item/inventory code.

**The main goal is to ESCAPE the prison.** Escape is the intended primary objective — though the player is never *forced* to escape and can keep living the prison life. Every other system (schedule compliance, social standing, crafting, contraband) exists to **enable, fund, or cover for an escape attempt**.

**Escape is never freedom until the very top.** Crossing the wall gets you *caught and transferred* to the next, harder facility — the career ladder itself is the game ([[Prison Career Ladder]]).

## The prison career ladder

One career, nine facilities, strictly increasing difficulty ([[Prison Career Ladder]] — full catalog, curves, transfer rules):

1. **County Detention Center** — start here; escape *or* serve the 7-day sentence to graduate
2. **State** — Minimum → Medium → Maximum
3. **Federal** — Camp → Low → Medium → High → **ADX** (escape it = career win; the world stays playable)

The current Minimum-Security prison ([[Prison Layout — Minimum Security]]) is the **Dev Sandbox** — the layout/tooling/playtest facility, not a rung on the career ladder.

Careers live in **named world saves**; only unlocked facilities are enterable (locked = black silhouettes), and revisiting an easier prison to farm cash/respect is a legitimate strategy — global carry (money, respect, gang, stats, recipes) persists while each run's local state starts fresh ([[World Saves & Start Screen]] · [[Facility Transfer & Graduation]]).

## Core loop (single-player)

```
Follow the daily routine (or appear to)
        ↓
Learn the schedule, guard patterns, and map
        ↓
Acquire: money, items, contraband, allies
        ↓
Craft tools → open escape routes → hide the evidence
        ↓
Defeat night checks / shakedowns / guard detection
        ↓
CROSS THE WALL → caught & transferred UP the ladder
(or get caught attempting → solitary, suspicion)
```

The prison day is micro-managed to the minute around **headcounts**: morning count, meals, work/education/program blocks, midday and evening counts, yard & recreation, final lockdown, lights out (see [[Time & Schedule]]). The player must **be in the right place at the right time** — or fake it — while secretly preparing an escape. Work assignments and the 17:00–21:00 recreation window are where the plan gets built.

## Pillars

| Pillar | Meaning |
|---|---|
| **Routine is the puzzle** | The schedule creates windows of opportunity; mastery of the routine is mastery of the game |
| **Every system feeds escape** | Social, economy, crafting, and stealth all exist to serve escape plans |
| **Appear compliant** | The tension is between what guards see and what the player is actually doing |
| **Fail-forward** | Getting caught raises heat/suspicion rather than instantly ending the run |
| **Time investment is strategy** | Easier facilities have better money/loot/favor rates; harder ones demand more cash, favors, and power to open routes. Leaving early is possible — and punishing later ([[Prison Career Ladder]]) |

## Ways to escape (Dev Sandbox / Minimum Security)

From the design spec (see [[Escape Routes & Mechanics]] for implementation status):

1. **Front entrance** — must get through Main Security somehow
2. **Cut through the barbed-wire fence** in the courtyard
3. **Vent network** — plumbing/electrical corridors behind the cells, traversable via unscrewable vents

## Known design gap

Escape **systems** are live ([[Escape Completion System]] · [[Facility Transfer & Graduation]]): career facilities end in **CAUGHT — TRANSFERRED** / sentence / career-cleared ceremonies; only the Dev Sandbox still shows **YOU ESCAPED**. The remaining gap is **route geometry** — vent corridors and fence cut so the boundary is reachable in normal play ([[Roadmap & Priorities]]).

## Related notes

- [[Prison Career Ladder]] — the career-spanning progression this note summarizes
- [[World Rules]] — the laws of the game world
- [[Prison Layout — Minimum Security]] — the Dev Sandbox map
- [[Systems Overview]] — every implemented system
