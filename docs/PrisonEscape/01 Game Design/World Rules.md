# World Rules

The laws of the game world — what is always true, as implemented. Change a rule here first, then in code.

## The day

1. The prison runs a fixed repeating schedule ([[Time & Schedule]]): Morning Roll Call → Breakfast → Free Time → Lunch → Free Time → Dinner → Free Time → Lights Out → Night Roll Call. One full day ≈ **13.25 real minutes**.
2. **Every phase is mandatory except Free Time.** Mandatory = you must be at your assigned location.
3. Entering a mandatory phase grants **50 seconds of travel grace** — you count as compliant while walking there. Morning roll call grants **no grace**.
4. Free time before a mandatory phase shows a **warning** on the HUD.

## Where you must be

| Phase | Required location |
|---|---|
| Morning Roll Call | Your cell's roll-call stand point (or inside your cell) |
| Meals | Cafeteria |
| Free Time | Yard or Cafeteria |
| Lights Out / Night Roll Call | Your cell (in bed coverage) |

## Enforcement

5. Guards spot you if you are **non-compliant** and within their **10 m / 90° vision cone** or **6 m proximity** ([[Guard AI]]).
6. Spotted → arrested at 2 m → escorted to your cell → movement unblocked after 1 s. No health/damage — enforcement is positional.
7. Cell doors **open during day phases, lock at Lights Out and Night Roll Call**.

## Morning roll call & shakedown

8. A guard sweeps every cell each morning; the phase ends when **all cells pass** (or 600 s cap). You are released early once your cell is cleared ([[Roll Call & Shakedown]]).
9. The sweep **confiscates Contraband, Tools, and Weapons** left in the open (world pickups). Crafting parts and consumables are safe.
10. Items **hidden in the pillow stash or carried in your inventory are not confiscated** — hiding works.

## Night

11. At night, verifier guards check every bed with a presence sphere (radius = cell interior radius, default 2.5 m).
12. A **fake bed dummy passes the check**; an empty bed triggers a **Lockdown alert**.
13. Fake dummies are **discovered and destroyed at morning line-up**, raising a Suspicion alert.

## Items & crafting

14. Inventory = **6 slots**; only crafting parts stack.
15. Crafting consumes ingredients and produces exactly the recipe result ([[Crafting]]).
16. Loot spawns are seeded — the same world seed reproduces the same item layout.

## Social

17. Affinity per inmate ranges **-100 to +100**; positive gains shrink as affinity rises (soft cap); betrayals hit hard (-50) and are never capped ([[Social & Reputation]]).
18. One greeting per inmate per phase counts; favors pay affinity for delivered items.
19. Prison-wide reputation = the average of all inmate affinities: Outsider < 25 ≤ Associate < 50 ≤ Respected < 75 ≤ Kingpin.

## Escape, capture & consequences

20. Escape is the goal; the game does not force it. Routes require tools, timing, and defeated checks ([[Escape Routes & Mechanics]]).
21. **Crossing the outer boundary = escaped** — win screen with run stats, any route, any time ([[Escape Completion System]]).
22. **Restricted zones** (perimeter band; cafeteria/workshop at night) count as escape attempts — compliance and grace don't protect you there.
23. Spotted in a restricted zone → **solitary confinement**: inventory confiscated (pillow stash survives), **Mental Health −20 / Strength −10**, day skips to the next Morning Roll Call.
24. After a caught escape, **suspicion lasts 2 days**: guard detection reaches 40% farther (10 → 14 m).
25. **Stats regenerate +5/day** at Morning Roll Call. Strength below 50 slows sprinting (×2.0 → ×1.5).

---

*When a rule changes (e.g. new phase, new confiscation category, real heat meter), update this note in the same change as the code.*
