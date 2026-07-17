# Prisoner AI & NPCs

NPC inmates follow the daily routine via NavMesh; the player follows the same compliance contract through zones and distances. Both implement `IPrisoner` so guards treat them identically.

## NPC routine (`PrisonerAI`)

1. Register as cell occupant; subscribe to schedule changes
2. On each phase change → resolve a **deterministic, spread** stand position from the registry (`GetStandPointForIndex` + lateral offset by `cellIndex`) → `SetDestination`
3. Compliant when within **0.85 m** (`arriveDistance`) of the resolved stand, or when travel grace / post-shakedown release applies. Near-zero velocity close to the stand also counts (crowd NavMesh never hits the exact point).
4. **On arrival:** `NavMeshAgent.isStopped = true` + `ResetPath` so idle NPCs stand still instead of circling a shared stand
5. After morning shakedown clears their cell → path toward the **next** phase destination early
6. During `WorkProgram` phases the registry routes everyone to the Workshop zone (per-inmate kitchen/laundry/classroom assignments: follow-up — [[Locations, Zones & Cells]])
7. When arrested: agent disabled; `SendToCell` teleports to cell spawn, releases after **1 s**

## Player compliance (`PrisonerController`)

- Compliant within **3 m** (`compliantDistance`) of the stand point
- Zone-based rules: own Cell (all counts / night), Cafeteria (meals), Yard **or** Cafeteria (free time), Workshop during `WorkProgram` ([[Time & Schedule]])
- Morning count also accepts standing inside the cell interior sphere
- Travel grace ([[Time & Schedule]]) → compliant while moving
- `MovementBlocked` (arrest) disables the movement controller

## Identities & social (v3 on `dev`)

Every NPC (prisoner *and* guard) gets a generated **`NPCIdentity`** — name, archetype (Shot-Caller, Soldier, Hustler, Old-Timer, Bruiser, Snitch, Loner / guard archetypes), five trait axes, gang affiliation — seeded from the visit/world seed. `NPCPersonalityData` (v1) is **deleted**. Design: **[[Social Ecosystem & Gangs]]** · system: [[Social & Reputation]].

Ambient social: territory warn-offs, chats/arguments (`SocialSimulationTicker`). Player interaction: [[Talk Menu & NPC Profile]] via `PrisonerSocialPresenter` → `SocialInteractionMenu`. Strategy view: [[Social Dossier — Relationships & Gangs]] (`SocialDossierUI`).

## Spawning

`GameManager` (execution order -1000) seeds RNG (`worldSeed` or random), spawns the player into cell index 0, NPC prisoners into their cells, and guards from the spawn table. NPC prefab: `Assets/Prefabs/NPCs/Prisoner_NPC.prefab` (orange jumpsuit + stripes, `PrisonerSocialPresenter`, name label "Inmate").

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Singleplayer/AI/PrisonerAI.cs` | NPC routine |
| `Assets/Scripts/Singleplayer/Player/PrisonerController.cs` | Player compliance |
| `Assets/Scripts/Shared/Prison/IPrisoner.cs` | Shared contract |
| `Assets/Scripts/Shared/Social/SocialRosterBuilder.cs` / `SocialTypes.cs` | Identities + roster |
| `Assets/Scripts/Shared/Social/SocialWorld.cs` | Social hub (built from `GameManager`) |
| `Assets/Scripts/Singleplayer/GameManager.cs` | Spawning + world boot + Social bridge |

Related: [[Locations, Zones & Cells]] · [[Guard AI]] · [[Social & Reputation]] · [[Time & Schedule]]
