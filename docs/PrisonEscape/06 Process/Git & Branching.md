# Git & Branching

Two-branch release model. Repo: `github.com/bruceblake/prison-escape`.

| Branch | Purpose |
|---|---|
| `dev` | Integration and testing. All day-to-day development targets this branch. |
| `main` | Production only. Stable, release-ready code. |

## Workflow

1. Start from `dev`: `git checkout dev && git pull origin dev`
2. Feature branches off `dev`: `feat/...`, `fix/...`, `chore/...`
3. PRs target **`dev`** (never `main` directly)
4. Test and playtest on `dev`
5. Promote `dev` → `main` only when releasing
6. Hotfixes (rare): branch from `main`, fix, PR to `main`, then sync back to `dev`

## Conventions

- Commit messages: focus on *why*; follow existing style (`feat(...)`, `fix(...)`, `docs(...)`, plain sentences also used)
- The `graphify-out/` knowledge graph regenerates on commit/branch-switch hooks — its churn is expected
- Unity `.meta` files always commit alongside their assets
- Large binaries (images) go through Git LFS

See also: [[Development Workflow]]
