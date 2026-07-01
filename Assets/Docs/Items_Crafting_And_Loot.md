# Items, Crafting & World Loot — Master Reference

_Last updated: 2026-07-01_

This is the **single source of truth** for every item, crafting recipe, loot table, and world-spawn rule in the prison-escape game. It replaces scattered notes in item descriptions and aligns design with the **actual code paths** on `main`.

**Design rule (confirmed):** There are **no searchable containers**. Items appear as **world pickups** scattered through the prison at `ItemSpawnNode` positions. The player walks up and presses **F** (`WorldItemPickup`).

Related docs: [Game Features & Test Coverage](Game_Features_And_Test_Coverage.md) · [Social & Reputation](Prison_Social_And_Reputation_System.md)

---

## 1. How loot works in code

| Piece | Script / asset | Role |
|---|---|---|
| Item definition | `ItemData` / `PartData` / `ToolData` / `WeaponData` (ScriptableObject) | Name, icon, category, rarity, `worldPrefab`, `networkId` |
| Registry | `ItemDatabase.allItemsInGame` | Every item the game knows about (multiplayer sync) |
| Spawn marker | `ItemSpawnNode` (MonoBehaviour on empty GameObjects in the scene) | Position + `lootTable` + `spawnChance` (0–1) |
| Weighted pool | `LootTable` (ScriptableObject) | List of `ItemData`; roll uses rarity weights (Common 60, Uncommon 25, Rare 10, Legendary 5) × `weightMultiplier` |
| Spawn pass | `GameManager.PopulateWorldSpawns()` | At game start, each node rolls `spawnChance`; on success, instantiates `item.worldPrefab` and assigns `WorldItemPickup.itemData` |
| Pickup | `WorldItemPickup` | Press F → `PlayerInventory.AddItem` → destroy pickup if successful |

**Legacy (do not use for new work):**

- `WorldContainer` — searchable desk/container UI; **not part of the design**. Ignore or remove from scenes.
- `ContrabandSpawner` + `ItemType` enum — old prefab-index spawner; superseded by `ItemSpawnNode` + `LootTable`.
- `PickupItem` — older pickup script; prefer `WorldItemPickup`.

### Spawn rules (design defaults)

| Setting | Recommended value | Notes |
|---|---|---|
| `ItemSpawnNode.spawnChance` | **0.35–0.55** general; **0.15–0.25** high-value zones | Keeps prison from feeling empty or flooded |
| Nodes per zone | 4–12 depending on area size | Yard/laundry need more; cells need fewer |
| Respawn | **None in v1** | Items are a finite daily resource; optional respawn at `LightsOut` is a future feature |
| `worldPrefab` | Required on every spawnable item | Without it, `GameManager` logs a warning and skips |

---

## 2. Item taxonomy

### Categories (`ItemCategory`)

| Category | Confiscated at morning shakedown? | Typical use |
|---|---|---|
| **CraftingPart** | No | Raw materials; safe to carry openly |
| **Tool** | Yes | Escape gear (screwdriver, wire cutters, ladder…) |
| **Weapon** | Yes | Molotov, improvised weapons |
| **Contraband** | Yes | Illegal goods (future: cigarettes, shivs) |
| **Consumable** | No | Food, medicine; used once (future) |

### Rarity (`ItemRarity`) → loot weight

| Rarity | Base weight | Design intent |
|---|---|---|
| Common | 60 | Everywhere; fuels basic crafts |
| Uncommon | 25 | Zone-specific (kitchen, yard, workshop) |
| Rare | 10 | One or two zones; key recipe parts |
| Legendary | 5 | Craft-only results or single hidden spawn |

### Confiscation

`MorningShakedownSweeper.ShouldConfiscate` removes **Tool**, **Weapon**, and **Contraband**. Stash tools in `PillowStash` before morning line-up.

---

## 3. Canonical item master list

**Status key:** ✅ asset exists · 🔧 needs asset cleanup · 📋 planned (no asset yet)

Duplicates to **merge** (keep one asset, delete the other):

- `Metal Scrap.asset` ✅ — delete `MetalScrap.asset` (duplicate)
- `Wood Scrap.asset` ✅ — delete `WoodScrap.asset` (duplicate)

### 3.1 Starting / cell items (not world-spawned)

