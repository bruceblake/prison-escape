# Status & World UI

Ambient readouts: stealth pressure, player condition, interaction feedback, and world-space labels.

## Widgets

| Widget | Purpose |
|---|---|
| `PrisonHeatUI` | 3-state attention eye ([[Security, Heat & Alerts]]) |
| `PlayerVitalsHUD` | Cash + Mental / Physical / Strength bars (bottom-left, always visible) |
| `CurrentLocationHUD` | Top-right zone label ("CELL 5", "CAFETERIA", …) |
| `ObjectiveWaypointUI` | Screen-space path guide (projected markers + edge arrow + breadcrumbs) — no 3D primitives, no bottom HUD strip |
| `ComplianceStatusHUD` | Center compliance readout (legacy) |
| `InteractionReticleView` | 6→20 px reticle |
| `CharacterNameLabel` | World-space TMP billboards ("You" / "Guard" / "Inmate") — **specced:** tint by social Standing band (Enemy→Confidant); see [[Talk Menu & NPC Profile]] |
| `PillowStashProximityUI` | Stash contents panel |
| `AffinityFloatPopup` / `PrisonSocialRowUI` | Social feedback — RowUI **deleted** in v3; popup **kept** for Trust/Respect deltas ([[Social & Reputation]] · [[Social Ecosystem & Gangs]]) |
| `CashUIController` | Legacy top-right cash readout (superseded by vitals panel cash line) |
| Overhead social markers | 📐 Specced: green `!` (open favor), coin (trade stock) — [[Talk Menu & NPC Profile]] |

## Polish backlog

- [ ] Heat eye needs final art — the three eye states are placeholder graphics.
- [x] Cash folded into `PlayerVitalsHUD` bottom-left (contraband tint preserved).

## Implemented (7/14/2026)

- [x] **Suspicion on the heat eye** — while the post-capture suspicion window is active the eye sits at **minimum "half"** for its 2-day duration (`PrisonSuspicion.IsSuspicionActive`).
- [x] **Player vitals HUD** — `PlayerVitalsHUD`: cash (`PlayerWallet`), Mental Health, Physical Health, Strength (0–100 each). Always visible; dark translucent backdrop (`PrisonUITheme.CommandStripBackdrop`). Fades with `UIMenuFocus`.
- [x] **Physical Health stat** — third player stat: solitary −10, +5/day at Morning Roll Call (with MH/STR). Shown on solitary overlay tick-down.
- [x] **Current location HUD** — `CurrentLocationHUD` **top-right** (below the routine strip); uses `PrisonerController.GetCurrentLocationLabel()` → `PrisonRoutineLabels.FormatPlayerLocation`.
- [x] **Objective waypoint** — `ObjectiveWaypointUI`: **screen-space guide** (destination/corner markers projected from world positions, off-screen edge arrow, NavMesh breadcrumb dots, distance label). No 3D `CreatePrimitive` markers. Bottom HUD strip over the hotbar **removed**. Destination via `PrisonRoutineDestination`.
- [x] **Runtime HUD bootstrap** — `HudBootstrap` spawns vitals, location, and waypoint canvases at runtime; also **deduplicates** scene `EventSystem`s (duplicate EventSystems spam every frame and tank FPS). Legacy scene `CashUIController` can be disabled to avoid duplicate cash readout.

## Key files

`Assets/Scripts/Shared/Prison/PrisonHeatUI.cs` · `PlayerStats.cs` · `PlayerStatsMath.cs` · `PrisonSuspicion.cs` · `PrisonRoutineDestination.cs` · `Assets/Scripts/Shared/UI/HudBootstrap.cs` · `PlayerVitalsHUD.cs` · `CurrentLocationHUD.cs` · `ObjectiveWaypointUI.cs` · `InteractionReticleView.cs` · `PillowStashProximityUI.cs` · `Assets/Scripts/Shared/Visuals/CharacterNameLabel.cs`

## Specced — social readability (v3)

From [[Social Ecosystem & Gangs]] / [[Talk Menu & NPC Profile]]:

- **Nameplate band tints:** Enemy (red) · Hostile (orange) · Neutral (default) · Friendly / Ally / Confidant (greens).
- **Overhead markers:** `!` = open favor; coin = trade stock today.
- Dossier lives in the notebook ([[Social Dossier — Relationships & Gangs]]), not as ambient HUD.

Related: [[UI & HUD]] · [[Security, Heat & Alerts]] · [[Social & Reputation]] · [[Social Ecosystem & Gangs]] · [[Talk Menu & NPC Profile]] · [[UI Theme & Style Guide]]
