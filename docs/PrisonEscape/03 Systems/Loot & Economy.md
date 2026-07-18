# Loot & Economy

Where escape ingredients come from, and the live cash system.

## Loot rolls (`LootTable`, weights tested)

Weighted random: final weight = rarity base × item `weightMultiplier` (min 0.01).

| Rarity | Base weight |
|---|---|
| Common | 60 |
| Uncommon | 25 |
| Rare | 10 |
| Legendary | 5 |

## Spawn surfaces

| System | Behavior | Status |
|---|---|---|
| `ItemSpawnNode` | `GameManager.PopulateWorldSpawns` rolls per node (`spawnChance` × career abundance) → instantiate `worldPrefab`, normalized via `SpawnPlacementUtility.FitWorldPickupOnFloor` (~0.4 m footprint) | ✅ Live — runtime `WorldLootBootstrap` + scene `WorldSpawns` nodes (**Prison → Setup Items & World Loot** to bake scene markers) |
| `WorldContainer` | Rolls 1–3 items into a browse list | ⚠️ Search shows names but **doesn't transfer to inventory** yet |
| `ContrabandSpawner` (legacy) | 6 items at random points via `ItemType` enum → prefab map | Legacy path |

World seed: `GameManager.worldSeed` (or career visit seed) seeds `Random` before spawns — same seed = same loot layout. Facility loot-abundance multiplier applies on career runs.

**Loot tables** — six room pools: `Assets/Resources/LootTables/` (runtime load for `WorldLootBootstrap`) and mirrored under `Assets/ScriptableObjects/LootTables/` (editor setup runner). Pools: Common, Yard, Kitchen, Workshop, Laundry, RareHidden — weights 60/25/10/5 via `LootTable.GetRarityBaseWeight` × per-item `weightMultiplier`.

## Economy (`PlayerWallet`) — live on `dev`

- Balance ≥ 0, NaN/Inf rejected; `SetContrabandCashState` flags dirty money (UI tint)
- Primary HUD: `PlayerVitalsHUD` cash line (legacy `CashUIController` superseded)

### Sources & sinks

| Path | Where |
|---|---|
| Trade buy / sell | `SocialInteractionMenu` Trade tab (stock from `TradingService`; cash via `PlayerWallet`) |
| Guard bribes | `SocialWorld.BribeCorrupt` |
| Favor cash costs / payouts | `SocialFavorRuntime` / `SocialWorld` |
| Light job pay | `PrisonJobPaymaster` |
| Career carry restore | `CareerRunBootstrap` / global cash on transfer |

Syndicate under-bed store delivery after morning count is part of the Social economy path when stock is active.

**Career scaling (7/17):** buy/source prices × `CareerSession.TradePriceMult`, bribes × `BribeCostMult`, stipend + favor payouts × `CashIncomeMult` via `TradeMath.ApplyFacilityPriceMult` — all default to 1 outside a career run ([[Prison Career Ladder]] economy table).

Design detail (prices, stock refresh, bribe tiers): [[Social Ecosystem & Gangs]] · [[Social & Reputation]].

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Prison/LootTable.cs` / `ItemSpawnNode.cs` / `WorldLootBootstrap.cs` / `SpawnPlacementUtility.cs` | Loot spawn + floor snap + pickup scale |
| `Assets/Scripts/Singleplayer/Items/ContrabandSpawner.cs` | Legacy spawner |
| `Assets/Scripts/Shared/Prison/PlayerWallet.cs` | Cash balance |
| `Assets/Scripts/Shared/Social/TradingService.cs` / `TradeMath.cs` | Trade stock + pricing |
| `Assets/Scripts/Shared/Social/PrisonJobPaymaster.cs` | Job pay |
| `Assets/Scripts/Shared/UI/PlayerVitalsHUD.cs` | Cash readout |

Related: [[Inventory & Items]] · [[Crafting]] · [[Social & Reputation]] · [[Social Ecosystem & Gangs]] · [[Prison Career Ladder]] · [[Roadmap & Priorities]]
