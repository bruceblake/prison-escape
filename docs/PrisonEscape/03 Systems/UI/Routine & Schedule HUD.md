# Routine & Schedule HUD

The top-of-screen command strip that tells the player where they must be and how long they have. Primary surface of the compliance loop ([[Time & Schedule]]).

## Current state (7/14/2026)

![[HUD in-game 2026-07-14.png]]

Top strip during Morning Roll Call: phase title, progress bar with ghost timer, `Next: BREAKFAST`, status line. Vitals panel (bottom-left), location label (bottom-right), and objective waypoint appear during mandatory non-compliance — see [[Status & World UI]]. *(Screenshot file not yet in git — re-capture when playtesting.)*

## Widgets

- **`RoutineNowNextBarUI`** (primary) — now/next phase bar with visual states:
  - `Chill` (default) · `MandatoryWarning` (free time ending, mandatory next) · `TravelGrace` (grace countdown fill) · `Enforcement` (non-compliant during mandatory — 2 Hz flash)
  - Status copy: `IN POSITION`, `TRAVEL GRACE ({0}s)`, `SHAKEDOWN {0}/{1}`, `WAITING TO BE CLEARED`, `NON-COMPLIANT`
  - `RoutineBarDisplayController` — CanvasGroup dim/juice (grace flash+punch, enforcement shake)
  - `RoutineBarTimerReadability` — keeps the ghost timer legible over the fill
- Legacy layers (still in repo, off by default): `PrisonScheduleUI` (clock + event + 75 s warning), `MinimalSchedulePhaseTextHUD` (prev/current/next), `TextRoutineComplianceHUD` (tactical text), `DailyRoutineBarUI` (full-day segment timeline)
- Presence model: HUD dims to "all clear" when compliant with >50% phase time left; full "pressure" under 25%, enforcement, grace, or warning
- **`ComplianceStatusHUD`** — during Morning Roll Call, `GO TO` shows **wait-in-cell** copy (`GetMorningRollCallLineUpDestinationLabel`), not the next meal venue

## Polish backlog

- [x] **Hide/dim while the notebook or bag is open** — in the crafting screenshot ([[Notebook & Crafting UI]]) the red enforcement strip renders behind and clips into the notebook. Any open menu should fade the strip out (see `UIMenuFocus`).
- [ ] The strip spans the full screen width with a lot of empty bar in the middle at 1080p+; consider max content width ~1400 px, centered.
- [ ] `Next: BREAKFAST` sits flush against the right screen edge — respect the strip's horizontal padding.

## To add

- Nothing for [[Escape Completion System]] — deliberately **no escape-progress UI** (players track their own plan).

## Key files

`Assets/Scripts/Shared/Prison/RoutineNowNextBarUI.cs` · `RoutineBarDisplayController.cs` · `RoutineBarTimerReadability.cs` · `ComplianceStatusHUD.cs` (+ legacy HUD scripts named above)

Related: [[UI & HUD]] · [[Time & Schedule]] · [[Roll Call & Shakedown]] · [[UI Theme & Style Guide]]
