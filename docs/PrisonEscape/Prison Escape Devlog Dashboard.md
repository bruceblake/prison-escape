# Devlog Dashboard

Newest first. Log milestones here after each work session (see [[Development Workflow]]).

## 7/16/2026 (night — small PR split execution)

- **Split & push** — Social+Career WIP resliced into micro-PRs: Career #51–#57, Social #58–#72, bridge draft #73, chores #49/#74–#84. Backup: `backup/pre-split-20260716`. Mega PRs #43–#47/#50 closed.
- **Integrated to `dev`** — Career #51–#57 + Social #58–#72 + bridge + chores #49/#74–#84 merged; open PRs closed; feature branches deleted. Tip `d25a3ad`.
- **Policy live** — [[Small PRs & Feature Slices]] · [[Social & Career PR Slice Plan]].
- **Still open after merge:** Career↔Social difficulty/gang-tag bridge; Social Resources assets; vault Implemented flip post-playtest; scene polish chores.

## 7/16/2026 (evening — small PR policy + slice plan)
- **Recurring process** — new vault note [[Small PRs & Feature Slices]]: one concern / one milestone per PR into `dev`; no epic mega-PRs; mega-diff recovery via backup ref + named-file slices; do not rewrite merged history without explicit order. Wired into [[Development Workflow]], [[Git & Branching]], Home, and Cursor rules (`git-branching.mdc`, `development-workflow.mdc`).
- **Social + Career slice plan** — [[Social & Career PR Slice Plan]]: Phase 0 docs → Career foundation → Career hub/transfer → Social M1 → Talk/Dossier → Economy/consequences → Guard polish → vault sync; unrelated polish as separate `chore/` PRs. Remaining implementation gaps listed (Social assets install, guard trust modifiers, Career M6+ scenes).
- **Locked:** leave PR #41 history alone; split the **uncommitted** local blob only. Execution of the split waits on your "go".
- Docs-only this session — no code/git slice execution yet.

## 7/16/2026 (social ecosystem v3 — vault design pass)
- **Research + vault expand** — deep pass on real prison social order (inmate code → gang governance / Skarbek), **The Escapists / Escapists 2** (Opinion, profile tabs, gifts, favors, trade markers, name colors; no real gangs), and **Back to Dawn** (exclusive gangs, rapport/gifts, relationship + faction UIs, gang shops). Synthesis locked into [[Social Ecosystem & Gangs]] **v3**.
- **Feature spec v3** — Standing bands Enemy→Confidant; complete gang membership (Outsider→Trusted, exclusive join, Traitor lockout, Syndicate under-bed store); career carry vs local reset; open questions closed (intimidation fail = report risk; snitch discovery = gossip + Old-Timer; cash = Hustlers + favors + light job).
- **New UI notes** — [[Talk Menu & NPC Profile]] (tabbed real-time overlay, overhead `!`/coin, band-tinted nameplates) · [[Social Dossier — Relationships & Gangs]] (notebook Relationships + Gangs pages — full visual social web).
- **Synced** — [[Social & Reputation]], [[UI & HUD]], [[Notebook & Crafting UI]], [[Status & World UI]], [[Inventory & Items]], [[Loot & Economy]], [[Systems Overview]], [[Prisoner AI & NPCs]], [[Home]], [[Roadmap & Priorities]]. Docs-only — no code.
- **Next:** implement on `feat/social-ecosystem` (worktree) starting M1 Foundation when you greenlight.

