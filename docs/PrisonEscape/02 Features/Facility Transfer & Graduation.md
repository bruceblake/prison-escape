# Facility Transfer & Graduation

**Status:** Implemented — 7/15/2026 (M4 + M5 shipped together)
**Design doc:** [[Prison Career Ladder]] (parent) · **System notes:** [[Escape Completion System]] · [[Escape Routes & Mechanics]] · [[Screens & Menus]]
**Branch:** `feat/transfer-graduation` (M4) + `feat/county-sentence-clock` (M5)
**Code:** `CareerTransfer` (pure state-change ordering: visitLog → respect/totals → unlock → confiscate) · `CareerTransferFlow` (boundary + sentence orchestration, atomic save, ceremony handoff) · `EscapeEndScreenUI` rebuilt as the transfer ceremony (CAUGHT — TRANSFERRED / SENTENCE COMPLETE / CAREER CLEARED, stats rows, ledger beat, next-stop card, ENTER-next/Prison-Select buttons) · `CareerRunBootstrap` Morning-Count day tick + `SentenceClockHUD` "Days served: N / 7" · caught-escaping −2 respect wired into `EscapeManager.OnCaughtEscaping`. Soft gates live in `CareerGates` (route content consumes them later — the boundary never rejects). EditMode coverage in `CareerTransferTests` (all green).

## What it is

The rewrite of what *winning a facility* means. Crossing the outer boundary no longer shows "YOU ESCAPED — you're free": you are **caught outside the wall and transferred** to the next, harder facility on the ladder. County alone offers a second graduation path — **serve out your sentence** (default 7 in-game days) and be transferred without escaping. Escaping the top facility (`FedAdx`) is the career win. Every transfer unlocks the next facility, applies global carry (cash, respect, gang, stats, recipes), confiscates all inventory, and returns the player to the Prison Select hub.

## Why it exists

It converts the escape fantasy from a single win screen into a career ([[Prison Career Ladder]]). It also fixes a tonal problem: freedom-as-ending kills the game loop, while caught-and-graduated *feeds* it — every success manufactures the next, harder problem.

## Design details

### Trigger 1 — boundary crossing (any facility)

- Existing `EscapeManager` boundary detection is unchanged (any route, any phase, guards are the only gate — [[Escape Completion System]]).
- On cross at facility tier *N* < 8 → **transfer ceremony** instead of the win screen.
- On cross at `FedAdx` (tier 8) → **career-win ceremony**.
- Soft gates: at facilities with a `transferThreshold` (Fed Med/High/ADX), the boundary route's final gate is expressed in-fiction *before* the boundary (fixer payment / gang backing — [[Prison Career Ladder]] § Soft transfer gates). The boundary trigger itself never rejects a player who reaches it.

### Trigger 2 — County sentence clock

- County's `FacilityDefinition.sentenceDays = 7`. Day counter increments at each Morning Count ([[Time & Schedule]]).
- HUD: "Days served: N / 7" line near the routine bar ([[Routine & Schedule HUD]]).
- At Morning Count of day 8 → **sentence-complete ceremony** → transfer to State Min. Identical mechanics to escape-transfer; different framing and +5 respect instead of the escape award.
- Escaping County before day 7 transfers immediately (caught framing) — skipping County's farming is the player's first tempo decision.

### The transfer ceremony (replaces the v1 end screen)

Rebuild of `EscapeEndScreenUI` (runtime-constructed, [[UI Theme & Style Guide]]):

1. **Headline:** "CAUGHT — TRANSFERRED" / County clock: "SENTENCE COMPLETE" / ADX: "CAREER CLEARED".
2. **Run stats** (kept from v1): in-game days, real play time, arrests, solitary stays, items crafted, reputation tier — formatted as label/value rows (clears the existing [[Screens & Menus]] polish item).
3. **Ledger beat:** cash carried, respect gained (`+8 + 2×tier` escape / +5 sentence — [[Prison Career Ladder]] § Career Respect), inventory confiscated (item count), next facility unlocked.
4. **Next stop card:** the next facility's silhouette flips to its unlocked art.
5. Buttons: **ENTER {next facility}** (when its scene exists) · **PRISON SELECT**. Career win adds **KEEP PLAYING** copy — world stays fully playable.

### State changes at transfer (order matters)

1. Append `visitLog` entry (facilityId, visitIndex, daysSpent, escaped).
2. Apply respect award; increment `totalTransfers`, add `daysSpent` to `totalDaysLived`.
3. `Unlock(next)`; set `currentFacilityId = next`; ADX escape sets `careerWon = true`.
4. **Confiscate:** discard `FacilityRunState` wholesale (inventory *and* pillow stash — the stash-survives rule of [[World Rules]] 24 applies to *solitary*, not transfer).
5. Save world JSON (atomic), then show ceremony → hub.

### Revisit reset (same milestone — it's the same code path)

Entering any unlocked facility calls `BeginVisit`: fresh `FacilityRunState`, Day 1, seed `hash(worldId, facilityId, visitIndex)`, fresh NPC population (career respect seeds the starting band), all doors/vents/fences restored, cell re-rolled. Global carry (cash, respect, gang, stats, recipes) applied on entry.

### Explicitly unchanged

- **Caught escaping** (spotted in a restricted zone) → solitary, stat hits, 2-day suspicion — exactly as shipped in [[Escape Completion System]]. Only *succeeding* transfers you.
- Stats regen, fake-bed, shakedown rules — untouched.

## Systems it touches

- [[Escape Completion System]] — win path rewritten; capture path untouched
- [[World Saves & Start Screen]] — writes unlocks/carry; hub is the landing surface
- [[Time & Schedule]] — Morning Count tick drives the sentence clock
- [[Routine & Schedule HUD]] — County days-served line
- [[Inventory & Items]] — transfer confiscation (full, stash included)
- [[Social Ecosystem & Gangs]] — respect award writes the shared `global.respect`; gang id persists
- [[Screens & Menus]] — `EscapeEndScreenUI` rebuild

## Data & tuning

Per-facility (`FacilityDefinition`): `sentenceDays`, `transferThresholdCash/Respect`, respect awards. Global: ceremony timings, HUD copy strings. All designer-tunable; no magic numbers in `EscapeManager`.

## Test plan

- **EditMode:** next-facility resolution across all 9 tiers (8 → win, not 9); respect-award math per tier; sentence-clock boundary (day 7 vs 8, no double-fire on the same Morning Count); transfer state-change ordering (visitLog before unlock before save); confiscation empties inventory *and* stash; revisit produces `visitIndex+1` and a different seed; `careerWon` only from tier 8.
- **Manual:** cross Dev-stub boundary → ceremony → County(+1) unlocked → hub; wait 7 County days → sentence ceremony; re-enter County → Day 1, empty inventory, cash kept; ADX flag (debug-unlock) → CAREER CLEARED → world still playable.

## Out of scope

- Geometry/routes for facilities beyond the County stub
- Smuggle-slot perk (open question 3 in [[Prison Career Ladder]])
- Gang-switching costs (Social v2)
- Sentence clocks anywhere but County
