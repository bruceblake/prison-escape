# Systems Overview

Map of every implemented system. One note per system in this folder — keep it that way as the game grows.

## How everything connects

```
                    PrisonSchedule (asset)
                            │
                    PrisonTimeManager ──────────── OnEventChanged ────────┐
                            │                                             │
       ┌────────────┬───────┴──────┬──────────────┬───────────┐          │
       ▼            ▼              ▼              ▼           ▼          ▼
  Cell Doors   Guard Shifts   Prisoner AI   Social favors   HUDs   Fake Bed Dummy
                    │              │
                    ▼              ▼
             Guard Detection ← IsCompliant ← Zones/Registry (stand points)
                    │
                    ▼
             Escort / Arrest → SendToCell
                    │
             Shakedown Sweeper → RollCall Tracker → advances schedule
                    │
             confiscate world pickups (Contraband/Tool/Weapon)

  Loot nodes → World pickups → Inventory (6 slots) → Crafting → Escape tools
                                        │                          │
                              Social favors (items)      Vent / Stash / Fake Bed
```

## System notes

| Note | One-liner | Test status |
|---|---|---|
| [[Time & Schedule]] | The daily phase clock everything reacts to | 🟡 rules tested |
| [[Locations, Zones & Cells]] | Where inmates must be; cells and doors | 🟡 doors ✅ |
| [[Roll Call & Shakedown]] | Morning sweep, confiscation, early release | 🟡 confiscation ✅ |
| [[Guard AI]] | Patrol / detection / escort / night checks | ⚪ planned |
| [[Prisoner AI & NPCs]] | NPC routines, player compliance, personalities | ⚪ planned |
| [[Social & Reputation]] | Affinity math, tiers, favors, greetings | 🟡 math ✅ |
| [[Security, Heat & Alerts]] | Attention eye + lockdown/suspicion hooks | ✅ alerts |
| [[Player & Interaction]] | FPS controls + raycast interactions | ⚪ planned |
| [[Inventory & Items]] | 6-slot inventory + item taxonomy | ✅ core |
| [[Crafting]] | 7 recipes → escape tools | ✅ |
| [[Loot & Economy]] | Weighted loot + (dormant) wallet | 🟡 weights ✅ |
| [[Escape Routes & Mechanics]] | Vents, fake bed, stash + boundary win on `feat/escape-completion` | 🟡 routes partial, win v1 ✅ |
| [[Multiplayer & Networking]] | Riptide FPS layer, lobby, weapons | ⚪ |
| [[UI & HUD]] | Routine bar, heat eye, vitals, location, waypoint, notebook, inventory | 🟢 core HUD done (7/14) |

## The three biggest implementation gaps

1. **Escape routes geometry** — win/lose systems live on `feat/escape-completion`; vent corridors and fence cut still needed to reach the boundary ([[Roadmap & Priorities]] #1)
2. **Guard AI depth** — patrol/detection exist; full FSM polish and consequence wiring still thin
3. **Social content unauthored** — no personality or favor assets; gift/betrayal UI missing

Related: [[Game Vision & Core Loop]] · [[World Rules]] · [[Codebase Map]]