| Item | Category | Rarity | Source | Escape / social role |
|---|---|---|---|---|
| Pillow ✅ | CraftingPart | Common | Player cell at start | Fake bed dummy; pillow stash |
| Bed Sheet ✅ | CraftingPart | Common | Player cell at start | Fake bed dummy |

### 3.2 Crafting parts — world spawn pool

| Item | Cat. | Rarity | Typical zones | Notes |
|---|---|---|---|---|
| Paperclip ✅ | Part | Common | Cells, office, cafeteria | Shim; future lockpick |
| Plastic Bottle ✅ | Part | Common | Yard, cafeteria, laundry | High-volume filler |
| Rag ✅ | Part | Common | Laundry, kitchen, yard | Molotov wick; future rope |
| Wood Scrap ✅ | Part | Common | Yard, workshop, maintenance | Handles, ladder |
| Soap ✅ | Part | Common | Laundry, showers | **Finch** favored gift |
| Metal Rod ✅ 🔧 | Part | Common | Workshop | Screwdriver recipe; legacy `ItemType` id 1 |
| Flat Metal ✅ 🔧 | Part | Common | Workshop | Screwdriver recipe; legacy `ItemType` id 2 |
| Metal Scrap ✅ | Part | Uncommon | Yard, workshop, industrial | Core metal for most tools |
| Duct Tape ✅ | Part | Uncommon | Maintenance, workshop | Binds almost everything |
| Wire ✅ | Part | Uncommon | Workshop, electrical closet | Grappling hook, wire cutters |
| Charcoal ✅ | Part | Uncommon | Kitchen (grill/storage) | Distraction / future smoke |
| Coin ✅ | Part | Uncommon | Yard, cafeteria | **Sly** favored gift; trade flavor |
| File ✅ | Part | Rare | Hidden maintenance, favor reward | Wire cutters |
| Mirror ✅ | Part | Rare | Cell craft / yard | **Finch** favored gift |
| Glass Bottle ✅ | Part | Rare | Kitchen, cafeteria | Molotov body |
| Alcohol ✅ | Part | Rare | Kitchen (locked store) | Molotov fuel |
| Bleach 📋 | Part | Uncommon | Laundry | Future: lock rust / distraction |
| Battery 📋 | Part | Uncommon | Workshop | Future: flashlight craft |
| Cigarette Pack 📋 | Contraband | Uncommon | Yard (NPC drop) | Social trade; not craftable |

### 3.3 Tools & weapons — craft-only (never in general loot tables)

| Item | Cat. | Rarity | Recipe | Escape route |
|---|---|---|---|---|
| Screwdriver ✅ | Tool | Uncommon | §4.1 | **Vent route** — unscrew vent covers |
| Fake Bed Dummy ✅ | Tool | Uncommon | §4.2 | **Night check** — decoy in bed |
| Wire Cutters ✅ | Tool | Rare | §4.3 | **Perimeter fence** — cut wire |
| Shovel ✅ | Tool | Rare | §4.4 | **Tunnel** — dig under wall |
| Ladder ✅ | Tool | Rare | §4.5 | **Wall** — climb short sections |
| Grappling Hook ✅ | Tool | Legendary | §4.6 | **Wall** — long throw + climb |
| Molotov ✅ | Weapon | Legendary | §4.7 | **Distraction** — riot / guard pull |
| Lockpick 📋 | Tool | Uncommon | §4.8 | Alternate cell/vent entry |
| Hacksaw 📋 | Tool | Rare | §4.9 | Cut cell bars (future route) |
| Stolen Guard Key 📋 | Tool | Legendary | Favor reward only | Bypass one locked door |

### 3.4 Multiplayer-only

| Item | Cat. | Notes |
|---|---|---|
| AK-47 ✅ (`WeaponData`) | Weapon | Multiplayer FPS; not part of SP escape loop |

---

## 4. Crafting recipes

Crafting uses `CraftingSystem.TryCraft` + `CraftingRecipe` ScriptableObjects. Recipes appear in:

- **Inventory UI** (`InventoryUI.recipes`)
- **Stolen notebook UI** (`StolenNotebookUI.recipes`) — contraband crafting during free time

### 4.1 Screwdriver ✅ _(implemented)_

