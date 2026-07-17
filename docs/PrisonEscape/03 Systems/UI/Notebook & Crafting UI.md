# Notebook & Crafting UI

The diegetic "stolen notebook" — the player's menu disguised as contraband paper. Crafting front-end for [[Crafting]]. Hosts the [[Social Dossier — Relationships & Gangs]].

## Current state (7/16/2026)

Crafting spread with readable category tabs, styled ASSEMBLE button, and muted `PrisonUITheme.InkGreen` for satisfied slots. Social tab hosts **`SocialDossierUI`** (Relationships + Gangs). Routine strip and hotbar fade out via `UIMenuFocus` while the notebook is open.

## Widgets

- `StolenNotebookUI` (Tab) — map / **Relationships · Gangs** (`SocialDossierUI`) / workbench / schedule pages
- `SocialDossierUI` — list + detail + gangs pages attached to the social panel
- `InventoryUI` (E) — bag + crafting spread (recipe index grid, 3 requirement slots, assemble)
- `RecipeRequirementSlotUI` — ingredient thumbnail + `have/need` readout
- `NotebookRecipeIndexEntry` — one recipe cell in the index grid
- `PauseManager` — timescale-0 pause (singleplayer only)

In-person Chat / Gift / Trade stay on [[Talk Menu & NPC Profile]] (not from the notebook).

## Polish backlog

- [x] **HUD bleed-through** — routine strip and hotbar fade while notebook is open (`UIMenuFocus`).
- [x] **Category tabs** — readable type + selected state.
- [x] **ASSEMBLE button** — styled button, not a text strip.
- [x] **Craftable-state color** — theme ink-green.
- [x] **Relationships + Gangs pages** — `SocialDossierUI` on `dev`.
- [ ] Requirement slots: paper-sketch art pass.
- [ ] Recipe count per category on the tab (e.g. `TOOLS 6`) — nice-to-have.
- [ ] Scribbled "new recipe" marker when a recipe becomes craftable — nice-to-have.

## Key files

`StolenNotebookUI.cs` · `SocialDossierUI.cs` · `InventoryUI.cs` · `RecipeRequirementSlotUI.cs` · `NotebookRecipeIndexEntry.cs` · `PauseManager.cs`

Related: [[UI & HUD]] · [[Crafting]] · [[Inventory & Hotbar UI]] · [[Social Dossier — Relationships & Gangs]] · [[Talk Menu & NPC Profile]] · [[UI Theme & Style Guide]]
