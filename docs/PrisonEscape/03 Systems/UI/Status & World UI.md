# Status & World UI

Ambient readouts: stealth pressure, player condition, interaction feedback, and world-space labels.

## Widgets

| Widget | Purpose |
|---|---|
| `PrisonHeatUI` | 3-state attention eye ([[Security, Heat & Alerts]]) |
| `PlayerVitalsHUD` | Cash + Mental / Physical / Strength bars (bottom-left, always visible) |
| `CurrentLocationHUD` | Bottom-right zone label ("CELL 5", "CAFETERIA", …) |
| `ObjectiveWaypointUI` | Mandatory-phase waypoint marker + distance |
| `ComplianceStatusHUD` | Center compliance readout (legacy) |
| `InteractionReticleView` | 6→20 px reticle |
| `CharacterNameLabel` | World-space TMP billboards ("You" / "Guard" / "Inmate") |
| `PillowStashProximityUI` | Stash contents panel |
| `AffinityFloatPopup` / `PrisonSocialRowUI` | Social feedback ([[Social & Reputation]]) |
| `CashUIController` | Legacy top-right cash readout (superseded by vitals panel cash line) |

## Polish backlog

- [ ] Heat eye needs final art — the three eye states are placeholder graphics.
- [x] Cash folded into `PlayerVitalsHUD` bottom-left (contraband tint preserved).

## Implemented (7/14/2026)

- [x] **Suspicion on the heat eye** — while the post-capture suspicion window is active the eye sits at **minimum "half"** for its 2-day duration (`PrisonSuspicion.IsSuspicionActive`).
- [x] **Player vitals HUD** — `PlayerVitalsHUD`: cash (`PlayerWallet`), Mental Health, Physical Health, Strength (0–100 each). Always visible; dark translucent backdrop (`PrisonUITheme.CommandStripBackdrop`). Fades with `UIMenuFocus`.
- [x] **Physical Health stat** — third player stat: solitary −10, +5/day at Morning Roll Call (with MH/STR). Shown on solitary overlay tick-down.
- [x] **Current location HUD** — `CurrentLocationHUD` bottom-right; uses `PrisonerController.GetCurrentLocationLabel()` → `PrisonRoutineLabels.FormatPlayerLocation`.
- [x] **Objective waypoint** — `ObjectiveWaypointUI`: screen marker + `LABEL — Nm` during mandatory non-compliance; on-screen dot or off-screen edge arrow. Smoothed world/screen position and distance (7/14 fix for jitter near stand points). Destination via `PrisonRoutineDestination` + `PrisonLocationRegistry.GetStandPointForEvent`.
- [x] **Runtime HUD bootstrap** — `HudBootstrap` spawns vitals, location, and waypoint canvases at runtime; legacy scene `CashUIController` can be disabled to avoid duplicate cash readout.

## Key files

`Assets/Scripts/Shared/Prison/PrisonHeatUI.cs` · `PlayerStats.cs` · `PlayerStatsMath.cs` · `PrisonSuspicion.cs` · `PrisonRoutineDestination.cs` · `Assets/Scripts/Shared/UI/HudBootstrap.cs` · `PlayerVitalsHUD.cs` · `CurrentLocationHUD.cs` · `ObjectiveWaypointUI.cs` · `InteractionReticleView.cs` · `PillowStashProximityUI.cs` · `Assets/Scripts/Shared/Visuals/CharacterNameLabel.cs`

Related: [[UI & HUD]] · [[Security, Heat & Alerts]] · [[Social & Reputation]] · [[UI Theme & Style Guide]]
