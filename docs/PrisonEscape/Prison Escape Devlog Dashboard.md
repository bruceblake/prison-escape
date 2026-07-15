# Devlog Dashboard

Newest first. Log milestones here after each work session (see [[Development Workflow]]).

## 7/14/2026 (evening)
- **Layout geometry pass** — doorway walls with jambs/lintels, roof overhang + soffits, floor Y synced to cell spawns, cell-wing wall keep-out, **jail cell shell cleanup** (strip 20 m legacy walls, rebuild 4×5.5 m shells + partitions). Rebuilt `PrisonLevel1` via **Run Full Build**. See [[Prison Layout — Minimum Security]] · [[Editor Tooling]].
- **HUD fixes** — objective waypoint smoothing (jitter near stand points); hotbar compact 56×56 slots; morning roll call compliance label shows wait-in-cell. See [[UI & HUD]] · [[Status & World UI]] · [[Inventory & Hotbar UI]].
- **PR #40** — `feat/escape-completion` → `dev` (escape completion v1 + layout + HUD). Vault synced to match implementation per [[Development Workflow]].

## 7/14/2026
- **Development workflow rule** — `.cursor/rules/development-workflow.mdc` enforces vault → verify → devlog → git loop; agents must run end-of-task checklist and ask before commit/push.
- **UI vitals + navigation HUD** — `PlayerVitalsHUD` (cash, mental/physical health, strength), `CurrentLocationHUD`, `ObjectiveWaypointUI` with distance during mandatory non-compliance. Physical Health stat added (−10 solitary, +5/day regen). `UIMenuFocus` menu fade, hotbar polish, `InkGreen` crafting theme. `HudBootstrap` runtime spawn. See [[UI & HUD]] · [[Status & World UI]]. Branch: `feat/escape-completion` (`c3b5f0b`).
- **Vault restructured as full source of truth** — complete design docs for every system, content inventory, engineering docs, and process notes. Entry point: [[Home]].
- **Connected diagram layout implemented** — `PrisonLevel1` rebuilt with 18 shared-edge floor plates matching the layout diagram. Doorway walls, roof overhang/soffits, ~370 lights, scratch-built furniture. *(Superseded by evening geometry pass — see above.)*
- `dev` and `main` synced at the same commit; Obsidian-source-of-truth rule added to `.cursor/rules/`.

## Earlier (pre-dashboard highlights)
- East cell block renamed to cells 09–16; registry wired for 16 cells
- Low-poly character visual pipeline (Player/Guard/Prisoner, 2.6 m rigs) — [[Character Visuals]]
- 136 EditMode tests green — [[Testing & QA]]
- Core sim systems in place: schedule, zones, roll call/shakedown, guard/prisoner AI, social math, crafting, inventory, vent/fake-bed/stash escape mechanics — [[Systems Overview]]
