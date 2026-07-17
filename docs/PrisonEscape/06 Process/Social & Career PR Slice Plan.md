# Social & Career PR Slice Plan

**Status:** Complete — Career/Social integrated; Vault Phase 7 done; **7/17 gap-audit** landed (#90–#95). Post-merge gaps below.
**Policy:** [[Small PRs & Feature Slices]]
**Specs:** [[Social Ecosystem & Gangs]] · [[Prison Career Ladder]] · [[World Saves & Start Screen]] · [[Facility Transfer & Graduation]]
**Backup of pre-split WIP:** `backup/pre-split-20260716`. Open feature PRs: none.

## Remaining gaps (not vault drift)

1. **`Resources/Social/`** — run **Tools → Prison → Social → Install Social Assets** and commit SOs (code catalogs fallback today).
2. **Social polish** — richer Talk chrome / dossier art pass ([[Talk Menu & NPC Profile]]).
3. **Career M6+** — State ×3 / Federal ×5 facility scenes (County stub + Dev Sandbox exist).
4. **Escape route geometry** — vent corridors + courtyard fence so the boundary is reachable ([[Roadmap & Priorities]]).
5. **Playtest** — Career hub → facility enter → Talk / dossier / transfer ceremony smoke after next Unity session.
6. **PrisonLevel1 scene drift** — 3 EditMode layout/door tests failing (#81 snapshot); run **Prison → Fix Cell Doors & Waypoints**.

## Historical execution (2026-07-16)

| Stack | PRs | Outcome |
|---|---|---|
| Process | #42 · #48 · #85 | Small-PR policy on `dev` |
| Career | #51–#57 | Worlds, facilities, hub, transfer, County scene |
| Social | #58–#72 | v3 `Shared/Social` + v1 teardown |
| Bridge / chores | #73 draft · #49 · #74–#84 | Difficulty/gang hooks + polish |
| Vault Phase 7 | #86–#88 (+ P3) | Obsidian hub/engineering/surface truth |

Mega PRs #38 · #43–#47 · #50 closed/superseded. Do **not** rewrite merged `dev` history.

```mermaid
flowchart LR
  snap[Backup snapshot]
  career[Career micro-stack]
  social[Social micro-stack]
  bridge[Bridge and chores]
  vault[Vault Phase 7]
  snap --> career --> social --> bridge --> vault
```

## Locked process rules (still in force)

1. One concern per PR into `dev` ([[Small PRs & Feature Slices]]).
2. Feature work in worktrees; primary checkout stays on `dev`.
3. Playtest gate for gameplay; docs-only = N/A ([[Development Workflow]]).

Related: [[Development Workflow]] · [[Git & Branching]] · [[Prison Escape Devlog Dashboard]]
