# Development Workflow

How features get built in this project. **This vault is the source of truth** — design lives here first, code implements it.

## The loop: Brainstorm → Design → Implement → Verify → Sync

```
Chat (brainstorm) → Obsidian (spec) → Cursor (implement) → Unity MCP (execute & verify) → Git (record)
```

### 0. Brainstorm in chat first
- Before writing any spec, talk the feature through: what's the fantasy, how does it use existing systems, what are the edge cases, what's out of scope.
- The agent should push back, surface constraints from the codebase, and propose options with trade-offs — not just agree.
- Nothing is decided during brainstorming. The conversation ends by distilling decisions into a spec note.

### 1. Design in Obsidian
- Every feature starts as a note in this vault (use [[Feature Spec Template]]) — the distilled output of the brainstorm.
- Spatial features get a diagram or canvas. Numbers beat adjectives ("cells are 4×5.5 m", not "cells are small").
- If it's not written in the vault, it's not decided yet.

### 2. Implement with Cursor
- Reference the vault note explicitly when starting a chat.
- One feature per chat / per `feat/...` branch in a **worktree** when practical (see [[Git & Branching]]).
- **Small PRs only** — one concern / one milestone per PR into `dev`. Never accumulate an epic on one branch and dump it. Policy: [[Small PRs & Feature Slices]]. Current Social/Career split: [[Social & Career PR Slice Plan]].
- Prefer **repeatable editor scripts** (menu items under `Prison/...`) over manual scene edits — see [[Editor Tooling]].

### 3. Execute & verify with Unity MCP
- Cursor runs the editor scripts in the live editor, checks console logs, queries object positions, and fixes issues without leaving chat.
- Gotcha: after editing a script, Unity must **recompile** before re-running it (stale DLL = old layout/HUD code runs silently).
- Layout changes: re-run **Prison → Layout → Run Full Build** and confirm `[PrisonLayout]` log lines (wall count, cell keep-outs, light count).

### 4. Sync back
- If implementation deviated from the spec, **update the vault note** so docs match reality.
- Commit to `dev`, playtest, promote to `main` only for releases.

## Rules that keep this working

1. **Obsidian wins conflicts.** If code/scenes disagree with the vault, the vault is right (unless explicitly overridden in chat).
2. **Don't hand-edit generated scene objects.** Change the editor script and re-run, or manual tweaks get wiped on the next rebuild.
3. **Update `Assets/Docs/` only for technical/test docs** — design belongs here.
4. **Log milestones** in [[Prison Escape Devlog Dashboard]].
5. **Cursor enforces this loop** — `.cursor/rules/development-workflow.mdc` (always on). Agents must run the end-of-task checklist, report gaps, and **ask you before commit/push or declaring done**.
6. **Small PRs are mandatory** — see [[Small PRs & Feature Slices]]. Agents propose a slice list before committing a large feature; refuse mixed-system mega-PRs without an explicit waiver.

## When YOU test vs when YOU commit

| Step | Who | Rule |
|------|-----|------|
| Agent verify (compile, MCP, EditMode tests) | Cursor agent | After every code/scene change — automatic |
| **Playtest in Unity** | **You** | **Before** telling the agent to commit/push anything that affects gameplay, UI, layout, or scenes |
| Commit / push / PR | Cursor agent | **Only when you explicitly ask in chat** — never implied by "keep going" |
| Merge to `dev` | You (via PR) | After branch playtest passes |

**Playtest gate:** If you ask to commit without confirming playtest, the agent **must stop and ask** — pass, fail, not yet, or `skip playtest, commit anyway`. The agent does not commit until you answer.

**Doc-only / rules-only changes:** Playtest N/A; agent still asks before commit.

## End-of-task checklist (agents)

Before closing a session, verify and report:

- [ ] Vault spec + hub notes updated (including per-surface UI docs when UI changes)
- [ ] [[Systems Overview]] / feature spec status current
- [ ] Devlog entry added (newest first)
- [ ] Unity verified (compile / MCP / tests — agent)
- [ ] **User playtest** (pass / fail / not yet / waived — **required before commit** for runtime changes)
- [ ] Git: correct branch; **user explicitly requested commit/push in chat**
- [ ] `graphify-out/` refreshed after code changes (hook or manual)

## Where things go

| Content | Location |
|---|---|
| Vision, world rules, layout | `01 Game Design/` |
| System specs (one note per system) | `03 Systems/` |
| Content inventory (scenes, prefabs, materials) | `04 Content & Assets/` |
| Codebase map, tooling, testing | `05 Engineering/` |
| Workflow, templates | `06 Process/` |
| Devlog | vault root dashboard |
