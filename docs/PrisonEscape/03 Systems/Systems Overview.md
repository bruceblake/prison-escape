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
                    │              │              │
                    ▼              ▼              ▼
             Guard Detection ← IsCompliant   SocialWorld (Respect/Trust)
                    │                    Talk / Dossier / Gangs / Trade
                    ▼
             Escort / Arrest → SendToCell
                    │
             Shakedown Sweeper → RollCall Tracker → advances schedule
                    │
             confiscate world pickups (Contraband/Tool/Weapon)

  Loot nodes → World pickups → Inventory (6 slots) → Crafting → Escape tools
                                        │                          │
                              Social favors (items)      Vent / Stash / Fake Bed

  CareerSession ← CareerWorldStore (JSON worlds)
       │                │
       ▼                ▼
  FacilityDefinition  MainMenu hub → enter facility → CareerRunBootstrap
       │
       ▼
  Boundary / County clock → CareerTransferFlow → EscapeEndScreenUI ceremony
```

## System notes

| Note | One-liner | Test status |
|---|---|---|
| [[Time & Schedule]] | The daily phase clock everything reacts to — **realistic 13-phase count-driven day (implemented 7/15)** | 🟡 rules tested |
| [[Locations, Zones & Cells]] | Where inmates must be; cells and doors | 🟡 doors ✅ |
| [[Roll Call & Shakedown]] | Morning sweep, confiscation, early release | 🟡 confiscation ✅ |
| [[Guard AI]] | Patrol / detection / escort / night checks — facility difficulty + career detection mult wired | ⚪ polish |
| [[Prisoner AI & NPCs]] | NPC routines, player compliance; social identities via [[Social & Reputation]] | ⚪ polish |
| [[Social & Reputation]] | **v3 on `dev`**: Respect/Trust, Standing bands, gangs, Talk, dossier, trade/bribes/favors/snitch — design: [[Social Ecosystem & Gangs]] | ✅ core EditMode |
| [[Security, Heat & Alerts]] | Attention eye + lockdown/suspicion hooks; snitch tips feed shakedowns | ✅ alerts |
| [[Player & Interaction]] | FPS controls + raycast interactions | ⚪ planned |
| [[Inventory & Items]] | 6-slot inventory + item taxonomy | ✅ core |
| [[Crafting]] | 7 recipes → escape tools | ✅ |
| [[Loot & Economy]] | Weighted loot + **live wallet** (trade / bribes / jobs / favors) | 🟡 weights ✅ |
| [[Escape Routes & Mechanics]] | Vents, fake bed, stash + boundary — career path = caught-transfer ([[Prison Career Ladder]]); sandbox = YOU ESCAPED | 🟡 routes partial, win ✅ |
| [[Multiplayer & Networking]] | Riptide FPS layer, lobby, weapons | ⚪ |
| [[UI & HUD]] | Routine bar, heat eye, vitals, location, waypoint, notebook, inventory, sentence clock | 🟢 core HUD done |

## Meta-progression (on `dev`)

| Spec | Status |
|---|---|
| [[Prison Career Ladder]] | ✅ M1–M5 code; M6+ facility scenes |
| [[World Saves & Start Screen]] | ✅ worlds JSON + MainMenu hub |
| [[Facility Transfer & Graduation]] | ✅ ceremony + County clock + carry/reset |

## The three biggest remaining gaps

1. **Escape routes geometry** — win/lose + transfer systems are on `dev`; vent corridors and fence cut still needed to reach the boundary ([[Roadmap & Priorities]] #1 under Next)
2. **Guard AI depth** — patrol/detection exist; full FSM polish and per-guard Trust→detection still thin
3. **Career / Social polish** — `Resources/Social/` ScriptableObjects not installed (code fallbacks); M6+ facility scenes beyond County stub; overhead Talk markers / dossier widgets backlog

Related: [[Game Vision & Core Loop]] · [[World Rules]] · [[Codebase Map]]
