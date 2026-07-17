# Security, Heat & Alerts

The "being watched / in trouble" layer: an attention eye HUD, guard enforcement, and global alert events.

## Heat — the truth

**There is no numeric heat meter.** "Heat" today is a **3-state attention UI** (`PrisonHeatUI`) plus static alert events:

| Eye state | Meaning | Triggers |
|---|---|---|
| Hidden | Safe | Not blocked, no guard attention, not in grace |
| Half | Caution | Any guard's attention zone covers you, OR mandatory travel grace active |
| Open | Danger | Arrested (`MovementBlocked`) OR (mandatory phase + not grace + non-compliant) |

If a future design wants accumulating/decaying heat, that's a new system — spec it here first.

## Alerts (`PrisonSecurityAlerts`, static events, tested)

| Event | Raised by |
|---|---|
| `OnLockdown` | Night bed check failure (`"Bed check failed — cell {n}"`) · midday/evening count mismatch (`FormalCountMonitor`) |
| `OnSuspicion` | Fake bed dummy discovered at morning line-up |

**No listeners implement consequences yet** — no lockdown mode, chase escalation, or punishment flows. These hooks are where the escape-failure loop should attach ([[Roadmap & Priorities]]).

> ✅ **Implemented (7/15/2026):** `OnLockdown` is raised for **any cell-count mismatch** — `FormalCountMonitor` checks presence when a midday/evening count ends (`"Count mismatch — Midday Count: 15/16 accounted for"`), alongside the existing night bed-check lockdown ([[Time & Schedule]] § The Count). Consequence listeners remain the open gap above.

> ✅ **Snitch tips (on `dev`):** `SnitchSystem` / `SocialWorld` can raise suspicion events and queue a **targeted morning shakedown** via `MorningShakedownSweeper`. See [[Social & Reputation]].
>
> 🔭 **Still polish:** snitch path does **not** currently floor the heat eye at half for 1 day via `PrisonSuspicion` (that floor is the post-capture suspicion window). Per-guard Trust → ±2 m / +10 s tolerance is also still polish ([[Guard AI]]).

## Enforcement chain

```
Schedule (mandatory phase) → prisoner non-compliant
    → GuardDetection spots (10 m cone / 6 m proximity)
    → GuardFSM Escort (arrest at 2 m)
    → SendToCell → released after 1 s
```

Travel grace (50 s) suppresses enforcement while inmates walk to a newly mandatory phase ([[Time & Schedule]]).

## Night security

Night-verifier guards walk each cell during Lights Out / Night Roll Call and verify bed presence (sphere check). A [[Escape Routes & Mechanics|fake bed dummy]] passes the check; an empty bed raises lockdown.

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Prison/PrisonHeatUI.cs` | Attention eye HUD |
| `Assets/Scripts/Singleplayer/Security/PrisonSecurityAlerts.cs` | Static alert events |
| `Assets/Scripts/Singleplayer/AI/GuardDetection.cs` | Attention zones + bed checks |

Related: [[Guard AI]] · [[Roll Call & Shakedown]] · [[Escape Routes & Mechanics]] · [[UI & HUD]]
