# Inventory & Items

6-slot hotbar inventory shared between single- and multiplayer, with an item taxonomy that drives confiscation, crafting, and loot.

## Inventory rules (`PlayerInventory`, fully tested)

| Rule | Value |
|---|---|
| Max slots | **6** (hotbar keys 1–6, wheel cycles) |
| Stacking | Only `CraftingPart` items stack (no max enforced in code; `PartData.maxStackSize = 5` is display-only) |
| Identity | Reference equality OR same `itemName` |
| Equip | Selected slot = held item; required for tools (screws), gifts, stash |
| Illegal tint (UI) | Weapon, Tool, Contraband |

Bag UI (`InventoryUI`, key E) supports drag-swap; `StolenNotebookUI` (Tab) hosts map/social/workbench/schedule tabs.

## Item taxonomy

- **`ItemCategory`:** CraftingPart · Tool · Weapon · Consumable · Contraband
- **`ItemRarity`:** Common · Uncommon · Rare · Legendary
- **Confiscation** (morning shakedown): Contraband, Tool, Weapon — see [[Roll Call & Shakedown]]
- `ToolData` adds `durability` (100, unused yet) and `interactionSpeedModifier` (1.5 = 33% faster holds)

## Item catalog

All under `Assets/ScriptableObjects/` — full table in [[Item Catalog]]. Summary:

| Group | Items |
|---|---|
| Common parts | Paperclip, Soap, Pillow, Bed Sheet, Rag, Plastic Bottle, Wood Scrap, Metal Rod, Flat Metal |
| Uncommon parts | Coin, Charcoal, Duct Tape, Wire, Metal Scrap |
| Rare parts | Glass Bottle, Alcohol, Mirror, File |
| Tools | Screwdriver (Uncommon), Fake Bed Dummy (Uncommon), Shovel / Wire Cutters / Ladder (Rare), Grappling Hook / Molotov (Legendary) |
| Weapons | AK-47 (multiplayer `WeaponData`) |

## Known data issues

- Duplicate assets: `Metal Scrap` / `MetalScrap`, `Wood Scrap` / `WoodScrap`
- `networkId` collisions (several items at 0; Metal Rod=1 vs AK-47=1) — matters if inventory ever networks
- Molotov is categorized **Tool**, not Consumable
- Coin exists but is **not wired** to the wallet ([[Loot & Economy]])

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Multiplayer/Player/PlayerInventory.cs` | Slots + rules |
| `Assets/Scripts/Singleplayer/Items/ItemData.cs` (+ ToolData, PartData, WeaponData) | Item SOs |
| `Assets/Scripts/Shared/UI/InventoryUI.cs` / `HotbarUI.cs` / `InventorySlotUI.cs` | UI |
| `Assets/ScriptableObjects/Items/` | Item assets |

Related: [[Crafting]] · [[Loot & Economy]] · [[Item Catalog]] · [[Player & Interaction]]
