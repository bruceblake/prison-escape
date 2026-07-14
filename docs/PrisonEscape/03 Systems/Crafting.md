# Crafting

Turn scavenged parts into escape tools via the notebook/workbench UI. Fully unit-tested core.

## Mechanics

- `CanCraft`: inventory holds every ingredient at required amounts
- `TryCraft`: remove ingredients → add result (`resultAmount = 1` for all current recipes)
- ⚠️ Known quirk: if the inventory is full, ingredients are still consumed and the craft "succeeds" with a warning — worth fixing
- UI: `InventoryUI` category tabs + 3 requirement slots + Assemble button; `StolenNotebookUI` workbench tab; colored have/need text via `CraftingRecipeDescription`

## Recipe book (all 7, from `Assets/ScriptableObjects/Recipes/`)

| Recipe | Ingredients | Result | Escape use |
|---|---|---|---|
| **Screwdriver** | 1 Metal Rod + 1 Flat Metal | Screwdriver | Open vents (faster holds) |
| **Fake Bed Dummy** | 1 Pillow + 1 Bed Sheet | Fake Bed Dummy | Defeat night bed check |
| **Wire Cutters** | 2 Metal Scrap + 1 File + 1 Duct Tape | Wire Cutters | Cut the courtyard fence (route pending) |
| **Shovel** | 2 Metal Scrap + 1 Wood Scrap + 1 Duct Tape | Shovel | Digging (route pending) |
| **Ladder** | 3 Wood Scrap + 1 Duct Tape | Ladder | Climbing (route pending) |
| **Grappling Hook** | 3 Metal Scrap + 2 Wire + 1 Duct Tape | Grappling Hook | Climbing (route pending) |
| **Molotov** | 1 Glass Bottle + 1 Alcohol + 1 Rag | Molotov | Distraction/weapon |

Design intent: **every recipe should map to an escape route or escape support** — when adding routes, add/balance recipes here first.

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Crafting/CraftingSystem.cs` | CanCraft / TryCraft |
| `Assets/Scripts/Shared/Crafting/CraftingRecipe.cs` | Recipe SO |
| `Assets/Scripts/Shared/Crafting/CraftingRecipeDescription.cs` | UI text |
| `Assets/ScriptableObjects/Recipes/` | The 7 recipe assets |

Related: [[Inventory & Items]] · [[Escape Routes & Mechanics]] · [[Loot & Economy]]
