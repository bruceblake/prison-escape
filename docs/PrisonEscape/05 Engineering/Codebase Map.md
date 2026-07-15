# Codebase Map

Where code lives and how it's organized. ~2,200 graph nodes across 253 files; no import cycles.

## Script layout

```
Assets/Scripts/
├── Shared/          # Both SP & MP
│   ├── Prison/      # Time, zones, cells, social, heat, wallet, labels
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

## Knowledge graph

`graphify-out/` contains a code knowledge graph (rebuilt on commit hooks). `GRAPH_REPORT.md` lists hubs/communities; `graph.html` is an interactive view. The graphify CLI isn't on PATH in this shell — read the report file directly.

## Conventions

- Pure logic goes in static classes (`SocialMath`, `CraftingSystem`, `PrisonEventRules`) so it stays EditMode-testable
- ScriptableObjects for designer data (schedule, items, recipes, personalities, favors)
- Editor runners (menu items) for repeatable scene generation — never hand-edit generated objects; structural layout walls use `CreateWallBlock` (colliders on), props use `CreateBlock` (visual only)
- Namespace `Prison` / `Prison.Visuals` / `Prison.Tests`

Related: [[Systems Overview]] · [[Editor Tooling]] · [[Testing & QA]]
