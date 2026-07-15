# Devlog Dashboard

Newest first. Log milestones here after each work session (see [[Development Workflow]]).

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

