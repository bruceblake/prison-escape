# Codebase Map

Where code lives and how it's organized. ~2,200 graph nodes across 253 files; no import cycles.

## Script layout

```
Assets/Scripts/
├── Shared/          # Both SP & MP
│   ├── Prison/      # Time, zones, cells, social, heat, wallet, labels
│   ├── Career/      # Prison Career Ladder: worlds, facility catalog/SOs, transfer, hub UI (namespace Prison.Career)
│   ├── Crafting/    # CraftingSystem, recipes, descriptions
│   ├── Interaction/ # IInteractable, pickups, vent cover, pillow stash
│   ├── UI/          # Inventory, hotbar, notebook, HUDs (vitals, location, waypoint), pause, UIMenuFocus
│   └── Visuals/     # Low-poly character pipeline
├── Singleplayer/
│   ├── AI/          # GuardFSM, GuardDetection, PrisonerAI, shakedown
│   ├── Player/      # PrisonerController, PlayerInteractor
│   ├── Items/       # ItemData SOs, screws, fake bed, spawners
│   ├── Security/    # PrisonSecurityAlerts
│   └── GameManager.cs
├── Multiplayer/     # NetworkManager (Riptide), lobby, Player/, weapons
└── Editor/          # SocialBalanceSimulatorWindow

Assets/Editor/       # PrisonLevelLayoutRunner, CharacterVisualSetupRunner, PrisonOverhaulRunner
Assets/Tests/Editor/ # EditMode suites (153 tests incl. EscapeSystemTests)
Assets/ScriptableObjects/  # Items, Recipes, WeaponData, PrisonSchedule
```

No `.asmdef` files — everything compiles into `Assembly-CSharp` (deliberate: zero-setup EditMode tests; PlayMode tests deferred until asmdef migration).

## Core abstractions (most-connected nodes)

| Class | Role |
|---|---|
| `PrisonTimeManager` | The clock — nearly every system subscribes to `OnEventChanged` |
| `PlayerInventory` | Bridge between items, crafting, UI, interaction, favors |
| `RoutineNowNextBarUI` | Primary HUD; consumes most sim state |
| `ItemData` (+ Tool/Part/Weapon) | Item taxonomy everything shares |
| `PrisonLocationRegistry` | Spatial truth: cells, zones, stand points |
| `IPrisoner` | Contract guards use on both player & NPCs |
| `PrisonLevelLayoutRunner` | Level generator (editor) |

## Key singletons

`PrisonTimeManager` · `PrisonLocationRegistry` · `MorningRollCallTracker` · `FormalCountMonitor` (midday/evening count → lockdown) · `SocialManager` · `PlayerWallet` · `NetworkManager` · `GameManager` (exec order -1000)