| | |
|---|---|
| **Result** | 1× Screwdriver |
| **Ingredients** | 1× Metal Rod + 1× Flat Metal |
| **Enables** | Unscrew vent screws (`InteractableScrew.requiredTool`) |

### 4.2 Fake Bed Dummy ✅ _(implemented)_

| | |
|---|---|
| **Result** | 1× Fake Bed Dummy |
| **Ingredients** | 1× Pillow + 1× Bed Sheet |
| **Enables** | Place on `CellBed`; survives night check until morning line-up |

### 4.3 Wire Cutters ✅ _(implemented)_

| | |
|---|---|
| **Result** | 1× Wire Cutters |
| **Ingredients** | 2× Metal Scrap + 1× File + 1× Duct Tape |
| **Enables** | Cut perimeter fence (route TBD — needs `EscapeZone`) |

### 4.4 Shovel ✅ _(implemented)_

| | |
|---|---|
| **Result** | 1× Shovel |
| **Ingredients** | 2× Metal Scrap + 1× Wood Scrap + 1× Duct Tape |
| **Enables** | Tunnel escape (route TBD) |

### 4.5 Ladder ✅ _(implemented)_

| | |
|---|---|
| **Result** | 1× Ladder |
| **Ingredients** | 3× Wood Scrap + 1× Duct Tape |
| **Enables** | Climb medium wall sections |

### 4.6 Grappling Hook ✅ _(implemented)_

| | |
|---|---|
| **Result** | 1× Grappling Hook |
| **Ingredients** | 3× Metal Scrap + 2× Wire + 1× Duct Tape |
| **Enables** | Climb tall wall / throw over fence |

### 4.7 Molotov ✅ _(implemented)_

| | |
|---|---|
| **Result** | 1× Molotov |
| **Ingredients** | 1× Glass Bottle + 1× Alcohol + 1× Rag |
| **Enables** | Create diversion; raises heat (`PrisonSecurityAlerts`) |

### 4.8 Lockpick 📋 _(planned)_

| | |
|---|---|
| **Result** | 1× Lockpick |
| **Ingredients** | 2× Paperclip + 1× Metal Scrap |
| **Enables** | Faster vent screw bypass OR low-security door (design choice) |

### 4.9 Hacksaw 📋 _(planned)_

| | |
|---|---|
| **Result** | 1× Hacksaw |
| **Ingredients** | 2× Metal Scrap + 1× File + 1× Duct Tape |
| **Enables** | Cut barred window / cell bars (new route type) |

### 4.10 Rope 📋 _(planned intermediate)_

| | |
|---|---|
| **Result** | 1× Rope |
| **Ingredients** | 2× Rag + 1× Wire |
| **Used in** | Optional rework: Ladder → 2× Wood Scrap + 1× Rope + 1× Duct Tape; Grappling Hook → 2× Metal Scrap + 1× Rope + 1× Wire |

---

## 5. Escape route ↔ item dependency map

```
                    ┌─────────────────────────────────────────┐
                    │         WORLD LOOT (spawn nodes)         │
                    │  Metal, Wood, Tape, Wire, Rag, File…   │
                    └─────────────────┬───────────────────────┘
                                      │ craft (notebook / inventory UI)
          ┌───────────────────────────┼───────────────────────────┐
          ▼                           ▼                           ▼
   ┌─────────────┐            ┌─────────────┐            ┌─────────────┐
   │ Screwdriver │            │ Fake Dummy  │            │ Wire Cutters│
   │  (vent)     │            │ (pillow+    │            │ Shovel      │
   │             │            │  sheet)     │            │ Ladder/Hook │
   └──────┬──────┘            └──────┬──────┘            └──────┬──────┘
          │                          │                          │
          ▼                          ▼                          ▼
   VentCover open              Night bed check            Perimeter / wall /
   → passage collider          passes while away          tunnel exit
          │                          │                          │
          └──────────────────────────┴──────────────────────────┘
                                      │
                                      ▼
                            ⚠️ EscapeZone / win state
                               (NOT IMPLEMENTED YET)
```

**Supporting systems (not items):**

- `PillowStash` — hide Tool/Weapon/Contraband from morning shakedown
- `Molotov` — optional heat/diversion during attempt
- Guard detection — obstacle, not an item

