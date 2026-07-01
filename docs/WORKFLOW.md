# Development Workflow

This document is the **canonical branching policy** for Prison Escape. Cursor agents, contributors, and future-you should follow it.

## Branches

| Branch | Role |
|--------|------|
| **`main`** | Production. Stable, release-ready builds only. |
| **`dev`** | Integration + **Bruce playtesting**. Day-to-day target for merged work. |
| **`feat/*`, `fix/*`, `chore/*`** | Short-lived feature branches cut from `dev`. |

## Flow

1. **Branch** from `dev` → `feat/my-thing`
2. **Implement** + automated tests + Unity verification on the feature branch
3. **PR → `dev`** (base branch is always `dev` for new features)
4. **Bruce tests** in Unity on `dev` and gives feedback
5. **When Bruce explicitly asks**, open PR **`dev` → `main`** for production

> **Rule:** Nothing goes to `main` until Bruce says to release/merge to production.

## Commands (cheat sheet)

```bash
git checkout dev
git pull origin dev
git checkout -b feat/my-feature

# ... work, commit ...

git push -u origin feat/my-feature
# Open PR: base = dev

# After Bruce approves playtest on dev and asks for production:
# Open PR: base = main, compare = dev
```

## Testing expectations before merging to `dev`

- EditMode tests pass (**Window → General → Test Runner → EditMode**)
- No compile errors in Unity console
- For level/layout work: run relevant **Prison/** menu items and note the test checklist in the PR

## Related docs

- `docs/PR_PLAN.md` — historical PR stack structure
- `Assets/Docs/Prison_Rebuild_Master_Plan.md` — prison level full rebuild plan