## Added 7/15/2026 (realistic schedule + scene repair)

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Prison/FormalCountMonitor.cs` | Cell-count mismatch → `RaiseLockdown` |
| `Assets/Editor/PrisonDoorAndWaypointFixer.cs` | *Prison → Fix Cell Doors & Waypoints* — door realign/dedupe, stand points, waypoint snap, registry, save |
| `Assets/Editor/PrisonPolishPass.cs` | *Prison → Polish Pass* — prop colliders, procedural textures, extra props, ambient, NavMesh rebake, patrol route generation |
| `Assets/Editor/PrisonBatchRunner.cs` | `RunFullSetup` — batchmode chain (character visuals → polish → fixer → save) |
| `Assets/Animations/Characters/Char_Locomotion_{Prisoner,Guard}.controller` | Per-role locomotion + Jump (generated) |

## Added 7/15/2026 (Prison Career Ladder M1–M5)

All under `Assets/Scripts/Shared/Career/` unless noted. Design: [[Prison Career Ladder]].

| File | Role |
|---|---|
| `FacilityIds` / `FacilityCatalog` | Canonical ids + locked design numbers for the 10 slots (dev sandbox + ladder 0–8) |
| `CareerWorld` / `CareerWorldStore` | Named career saves; JSON per world, atomic writes, schema migration |
| `CareerSeed` / `CareerRespectMath` / `SentenceClockMath` / `CareerTransfer` / `CareerGates` | Pure career logic (EditMode-tested) |
| `FacilityDefinition` (SO) / `FacilityDirectory` | Tunable per-facility assets in `Resources/Facilities` with catalog fallback |
| `CareerSession` | Static run context: active world/facility, difficulty multipliers (all default 1 outside careers) |
| `CareerRunBootstrap` | Scene-entry glue: per-visit seed, carry apply, Morning-Count day tick, autosave, sentence clock |
| `CareerTransferFlow` / `EscapeEndScreenUI` (UI/) | Transfer orchestration + ceremony screen rebuild |
| `CareerMainMenuUI` / `CareerQuitConfirmUI` / `SentenceClockHUD` | Hub UI over MainMenu scene, pause-quit confirm, County HUD line |
| `Assets/Editor/FacilityDefinitionInstaller.cs` | *Tools → Prison → Career → Install Facility Definitions* |
| `Assets/Editor/CareerTestRunner.cs` | *Tools → Prison → Career → Run Career EditMode Tests* (writes result log) |
| `Assets/Scenes/CountyJail.unity` | County's own scene (stub copy of the dev layout; free to diverge) |

Career hooks in existing code: `EscapeManager` (boundary → transfer flow, −2 respect on capture), `GameManager` (loot abundance ×, arrival-affinity seed), `GuardDetection` (facility detection-range ×), `MorningShakedownSweeper` (strictness skip), `PlayerStats.ApplyCareerCarry`, `PauseManager` (QUIT TO PRISON SELECT).

## Added 7/16/2026 (loading screen, waypoint guidance, collision repair)

| File | Role |
|---|---|
| `Shared/Career/SceneTransitionScreen.cs` | Async loading screen (facility title + progress bar) for every career scene change |
| `Shared/UI/WaypointWorldGuide.cs` | Physical objective guidance: caution-yellow route line on the floor along the NavMesh path + pulsing beacon (beam + ground ring) at the destination; driven by `ObjectiveWaypointUI` |
| `Shared/Prison/CellDoorNavMeshLink.cs` | Doorway `NavMeshLink` gated by the door schedule — the only cell↔corridor NavMesh connection; active only while the door is open |
| `Assets/Editor/PrisonCollisionAndCameraFixer.cs` | *Prison → Fix Collision & Camera Clipping* — missing MeshColliders (438/scene), duplicate `GlobalNavMesh` removal, legacy scene-bake clear, doors-open rebake, door obstacles+links, camera near-clip → 0.05 |

**Collision root causes found (7/16):** a stale duplicate `GlobalNavMesh (1)` surface plus a legacy built-in scene bake unioned into the runtime NavMesh and still contained the pre-wall layout (agents pathed straight through cell walls); 438 renderers per facility scene (lights, pipes, ducts, props, signs, doors) had no colliders; the Humanoid bake radius of 0.5 sealed the 1.2 m cell doorways (lowered to 0.3 in ProjectSettings — doorways keep a 0.6 m channel, 0.1 m walls still block at voxel 0.05); cell interiors connect to corridors exclusively via the schedule-gated door links; `LocalPlayer` camera near-clip was 0.3 (saw through walls when hugging them) → 0.05. Guards/inmates already carried CapsuleColliders — their wall-clipping was entirely the NavMesh, not physics.

## Knowledge graph

`graphify-out/` contains a code knowledge graph (rebuilt on commit hooks). `GRAPH_REPORT.md` lists hubs/communities; `graph.html` is an interactive view. The graphify CLI isn't on PATH in this shell — read the report file directly.

## Conventions

- Pure logic goes in static classes (`SocialMath`, `CraftingSystem`, `PrisonEventRules`) so it stays EditMode-testable
- ScriptableObjects for designer data (schedule, items, recipes, personalities, favors)
- Editor runners (menu items) for repeatable scene generation — never hand-edit generated objects; structural layout walls use `CreateWallBlock` (colliders on), props use `CreateBlock` (visual only)
- Namespace `Prison` / `Prison.Visuals` / `Prison.Tests`

Related: [[Systems Overview]] · [[Editor Tooling]] · [[Testing & QA]]
