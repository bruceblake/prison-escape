# Status & World UI

Ambient readouts: stealth pressure, player condition, interaction feedback, and world-space labels.

## Widgets

| Widget | Purpose |
|---|---|
| `PrisonHeatUI` | 3-state attention eye ([[Security, Heat & Alerts]]) |
| `PlayerVitalsHUD` | Cash + Mental / Physical / Strength bars (bottom-left, always visible) |
| `CurrentLocationHUD` | Top-right zone label ("CELL 5", "CAFETERIA", …) |
| `ObjectiveWaypointUI` | Screen-space markers (projected destination / corner / edge arrow / breadcrumbs) + drives `WaypointWorldGuide` |
| `WaypointWorldGuide` | World-space floor `LineRenderer` path + pulsing destination beacon (beam + ground ring) |
| `SentenceClockHUD` | County career day counter under the routine strip (`CareerRunBootstrap`) |
| `ComplianceStatusHUD` | Center compliance readout (legacy) |
| `InteractionReticleView` | 6→20 px reticle |
| `CharacterNameLabel` | World-space TMP billboards — Standing **band tints** via `StandingBandUI` / `PrisonerSocialPresenter` |
| `PillowStashProximityUI` | Stash contents panel |
| `AffinityFloatPopup` | Trust/Respect delta floaters (v1 RowUI deleted) |
| `CashUIController` | Legacy top-right cash readout (superseded by vitals panel cash line) |
| Overhead social markers | Green `!` (open favor) / coin (trade stock) on `PrisonerSocialPresenter` |

## Polish backlog

- [ ] Heat eye needs final art — the three eye states are placeholder graphics.
- [x] Cash folded into `PlayerVitalsHUD` bottom-left (contraband tint preserved).

## Implemented

- [x] **Suspicion on the heat eye** — while the post-capture suspicion window is active the eye sits at **minimum "half"** for its 2-day duration (`PrisonSuspicion.IsSuspicionActive`). Snitch tips raise `PrisonSecurityAlerts` / queue shakedowns but do **not** currently floor the eye the same way.
- [x] **Player vitals HUD** — cash (`PlayerWallet`), MH / PH / STR. Fades with `UIMenuFocus`.
- [x] **Current location HUD** — top-right; `PrisonerController.GetCurrentLocationLabel()`.
- [x] **Objective guidance (hybrid)** — `ObjectiveWaypointUI` (screen) + `WaypointWorldGuide` (world). No bottom strip over the hotbar. Destination via `PrisonRoutineDestination`.
- [x] **Sentence clock** — County days remaining when on a career County run.
- [x] **Social readability** — band-tinted nameplates + overhead `!` / coin markers.
- [x] **Runtime HUD bootstrap** — `HudBootstrap` spawns vitals, location, and waypoint canvases; dedupes `EventSystem`s. Sentence clock is spawned by career bootstrap, not HudBootstrap.

## Key files

`PrisonHeatUI.cs` · `PlayerStats.cs` · `PrisonSuspicion.cs` · `PrisonRoutineDestination.cs` · `HudBootstrap.cs` · `PlayerVitalsHUD.cs` · `CurrentLocationHUD.cs` · `ObjectiveWaypointUI.cs` · `WaypointWorldGuide.cs` · `SentenceClockHUD.cs` · `CharacterNameLabel.cs` · `StandingBandUI.cs` · `PrisonerSocialPresenter.cs`

Related: [[UI & HUD]] · [[Security, Heat & Alerts]] · [[Social & Reputation]] · [[Talk Menu & NPC Profile]] · [[Prison Career Ladder]] · [[UI Theme & Style Guide]]
