# World Saves & Start Screen

**Status:** Implemented — 7/15/2026 (M1 + M2 shipped together)
**Design doc:** [[Prison Career Ladder]] (parent) · **System note:** [[Screens & Menus]]
**Branch:** `feat/career-world-saves` (M1 — model/IO) then `feat/start-screen-prison-select` (M2 — UI)
**Code:** `CareerWorld` / `CareerWorldStore` (JSON at `persistentDataPath/worlds/{id}.json`, atomic temp-file swap, v0→v1 migration) · `CareerMainMenuUI` (runtime-built hub over the MainMenu scene: Continue / New World / Load Worlds / Quit, name prompt, destructive delete confirm, Prison Select grid with black silhouettes + UNDER CONSTRUCTION variants) · `CareerSession.EnterFacility` loads each facility's own scene and `CareerRunBootstrap` applies the carry. EditMode coverage in `CareerWorldStoreTests` (all green).

## What it is

Multiple **named career saves ("worlds")**, each with its own ladder progression, plus the front-of-house UI: a title screen with Continue / New World / Load Worlds, and a **Prison Select hub** where the whole 9-facility ladder is visible but only unlocked facilities are enterable — locked ones are black silhouettes. The player picks a world by name, picks an unlocked prison, and plays a fresh local run with their global carry applied.

## Why it exists

The career ladder ([[Prison Career Ladder]]) needs a durable spine: progression that outlives any one facility run, and a hub that makes the ladder *visible* — eight looming silhouettes are the long-term goal rendered as UI. Named worlds let a player run parallel careers (speedrun world vs farming world) without save juggling.

## Design details

### Save model (M1 — no UI dependency)

- `CareerWorld` serialized as **JSON** at `Application.persistentDataPath/worlds/{id}.json`; one file per world; `schemaVersion` int + forward migration from v1. Full field list in [[Prison Career Ladder]] § Data model.
- API surface (pure C#, EditMode-testable): `CareerWorldStore.List() / Create(name) / Load(id) / Save(world) / Delete(id)`; `world.Unlock(facilityId)`; `world.BeginVisit(facilityId)` → `FacilityRunState` with seed `hash(world.id, facilityId, visitIndex)`.
- `Create(name)`: new guid id, County unlocked (+ `dev_sandbox` in Development builds), globals zeroed (stats 100, cash 0, respect 0), `currentFacilityId = county`.
- Write strategy: save on transfer, on quit-to-menu, and at each Morning Count (day boundary autosave). Atomic write (temp file + rename) so a crash can't corrupt a world.
- Display name is free text (1–24 chars); file identity is the guid, so renames and duplicate names are safe.

### Title screen

`MainMenu` scene rebuilt (runtime-constructed UI like the other screens; [[UI Theme & Style Guide]] `PrisonUITheme` — dark institutional chrome, caution-yellow accents):

- **CONTINUE** — most recent world by `lastPlayedUtc`, subtitle "*{name}* — {facility}, Day {n}". Hidden when no worlds exist.
- **NEW WORLD** — name prompt → create → straight to Prison Select.
- **LOAD WORLDS** — list rows: name, last-played facility, day, cash, respect, last-played date. Select → Prison Select. Per-row DELETE with type-nothing confirm dialog ("Transfer paperwork shredded — this world is gone forever." destructive-red per theme).
- **QUIT**.

### Prison Select hub

- Grid/list of the 9 career facilities in ladder order, + Dev Sandbox slot (dev builds only). Header shows world name, cash, respect.
- **Unlocked + scene built:** facility icon, title, one-line description, `recommendedStayDays` hint, ENTER button. Current facility highlighted (caution-yellow frame).
- **Unlocked + scene not built yet:** silhouette variant + "UNDER CONSTRUCTION" — acknowledges progress without lying about content ([[Prison Career Ladder]] § Data model).
- **Locked:** **black silhouette**, greyed title only, not focusable/enterable. No description, no spoilers.
- ENTER → load `sceneName` → apply global carry → fresh `FacilityRunState` (Day 1, empty inventory, new seed).
- ESC from in-game pause gains "Quit to Prison Select" (saves globals; abandons the local run — confirm dialog states this).

### Wireframes (text)

```
LOAD WORLDS                          PRISON SELECT — "Lifer"   $1,240 · R38
┌────────────────────────────┐      ┌──────┬──────┬──────┐
│ Lifer     FedLow  D3  $1.2k│      │County│StateM│State │  ██████ = black
│ Speedrun  StateMed D1 $90  │      │  ✓   │ in ✓ │ ██████│  silhouette
│ + NEW WORLD                │      ├──────┼──────┼──────┤
└────────────────────────────┘      │██████│██████│██████│
                                    └──────┴──────┴──────┘
```

## Systems it touches

- [[Screens & Menus]] — `MainMenu` scene replaced; `EscapeEndScreenUI` already loads `"MainMenu"`, so the transfer ceremony lands here naturally
- [[Prison Career Ladder]] / [[Facility Transfer & Graduation]] — unlock flags + carry are this feature's data
- [[UI Theme & Style Guide]] — all chrome
- [[Loot & Economy]] — wallet read for world rows / hub header
- `GameManager` — scene bootstrap gains "which world + which facility" context instead of a bare scene load

## Data & tuning

World-name length limits, autosave cadence, Continue-button subtitle format, silhouette tint, grid layout counts — all serialized on the menu components. `FacilityDefinition` supplies icon/silhouette/description per slot.

## Test plan

- **EditMode:** create/load/save/delete round-trip; schema migration (v1 file loads in v2 loader); unlock idempotence; visit-seed determinism (`same world+facility+visitIndex → same seed`, `visitIndex+1 → different`); Continue-pick = max `lastPlayedUtc`; atomic-write leaves no partial file on simulated failure.
- **Manual:** new world → only County (+Dev in editor) enterable, 8 silhouettes; delete world with confirm; two worlds progress independently; enter facility → Day 1 with carried cash/respect visible.

## Out of scope

- Cloud saves / cross-device
- Multiplayer careers
- World rename UI (create-time name only for v1)
- Any facility geometry — this is UI + persistence only
