# Prison Rebuild Master Plan — ProBuilder Pipeline

_Last updated: 2026-07-01_

**Goal:** Replace the broken hybrid prison shell (legacy vendor walls + procedural patches + open gaps) with a **single coherent, fully enclosed prison** authored in **ProBuilder**, baked into static runtime geometry, and guarded by **automated layout tests**.

**Companion docs:** [Prison Level & Loot Build Plan](Prison_Level_And_Loot_Build_Plan.md) · [Items & Loot](Items_Crafting_And_Loot.md) · [Workflow](../../docs/WORKFLOW.md)

---

## 0. Why rebuild from scratch

Current `PrisonLevel1` problems (confirmed in playtest):

| Problem | Root cause |
|---------|------------|
| Sky visible through cell ceilings / walls | Mixed legacy shell + procedural `PrisonLayout` patches; roof colliders disabled; walls moved/disabled by old rebuild passes |
| Player spawns on roof / falls through floor | Raycast snap hits roof deck; floor colliders disabled under cells |
| Open, non-prison feel | Perimeter walls pushed outward or disabled; corridors not fully enclosed |
| Duplicate / missing zones | Kitchen vs cafeteria overlap; yard gate inconsistent |
| NavMesh bake failures | Geometry spread too wide; too many tiles |

**Decision:** Archive the **outer legacy shell** and rebuild the **interior prison footprint** as one authored volume set. Keep **cell block props** (beds, doors, vents) where possible; replace **shell geometry** entirely.

---

## 1. Target layout (single enclosed building)

All rooms connect through **doors or gated openings** — no wide open world gaps.

```
                         ┌──────────────────────────────┐
                         │     EXERCISE YARD (north)      │
                         │  fence + YardZone + spawns     │
                         └──────────────┬───────────────┘
                                        │ yard gate (door)
┌─────────────┐   ┌─────────────────────┴─────────────────────┐   ┌─────────────┐
│  WORKSHOP   │───│  MAIN CORRIDOR + 8 CELLS (existing props) │───│   LAUNDRY   │
│  west wing  │   │         + roll-call strip (south)         │   │  east wing  │
└─────────────┘   ├───────────────────────────────────────────┤   └─────────────┘
                  │              CAFETERIA (existing)          │
                  │         kitchen = serving area only        │
                  └──────────────────────────────────────────┘
                              guard / entry south
```

### Footprint constants (meters, world space)

Defined in `PrisonLayoutAnchors` + `PrisonLayoutSpec` (single source of truth for tests):

| Zone | Center (approx) | Size (W×D) |
|------|-----------------|------------|
| Cell block | (-26, 0.7, 10) | 58 × 76 |
| Main corridor | (-26, 0.6, -45) | 52 × 8 |
| Workshop | (-88, 0.6, -42) | 20 × 16 |
| Laundry | (48, 0.6, -42) | 20 × 16 |
| Cafeteria | (-16, 0.6, -62) | 28 × 24 |
| Yard | (-26, 0, 78) | 70 × 40 |
| Inner walls | X = -52 / 0 | height 3.5 |

---

## 2. ProBuilder authoring pipeline

### Philosophy

1. **Author in ProBuilder** (fast iteration, snap grid, boolean cuts for doorways)
2. **Bake to static meshes** (runtime performance, stable colliders, NavMesh-friendly)
3. **Wire zones/spawns/NavMesh** via editor menus (deterministic, testable)
4. **Validate with EditMode tests** after every bake

### Scene hierarchy (target)

```
PrisonLevel1
├── _LegacyShell          ← archived old outer walls (disabled, kept for reference)
├── JailCells             ← KEEP: cells, beds, doors, vents (may reposition spawns)
├── Cafeteria             ← KEEP: dining props
├── Managers / GameManager / NavMeshSurface / ...
└── PrisonBuild           ← NEW root for rebuild
    ├── ProBuilderDraft   ← ProBuilder meshes while authoring (editor-only feel)
    ├── BakedShell        ← exported static geometry + colliders (runtime)
    ├── Zones             ← trigger boxes wired to PrisonLocationRegistry
    └── WorldSpawns       ← ItemSpawnNode hierarchy
```

### Editor menus (new)

| Menu | Phase | Action |
|------|-------|--------|
| **Prison / Rebuild / 0 — Strip Legacy Outer Shell** | 0 | Disable + move legacy perimeter walls/fences to `_LegacyShell` |
| **Prison / Rebuild / 1 — Create ProBuilder Workspace** | 1 | Create `PrisonBuild/ProBuilderDraft` grid + room guide boxes |
| **Prison / Rebuild / 2 — Bake ProBuilder Draft To Shell** | 2 | Convert PB meshes → combined static meshes under `BakedShell` |
| **Prison / Rebuild / 3 — Wire Zones And Spawns** | 3 | Zones, registry, loot nodes from anchors |
| **Prison / Rebuild / 4 — Validate Layout (Report)** | 4 | Run all layout checks, log pass/fail |
| **Prison / Rebuild / 5 — Full Rebuild Pipeline** | all | Runs 0→4 in order |

Legacy menu **Prison / Rebuild Prison Layout** (v4 procedural) will be **deprecated** once ProBuilder pipeline is stable.

---

## 3. Phase breakdown

### Phase 0 — Strip & archive (1 day)

