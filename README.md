# Prison Escape

A **single-player prison-escape simulation** built in **Unity 6000.4.4f1 (URP)**, with a secondary **multiplayer FPS** layer.

> **Main goal: escape the prison through various escape routes.** Study the daily routine, craft the right tools, defeat the guards' detection (especially the night bed check), and slip out — without getting caught. Every other system (schedule compliance, social standing, crafting, contraband) exists to enable, fund, or cover an escape attempt.

## Core loop

The prison runs on a **daily schedule** (roll call, meals, free time, lights out, night/morning checks). The player is an inmate who must *stay (or appear) compliant* while secretly preparing an escape:

- **Schedule & compliance** — timed phases, location zones, routine HUD.
- **Social & reputation** — greet, gift, do favors, or betray inmates to climb reputation tiers.
- **Economy & crafting** — earn cash, loot/hide contraband, craft tools and weapons.
- **AI** — guards patrol, detect non-compliance, run shakedowns, and verify bed presence at night; prisoners follow the routine.
- **Escape mechanics** — vent routes (unscrew covers), fake-bed dummies, contraband stashes, custom barred cell doors.

## Project layout

```
Assets/
  Scripts/
    Shared/        # Prison sim, social, crafting, interaction, UI (SP + MP)
    Singleplayer/  # AI, player, items, security, interaction, managers
    Multiplayer/   # Networking, lobby, FPS player + weapons
    Editor/        # Editor tooling (e.g. social balance simulator)
  Tests/Editor/    # EditMode unit tests
  Scenes/          # MainMenu, Game, PrisonLevel1, SinglePlayerScene, ...
  Docs/            # Feature & system documentation
docs/              # Engineering docs (PR plan, etc.)
```

## Getting started

1. Install **Unity 6000.4.4f1** (URP).
2. This repo uses **Git LFS** for large binary assets — install it once: `git lfs install`, then clone normally (LFS objects pull automatically).
3. Open the project folder in Unity Hub and load a scene from `Assets/Scenes/`.

## Branching & release workflow

| Branch | Purpose |
|--------|---------|
| **`feat/*`, `fix/*`** | New work in isolation |
| **`dev`** | Integration — **playtest here** |
| **`main`** | Production — merge only when explicitly releasing |

See **`docs/WORKFLOW.md`** for the full policy (feature → `dev` → `main` on request).

## Documentation

- `docs/WORKFLOW.md` — branching policy (dev for testing, main for production)
- `Assets/Docs/Prison_Rebuild_Master_Plan.md` — full prison ProBuilder rebuild plan
- `Assets/Docs/Game_Features_And_Test_Coverage.md` — features + test coverage
- `Assets/Docs/Prison_Social_And_Reputation_System.md` — social system design
- `docs/PR_PLAN.md` — how the codebase history is structured into feature PRs

## Tests

EditMode unit tests live in `Assets/Tests/Editor/` (door logic, social math, prison rules/labels, crafting/inventory/loot, **prison layout validation**). Run them via **Window → General → Test Runner → EditMode**.

For prison level work, also use **Prison → Rebuild → 4 — Validate Layout (Report)** in the Unity editor.
