# Screens & Menus

Fullscreen takeover states — the win/lose bookends of [[Escape Completion System]], pause, and the world/prison-select front end of [[Prison Career Ladder]].

## Widgets (on `dev`)

- `EscapeEndScreenUI` — transfer/graduation ceremony (runtime-built). Headlines:
  - **CAUGHT — TRANSFERRED** — career facility boundary cross
  - **SENTENCE COMPLETE** — County sentence clock graduation
  - **CAREER CLEARED** — Federal ADX escape (career win)
  - **YOU ESCAPED** — Dev Sandbox only (not on the ladder)
  Ledger beat + next-facility unlock card when a `CareerTransfer` result is present. Return-to-menu / continue into unlocked facility. Spec: [[Facility Transfer & Graduation]].
- `SolitaryScreenUI` — caught-escaping overlay: Mental Health −20 / **Physical Health −10** / Strength −10 tick down on unscaled time, then fades and releases the player.
- `PauseManager` — timescale-0 pause (singleplayer only). **Quit to Prison Select** via `CareerQuitConfirmUI` → `CareerSession.QuitToPrisonSelect()`.
- `CareerMainMenuUI` — worlds list + prison-select hub over the stock `MainMenu` scene ([[World Saves & Start Screen]]).
- `SceneTransitionScreen` — fade during facility enter / transfer loads.

## Start screen, worlds & prison select

Live on `dev` (M1–M2):

- **Title / worlds** — CONTINUE (most-recent world + facility/day subtitle) · NEW WORLD · LOAD WORLDS (rows: facility, day, cash, respect; delete w/ confirm) · QUIT.
- **Prison Select hub** — all 9 career facilities + Dev Sandbox (dev builds): unlocked = icon/title/description/ENTER; locked = **black silhouette**, title greyed; current facility highlighted; unbuilt unlocked slots can show construction framing as scenes land (M6+).

## Polish backlog

- [ ] End screen stats block is still a dense text blob; format as tighter label/value rows.
- [ ] Solitary screen could darken gameplay audio while up (needs audio system hooks first).
- [ ] Full art/UX pass on MainMenu chrome (functional hub first).

## Key files

`Assets/Scripts/Shared/UI/EscapeEndScreenUI.cs` · `SolitaryScreenUI.cs` · `PauseManager.cs` · `Assets/Scripts/Shared/Career/CareerMainMenuUI.cs` · `CareerQuitConfirmUI.cs` · `SceneTransitionScreen.cs` · `Assets/Scripts/Singleplayer/Escape/EscapeManager.cs` · `CareerTransferFlow.cs`

Related: [[UI & HUD]] · [[Escape Completion System]] · [[World Saves & Start Screen]] · [[Facility Transfer & Graduation]] · [[UI Theme & Style Guide]]