- [ ] Identify legacy objects: `LeftPrisonWall*`, `RightPrisonWall*`, outer `Ground` extensions, misplaced `LevelBuild`, old `PrisonLayout` procedural root
- [ ] Move to `_LegacyShell`, disable renderers/colliders
- [ ] Restore cell **floor** colliders; disable cell **roof deck** colliders for player raycasts
- [ ] **Tests:** `StripLegacyShell_DoesNotDeleteJailCells`, `CellFloors_HaveEnabledColliders`

### Phase 1 — ProBuilder workspace (2–3 days)

- [ ] Add `com.unity.probuilder` package
- [ ] Create draft room volumes: cell block envelope, corridor tunnel, wings, yard fence
- [ ] Grid: 1m snap, wall thickness 0.25m, wall height 3.5m, door opening 2m × 2.5m
- [ ] **Tests:** `ProBuilderWorkspace_HasExpectedRoots`, anchor constants self-consistent

### Phase 2 — Bake shell (2 days)

- [ ] ProBuilder → static mesh export under `BakedShell/`
- [ ] Auto-assign prison materials from `Assets/Materials/Prison/`
- [ ] MeshCollider or BoxCollider compound per room (prefer boxes for NavMesh)
- [ ] Doorway cuts at wing connectors + yard gate
- [ ] **Tests:** `BakedShell_HasColliders`, `BakedShell_NoMissingMaterials`, room AABB bounds match spec ± tolerance

### Phase 3 — Gameplay wiring (2 days)

- [ ] Reuse `PrisonLootSetupRunner` + anchor-based spawn placement
- [ ] Wire all `ZoneType` triggers: Cells, Cafeteria, Yard, Workshop, Laundry, RollCall
- [ ] Fix 8 cell spawn points on floor (Y ≈ 0.74)
- [ ] Rebake NavMesh with **tight bounds** around prison (fix tile overflow)
- [ ] **Tests:** `Registry_HasAllZones`, `SpawnPoints_OnCellFloor`, `NavMeshSurface_Exists`

### Phase 4 — Cells & props pass (2 days)

- [ ] Re-seat cell doors to new wall openings
- [ ] Roll call strip alignment
- [ ] Cafeteria — no duplicate kitchen signage
- [ ] Yard fence + gate door
- [ ] **Playtest checklist** on `dev`

### Phase 5 — Hidden areas (later)

- [ ] Maintenance tunnel (workshop ↔ laundry)
- [ ] Vent crawlspace (cells ↔ ceiling)
- [ ] Rare loot spawns

### Phase 6 — Lighting & polish (later)

- [ ] Zone light pass, emissive signs, lightmap rebake
- [ ] Escape anchor placeholders (vent, fence, wall)

---

## 4. Test strategy (maximize automation)

All tests live in `Assets/Tests/Editor/PrisonLayoutValidationTests.cs`.

### Tier A — Pure spec (always run, fast)

- Anchor centers within prison bounds
- Expected cell count = 8
- Corridor width ≥ 2.5m
- Wall height = 3.5m
- Zone names / enum coverage

### Tier B — Scene validation (run after bake; loads `PrisonLevel1`)

- `_LegacyShell` exists and is inactive/disabled
- `PrisonBuild/BakedShell` has ≥ N colliders
- No collider named `Roof` enabled above cell spawn Y in cell block bounds
- `JailCells` has 8 children with `SpawnPoint`
- `PrisonLocationRegistry` non-null for: yard, cafeteria, roll call
- Raycast from each spawn hits floor, not roof (height band test)

### Tier C — PlayMode smoke (optional CI)

- Player spawns inside cell, CharacterController grounded
- Can reach cafeteria NavMesh path (if NavMesh valid)

### Running tests

**Window → General → Test Runner → EditMode → run `PrisonLayoutValidationTests`**

After layout changes, always run **Prison / Rebuild / 4 — Validate Layout** in Unity for a human-readable report.

---

## 5. PR / branch strategy

Follow [docs/WORKFLOW.md](../../docs/WORKFLOW.md):

| PR | Branch | Contents |
|----|--------|----------|
| **#TBD** | `feat/prison-probuilder-rebuild` | Phase 0–1: strip, ProBuilder package, workspace, spec tests |
| **#TBD** | `feat/prison-baked-shell` | Phase 2–3: bake pipeline, zones, spawns |
| **#TBD** | `feat/prison-layout-polish` | Phase 4–6: props, lighting, hidden areas |

Each PR merges to **`dev`** for Bruce to playtest. **`main`** only when Bruce explicitly requests production release.

---

## 6. Immediate next steps (agent)

1. ✅ Document workflow in `.cursor/rules/git-branching.mdc` + `docs/WORKFLOW.md`
2. ✅ Create `PrisonLayoutSpec` + validation tests
3. ✅ Add ProBuilder package + Phase 0 strip menu + ProBuilder workspace menu
4. ⬜ Bruce playtests Phase 0 strip on `dev`
5. ⬜ Author ProBuilder draft rooms (Phase 1 — requires Unity editor time)
6. ⬜ Bake + wire + validate (Phase 2–3)

---

## 7. Success criteria

Bruce can Play Mode on `dev` and:

- [ ] Spawn standing on cell floor, eye height ~2.15m
- [ ] Cell is fully enclosed (no sky through ceiling)
- [ ] Walk through corridors to workshop, laundry, cafeteria without clipping or open voids
- [ ] Reach yard only through north gate/door
- [ ] See loot spawn cubes in corridor, wings, yard
- [ ] EditMode layout tests all green
