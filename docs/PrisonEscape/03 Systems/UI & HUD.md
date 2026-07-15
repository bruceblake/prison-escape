# UI & HUD

The player-facing surface of the prison sim. Several HUD layers coexist (legacy + current).

## Routine / schedule HUD

- **`RoutineNowNextBarUI`** (primary) — now/next phase bar with visual states:
  - `Chill` (default) · `MandatoryWarning` (free time ending, mandatory next) · `TravelGrace` (grace countdown fill) · `Enforcement` (non-compliant during mandatory — 2 Hz flash)
  - Status copy: `IN POSITION`, `TRAVEL GRACE ({0}s)`, `SHAKEDOWN {0}/{1}`, `WAITING TO BE CLEARED`, `NON-COMPLIANT`
- `PrisonScheduleUI` — clock + event + compliance (75 s phase-end warning)
- `MinimalSchedulePhaseTextHUD` — prev/current/next + countdown (labels FreeTime "Yard Time")
- `TextRoutineComplianceHUD` — tactical text modes; danger flash 2 Hz
- `DailyRoutineBarUI` — full-day segment timeline
- Presence model: HUD dims to "all clear" when compliant with >50% phase time left; goes full "pressure" under 25%, enforcement, grace, or warning

## Status & world UI

| Widget | Purpose |
|---|---|
| `PrisonHeatUI` | 3-state attention eye ([[Security, Heat & Alerts]]) |
| `ComplianceStatusHUD` | Center compliance readout |
| `InteractionReticleView` | 6→20 px reticle |
| `CharacterNameLabel` | World-space TMP billboards ("You" / "Guard" / "Inmate") |
| `PillowStashProximityUI` | Stash contents panel |
| `AffinityFloatPopup` / `PrisonSocialRowUI` | Social feedback ([[Social & Reputation]]) |
| `CashUIController` | `$0.00` wallet display |

## Menus & inventory

- `InventoryUI` (E) — bag, drag-swap, crafting tab with 3 requirement slots
- `HotbarUI` — 6 slots, keys 1–6
- `StolenNotebookUI` (Tab) — diegetic notebook: map / social / workbench / schedule
- `ItemTooltipUI`, `HeldItemDisplay`, `PauseManager` (timescale 0 pause in SP only)

## Theme

`PrisonUITheme`: caution `#F4D03F`, hazard `#C0392B`. Warm light fixtures; dark translucent backdrops.

## Key files

All under `Assets/Scripts/Shared/UI/` and `Assets/Scripts/Shared/Prison/` (HUD scripts named as above).

Related: [[Time & Schedule]] · [[Inventory & Items]] · [[Security, Heat & Alerts]]



This is the Current UI: 

![[Pasted image 20260714200048.png]]


This is the current inside of inventory UI: 
![[Pasted image 20260714200128.png]]

this is the current you escaped screen: ![[Pasted image 20260714201309.png]]