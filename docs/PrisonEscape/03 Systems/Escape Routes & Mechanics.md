# Escape Routes & Mechanics

The main goal of the game. The *ingredients* of escape are implemented; the **win condition is not** (top roadmap item).

## Designed routes (Minimum Security)

| Route | Design | Implementation status |
|---|---|---|
| **Vent network** | Plumbing/electrical corridors behind cell rows, entered via unscrewable vents | Vent mechanic ✅; corridor geometry not built in `PrisonLevel1` yet |
| **Courtyard fence** | Cut through barbed wire with Wire Cutters | Item + recipe ✅; fence & cut interaction ❌ |
| **Front entrance** | Get through Main Security somehow | Room exists; mechanic undesigned — spec needed |

## Implemented mechanics

### Vent route
- Vent cover holds **4 screws** (scene-authored array)
- Hold LMB **2 s** per screw (**~1.33 s** with Screwdriver's 1.5× modifier); requires the tool equipped
- Screw animation: 1080° rotation, 0.05 move-out; last screw → cover slides open (1 unit, 0.5 s) and enables the passage collider
- ⚠️ Scene note: `passageCollider` currently unassigned on the `SinglePlayerScene` vent

### Fake bed dummy (defeat night checks)
- Craft (1 Pillow + 1 Bed Sheet), equip, press F at your `CellBed`
- Night verifier's bed-presence sphere accepts the dummy → check passes while you roam
- At morning line-up the dummy is **discovered**: raises Suspicion alert, then destroyed

### Pillow stash (hide contraband)
- Press F at the pillow: hide the equipped item (capacity **1**) or retrieve it
- Stashed items survive the morning shakedown (sweep only destroys world pickups)
- Proximity UI shows stored item or "Empty — equip an item to hide it"

### Cell doors
- Open during day phases, closed during Lights Out / Night Roll Call — the nightly lock-in that makes the vent/dummy combo necessary ([[Locations, Zones & Cells]])

## The escape loop (as designed)

```
Craft Screwdriver → stash it through shakedowns
→ craft Fake Bed Dummy → place at Lights Out
→ open vent while beds "pass" checks
→ traverse vent corridor → reach exit → WIN (not yet implemented)
```

## Escape completion (implemented v1)

The win/lose keystone exists — see [[Escape Completion System]]:

- **`EscapeBoundary`** ring outside the walls → crossing it = win (end screen + stats + ladder framing)
- **`RestrictedZone`** volumes (perimeter band always; cafeteria/workshop at night) — spotted inside → **caught escaping**
- **`EscapeManager`** runs the caught flow: confiscation (stash survives), solitary block in Main Security, −20 MH / −10 STR, day skip, 2-day suspicion
- Route geometry (vent corridors, fence cut) is what makes the boundary *reachable* — next on [[Roadmap & Priorities]]

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Singleplayer/Items/InteractableScrew.cs` + `Shared/Interaction/VentCover.cs` | Vent route |
| `Assets/Scripts/Singleplayer/Interaction/CellBed.cs` + `Items/FakeBedDummy.cs` | Fake bed |
| `Assets/Scripts/Shared/Interaction/PillowStash.cs` | Stash |
| `Assets/Scripts/Singleplayer/Security/PrisonSecurityAlerts.cs` | Failure hooks |

Related: [[Game Vision & Core Loop]] · [[Crafting]] · [[Guard AI]] · [[Security, Heat & Alerts]] · [[Prison Layout — Minimum Security]]
