# Prisoner AI & NPCs

NPC inmates follow the daily routine via NavMesh; the player follows the same compliance contract through zones and distances. Both implement `IPrisoner` so guards treat them identically.

## NPC routine (`PrisonerAI`)

1. Register as cell occupant; subscribe to schedule changes
2. On each phase change → get stand point from the registry → `SetDestination`
3. Compliant when within **0.5 m** (`arriveDistance`) of the stand point, or when travel grace / post-shakedown release applies
4. After morning shakedown clears their cell → path toward the **next** phase destination early
5. When arrested: agent disabled; `SendToCell` teleports to cell spawn, releases after **1 s**

## Player compliance (`PrisonerController`)

- Compliant within **3 m** (`compliantDistance`) of the stand point
- Zone-based rules: own Cell (roll calls / night), Cafeteria (meals), Yard **or** Cafeteria (free time)
- Morning roll call also accepts standing inside the cell interior sphere
- Travel grace ([[Time & Schedule]]) → compliant while moving
- `MovementBlocked` (arrest) disables the movement controller

## Personalities (`NPCPersonalityData`, ScriptableObject)

| Field | Default | Wired to gameplay? |
|---|---|---|
| `affinityGainMultiplier` | 1 | ✅ scales positive affinity ([[Social & Reputation]]) |
| `betrayalPenalty` | -50 | ✅ overrides betrayal delta |
| `favoredItems` | list | ✅ gift doubling |
| `snitchThreshold` | -50 | ⚠️ UI hint only |
| `minAffinityToInteract` | -100 | ⚠️ not gated yet |

> ⚠️ **No personality `.asset` instances exist yet** — the type is ready but content needs authoring (`Create → Prison/Social/Personality`).

## Spawning

`GameManager` (execution order -1000) seeds RNG (`worldSeed` or random), spawns the player into cell index 0, NPC prisoners into their cells, and guards from the spawn table. NPC prefab: `Assets/Prefabs/NPCs/Prisoner_NPC.prefab` (orange jumpsuit + stripes, `PrisonerSocialPresenter`, name label "Inmate").

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Singleplayer/AI/PrisonerAI.cs` | NPC routine |
| `Assets/Scripts/Singleplayer/Player/PrisonerController.cs` | Player compliance |
| `Assets/Scripts/Shared/Prison/IPrisoner.cs` | Shared contract |
| `Assets/Scripts/Shared/Prison/NPCPersonalityData.cs` | Personality SO |
| `Assets/Scripts/Singleplayer/GameManager.cs` | Spawning + world boot |

Related: [[Locations, Zones & Cells]] · [[Guard AI]] · [[Social & Reputation]] · [[Time & Schedule]]
