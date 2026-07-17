# 🔑 Prison Escape — Vault Home

**This vault is the source of truth for the entire game.** Design decisions live here first; Unity code implements them. Start every feature from [[Development Workflow]].

## 01 Game Design
- [[Game Vision & Core Loop]] — what the game is, pillars, the escape fantasy
- [[Prison Career Ladder]] — ✅ M1–M5 on `dev` (County → State ×3 → Federal ×5; escape = caught & transferred; worlds, global carry, difficulty curves). M6+ = facility scenes
- [[World Rules]] — the laws of the game world (rules 1–33 as implemented; social = Respect/Trust v3)
- [[Prison Layout — Minimum Security]] — the Dev Sandbox map (diagram + dimensions)
- [[Roadmap & Priorities]] — what's next (top: **escape route geometry** — vents / fence)
- [[Prison Escape]] — original founding note (multi-prison vision)

## 02 Features — active feature specs
- [[Escape Completion System]] — ✅ on `dev`: capture path unchanged; career boundary → transfer ceremony; Dev Sandbox still "YOU ESCAPED"
- [[Social Ecosystem & Gangs]] — ✅ **v3** on `dev` (Respect/Trust, Standing bands, gangs, Talk Menu, dossier, trading & bribes, favors, snitching). Polish/assets still open — see system note
- [[World Saves & Start Screen]] — ✅ on `dev` (named career worlds, JSON saves, MainMenu → worlds + prison-select hub, locked silhouettes)
- [[Facility Transfer & Graduation]] — ✅ on `dev` (escape → caught-transfer ceremony, County sentence clock, global carry / local reset, career win)

## 03 Systems — one note per system
- [[Systems Overview]] — connection map + status of everything
- Core sim: [[Time & Schedule]] · [[Locations, Zones & Cells]] · [[Roll Call & Shakedown]]
- AI: [[Guard AI]] · [[Prisoner AI & NPCs]]
- Progression: [[Social & Reputation]] (v3 — `SocialWorld`; design: [[Social Ecosystem & Gangs]]) · [[Inventory & Items]] · [[Crafting]] · [[Loot & Economy]]
- The goal: [[Escape Routes & Mechanics]]
- Pressure: [[Security, Heat & Alerts]]
- Presentation: [[UI & HUD]] (incl. [[Talk Menu & NPC Profile]] · [[Social Dossier — Relationships & Gangs]])
- Online: [[Multiplayer & Networking]]

## 04 Content & Assets
- [[Content Inventory]] — scenes, prefabs, materials, models, packages
- [[Item Catalog]] — every item + recipe usage + data issues
- [[Character Visuals]] — procedural low-poly character pipeline
- [[Blender Asset Kit]] — modular 3D kit + assembled prison (PrisonKit.blend → FBX pipeline)

## 05 Engineering
- [[Codebase Map]] — folder layout, core abstractions, conventions
- [[Editor Tooling]] — layout runner and friends (regenerate, don't hand-edit)
- [[Testing & QA]] — EditMode coverage matrix and testing rules *(P1 vault sync: count ~195 on tip)*

## 06 Process
- [[Development Workflow]] — Obsidian → Cursor → Unity MCP → Git loop
- [[Small PRs & Feature Slices]] — **mandatory** one-concern PRs; mega-diff recovery
- [[Social & Career PR Slice Plan]] — post-merge remaining gaps (Social assets, M6+ scenes, polish)
- [[Feature Spec Template]] — copy for every new feature
- [[Git & Branching]] — dev/main model

## Devlog
- [[Prison Escape Devlog Dashboard]]

---

### The game in one paragraph
A prison-escape sim: a count-driven daily routine (morning count → meals → work & programs → midday/evening counts → yard time → final lockdown) creates the puzzle; the player appears compliant while looting parts, crafting tools, building social standing, and defeating guard checks (vision cones, morning shakedowns, night bed verification) to open an escape route. Escaping never means freedom until the top: you're caught and transferred up a career ladder of nine facilities — County → State ×3 → Federal ×5 — carrying cash, respect, and gang ties with you ([[Prison Career Ladder]]). The current minimum-security prison is the Dev Sandbox.
