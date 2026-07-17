# Codebase Map

Where code lives and how it's organized. Knowledge graph ~**2,800** nodes across ~**390** files (`graphify-out/GRAPH_REPORT.md`); no import cycles.

## Script layout

```
Assets/Scripts/
├── Shared/          # Both SP & MP
│   ├── Prison/      # Time, zones, cells, heat, wallet, labels, doors
│   ├── Career/      # Ladder: worlds, facilities, transfer, hub UI (Prison.Career)
│   ├── Social/      # v3 social hub: Respect/Trust, gangs, Talk, trade (Prison.Social)
│   ├── Crafting/    # CraftingSystem, recipes, descriptions
│   ├── Interaction/ # IInteractable, pickups, vent cover, pillow stash
│   ├── UI/          # Inventory, hotbar, notebook, HUDs, pause, EscapeEndScreenUI
│   └── Visuals/     # Low-poly / BlenderKit character pipeline
├── Singleplayer/
│   ├── AI/          # GuardFSM, GuardDetection, PrisonerAI, shakedown
│   ├── Player/      # PrisonerController, PlayerInteractor
│   ├── Items/       # ItemData SOs, screws, fake bed, spawners
│   ├── Security/    # PrisonSecurityAlerts
│   └── GameManager.cs
├── Multiplayer/     # NetworkManager (Riptide), lobby, Player/, weapons
└── Editor/          # SocialBalanceSimulatorWindow (Tools → Prison → Social → Balance Simulator)

Assets/Editor/       # Facility install, polish, doors, collision, Career/Social installers & test runners
Assets/Tests/Editor/ # EditMode suites (~195 [Test] + TestCases ≈ 256 runnable)
Assets/ScriptableObjects/  # Items, Recipes, WeaponData, PrisonSchedule
Assets/Resources/Facilities/  # FacilityDefinition SOs (10)
# Assets/Resources/Social/    # optional — created by SocialAssetInstaller (code catalogs fallback)
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
| `SocialWorld` | Social hub (replaces deleted v1 `SocialManager`) |
| `CareerSession` / `CareerWorldStore` | Active career run context + JSON worlds |
| `PrisonLevelLayoutRunner` / `PrisonFacilityInstaller` | Level generation (editor) |

## Key singletons / hubs

`PrisonTimeManager` · `PrisonLocationRegistry` · `MorningRollCallTracker` · `FormalCountMonitor` · **`SocialWorld`** · `PlayerWallet` · `NetworkManager` · `GameManager` (exec order -1000) · `CareerSession` (static)

## Career (M1–M5 on `dev`)

All under `Assets/Scripts/Shared/Career/` unless noted. Design: [[Prison Career Ladder]].

| File / group | Role |
|---|---|
| `FacilityIds` / `FacilityCatalog` / `FacilityRunState` | Canonical ids + design numbers; per-visit run state |
| `CareerWorld` / `CareerWorldStore` | Named career saves; JSON, atomic writes, migration |
| `CareerSeed` / `CareerRespectMath` / `SentenceClockMath` / `CareerTransfer` | Pure career logic (`CareerGates` is nested in `CareerTransfer`) |
| `FacilityDefinition` (SO) / `FacilityDirectory` | Tunables in `Resources/Facilities` with catalog fallback |
| `CareerSession` | Active world/facility; difficulty multipliers |
| `CareerRunBootstrap` | Scene-entry: seed, carry, day tick, autosave, sentence clock |
| `CareerTransferFlow` | Boundary / sentence → ceremony orchestration |
| `CareerMainMenuUI` / `CareerQuitConfirmUI` / `SentenceClockHUD` / `SceneTransitionScreen` | Hub, quit confirm, County HUD, loading fade |
| `Shared/UI/EscapeEndScreenUI.cs` | Transfer / sandbox end ceremony |
| `Assets/Editor/FacilityDefinitionInstaller.cs` · `CareerTestRunner.cs` | Install SOs + EditMode runner |
| `Assets/Scenes/CountyJail.unity` | County stub scene |

Hooks: `EscapeManager`, `GameManager` (loot ×, Social arrival seed, gang tag), `GuardDetection`, `MorningShakedownSweeper`, `PlayerStats.ApplyCareerCarry`, `PauseManager`.

## Social v3 (on `dev`)

Under `Assets/Scripts/Shared/Social/` (**27** scripts). System note: [[Social & Reputation]]. Spec: [[Social Ecosystem & Gangs]].

| Group | Role |
|---|---|
| `SocialWorld` | Runtime hub: roster, relationships, gangs, acts |
| `RelationshipStore` / `RelationshipMath` / `SocialActs` | Respect/Trust axes, soft cap, standing, tiers |
| `GangManager` / `GangDefinition` / `GangTerritoryMonitor` | Exclusive gangs |
| `SocialMemory` / `GossipSystem` / `SocialSimulationTicker` | Memory + ambient tick |
| `SocialInteractionMenu` / `SocialDossierUI` / `StandingBandUI` | Talk + notebook dossier |
| `TradingService` / `TradeMath` / `FavorService` / `SnitchSystem` | Economy & consequences |
| `ArchetypeDefinition` / `SocialRosterBuilder` / `SocialNameGenerator` | Identities |
| `Assets/Editor/SocialAssetInstaller.cs` · `SocialTestRunner.cs` | Optional SO install + tests |

v1 deleted: `SocialManager`, `SocialMath`, `FavorOfferDefinition`, `NPCPersonalityData`, `PrisonSocialRowUI`.

## Scene / collision repair (7/16)

| File | Role |
|---|---|
| `Shared/UI/WaypointWorldGuide.cs` | Floor path line + destination beacon (`ObjectiveWaypointUI`) |
| `Shared/Prison/CellDoorNavMeshLink.cs` | Schedule-gated doorway NavMeshLink |
| `Assets/Editor/PrisonCollisionAndCameraFixer.cs` | Colliders, NavMesh cleanup, door links, camera near-clip |
| `Assets/Editor/PrisonPolishPass.cs` · `PrisonDoorAndWaypointFixer.cs` · `PrisonBatchRunner.cs` | Polish, doors/waypoints, batch chain |

## Knowledge graph

`graphify-out/` — rebuilt on commit hooks. `GRAPH_REPORT.md` hubs/communities; `graph.html` interactive. CLI may be off PATH in PowerShell — read the report file.

## Conventions

- Pure logic in static classes (`RelationshipMath`, `TradeMath`, `CraftingSystem`, `PrisonEventRules`, career math) so it stays EditMode-testable
- ScriptableObjects for designer data (schedule, items, recipes, facility defs; social archetypes/gangs when installed)
- Editor runners for repeatable scene generation — never hand-edit generated objects
- Namespaces: `Prison` · `Prison.Career` · `Prison.Social` · `Prison.Visuals` · `Prison.Tests`

Related: [[Systems Overview]] · [[Editor Tooling]] · [[Testing & QA]]
