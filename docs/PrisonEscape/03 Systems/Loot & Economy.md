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
| `ItemSpawnNode` | `GameManager.PopulateWorldSpawns` rolls per node (`spawnChance` default 0.5) → instantiate `worldPrefab` as pickup | Working; **nodes need placing in the level** |
| `WorldContainer` | Rolls 1–3 items into a browse list | ⚠️ Search shows names but **doesn't transfer to inventory** yet |
| `ContrabandSpawner` (legacy) | 6 items at random points via `ItemType` enum → prefab map | Legacy path |

World seed: `GameManager.worldSeed` (or career visit seed) seeds `Random` before spawns — same seed = same loot layout. Facility loot-abundance multiplier applies on career runs.

> ⚠️ **No `LootTable` ScriptableObject assets exist yet** — the type is ready; tables are authored on scene nodes. Creating proper table assets per room type is content work tied to escape-route pacing.

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

Design detail (prices, stock refresh, bribe tiers): [[Social Ecosystem & Gangs]] · [[Social & Reputation]].

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Prison/LootTable.cs` / `ItemSpawnNode.cs` / `WorldContainer.cs` | Loot |
| `Assets/Scripts/Singleplayer/Items/ContrabandSpawner.cs` | Legacy spawner |
| `Assets/Scripts/Shared/Prison/PlayerWallet.cs` | Cash balance |
| `Assets/Scripts/Shared/Social/TradingService.cs` / `TradeMath.cs` | Trade stock + pricing |
| `Assets/Scripts/Shared/Social/PrisonJobPaymaster.cs` | Job pay |
| `Assets/Scripts/Shared/UI/PlayerVitalsHUD.cs` | Cash readout |

Related: [[Inventory & Items]] · [[Crafting]] · [[Social & Reputation]] · [[Social Ecosystem & Gangs]] · [[Prison Career Ladder]] · [[Roadmap & Priorities]]
