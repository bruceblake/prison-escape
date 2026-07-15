# 🔑 Prison Escape — Vault Home

**This vault is the source of truth for the entire game.** Design decisions live here first; Unity code implements them. Start every feature from [[Development Workflow]].

## 01 Game Design
- [[Game Vision & Core Loop]] — what the game is, pillars, the escape fantasy
- [[World Rules]] — the 21 laws of the game world as implemented
- [[Prison Layout — Minimum Security]] — the MVP map (diagram + dimensions)
- [[Roadmap & Priorities]] — what's next (top: **escape win condition**)
- [[Prison Escape]] — original founding note (multi-prison vision)

## 02 Features — active feature specs
- [[Escape Completion System]] — 🚧 in progress (win state, solitary, suspicion, stats)
- [[Social Ecosystem & Gangs]] — 📐 specced (v1 social teardown; identities & personalities, memory & gossip, gangs, trading & bribes, two-way favors, snitching)

## 03 Systems — one note per system
- [[Systems Overview]] — connection map + status of everything
- Core sim: [[Time & Schedule]] · [[Locations, Zones & Cells]] · [[Roll Call & Shakedown]]
- AI: [[Guard AI]] · [[Prisoner AI & NPCs]]
- Progression: [[Social & Reputation]] (v1 — being replaced by [[Social Ecosystem & Gangs]]) · [[Inventory & Items]] · [[Crafting]] · [[Loot & Economy]]
- The goal: [[Escape Routes & Mechanics]]
- Pressure: [[Security, Heat & Alerts]]
- Presentation: [[UI & HUD]]
- Online: [[Multiplayer & Networking]]

## 04 Content & Assets
- [[Content Inventory]] — scenes, prefabs, materials, models, packages
- [[Item Catalog]] — every item + recipe usage + data issues
- [[Character Visuals]] — procedural low-poly character pipeline
- [[Blender Asset Kit]] — modular 3D kit + assembled prison (PrisonKit.blend → FBX pipeline)

## 05 Engineering
- [[Codebase Map]] — folder layout, core abstractions, conventions
- [[Editor Tooling]] — layout runner and friends (regenerate, don't hand-edit)
- [[Testing & QA]] — 136 EditMode tests, coverage matrix, testing rules

## 06 Process
- [[Development Workflow]] — Obsidian → Cursor → Unity MCP → Git loop
- [[Feature Spec Template]] — copy for every new feature
- [[Git & Branching]] — dev/main model

## Devlog
- [[Prison Escape Devlog Dashboard]]

---

### The game in one paragraph
A prison-escape sim: a fixed daily routine (roll call → meals → free time → lights out) creates the puzzle; the player appears compliant while looting parts, crafting tools, building social standing, and defeating guard checks (vision cones, morning shakedowns, night bed verification) to open an escape route. Minimum-security prison first; Medium, High, and Supermax to follow.
