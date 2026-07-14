# Item Catalog

Every item asset in `Assets/ScriptableObjects/`. Rarity drives loot weight (Common 60 / Uncommon 25 / Rare 10 / Legendary 5 — see [[Loot & Economy]]).

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
- Items with no recipe or spawn source yet: Paperclip, Soap, Plastic Bottle, Charcoal, Mirror, Coin — give them purposes or loot-table slots

Related: [[Inventory & Items]] · [[Crafting]] · [[Loot & Economy]]
