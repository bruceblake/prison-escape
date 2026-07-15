# Escape Completion System

**Status:** Implemented (v1) — 7/14/2026. Note: the boundary is only *reachable* once escape route geometry (vent corridors, fence cut) exists; the win/lose systems are live.
**System notes:** [[Escape Routes & Mechanics]] · [[Security, Heat & Alerts]] · [[Guard AI]] · [[Prison Layout — Minimum Security]]
**Branch:** merged to `dev` (7/14/2026)
**Brainstormed:** 7/14/2026 (chat) — decisions below are final unless overridden here.

## What it is

The win/lose keystone of the game. Cross the prison's outer boundary → **you escaped** (end screen + stats + ladder to the next prison). Get spotted in a restricted zone → **caught escaping**: arrested, thrown in solitary confinement, inventory confiscated, suspicion raised.

## Why it exists

Escape is the game's stated goal but currently has no completion state. This feature closes the loop and gives the lockdown/suspicion alert hooks real consequences.

## Design details

### Winning — the escape boundary
- One **general "outside the walls" boundary** encircling the facility (beyond the perimeter loop corridors and courtyard fence). Crossing it from inside = escaped, regardless of route.
- **Anytime escape:** no phase restriction. Opened routes stay open (unscrewed vents stay unscrewed). Guards are the only gate.
- **No progress UI** — the player tracks their own plan.

### End screen (win)
- Stats: in-game days to escape, real play time, times arrested, solitary stays, items crafted, final reputation tier.
- Ladder framing: "MINIMUM SECURITY: ESCAPED → Next stop: Medium Security." Button returns to main menu (until Medium exists).

### Restricted zones
- New `RestrictedZone` volumes. Two flavors:
  - **Always restricted:** vent/plumbing corridors, beyond the fence line, the outer band between loop corridors and the escape boundary.
  - **Phase-restricted:** restricted only during listed phases (e.g. cafeteria during the 22:00–05:00 Lights Out window, workshop outside its `WorkProgram` block — [[Time & Schedule]]).
- Being inside an active restricted zone = **escape attempt**. Compliance/grace do not protect you.
- Guard spots you in one (normal detection geometry: 10 m / 90° cone / 6 m proximity) → **caught escaping** (not the gentle escort).

### Getting caught → solitary confinement
- Immediate: screen pops up showing **Mental Health −20**, **Physical Health −10**, and **Strength −10** ticking down.
- **Inventory confiscated** (all slots). **Pillow stash contents survive** — hiding rewards planning.
- Player teleports to a solitary cell; time **skips to the next Morning Count** (lose the rest of the day).
- **Suspicion** active for **2 in-game days** (2 Morning Counts): guard detection range 10 → **14 m**; heat eye sits at "half" while suspicious.

### Solitary confinement block (map change)
- New **section of 4 solitary cells** inside Main Security (south end), scratch-built like other furnishing. Each cell ~3×4 m with a spawn point.
- Layout note updated: [[Prison Layout — Minimum Security]].

### Player stats (new, minimal v1)
- **Mental Health**, **Physical Health**, and **Strength**, all 0–100, start 100.
- Solitary stay: −20 MH, −10 PH, −10 STR.
- Regen: **+5 each per day** (applied at Morning Count).
- v1 effects: Strength < 50 → sprint multiplier 2.0 → 1.5. Mental health effects reserved for later (stored properly now).

## Systems it touches

[[Guard AI]] (caught-escaping arrest path) · [[Security, Heat & Alerts]] (suspicion becomes real; alert hooks get consequences) · [[Time & Schedule]] (day skip; daily regen tick) · [[Inventory & Items]] (confiscation) · [[Escape Routes & Mechanics]] (this is its keystone) · [[UI & HUD]] (end screen, solitary screen) · [[Prison Layout — Minimum Security]] (solitary block, boundary, restricted volumes)

## Data & tuning (designer-facing)

Solitary stat costs, regen rate, suspicion duration (days) and detection multiplier, restricted phase lists per zone, boundary placement.

## Test plan

- EditMode: stats clamp/regen math, suspicion day-window logic, restricted-zone phase logic, escape state transitions (Free → Caught → Solitary → Released; Free → Escaped).
- PlayMode/manual: cross boundary → win screen; get spotted in vent corridor → solitary flow; stash survives; sprint slows at low strength.

## Out of scope

- Medium Security prison (ladder is framing only)
- Mental-health gameplay effects beyond storage
- Numeric heat meter beyond the suspicion window
- Escape progress UI
