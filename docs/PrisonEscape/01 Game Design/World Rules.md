# World Rules

The laws of the game world — what is always true, as implemented. Change a rule here first, then in code.

## The day

1. The prison runs a fixed repeating schedule ([[Time & Schedule]]) built around predictability, control, and headcount: Morning Count (05:00) → Breakfast → Movement → Work/Programs → Midday Count → Lunch → Movement → Afternoon Work → Evening Count → Dinner → Yard & Recreation (17:00–21:00) → Final Lockdown & Night Count → Lights Out (22:00–05:00). One full day = **24 real minutes** at the default time scale.
2. **Every phase is mandatory except Free Time** (movement windows and yard/recreation). Mandatory = you must be at your assigned location.
3. **Everything stops for a count.** 4 formal counts per day (morning, midday, evening, night bed check). A count mismatch raises a facility-wide **Lockdown alert** (`FormalCountMonitor`); consequence wiring for lockdowns is still pending ([[Security, Heat & Alerts]]).
4. Entering a mandatory phase grants **50 seconds of travel grace** — you count as compliant while walking there. Morning count grants **no grace**.
5. Free time before a mandatory phase shows a **warning** on the HUD.

## Where you must be

| Phase | Required location |
|---|---|
| Morning Count | Your cell's roll-call stand point (or inside your cell) |
| Meals | Cafeteria (strict 15–20 min eating window) |
| Work / Education / Programs | The Workshop zone (per-inmate kitchen/laundry/classroom assignments: follow-up) |
| Midday / Evening Count | Your cell (stand point or interior) |
| Free Time (movement, yard & recreation) | Yard or Cafeteria |
| Lights Out / Night Count | Your cell (in bed coverage) |

## Enforcement

6. Guards spot you if you are **non-compliant** and within their **10 m / 90° vision cone** or **6 m proximity** ([[Guard AI]]).
7. Spotted → arrested at 2 m → escorted to your cell → movement unblocked after 1 s. No health/damage — enforcement is positional.
8. Cell doors **open during day phases (05:00–21:00), lock at Final Lockdown (Night Count) and Lights Out**.

## Morning count & shakedown

9. A guard sweeps every cell each morning; the phase ends when **all cells pass** (or 600 s cap). You are released early once your cell is cleared ([[Roll Call & Shakedown]]).
10. The sweep **confiscates Contraband, Tools, and Weapons** left in the open (world pickups). Crafting parts and consumables are safe.
11. Items **hidden in the pillow stash or carried in your inventory are not confiscated** — hiding works.

## Night

12. At night, verifier guards check every bed with a presence sphere (radius = cell interior radius, default 2.5 m).
13. A **fake bed dummy passes the check**; an empty bed triggers a **Lockdown alert**.
14. Fake dummies are **discovered and destroyed at morning line-up**, raising a Suspicion alert.

## Items & crafting

15. Inventory = **6 slots**; only crafting parts stack.
16. Crafting consumes ingredients and produces exactly the recipe result ([[Crafting]]).
17. Loot spawns are seeded — the same world seed reproduces the same item layout.

## Social

18. Affinity per inmate ranges **-100 to +100**; positive gains shrink as affinity rises (soft cap); betrayals hit hard (-50) and are never capped ([[Social & Reputation]]).
19. One greeting per inmate per phase counts; favors pay affinity for delivered items.
20. Prison-wide reputation = the average of all inmate affinities: Outsider < 25 ≤ Associate < 50 ≤ Respected < 75 ≤ Kingpin.

## Escape, capture & consequences

21. Escape is the goal; the game does not force it. Routes require tools, timing, and defeated checks ([[Escape Routes & Mechanics]]).
22. **Crossing the outer boundary = escaped** — win screen with run stats, any route, any time ([[Escape Completion System]]).
23. **Restricted zones** (perimeter band; cafeteria/workshop at night) count as escape attempts — compliance and grace don't protect you there.
24. Spotted in a restricted zone → **solitary confinement**: inventory confiscated (pillow stash survives), **Mental Health −20 / Strength −10**, day skips to the next Morning Count.
25. After a caught escape, **suspicion lasts 2 days**: guard detection reaches 40% farther (10 → 14 m).
26. **Stats regenerate +5/day** at Morning Count. Strength below 50 slows sprinting (×2.0 → ×1.5).

---

*When a rule changes (e.g. new phase, new confiscation category, real heat meter), update this note in the same change as the code.*