---

## 6. Loot tables (ScriptableObjects to create)

Create under `Assets/ScriptableObjects/LootTables/`. Assign one table per `ItemSpawnNode` (or reuse the same asset on many nodes).

### 6.1 `LootTable_Prison_Common`

General hallway / cell-block spawns. **Spawn chance 0.45.**

| Item | Rarity | weightMult |
|---|---|---|
| Paperclip | Common | 1.0 |
| Plastic Bottle | Common | 1.0 |
| Rag | Common | 1.0 |
| Wood Scrap | Common | 0.8 |
| Soap | Common | 0.6 |

### 6.2 `LootTable_Yard`

Yard exercise area. **Spawn chance 0.50.**

| Item | Rarity | weightMult |
|---|---|---|
| Plastic Bottle | Common | 1.2 |
| Wood Scrap | Common | 1.0 |
| Rag | Common | 0.8 |
| Metal Scrap | Uncommon | 0.7 |
| Coin | Uncommon | 0.4 |

### 6.3 `LootTable_Kitchen`

Kitchen / cafeteria back rooms (during non-meal phases or hidden corners). **Spawn chance 0.40.**

| Item | Rarity | weightMult |
|---|---|---|
| Plastic Bottle | Common | 1.0 |
| Rag | Common | 0.8 |
| Charcoal | Uncommon | 1.0 |
| Glass Bottle | Rare | 0.5 |
| Alcohol | Rare | 0.3 |

### 6.4 `LootTable_Workshop`

Maintenance / workshop / industrial. **Spawn chance 0.35.**

| Item | Rarity | weightMult |
|---|---|---|
| Metal Scrap | Uncommon | 1.2 |
| Wire | Uncommon | 1.0 |
| Duct Tape | Uncommon | 0.8 |
| Metal Rod | Common | 0.6 |
| Flat Metal | Common | 0.6 |
| File | Rare | 0.25 |

### 6.5 `LootTable_Laundry`

Laundry / showers. **Spawn chance 0.40.**

| Item | Rarity | weightMult |
|---|---|---|
| Soap | Common | 1.2 |
| Rag | Common | 1.0 |
| Bed Sheet | Common | 0.15 |
| Bleach 📋 | Uncommon | 0.5 |

### 6.6 `LootTable_RareHidden`

1–3 nodes per level, behind optional lockpick / vent / favor gate. **Spawn chance 0.20.**

| Item | Rarity | weightMult |
|---|---|---|
| File | Rare | 1.0 |
| Mirror | Rare | 0.8 |
| Alcohol | Rare | 0.5 |
| Coin | Uncommon | 0.6 |

**Never add craft results** (Screwdriver, Wire Cutters, Molotov, etc.) to loot tables — players must craft them.

---

## 7. Zone placement guide (scene authoring)

Place empty GameObjects with `ItemSpawnNode` throughout `PrisonLevel1` (and future levels):

| Zone | Suggested node count | Loot table | spawnChance |
|---|---|---|---|
| Cell block corridors | 6–8 | Common | 0.40 |
| Yard | 8–12 | Yard | 0.50 |
| Cafeteria (off-hours corners) | 4–6 | Kitchen | 0.35 |
| Kitchen storage | 3–5 | Kitchen | 0.45 |
| Workshop / maintenance | 5–8 | Workshop | 0.35 |
| Laundry | 4–6 | Laundry | 0.40 |
| Hidden / vent-adjacent | 1–3 | RareHidden | 0.20 |

**Tips:**

- Put nodes on the floor or shelf surfaces, not inside colliders.
- Slightly randomize rotation on the spawned prefab (future enhancement in `PopulateWorldSpawns`).
- Avoid spawning inside guard patrol cones during `PopulateWorldSpawns` (future: defer spawn to `FreeTime` phase).

---

## 8. Social, favors & economy ties

| Item | Social use |
|---|---|
| Soap, Mirror | **Finch** favored gifts (×2 affinity) |
| Coin | **Sly** favored gift |
| Any `ItemData` | Gift action; repeat same **category** to same NPC = ×0.5 (unless favored) |
| Specific items | `FavorOfferDefinition.requiredItem` — phase-locked mini-quests |

**Example favor offers to add** (create `FavorOfferDefinition` assets):

