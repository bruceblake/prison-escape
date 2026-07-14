# Devlog Dashboard

Newest first. Log milestones here after each work session (see [[Development Workflow]]).

## 7/14/2026
- **Vault restructured as full source of truth** — complete design docs for every system, content inventory, engineering docs, and process notes. Entry point: [[Home]].
- **Connected diagram layout implemented** — `PrisonLevel1` rebuilt with 18 shared-edge floor plates matching the layout diagram (Security | Cells 1–8 | Cafeteria | Cells 9–16 | Workshop, courtyard north, showers south, perimeter loop). Walls only on exterior edges, roofs except courtyard, ~635 lights, scratch-built furniture. See [[Prison Layout — Minimum Security]].
- `dev` and `main` synced at the same commit; Obsidian-source-of-truth rule added to `.cursor/rules/`.

## Earlier (pre-dashboard highlights)
- East cell block renamed to cells 09–16; registry wired for 16 cells
- Low-poly character visual pipeline (Player/Guard/Prisoner, 2.6 m rigs) — [[Character Visuals]]
- 136 EditMode tests green — [[Testing & QA]]
- Core sim systems in place: schedule, zones, roll call/shakedown, guard/prisoner AI, social math, crafting, inventory, vent/fake-bed/stash escape mechanics — [[Systems Overview]]
