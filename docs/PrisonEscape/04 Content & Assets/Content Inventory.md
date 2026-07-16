# Content Inventory

What exists in `Assets/` — scenes, prefabs, materials, models. See [[Item Catalog]] for items and [[Character Visuals]] for the character pipeline.

## Scenes

| Scene | Role | In build? |
|---|---|---|
| `Scenes/MainMenu` | Lobby/menu (Steam, find game, map vote) | ✅ |
| `Scenes/Warehouse` / `Scenes/Factory` | Multiplayer FPS maps | ✅ |
| `Scenes/PrisonLevel1` | **Current prison level** (layout runner target) | ⚠️ disabled |
| `Scenes/SinglePlayerScene` | Older SP prison scene | disabled (+ root & `_Recovery` copies; one recovery copy enabled) |
| `Scenes/Warmup` | Pre-match role/timer UI | not listed |
| `Scenes/Game` | Legacy | disabled |

> Cleanup candidates: duplicate `SinglePlayerScene` copies; build list should eventually enable `PrisonLevel1` and drop recovery entries.

## Prefabs (`Assets/Prefabs/`, 22)

| Group | Prefabs |
|---|---|
| Players | `Player` (networked avatar), `LocalPlayer` (first-person local), `GhostPrefab` (prediction ghost) |
| AI | `AIPrefabs/Guard`, `AIPrefabs/Prisoner`, `NPCs/Prisoner_NPC`, waypoints 1–3 |
| Combat/FX | `assault1/2`, `Bomb`, `BulletTracer`, `Hitmarker`, `Effects/BulletImpactStoneEffect`, `Particle System` |
| Items/UI | `PartA/B` (legacy pickups), `Slot`, `MapButton`, `PlayerEntryPrefab`, `PlayerListEntryPrefab` |

## Materials

### Prison set (`Materials/Prison/` + `Accents/`)
Concrete walls (0.28, 0.27, 0.25) / ceilings / floor tile / cafeteria tile, metal bars, shelf/sink metal, porcelain, bed mattress/blanket/pillow, emissive light fixture (~2.5× warm emission), accents: caution yellow, panel white, security red, sign blue. Procedural **cinderblock** (`T_CinderBlockWall`) and **concrete grain** (`T_ConcreteGrain`) textures applied via Polish Pass (7/15).

### Character set (`Materials/Characters/`)
Shared skin (0.82, 0.62, 0.48); per-role palettes:

| Role | Clothing | Accent | Label |
|---|---|---|---|
| Player | blue (0.18, 0.35, 0.52) | teal vest/bandana | "You" |
| Guard | navy (0.14, 0.24, 0.42) | gold badge/epaulettes/cap | "Guard" |
| Prisoner | orange (0.92, 0.48, 0.18) | white stripes/pocket | "Inmate" |

## Models & props

`Models/BlenderKit/` — **the canonical asset kit** (see [[Blender Asset Kit]]): 61 modular pieces (walls, doorways, cells, furniture, props, fence), 25 item pickups in `Items/`, `PrisonFacility.fbx` (whole assembled prison), tileable textures in `Textures/`. Master file: `ArtSource/PrisonKit.blend`.

Legacy `Models/`: CafeteriaTable, ConcretePillar, GymBench, PrisonBed, PrisonCellDoor, PrisonCellModule, StorageCrate + `Modular/` kit — superseded by BlenderKit; candidates for removal once the level uses BlenderKit meshes.
Note: the current level build still **scratch-builds furniture from cubes** via the layout runner; migrating it to BlenderKit FBX is an open task.

## Third-party / support

- `Low Poly Guns/` (gun pack), `UnityTechnologies/EffectExamples` (VFX)
- `Sprites/Icons/` — ~26 item icons matching item names
- `Sound Effects/`, `TextMesh Pro/`, `Settings/` (URP), `Maps/` (lobby thumbnails)
- `StreamingAssets/Server/3dgameserver.exe` — bundled dedicated server

## Packages (key)

URP 17.4.0 · ProBuilder 6.0.9 · AI Navigation 2.0.12 · Input System 1.19.0 · Steamworks.NET (git) · Test Framework 1.6.0 · Riptide (embedded DLL, not UPM)

Related: [[Prison Layout — Minimum Security]] · [[Editor Tooling]] · [[Multiplayer & Networking]]
