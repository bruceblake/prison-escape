# Screens & Menus

Fullscreen takeover states — the win/lose bookends of [[Escape Completion System]], pause, and (specced) the world/prison-select front end of [[Prison Career Ladder]].

## Widgets

- `EscapeEndScreenUI` — "YOU ESCAPED" end screen, run stats, "Next stop: MEDIUM SECURITY" ladder framing, return-to-menu. Built entirely at runtime (no scene wiring). **Specced for rewrite** into the transfer/graduation ceremony ("CAUGHT — TRANSFERRED" / "SENTENCE COMPLETE" / "CAREER CLEARED") — [[Facility Transfer & Graduation]].
- `SolitaryScreenUI` — caught-escaping overlay: Mental Health −20 / **Physical Health −10** / Strength −10 tick down on unscaled time, then fades and releases the player.
- `PauseManager` — timescale-0 pause (singleplayer only). Gains "Quit to Prison Select" under the career ladder.

## Specced — start screen, worlds & prison select ([[World Saves & Start Screen]])

The stock `MainMenu` scene becomes the career hub, themed via [[UI Theme & Style Guide]]:

- **Title screen** — CONTINUE (most-recent world + facility/day subtitle) · NEW WORLD (name prompt) · LOAD WORLDS (rows: facility, day, cash, respect; delete w/ confirm) · QUIT.
- **Prison Select hub** — all 9 career facilities + Dev Sandbox (dev builds): unlocked = icon/title/description/ENTER; locked = **black silhouette**, title greyed, no spoilers; current facility highlighted; unlocked-but-unbuilt = "UNDER CONSTRUCTION" silhouette variant.

## Polish backlog

- [ ] End screen stats block is a single centered text blob; format as label/value rows *(absorbed by the transfer-ceremony rewrite)*.
- [ ] Solitary screen could darken gameplay audio while up (needs audio system hooks first).

## To add

- [ ] ~~Main menu is still the stock scene — full art/UX pass someday~~ → superseded: MainMenu becomes the worlds/prison-select hub ([[World Saves & Start Screen]], milestone M2).

## Key files

`Assets/Scripts/Shared/UI/EscapeEndScreenUI.cs` · `SolitaryScreenUI.cs` · `PauseManager.cs` · `Assets/Scripts/Singleplayer/Escape/EscapeManager.cs`

Related: [[UI & HUD]] · [[Escape Completion System]] · [[UI Theme & Style Guide]]