| Request label | Required item | Phase | Personality filter |
|---|---|---|---|
| "Need soap for laundry job" | Soap | FreeTime | any |
| "Get me a file from workshop" | File | FreeTime | Broker |
| "Bring a bottle from kitchen" | Glass Bottle | Dinner | Bully |
| "Smuggle me a mirror" | Mirror | LightsOut | Finch |

**Economy:** `PlayerWallet` / `CashUIController` exist; Coin could later be sold to Broker for cash. Not wired yet.

---

## 9. Network ID allocation (cleanup required)

Most items have `networkId: 0`, which breaks multiplayer item sync. Proposed stable IDs (assign in Unity Inspector, register all in `ItemDatabase`):

| ID | Item |
|---|---|
| 1 | Metal Rod |
| 2 | Flat Metal |
| 3 | Screwdriver |
| 10 | Paperclip |
| 11 | Plastic Bottle |
| 12 | Rag |
| 13 | Wood Scrap |
| 14 | Soap |
| 15 | Pillow |
| 16 | Bed Sheet |
| 20 | Metal Scrap |
| 21 | Duct Tape |
| 22 | Wire |
| 23 | Charcoal |
| 24 | Coin |
| 30 | File |
| 31 | Mirror |
| 32 | Glass Bottle |
| 33 | Alcohol |
| 40 | Fake Bed Dummy |
| 41 | Wire Cutters |
| 42 | Shovel |
| 43 | Ladder |
| 44 | Grappling Hook |
| 50 | Molotov |

Reserve **100+** for future items. Delete duplicate assets before renumbering.

---

## 10. Implementation checklist

### Done in code

- [x] `ItemData` + categories / rarity
- [x] `LootTable.GetRandomItem()` weighted rolls
- [x] `ItemSpawnNode` + `GameManager.PopulateWorldSpawns()`
- [x] `WorldItemPickup` (correct null/full-inventory handling)
- [x] 7 crafting recipes (ScriptableObjects in `Assets/ScriptableObjects/Recipes/`)
- [x] 24 item assets (with 2 duplicates to remove)

### To do (design → scene → code)

- [ ] **Remove / ignore `WorldContainer`** from design and any scenes
- [ ] **Delete duplicate** `MetalScrap.asset`, `WoodScrap.asset`
- [ ] **Assign unique `networkId`** to every item (§9)
- [ ] **Assign `worldPrefab`** on every spawnable part (required for spawn)
- [ ] **Create loot table assets** (§6) and place `ItemSpawnNode` objects in `PrisonLevel1` (§7)
- [ ] **Wire `InventoryUI.recipes` and `StolenNotebookUI.recipes`** with all 7 recipe assets
- [ ] **Register all items** in scene `ItemDatabase.allItemsInGame`
- [ ] **Fix `CraftingSystem.TryCraft`** — return `false` and restore ingredients when inventory full
- [ ] **Add planned items/recipes** (§3.3 📋, §4.8–4.10) as needed for new routes
- [ ] **Build `EscapeZone` + win state** so tools connect to an actual goal

---

## Appendix A — Quick recipe card (printable)

```
SCREWDRIVER      = Metal Rod + Flat Metal
FAKE BED DUMMY   = Pillow + Bed Sheet
WIRE CUTTERS     = 2× Metal Scrap + File + Duct Tape
SHOVEL           = 2× Metal Scrap + Wood Scrap + Duct Tape
LADDER           = 3× Wood Scrap + Duct Tape
GRAPPLING HOOK   = 3× Metal Scrap + 2× Wire + Duct Tape
MOLOTOV          = Glass Bottle + Alcohol + Rag

PLANNED:
LOCKPICK         = 2× Paperclip + Metal Scrap
HACKSAW          = 2× Metal Scrap + File + Duct Tape
ROPE             = 2× Rag + Wire
```

## Appendix B — Item count summary

| Bucket | Count |
|---|---|
| Crafting parts (implemented) | 18 |
| Tools / weapons (implemented) | 7 |
| Planned additions | 5 |
| **Total target catalog** | **~30** |

---

_When adding or changing an item, update this file first, then create the ScriptableObject, assign network ID + world prefab, add to `ItemDatabase`, and add to the appropriate loot table._
