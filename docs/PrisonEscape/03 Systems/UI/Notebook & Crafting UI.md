# Notebook & Crafting UI

The diegetic "stolen notebook" — the player's menu disguised as contraband paper. Crafting front-end for [[Crafting]].

## Current state (7/14/2026)

![[Notebook crafting 2026-07-14.png]]

Crafting spread: recipe card on the left page (title, result, 2 requirement slots, ASSEMBLE), category tabs + recipe grid on the right page. Visible problems: the enforcement HUD strip renders behind/through the notebook, category tab labels are unreadably small, and the ASSEMBLE button reads as a flat strip.

## Widgets

- `StolenNotebookUI` (Tab) — map / social / workbench / schedule pages
- `InventoryUI` (E) — bag + the crafting spread shown above (recipe index grid, 3 requirement slots, assemble)
- `RecipeRequirementSlotUI` — ingredient thumbnail + `have/need` readout (green satisfied / red short)
- `NotebookRecipeIndexEntry` — one recipe cell in the index grid
- `PauseManager` — timescale-0 pause (singleplayer only)

## Polish backlog

- [x] **HUD bleed-through** — the routine strip and hotbar must fade out while the notebook is open (shared fix, see `UIMenuFocus` in [[Routine & Schedule HUD]]).
- [x] **Category tabs** — `TOOLS / WEAPONS / PARTS / CONSUMABLES` labels are ~6 px tall; make the tabs real buttons with readable type and a clear selected state.
- [x] **ASSEMBLE button** — style as a button (padding, hover/disabled states), not a text strip.
- [x] **Craftable-state color** — the saturated pure-green (`0.1, 0.9, 0.1`) rows/slots clash with the paper theme; use the theme's muted ink-green.
- [ ] Requirement slots: pull the pale placeholder swatches toward paper-sketch style (art pass).

## To add

- [ ] Recipe count per category on the tab (e.g. `TOOLS 6`) — nice-to-have.
- [ ] Scribbled "new recipe" marker when a recipe becomes craftable for the first time — nice-to-have.

## Key files

`Assets/Scripts/Shared/UI/StolenNotebookUI.cs` · `InventoryUI.cs` · `RecipeRequirementSlotUI.cs` · `NotebookRecipeIndexEntry.cs` · `PauseManager.cs`

Related: [[UI & HUD]] · [[Crafting]] · [[Inventory & Hotbar UI]] · [[UI Theme & Style Guide]]