## 7/15/2026 (career ladder — design specced, D0)
- **Prison Career Ladder specced** — new primary design doc [[Prison Career Ladder]]: 9-facility career (County → State ×3 → Federal ×5) + Dev Sandbox (the current Min-Sec map leaves the career path); **escape = caught & transferred**, never freedom until Federal ADX (career win, world stays playable); County alternatively serves a 7-day sentence; named **world saves** with global carry (cash, respect, gang, stats, recipes) vs full local reset per facility entry — farming easier prisons is intended play; full difficulty/economy curve table + soft Fed-tier transfer gates; JSON save model + per-visit seeds.
- **Feature specs** — [[World Saves & Start Screen]] (CareerWorld JSON store, MainMenu → worlds list + prison-select hub, black-silhouette locked slots) and [[Facility Transfer & Graduation]] (boundary/sentence → transfer ceremony, `EscapeEndScreenUI` rewrite, state-change ordering, revisit reset).
- **Synced** — [[Game Vision & Core Loop]] (ladder + "time investment is strategy" pillar; stale escape-completion gap fixed), [[World Rules]] (rule 22 annotated; career rules 27–33 added as specced), [[Escape Completion System]] (v2 rewrite callout), [[Screens & Menus]], [[Home]], [[Roadmap & Priorities]] (career ladder = Now #2; Later list updated).
- **Next:** M1 `feat/career-world-saves` on approval. Docs-only — no code touched.

## 7/15/2026 (night — playtest: idle, guards, lighting, HUD)
- **NPC idle circling** — stand points are now deterministic + laterally spread by `cellIndex`; on arrival `PrisonerAI` stops the NavMeshAgent (`isStopped` + `ResetPath`) so inmates hold still instead of repathing/circling crowded meal/yard stands. See [[Prisoner AI & NPCs]].
- **Guards** — spawn table already had 4 rows; spawns now **snap to NavMesh**, normalize empty shift windows to always-on, and log each spawn so missing guards are diagnosable. See [[Guard AI]].
- **Flat look** — root cause: empty/broken `PrisonPostProcess` profile + ~222 BlenderKit `Light_*` meshes with **no Light components** (only 2 directionals). New menu **Prison → Lighting/Configure Post-Process + Fixture Lights** (mild bloom, ACES, color grade; thinned realtime point lights on fixtures). Apply in Editor after domain reload. See [[Editor Tooling]].
- **HUD** — removed ObjectiveWaypointUI bottom strip over the hotbar (kept world beacon/breadcrumbs); moved `CurrentLocationHUD` to **top-right**. Duplicate EventSystem already cleaned in-scene; `HudBootstrap` still dedupes at runtime. See [[Status & World UI]] · [[UI & HUD]].
- **Graphify** — CLI unavailable this session; graph noted stale.

## 7/15/2026 (evening — walk cycle, fit doors, nav, guide)
- **Walk cycle axis** — procedural legs were swinging on local Z (sideways). Now auto-picks the sagittal axis (usually local X); smoothed speed cuts jitter. See [[Character Visuals]].
- **Player/NPC fit** — `VisualScale` 1.3→1.0, capsule radius **0.38 m** so characters fit ~1.2 m cell doors. Re-run **Prison → Setup Character Visuals**.
- **Cell doors schedule** — closed for night **and** cell counts (`MorningRollCall` / midday / evening); open from **Breakfast** onward (meals / work / free time). See [[Locations, Zones & Cells]].
- **NavMesh** — rebake prefers PhysicsColliders + 0.05 m voxels so agents don't path through 0.2 m walls. **Prison → Polish Pass**.
- **World objective guide** — yellow destination beacon + moving next-corner marker + path breadcrumbs (not a static HUD-only arrow). See [[Status & World UI]].

## 7/15/2026 (evening — cell exit + NPC stance)
- **Cell doors exit path** — root cause of blocked cell doors: Play Mode `Start` re-captured a door left at the **open** pose as `closedLocalPosition`, then slid further by `openOffset` (~1.35 m), leaving bars across the doorway. Fix: restore authored FBX poses, bake closed with `hasAuthoredClosedPosition`, snap scene doors back to closed after setup. Shell-center align remains legacy-only. Verified in Play: closed stays authored; day phase reaches true open; ray from spawn through doorway is clear. See [[Locations, Zones & Cells]].
- **NPC frozen mid-stride** — procedural locomotion preferred; standing rest from Idle sample / L–R average so idle is not bind-pose walk. Re-ran **Prison → Setup Character Visuals**. See [[Character Visuals]].
- **HUD** — routine bar fill uses a solid 32×32 sprite (not `whiteTexture`); objective compass uses a chevron sprite; in-cell objectives say `WAIT IN …`. See [[UI & HUD]].
- **Apply in Editor:** scene already saved with 16/16 doors at authored closed. If doors drift again: **Prison → Fix Cell Doors & Waypoints** (edit mode, not while Playing).

## 7/15/2026 (feature integration — all branches → `dev`)
- **Feature branches integrated into `dev`** — audited all open `feat/*` branches. `feat/escape-completion`, `feat/blenderkit-assets`, and `feat/realistic-schedule` were already on `dev`. **Cherry-picked** the ProBuilder rebuild pipeline from `feat/prison-probuilder-rebuild` (`PrisonProBuilderRebuildRunner`, `PrisonLayoutRebuildRunner`, `PrisonLayoutSpec`/`Validator`, layout validation tests, `SpawnPlacementUtility`). Pulled `PrisonLootSetupRunner` from `feat/prison-level-layout-and-loot`. Did **not** wholesale-merge `feat/low-poly-character-visuals` or old layout scene commits — superseded by BlenderKit characters + facility install. See [[Git & Branching]] integration table.
- **Vault synced** — [[Roadmap & Priorities]] (escape completion → done on `dev`; social v2 is now #1), [[Systems Overview]], [[Escape Routes & Mechanics]], [[Escape Completion System]], [[Home]], [[Editor Tooling]], [[Testing & QA]] (146 tests), [[Content Inventory]].
- **Scene polish in progress** — cinderblock/concrete textures, character materials, animation controllers, prefabs (local WIP on `dev`).

## 7/15/2026 (playtest fixes — HUD, NPCs, doors, navmesh, textures)
- **Routine bar simplified** — the top bar now shows **one** plain-language, colour-coded instruction ("Go to the Cafeteria · 29m", "You're in the right place", "Free time — go anywhere", "Wait in your cell for roll call", "Out of position — … now") instead of a jargon status word plus a "HERE TO DEST" GPS fragment. Phase title lost its brackets. See [[UI & HUD]] · [[Routine & Schedule HUD]].
- **Waypoint ↔ top-bar consistency** — `PrisonRoutineDestination.ResolveActiveDestination` is now the single source both the floating objective waypoint and the routine bar read, so they can't disagree; travel grace targets the **current** phase venue (was wrongly showing the next phase). New `PrisonRoutineLabels.GetInstruction`.
- **NPC animation** — `BlenderKitLocomotionAnimator` now drives the blend from the max of NavMesh-agent velocity and actual body movement, so inmates/guards animate even when the agent's reported velocity lags; animator re-enabled when a valid avatar is present.
- **Cell doors** — `AlignDoorToCellWall` seats doors by **mesh-bounds center** in the wall plane (no outward nudge) so they line up in the doorway.
- **NavMesh** — rebake now carves from all render geometry at a 0.1 m voxel with `CollectObjects.All`, so 0.2 m walls block agents (fixes NPCs clipping walls and the shakedown guard failing to reach cells). Guards kept always-on so the morning sweep runs.
- **Textures** — procedural **cinderblock** wall texture now overrides the flat wall material (walls read like the cafeteria). *Apply: re-run Prison → Polish Pass, then Prison → Fix Cell Doors & Waypoints in the licensed editor.*

## 7/15/2026 (realistic schedule + scene/anim fixes)
- **Realistic count-driven prison day implemented** — the vault's redesigned schedule is now live in code. `PrisonEventType` gained `WorkProgram`(8) / `MiddayCount`(9) / `EveningCount`(10) (append-only, int values pinned by test); `PrisonSchedule.asset` + C# defaults re-authored to the 13-phase day (05:00 morning count → 22:00 lights out, 1440 min, 24 real-min day). New `IsCellCountPhase`/`IsFormalCount` predicates; routing (Workshop zone for work blocks), compliance, cell doors, waypoint objective, and **all** HUD labels handle the 13 phases (the 3 duplicate legacy `FormatEvent` switches now delegate to `PrisonRoutineLabels`). New `FormalCountMonitor` raises `RaiseLockdown` on a midday/evening count mismatch. See [[Time & Schedule]] · [[Roll Call & Shakedown]] · [[World Rules]].
- **Cell doors + waypoints fixed** — unified the two competing door-creation paths onto the canonical `PrisonFacilityInstaller.AlignDoorToCellWall` + 6 m slide + baked closed pose + collider; new **Prison → Fix Cell Doors & Waypoints** realigns all 16 doors, **deduplicates** doors stacked by the two build paths, creates missing cell stand points, and snaps patrol waypoints to the NavMesh. `AlignDoorToCellWall` clamps the bed inside shell bounds (side-wall flip fix). See [[Locations, Zones & Cells]].
- **Animations play full cycles** — root cause: FBX clips imported as frames 0-0 (static poses). `ConfigureCharacterImporter` now bakes real ranges from the takes (Idle 0-48, Walk 0-24, Run 0-16, Jump 0-24). Per-role controllers `Char_Locomotion_{Prisoner,Guard}` built from each rig's own clips (no cross-rig retargeting), each with the Speed blend **plus a Jump state**; `BlenderKitLocomotionAnimator` feeds continuous smoothed speed and fires Jump on takeoff; one Animator per character. See [[Character Visuals]].
- **Polish pass** — new **Prison → Polish Pass**: convex colliders on **623** props (fixes clip-through — `PlaceKit` had stripped them), procedural concrete/tile/metal textures on untextured mats, extra corridor/cafeteria/yard/wing props, trilight ambient, NavMesh rebake, and **full-facility guard patrol routes** (perimeter ring + inner corridors + cell wings) generated from the plate table and assigned by role. See [[Editor Tooling]].
- **Verification** — `Unity.exe -batchmode -runTests EditMode`: **169/169 pass** (added door/count/label/enum-pin tests). `PrisonBatchRunner.RunFullSetup` headless chain (character visuals → polish → door/waypoint fixer → save) exit 0. Branch: `feat/realistic-schedule`.

## 7/14/2026 (social redesign)
- **Social Ecosystem & Gangs specced (v2)** — full replacement of v1 social: NPC identities + 5-trait personalities for prisoners **and** guards (7 prisoner / 4 guard archetypes), two-axis Respect/Trust relationships (NPC↔player and NPC↔NPC), decaying event memory with witnessing + gossip, 2 gangs (territory, standing, Outsider→Trusted membership ladder with initiation), Talk Menu (chat/intel bands, gifts, trading + live wallet, favors both directions, intimidation), snitching → guard tips → targeted shakedowns, corrupt-guard bribes. v1 teardown table included; 6 implementation milestones. Spec: [[Social Ecosystem & Gangs]]. Synced: [[Social & Reputation]] (deprecated v1), [[Prisoner AI & NPCs]], [[Guard AI]], [[Security, Heat & Alerts]], [[Loot & Economy]], [[Systems Overview]], [[Roadmap & Priorities]] (#2), [[Home]]. Open questions for review at the bottom of the spec (gang names, intimidation fail, snitch discoverability, cash sources).

## 7/14/2026 (characters)
- **Rigged + animated Blender characters** — `SM_Char_Guard` / `SM_Char_Prisoner` in `PrisonKit.blend` (`Kit_Characters`): 2.6 m cube humanoids in doc palettes (guard navy/gold cap-badge-epaulettes-belt, prisoner orange/white stripes), 17-bone Unity-friendly rigs, rigid skinning, shared **Idle / Walk / Run / Jump** actions keyed to the procedural animator constants (38° leg, 28° arm swing). Exported with all takes to `Assets/Models/BlenderKit/Characters/`. See [[Character Visuals]] · [[Blender Asset Kit]]. Follow-up: Animator Controller + prefab visual swap in `CharacterVisualSetupRunner` (+ Player palette variant).

## 7/14/2026 (night)
- **Blender modular asset kit + full prison build** — new master file `ArtSource/PrisonKit.blend`: 61 modular kit pieces (walls/doorways/cells/furniture/props/fence, `SM_*` naming, 0.5 m snap grid, half-thickness shared partitions), full script-assembled prison matching the plate table, ~380-prop density pass, **25 item pickup models** (full [[Item Catalog]]), procedural tileable textures with box-projected UVs, and FBX export to `Assets/Models/BlenderKit/` (per-piece + `PrisonFacility.fbx` + `Textures/`). New source-of-truth note: [[Blender Asset Kit]]. Follow-up: Unity URP material extraction + migrating the layout runner to BlenderKit meshes.

## 7/14/2026 (late night)
- **BlenderKit migration** — `BlenderKitAssetSetup`, `BlenderKitCatalog`, `BlenderKitLayout`; `PrisonLevelLayoutRunner` now instantiates kit prefabs (floors/walls/roofs/lights/cells/furniture/fence/solitary) instead of scratch cubes when FBX are present. Item `worldPrefab` wired for all 25 pickups. Branch: `feat/blenderkit-assets`. See [[Blender Asset Kit]] · [[Editor Tooling]] · [[Prison Layout — Minimum Security]].

## 7/14/2026 (evening)
- **Layout geometry pass** — doorway walls with jambs/lintels, roof overhang + soffits, floor Y synced to cell spawns, cell-wing wall keep-out, **jail cell shell cleanup** (strip 20 m legacy walls, rebuild 4×5.5 m shells + partitions). Rebuilt `PrisonLevel1` via **Run Full Build**. See [[Prison Layout — Minimum Security]] · [[Editor Tooling]].
- **HUD fixes** — objective waypoint smoothing (jitter near stand points); hotbar compact 56×56 slots; morning roll call compliance label shows wait-in-cell. See [[UI & HUD]] · [[Status & World UI]] · [[Inventory & Hotbar UI]].
- **PR #40** — `feat/escape-completion` → `dev` (escape completion v1 + layout + HUD). Vault synced to match implementation per [[Development Workflow]].

## 7/14/2026
- **Development workflow rule** — `.cursor/rules/development-workflow.mdc` enforces vault → verify → devlog → git loop; agents must run end-of-task checklist and ask before commit/push.
- **UI vitals + navigation HUD** — `PlayerVitalsHUD` (cash, mental/physical health, strength), `CurrentLocationHUD`, `ObjectiveWaypointUI` with distance during mandatory non-compliance. Physical Health stat added (−10 solitary, +5/day regen). `UIMenuFocus` menu fade, hotbar polish, `InkGreen` crafting theme. `HudBootstrap` runtime spawn. See [[UI & HUD]] · [[Status & World UI]]. Branch: `feat/escape-completion` (`c3b5f0b`).
- **Vault restructured as full source of truth** — complete design docs for every system, content inventory, engineering docs, and process notes. Entry point: [[Home]].
- **Connected diagram layout implemented** — `PrisonLevel1` rebuilt with 18 shared-edge floor plates matching the layout diagram. Doorway walls, roof overhang/soffits, ~370 lights, scratch-built furniture. *(Superseded by evening geometry pass — see above.)*
- `dev` and `main` synced at the same commit; Obsidian-source-of-truth rule added to `.cursor/rules/`.

## Earlier (pre-dashboard highlights)
- East cell block renamed to cells 09–16; registry wired for 16 cells
- Low-poly character visual pipeline (Player/Guard/Prisoner, 2.6 m rigs) — [[Character Visuals]]
- 136 EditMode tests green — [[Testing & QA]]
- Core sim systems in place: schedule, zones, roll call/shakedown, guard/prisoner AI, social math, crafting, inventory, vent/fake-bed/stash escape mechanics — [[Systems Overview]]
## 7/15/2026 (polish pass complete)
- **Prison polish pass** — cell anchors from FBX bed/door; local-axis door slide; 8 kit textures on URP mats; `Char_Prisoner`/`Char_Guard` rigged prefabs + locomotion controller; in-cell waypoints + floor marker; full build + NavMesh rebake. Branch: `feat/blenderkit-assets`.

