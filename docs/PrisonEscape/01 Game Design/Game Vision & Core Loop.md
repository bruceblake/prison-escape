# Game Vision & Core Loop

## What the game is

A **prison-escape simulation** built in **Unity 6 (URP)**. Single-player core with a secondary **multiplayer FPS** layer sharing the same item/inventory code.

**The main goal is to ESCAPE the prison.** Escape is the intended primary objective — though the player is never *forced* to escape and can keep living the prison life. Every other system (schedule compliance, social standing, crafting, contraband) exists to **enable, fund, or cover for an escape attempt**.

## The prison ladder

Multiple prisons planned, in order of difficulty:

1. **Minimum Security** ← current MVP ([[Prison Layout — Minimum Security]])
2. Medium Security
3. High Security
4. Supermax

Each escape presumably graduates the player to a harder facility.

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
ESCAPE (win) — or get caught and lose progress
```

The prison day moves through timed phases (roll call, meals, free time, lights out, night checks — see [[Time & Schedule]]). The player must **be in the right place at the right time** — or fake it — while secretly preparing an escape.

## Pillars

| Pillar | Meaning |
|---|---|
| **Routine is the puzzle** | The schedule creates windows of opportunity; mastery of the routine is mastery of the game |
| **Every system feeds escape** | Social, economy, crafting, and stealth all exist to serve escape plans |
| **Appear compliant** | The tension is between what guards see and what the player is actually doing |
| **Fail-forward** | Getting caught raises heat/suspicion rather than instantly ending the run |

## Ways to escape (Minimum Security)

From the design spec (see [[Escape Routes & Mechanics]] for implementation status):

1. **Front entrance** — must get through Main Security somehow
2. **Cut through the barbed-wire fence** in the courtyard
3. **Vent network** — plumbing/electrical corridors behind the cells, traversable via unscrewable vents

## Known design gap

There is **no escape completion / win state implemented yet** — the ingredients exist (vents, fake bed, tools, guard evasion) but no `EscapeZone`/`EscapeManager` keystone. This is the highest-priority gameplay feature. See [[Roadmap & Priorities]].

## Related notes

- [[World Rules]] — the laws of the game world
- [[Prison Layout — Minimum Security]] — the MVP map
- [[Systems Overview]] — every implemented system
