# UI & HUD

Hub note for the player-facing surface of the prison sim. Each surface has its own design doc in `03 Systems/UI/`:

| Doc | Covers |
|---|---|
| [[Routine & Schedule HUD]] | Now/next command strip, visual states, presence model |
| [[Status & World UI]] | Heat eye, vitals panel, location, waypoint, reticle, name labels (+ band-tinted social nameplates) |
| [[Inventory & Hotbar UI]] | 6-slot bag, hotbar, slot prefab, tooltips |
| [[Notebook & Crafting UI]] | Stolen notebook pages, crafting spread, pause |
| [[Talk Menu & NPC Profile]] | ✅ Real-time tabbed Talk overlay (`SocialInteractionMenu`) — **1320×640** panel; close via **X**, backdrop, Escape, interact-again |
| [[Social Dossier — Relationships & Gangs]] | ✅ Notebook Relationships + Gangs (`SocialDossierUI`) |
| [[Screens & Menus]] | Escape / transfer ceremony, solitary, MainMenu career hub |
| [[UI Theme & Style Guide]] | Palette, backdrop rules, menu-focus rule, typography |

Current screenshots: `HUD in-game 2026-07-14.png` · `Notebook crafting 2026-07-14.png` — **not yet committed**; re-capture during playtest and save under `03 Systems/UI/`.

## Status snapshot (7/16/2026)

- Routine strip, hotbar, notebook, heat eye, end/solitary / **transfer** screens: **implemented**.
- **Player vitals HUD**, **current location** (top-right), **objective hybrid guide** (`ObjectiveWaypointUI` + `WaypointWorldGuide`): **implemented** (`HudBootstrap` for vitals/location/waypoint).
- **Talk Menu** + **Social Dossier** + **SentenceClockHUD** (County): **implemented**.
- **UIMenuFocus** fades ambient HUD while bag/notebook/pause/Talk are open.
- Hotbar: **56×56 px slots**, bottom margin, 1–6 key hints. Crafting greens use `PrisonUITheme.InkGreen`.
- Objective guidance: **screen markers + world floor line / beacon** (no bottom strip over hotbar).
- Morning roll call: `ComplianceStatusHUD` uses **wait-in-cell** destination label.
- Suspicion floor on heat eye (half minimum while `PrisonSuspicion` active).

## Key files

All under `Assets/Scripts/Shared/UI/` and `Assets/Scripts/Shared/Prison/` — per-surface lists in each doc above.

Related: [[Time & Schedule]] · [[Inventory & Items]] · [[Security, Heat & Alerts]] · [[Social Ecosystem & Gangs]] · [[Systems Overview]]
