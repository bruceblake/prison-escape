# UI & HUD

Hub note for the player-facing surface of the prison sim. Each surface has its own design doc in `03 Systems/UI/`:

| Doc | Covers |
|---|---|
| [[Routine & Schedule HUD]] | Now/next command strip, visual states, presence model |
| [[Status & World UI]] | Heat eye, vitals panel, location, waypoint, reticle, name labels |
| [[Inventory & Hotbar UI]] | 6-slot bag, hotbar, slot prefab, tooltips |
| [[Notebook & Crafting UI]] | Stolen notebook pages, crafting spread, pause |
| [[Screens & Menus]] | Escape end screen, solitary overlay, main menu |
| [[UI Theme & Style Guide]] | Palette, backdrop rules, menu-focus rule, typography |

Current screenshots live next to the docs: `HUD in-game 2026-07-14.png` · `Notebook crafting 2026-07-14.png`.

## Status snapshot (7/14/2026)

- Routine strip, hotbar, notebook, heat eye, end/solitary screens: **implemented**.
- **Player vitals HUD** (cash, MH, PH, STR), **current location** readout, **objective waypoint** marker: **implemented** (runtime-built, auto-spawn from `EscapeManager`).
- **UIMenuFocus** fades ambient HUD (routine strip, hotbar, heat eye, vitals, location) while bag/notebook/pause are open.
- Hotbar: bottom margin, 1–6 key hints, stronger selection highlight. Crafting greens use `PrisonUITheme.InkGreen`.
- Suspicion floor on heat eye (half minimum while `PrisonSuspicion` active).

## Key files

All under `Assets/Scripts/Shared/UI/` and `Assets/Scripts/Shared/Prison/` — per-surface lists in each doc above.

Related: [[Time & Schedule]] · [[Inventory & Items]] · [[Security, Heat & Alerts]] · [[Systems Overview]]
