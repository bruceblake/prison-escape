# Git & Branching

Two-branch release model. Repo: `github.com/bruceblake/prison-escape`.

| Branch | Purpose |
|---|---|
| `dev` | Integration and testing. All day-to-day development targets this branch. |
| `main` | Production only. Stable, release-ready code. |

## Workflow

1. Start from `dev` on the **primary checkout** (`C:\Users\bruce\3dgame`): `git checkout dev && git pull origin dev`
2. Feature branches off `dev`: `feat/...`, `fix/...`, `chore/...` — do the work in a **git worktree** (below), not by parking the primary folder on the feature branch long-term
3. PRs target **`dev`** (never `main` directly)
4. Test and playtest on the feature worktree, then merge to `dev`
5. Promote `dev` → `main` only when releasing
6. Hotfixes (rare): branch from `main`, fix, PR to `main`, then sync back to `dev`

## Worktrees

Parallel feature folders live next to the repo:

| Path | Role |
|---|---|
| `C:\Users\bruce\3dgame` | Primary — keep on `dev` day-to-day |
| `C:\Users\bruce\3dgame-worktrees\<slug>` | One linked worktree per branch (`feat/foo` → `feat-foo`) |

```powershell
# New feature from latest dev
cd C:\Users\bruce\3dgame
git checkout dev && git pull origin dev
git worktree add -b feat/<name> C:\Users\bruce\3dgame-worktrees\feat-<name> dev

# Existing branch (must not already be checked out)
git worktree add C:\Users\bruce\3dgame-worktrees\feat-<name> feat/<name>
```

Open the **worktree** folder in Cursor and in Unity (each worktree has its own `Library/` — first open reimports). After merge: `git worktree remove <path>`.

Cursor agents follow `.cursor/rules/git-worktrees.mdc` (always on) and switch the chat root with `move_agent_to_root` after creating a worktree.

## Integrated feature branches (7/15/2026)

These branches are **fully represented on `dev`** — do not re-merge wholesale (they predate BlenderKit + schedule work and conflict heavily):

| Branch | Integration |
|---|---|
| `feat/escape-completion` | Merged via PR #40 (7/14) |
| `feat/blenderkit-assets` | Merged (facility install, polish pass) |
| `feat/realistic-schedule` | Merged (13-phase count-driven day) |
| `feat/prison-probuilder-rebuild` | Cherry-picked ProBuilder pipeline + layout tests (7/15); BlenderKit remains primary build |
| `feat/prison-level-layout-and-loot` | `PrisonLootSetupRunner` + `SpawnPlacementUtility` on `dev`; layout v4 superseded by BlenderKit |
| `feat/low-poly-character-visuals` | Merged via PR #37; superseded by rigged BlenderKit characters on `dev` |

## Conventions

- Commit messages: focus on *why*; follow existing style (`feat(...)`, `fix(...)`, `docs(...)`, plain sentences also used)
- The `graphify-out/` knowledge graph regenerates on commit/branch-switch hooks — its churn is expected
- Unity `.meta` files always commit alongside their assets
- Large binaries (images) go through Git LFS

See also: [[Development Workflow]]
