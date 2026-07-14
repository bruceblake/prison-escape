# Loot & Economy

Where escape ingredients come from, and the (mostly dormant) cash system.

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

World seed: `GameManager.worldSeed` (or random) seeds `Random` before spawns — same seed = same loot layout.

> ⚠️ **No `LootTable` assets exist yet** — the type is ready; tables are authored on scene nodes. Creating proper table assets per room type (cells, workshop, kitchen…) is content work tied to escape-route pacing.

## Economy (`PlayerWallet`)

- Balance ≥ 0, NaN/Inf rejected; `SetContrabandCashState` flags dirty money (UI tint)
- HUD: `CashUIController`, `"$0.00"` format, 0.5 s roll animation
- **Nothing pays or charges the wallet yet** — no gameplay code calls `Add`/`SetBalance`; the Coin item is not wired
- Favors pay **affinity**, not cash ([[Social & Reputation]])

Design intent: cash should become the medium for inmate trading/bribes — needs a spec note before implementation.

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Prison/LootTable.cs` / `ItemSpawnNode.cs` / `WorldContainer.cs` | Loot |
| `Assets/Scripts/Singleplayer/Items/ContrabandSpawner.cs` | Legacy spawner |
| `Assets/Scripts/Shared/Prison/PlayerWallet.cs` / `CashUIController.cs` | Economy |

Related: [[Inventory & Items]] · [[Crafting]] · [[Roadmap & Priorities]]
