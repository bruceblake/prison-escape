# Screens & Menus

Fullscreen takeover states — the win/lose bookends of [[Escape Completion System]] plus pause.

## Widgets

- `EscapeEndScreenUI` — "YOU ESCAPED" end screen, run stats, "Next stop: MEDIUM SECURITY" ladder framing, return-to-menu. Built entirely at runtime (no scene wiring).
- `SolitaryScreenUI` — caught-escaping overlay: Mental Health −20 / Strength −10 tick down on unscaled time, then fades and releases the player.
- `PauseManager` — timescale-0 pause (singleplayer only).

## Polish backlog

- [ ] End screen stats block is a single centered text blob; format as label/value rows.
- [ ] Solitary screen could darken gameplay audio while up (needs audio system hooks first).

## To add

- [ ] Main menu is still the stock scene — full art/UX pass someday ([[Roadmap & Priorities]] "Later").

## Key files

`Assets/Scripts/Shared/UI/EscapeEndScreenUI.cs` · `SolitaryScreenUI.cs` · `PauseManager.cs` · `Assets/Scripts/Singleplayer/Escape/EscapeManager.cs`

Related: [[UI & HUD]] · [[Escape Completion System]] · [[UI Theme & Style Guide]]
