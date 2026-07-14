# Development Workflow

How features get built in this project. **This vault is the source of truth** — design lives here first, code implements it.

## The loop: Design → Implement → Verify → Sync

```
Obsidian (design) → Cursor (implement) → Unity MCP (execute & verify) → Git (record)
```

### 1. Design in Obsidian first
- Every feature starts as a note in this vault (use [[Feature Spec Template]]).
- Spatial features get a diagram or canvas. Numbers beat adjectives ("cells are 4×5.5 m", not "cells are small").
- If it's not written in the vault, it's not decided yet.

### 2. Implement with Cursor
- Reference the vault note explicitly when starting a chat.
- One feature per chat / per `feat/...` branch (see [[Git & Branching]]).
- Prefer **repeatable editor scripts** (menu items under `Prison/...`) over manual scene edits — see [[Editor Tooling]].

### 3. Execute & verify with Unity MCP
- Cursor runs the editor scripts in the live editor, checks console logs, queries object positions, and fixes issues without leaving chat.
- Gotcha: after editing a script, Unity must recompile before re-running it.

### 4. Sync back
- If implementation deviated from the spec, **update the vault note** so docs match reality.
- Commit to `dev`, playtest, promote to `main` only for releases.

## Rules that keep this working

1. **Obsidian wins conflicts.** If code/scenes disagree with the vault, the vault is right (unless explicitly overridden in chat).
2. **Don't hand-edit generated scene objects.** Change the editor script and re-run, or manual tweaks get wiped on the next rebuild.
3. **Update `Assets/Docs/` only for technical/test docs** — design belongs here.
4. **Log milestones** in [[Prison Escape Devlog Dashboard]].

## Where things go

| Content | Location |
|---|---|
| Vision, world rules, layout | `01 Game Design/` |
| System specs (one note per system) | `03 Systems/` |
| Content inventory (scenes, prefabs, materials) | `04 Content & Assets/` |
| Codebase map, tooling, testing | `05 Engineering/` |
| Workflow, templates | `06 Process/` |
| Devlog | vault root dashboard |
