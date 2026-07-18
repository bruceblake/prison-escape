# Item Catalog

Every item asset under `Assets/ScriptableObjects/` (mostly `Items/`). Rarity drives loot weight (Common 60 / Uncommon 25 / Rare 10 / Legendary 5 — see [[Loot & Economy]]).

## How many can spawn?

| Pool | Count | Notes |
|---|---|---|
| **ItemData assets** | **~27** | 25 in `Items/` + Metal Rod + Flat Metal at ScriptableObjects root (plus duplicates MetalScrap/WoodScrap) |
| **In world loot tables** | **16 unique** | Paperclip, Soap, Plastic Bottle, Rag, Wood Scrap, Metal Scrap, Metal Rod, Flat Metal, Coin, Charcoal, Duct Tape, Wire, Glass Bottle, Alcohol, Mirror, File |
| **Not in world loot tables** | rest | Tools/crafted (Screwdriver, Shovel, Wire Cutters, Ladder, Grappling Hook, Molotov, Fake Bed Dummy, Pillow, Bed Sheet, …) — craft or find later |

Runtime: `WorldLootBootstrap` builds ~185 `ItemSpawnNode`s; `GameManager.PopulateWorldSpawns` typically creates **~500** floor pickups (check Console for `World loot: spawned N`).

> Pickup models: `Assets/Prefabs/BlenderKit/Items/SM_Item_*.prefab`. Spawns are **normalized to ~0.4 m** longest axis via `SpawnPlacementUtility.FitWorldPickupOnFloor` (replaces flat 6× boost).

## Crafting parts

| Item | Rarity | Weight × | Used in |
|---|---|---|---|
| Paperclip | Common | 1 | — |
| Soap | Common | 1 | — |
| Plastic Bottle | Common | 1.2 | — |
| Rag | Common | 1.5 | Molotov |
| Pillow | Common | 1 | Fake Bed Dummy |
| Bed Sheet | Common | 1 | Fake Bed Dummy |
| Wood Scrap | Common | 1 | Shovel, Ladder |
| Metal Rod | Common | 1 | Screwdriver |
| Flat Metal | Common | 1 | Screwdriver |
| Coin | Uncommon | 0.5 | ⚠️ not wired to wallet |
| Charcoal | Uncommon | 1 | — |
| Duct Tape | Uncommon | 1 | Shovel, Wire Cutters, Ladder, Grappling Hook |
| Wire | Uncommon | 0.8 | Grappling Hook |
| Metal Scrap | Uncommon | 1 | Shovel, Wire Cutters, Grappling Hook |
| Glass Bottle | Rare | 1 | Molotov |
| Alcohol | Rare | 0.5 | Molotov |
| Mirror | Rare | 0.5 | — |
| File | Rare | 0.3 | Wire Cutters |

## Tools

| Item | Rarity | Escape purpose |
|---|---|---|
| Screwdriver | Uncommon | Vent screws (1.5× hold speed, durability 100) |
| Fake Bed Dummy | Uncommon | Night bed-check defeat |
| Shovel | Rare | Digging (route pending) |
| Wire Cutters | Rare | Fence cut (route pending) |
| Ladder | Rare | Climbing (route pending) |
| Grappling Hook | Legendary | Climbing (route pending) |
| Molotov | Legendary | Distraction (⚠️ categorized Tool, not Consumable) |

## Weapons

| Item | Notes |
|---|---|
| AK-47 (`WeaponData`) | Multiplayer FPS gun, gunIndex 0 |

## Data hygiene issues to fix

- Duplicate assets: `Metal Scrap`/`MetalScrap`, `Wood Scrap`/`WoodScrap` — consolidate
- `networkId` collisions (several 0s; Metal Rod=1 vs AK-47=1)
- Items with no recipe yet still spawn from world loot tables (Paperclip, Soap, Plastic Bottle, Charcoal, Mirror, Coin) — see [[Loot & Economy]] room pools.

Related: [[Inventory & Items]] · [[Crafting]] · [[Loot & Economy]]
