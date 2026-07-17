# Small PRs & Feature Slices

**Status:** Active policy (7/16/2026)
**Also:** [[Development Workflow]] · [[Git & Branching]] · Cursor rules `git-branching.mdc` / `development-workflow.mdc`

## Why

Large omnibus PRs (docs + Career + Social + scene polish in one blob) make history hard to read, playtests hard to scope, and reverts expensive. **Every shippable unit of work should be a small PR into `dev`.**

This is a **recurring pattern** for the rest of development — not a one-time cleanup.

## What "small" means

| Prefer | Avoid |
|---|---|
| One milestone / one concern (e.g. "social relationship math + tests") | Whole epic in one PR |
| Reviewable in one sitting (~300–800 LOC code delta when practical) | Multi-thousand-line dumps |
| Clear playtest script of **3–8 steps** | "Play the whole game" |
| Vault sync for **that slice only** | Rewriting half the vault in a code PR |
| Stacked PRs when B needs A | Parallel unrelated features on one branch |

Hard caps are guidance, not religion: a 1,200-line pure UI surface with tests can still be one PR if it is **one concern**. A 200-line PR that mixes Career transfer + Social snitch + NavMesh is too big.

## Slice taxonomy (use these names)

| Prefix | Use for |
|---|---|
| `docs/...` | Vault / process / roadmap only |
| `chore/...` | Tooling, installers, asset moves, graphify, no gameplay |
| `feat/<system>-<slice>` | One system milestone (e.g. `feat/social-m1-foundation`) |
| `fix/...` | Bugfix only |

Branch = worktree slug = PR title theme. One PR per branch.

## Standard slice loop (every PR)

```
1. Spec already in vault (or tiny docs PR first)
2. Worktree off latest origin/dev
3. Implement ONLY this slice's files
4. EditMode tests for this slice
5. Unity MCP compile / console clean
6. Vault status lines for THIS slice only
7. You playtest THIS slice
8. Commit / push / PR → base:dev  (only when you ask)
9. Merge → next slice off new tip of dev
```

**Do not accumulate** five slices on one long-lived branch and open one mega-PR at the end.

## Stacking vs independent

- **Independent** (default): PR targets `dev`; can merge in any order if tests pass alone.
- **Stacked:** PR B targets branch A (or rebase onto A) only when B cannot compile without A. Prefer merging A to `dev` first, then branching B from `dev`.

## Splitting an existing blob (recovery pattern)

When the working tree already contains a mega-diff (like Social + Career local work):

1. **Do not rewrite already-merged history** on `dev`/`main` unless you explicitly order a history surgery.
2. Snapshot recoverably (`git stash create` → `refs/backup/pre-split-*`) before moving files.
3. Return primary checkout to **`dev`**, pull `origin/dev`.
4. Cut **ordered slices** into fresh worktrees/branches; stage **named files only** (no `git add -A`).
5. Leave unrelated dirty files (scene polish, NavMesh, anim controllers) for separate `chore/` PRs or discard only with your OK.
6. After each slice merges, vault + Devlog note that slice as landed.

Canonical split order for the current Social/Career blob: [[Social & Career PR Slice Plan]].

## Agent rules (mandatory)

Agents **must**:

- Propose a slice list **before** committing a large feature
- Refuse to open a PR that mixes unrelated systems without your explicit waiver (`ok mega-PR: <reason>`)
- Prefer worktrees; keep primary on `dev`
- Update this note if slice conventions change

Agents **must not**:

- Force-push or rewrite merged `dev`/`main` history without explicit chat approval
- Bundle "while we're here" scene/prefab polish into a systems PR

## Related

[[Development Workflow]] · [[Git & Branching]] · [[Social & Career PR Slice Plan]] · [[Feature Spec Template]]
