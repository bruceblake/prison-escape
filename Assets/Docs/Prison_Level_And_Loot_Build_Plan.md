# Prison Level Build Plan — Items, Loot Spawns & Map Expansion

_Last updated: 2026-07-01_

Companion to [Items, Crafting & World Loot](Items_Crafting_And_Loot.md). This plan covers **how to get from today's `PrisonLevel1` scene to a playable, good-looking prison** with world loot, finished zones, lighting, and hidden areas.

---

## 0. Current state (audit)

### What exists today in `PrisonLevel1`

| Area | Status | Notes |
|---|---|---|
| **Cell block** (8 cells) | ✅ Built | Beds, toilets, sinks, vents, sliding doors, cell lights |
| **Corridors** | ✅ Built | Cool corridors, railings, ceiling pipes, light fixtures |
| **Cafeteria** | ✅ Built | Tables, serving counter, floor tile, zone **wired** |
| **Yard** | ❌ Missing | `Ground` geometry exists; **no `ZoneType.Yard`**, registry `yard: null` |
| **Roll call area** | ⚠️ Partial | `RollCallPoint` transforms exist; registry `rollCallArea: null` |
| **Kitchen** | ❌ Not built | No back-of-house beyond serving counter |
| **Workshop / maintenance** | ❌ Not built | No dedicated room |
| **Laundry / showers** | ❌ Not built | Sinks in cells only |
| **Item spawns** | ❌ Not wired | Zero `ItemSpawnNode` objects; `PopulateWorldSpawns()` does nothing |
| **NavMesh** | ⚠️ Verify | Rebake after every major geometry change |
| **Lighting** | 🟡 Partial | Baked lightmaps + directional + scattered point/spot lights; needs zone pass |

### Code already supports (no redesign needed)

- `ItemSpawnNode` + `LootTable` + `GameManager.PopulateWorldSpawns()`
- `WorldItemPickup` (press F to pick up)
- `ItemData.worldPrefab` for spawn visuals
- Modular kit: walls, floors, doors, fence, cafeteria table, storage crate, gym bench, concrete pillar
- Prison material library (`Assets/Materials/Prison/`)

### Design constraint (confirmed)

**No searchable containers.** All loot is world pickups at spawn nodes, optionally in hidden/off-schedule areas.

---

## 1. Target player experience

By the end of this plan, a new player should be able to:

1. Wake in cell → follow schedule to cafeteria / yard
2. During **Free Time**, explore kitchen back rooms, workshop, laundry for crafting parts
3. Find items on shelves, floors, and in **hidden spots** (vent crawlspace, maintenance closet, yard fence line)
4. Craft tools in the stolen notebook during free time or in cell
5. Stash contraband in pillow before morning shakedown
6. Use crafted tools on **vent**, **fence**, or **wall** escape anchors (escape win state is a separate follow-up)

---

## 2. Map layout (target footprint)

Use the existing **`Ground`** + perimeter walls as the yard shell. Extend **west/east** off the main corridor for service wings.

```
                    ┌─────────────────────────────────────────┐
                    │              PERIMETER FENCE             │
                    │  ┌───────────────────────────────────┐  │
                    │  │           EXERCISE YARD             │  │
                    │  │   (GymBench, fence, yard spawns)    │  │
                    │  └───────────────────────────────────┘  │
                    │         ▲ roll call strip (south)        │
    ┌───────────────┼─────────┴───────────────────────────────┼───────────────┐
    │  WORKSHOP     │  CORRIDOR + 8 CELLS                      │   LAUNDRY     │
    │  (west wing)  │  (existing — refine props)               │  (east wing)  │
    │  metal, wire  │                                          │  soap, rags   │
    └───────┬───────┤                                          ├───────┬───────┘
            │       │  CAFETERIA (existing dining hall)        │       │
            │       │       └─ KITCHEN (new, behind counter)   │       │
            │       └──────────────────────────────────────────┘       │
            │                    ▲ main entry / guards                  │
            └──── maintenance tunnel (hidden, connects workshop ↔ laundry)
                         vent crawlspace (hidden, cells ↔ ceiling)
```

**Scale guidance:** Each new wing ≈ 1× cafeteria width. Keep walks under 15 seconds from corridor hub so schedule compliance stays fun, not tedious.

---

## 3. Phased delivery plan

Work in **stacked PRs** (content + small code). Each phase ends with a playable check in Unity.

| Phase | Name | Deliverable | Est. effort |
|---|---|---|---|
| **A** | Item pipeline | Clean assets, world prefabs, loot tables, `ItemDatabase` | 1–2 days |
| **B** | Spawn infrastructure | `ItemSpawnNode` prefab, scene hierarchy, populate pass | 1 day |
| **C** | Yard + roll call | Yard zone, fence, exercise props, roll call wiring | 2–3 days |
| **D** | Kitchen wing | Back-of-house kitchen, storage, spawns | 2 days |
| **E** | Workshop wing | Benches, crates, tool wall, spawns | 2 days |
| **F** | Laundry wing | Washers, showers, spawns | 2 days |
| **G** | Hidden areas | Vents, tunnel, secret closet, rare spawns | 2–3 days |
| **H** | Lighting & atmosphere | Zone lights, emissive pass, rebake, volume | 2 days |
| **I** | Polish & escape anchors | Props, signs, fence/wall interactables, QA | 2–3 days |

**Total:** ~2–3 weeks of focused level work (can parallelize D/E/F after C).

---

## Phase A — Item pipeline (prerequisite for all spawns)

Reference: [Items_Crafting_And_Loot.md §3–§9](Items_Crafting_And_Loot.md)

### A.1 Asset cleanup

- [ ] Delete duplicate assets: `MetalScrap.asset`, `WoodScrap.asset`
- [ ] Assign unique `networkId` to every item (see master doc §9)
- [ ] Fix items stuck at `networkId: 0`

### A.2 World pickup prefabs

Create **`Assets/Prefabs/Items/WorldPickup_Generic.prefab`**:

- Small mesh or icon billboard (can use `StorageCrate` scale 0.3× as placeholder)
- `WorldItemPickup` component
- Optional subtle point light or emissive strip for visibility in dark corridors
- `BoxCollider` for raycast

Per-item prefabs (optional polish later):

- `WorldPickup_Paperclip`, `WorldPickup_MetalScrap`, etc.

Assign each spawnable `ItemData.worldPrefab` → generic or specific prefab.

### A.3 Loot table ScriptableObjects

Create under `Assets/ScriptableObjects/LootTables/`:

| Asset | Used by zones |
|---|---|
| `LootTable_Prison_Common` | Corridors, cells |
| `LootTable_Yard` | Yard, roll call exterior |
| `LootTable_Kitchen` | Kitchen, cafeteria back |
| `LootTable_Workshop` | Workshop, maintenance |
| `LootTable_Laundry` | Laundry, showers |
| `LootTable_RareHidden` | Hidden areas only |

Pools defined in master item doc §6.

### A.4 Registry wiring

- [ ] Scene `ItemDatabase` — drag all `ItemData` assets into `allItemsInGame`
- [ ] `InventoryUI.recipes` + `StolenNotebookUI.recipes` — all 7 recipes
- [ ] Remove / ignore any `WorldContainer` in scenes

**Exit criteria:** Press Play → manually place one test `ItemSpawnNode` → item appears and is pickable.

---

## Phase B — Spawn infrastructure

### B.1 Scene hierarchy (empty parents)

Under `PrisonLevel1` → new root **`WorldSpawns`**:

```
WorldSpawns/
├── Spawn_Corridor/
├── Spawn_Cells/
├── Spawn_Cafeteria/
├── Spawn_Yard/          (Phase C)
├── Spawn_Kitchen/       (Phase D)
├── Spawn_Workshop/      (Phase E)
├── Spawn_Laundry/       (Phase F)
└── Spawn_Hidden/        (Phase G)
```

### B.2 ItemSpawnNode prefab

**`Assets/Prefabs/World/ItemSpawnNode.prefab`**

- Empty transform, gizmo icon
- `ItemSpawnNode` component
- Default `spawnChance = 0.45`
- Editor-only: colored `Gizmo` draw by loot table type (optional script)

### B.3 Spawn placement rules

| Rule | Detail |
|---|---|
| Height | Snap to floor; +0.05 Y to avoid z-fighting |
| Visibility | 70% in plain sight, 30% behind props / corners |
| Guard fairness | Not inside guard spawn or patrol pivot |
| Schedule | Accessible during **FreeTime** without breaking mandatory phases (kitchen reachable from cafeteria during meals too — that's OK) |
| Density | See §4 spawn budget table |

### B.4 Optional code tweak (small PR)

Improve `GameManager.PopulateWorldSpawns()`:

- Random Y rotation on spawn
- Debug log: `"Spawned {item} at {node}"` when `debugLogScheduleEvents`-style flag enabled
- Skip spawn if node collider overlaps guard only layer (future)

**Exit criteria:** 40+ nodes placed in existing areas (corridor + cafeteria + cells) → Play → ~18–25 pickups appear.

---

## Phase C — Yard & roll call

### C.1 Yard zone

On existing **`Ground`** (south of cell block):

1. Add **`PrisonLocationZone`** → `ZoneType.Yard`, `hudDisplayName: YARD`
2. Box collider trigger covering full yard floor
3. Child **`Stand_0`…`Stand_N`** transforms for free-time compliance
4. Wire **`PrisonLocationRegistry.yard`**

### C.2 Roll call area

South strip between cells and yard (where `RollCallPoint` objects already sit):

1. Parent empty **`RollCallArea`**
2. `ZoneType.RollCallArea` trigger
3. Wire **`PrisonLocationRegistry.rollCallArea`**
4. Align stand points with existing `RollCallPoint` transforms

### C.3 Yard build-out (geometry)

Use modular kit:

| Prop | Asset | Placement |
|---|---|---|
| Perimeter fence | `Fence_Panel_Modular` | Along `Wall_South` / yard edge |
| Exercise bench | `GymBench` | 2–3 along yard long axis |
| Concrete pillars | `ConcretePillar` | Yard corners |
| Storage crates | `StorageCrate` | 2–4 as cover + spawn anchors |
| Hazard stripes | `Accent_CautionYellow` mat | Near fence / yard gate |

### C.4 Yard spawns (8–12 nodes)

| Node ID | Location | Loot table | spawnChance |
|---|---|---|---|
| Y01 | Behind gym bench | Yard | 0.50 |
| Y02 | Fence corner NW | Yard | 0.45 |
| Y03 | Fence corner NE | Yard | 0.45 |
| Y04 | Near storage crate | Yard | 0.50 |
| Y05 | Center grass/dirt | Common | 0.40 |
| Y06 | Roll call strip edge | Yard | 0.35 |
| Y07–Y08 | Under bench / crate gap | Yard | 0.30 |

### C.5 NavMesh

- Bake after fence + props placed
- Run **`PrisonNavMeshValidator`** from scene debug object
- Guard waypoints: add **`Waypoints_Yard`** loop

**Exit criteria:** Free Time → HUD says YARD → compliance works standing in yard → yard loot spawns.

---

## Phase D — Kitchen (back of cafeteria)

Build **behind** the existing `ServingCounter` — door or archway from cafeteria (open during meals; back rooms reachable always via corridor door during free time).

### D.1 Room layout (~8m × 6m)

| Zone | Props (kitbash) | Notes |
|---|---|---|
| **Cook line** | Counter cubes + `PrisonMetal_Shelf` | Stove area (cubes + emissive) |
| **Walk-in storage** | `StorageCrate` × 4, `PrisonFloor_Tile` | Locked feel — narrow door |
| **Dry storage** | Shelves (`PrisonMetal_Shelf`), `Tank` props | Charcoal, bottles |
| **Dish pit** | `Sink` prefabs from cells | Rag spawns |

### D.2 Lighting

- Warm **Point Light** over cook line (2700K feel)
- Cooler fluorescent over storage (existing `CorridorLightFix` prefab pattern)
- Slightly **brighter** than corridors — kitchen should read as a destination

### D.3 Kitchen spawns (5–7 nodes)

| Node | Location | Table | Chance |
|---|---|---|---|
| K01 | Dry storage shelf | Kitchen | 0.45 |
| K02 | Walk-in floor | Kitchen | 0.40 |
| K03 | Dish pit | Kitchen | 0.40 |
| K04 | Under cook line | Kitchen | 0.35 |
| K05 | Cafeteria back corner (transition) | Kitchen | 0.35 |
| K06 | Hidden: walk-in back wall gap | RareHidden | 0.20 |

**Exit criteria:** Player can walk kitchen loop; finds glass bottle / alcohol / charcoal over several play sessions.

---

## Phase E — Workshop / maintenance (west wing)

New room west of corridor — connect via **`Wall_Doorway_Modular`** door (always open for v1; later: key/lockpick).

### E.1 Room layout

| Feature | Build |
|---|---|
| Workbench | `PrisonMetal_Shelf` + wood plank cubes |
| Tool pegboard | Flat quads + `Accent_PanelWhite` |
| Parts bins | `StorageCrate` with `MetalScrap`/`Wire` spawn nodes inside |
| Electrical closet | Sub-room 3×3m, door frame, **`VentCover`** optional secondary exit |

### E.2 Workshop spawns (6–8 nodes)

| Node | Location | Table | Chance |
|---|---|---|---|
| W01 | Workbench surface | Workshop | 0.40 |
| W02 | Parts bin 1 | Workshop | 0.45 |
| W03 | Parts bin 2 | Workshop | 0.45 |
| W04 | Floor under bench | Workshop | 0.35 |
| W05 | Electrical closet shelf | Workshop | 0.35 |
| W06 | Behind pegboard | Workshop | 0.30 |
| W07 | Hidden: vent to crawlspace | RareHidden | 0.20 |

**Exit criteria:** Reliable path to metal scrap, wire, duct tape, file (rare) for screwdriver / wire cutter crafts.

---

## Phase F — Laundry & showers (east wing)

Mirror workshop on east side — industrial tone, wet floor material variant.

### F.1 Room layout

| Feature | Build |
|---|---|
| Washer row | 3× `Tank` or cube + cylinder drums |
| Folding table | `CafeteriaTable` scaled down |
| Shower stalls | 3× partitioned cubes, `PrisonToilet_Porcelain`-adjacent drains |
| Soap dispenser | Small wall mesh + spawn node |

### F.2 Laundry spawns (5–6 nodes)

| Node | Location | Table | Chance |
|---|---|---|---|
| L01 | Washer top | Laundry | 0.45 |
| L02 | Folding table | Laundry | 0.40 |
| L03 | Shower floor | Laundry | 0.35 |
| L04 | Soap dispenser | Laundry | 0.50 |
| L05 | Linen cart corner | Laundry | 0.40 |
| L06 | Hidden: behind washer | RareHidden | 0.20 |

**Exit criteria:** Soap/rag common; occasional bed sheet for fake dummy craft.

---

## Phase G — Hidden areas & secrets

Hidden areas reward exploration and supply **RareHidden** loot table rolls.

### G.1 Vent crawlspace network

Connect existing **`Vent_System`** / cell vents:

| Segment | From → To | Requirement |
|---|---|---|
| VC01 | Cell 1 vent → ceiling plenum | Screwdriver (remove cover) |
| VC02 | Plenum → maintenance tunnel hatch | Crawl (no guard LOS) |
| VC03 | Tunnel → workshop electrical closet | One-way drop |

- Narrow pipe geometry (`Pipe1`/`Pipe2` assets)
- Dim **`PrisonLight_Emissive`** every 4m
- 2× **`Spawn_Hidden`** nodes (File, Mirror, Coin)

### G.2 Maintenance tunnel

Under or behind workshop ↔ laundry:

- Low ceiling, `Accent_SecurityRed` emergency lights
- 1 guard patrol **avoid** (schedule-only during FreeTime safe window)
- Connects wings without crossing yard CCTV (future)

### G.3 Other secret spots

| Secret | Location | Loot |
|---|---|---|
| Freezer back panel | Kitchen walk-in | Alcohol |
| Yard fence scrape | Behind fence panel (squeeze glitch or craft wire cutters first) | Coin, Metal Scrap |
| Cafeteria ceiling tile | Restroom adjacency | Paperclip stack |
| Guard break room | Near `GuardSpawnPoints` (LightsOut only?) | Mirror (Finch favor item) |

### G.4 Discovery feedback

- Subtle audio cue on first hidden entry (reuse existing UI sound slot)
- No map marker — player learns by vent interaction + visual cracks

**Exit criteria:** At least 3 distinct hidden routes; 6+ RareHidden spawn nodes total.

---

## Phase H — Lighting & atmosphere

### H.1 Zone lighting recipe

| Zone | Key light | Fill | Accent | Time-of-day |
|---|---|---|---|---|
| Cells | Warm spot `@CellLightFixture` | Low ambient | — | Dim at LightsOut |
| Corridor | Cool ceiling fixtures | Baked GI | Red security strips | 50% at night |
| Cafeteria | Bright overhead | Baked | Blue sign emissive | Full during meals |
| Yard | Directional sun | Light probe | — | Day only feel |
| Kitchen | Warm + cool mix | Point lights | Steam area dim | On always |
| Workshop | Industrial cool white | Shadows OK | Yellow caution | On always |
| Laundry | Damp cool | Slightly green tint | — | On always |
| Hidden | Minimal emissive | Near black | Red emergency | Always dim |

### H.2 Global setup

- **`MainDirectionalLight`** — sun for yard; rotate for afternoon feel
- **`Global Volume`** — slight bloom + color grading (existing URP)
- **Rebake lightmaps** after Phases C–G geometry locked
- **Reflection probe** per major room (kitchen, cafeteria already have baked data pattern in `PrisonLevel1/` folder)

### H.3 Optional code (Phase H PR)

`PrisonLightingController` (new, small):

- Subscribe `PrisonTimeManager.OnEventChanged`
- `LightsOut` / `NightRollCall` → dim corridor cell lights, boost moon on yard
- `FreeTime` → full interior

**Exit criteria:** Screenshot-ready rooms; readable item pickups at floor level at all times.

---

## Phase I — Polish, props & escape anchors

### I.1 Prop pass (use existing materials)

| Kit | Where |
|---|---|
| `Sign_CellBlock`, `Sign_Cafeteria` | Already in scene — add `Sign_Workshop`, `Sign_Laundry`, `Sign_Yard` (TextMeshPro on quads) |
| `PrisonScuff`, `WallScuffs`, `FloorStain` | Repeat on high-traffic paths |
| `Hazard_0/1/2` | Workshop + laundry |
| `CommandStrip`, `Accents` | Corridor wayfinding |

### I.2 Audio (if clips available)

- Cafeteria ambient loop
- Laundry machine hum
- Yard wind
- Footstep switch by floor material

### I.3 Escape route anchors (placeholder until EscapeManager)

| Route | Scene object | Item gate |
|---|---|---|
| Vent | Existing `VentCover` + screws | Screwdriver |
| Fence | New `FenceCutPoint` on south fence | Wire Cutters |
| Wall | New `ClimbPoint` + ladder volume | Ladder / Grappling Hook |
| Tunnel | Workshop pit (dig spot) | Shovel |
| Distraction | Yard center | Molotov (optional) |

Each gets a **`EscapeRouteMarker`** empty (future script hook) + visual hint prop.

### I.4 Corridor & cell spawn pass (fill existing built areas)

| Zone | Nodes | Table |
|---|---|---|
| Corridor | 6–8 | Common |
| Each cell (non-player) | 1 | Common (low chance 0.25) |
| Cafeteria dining | 4–6 | Kitchen + Common mix |

**Exit criteria:** Full playthrough: spawn → explore all wings → collect parts → craft screwdriver → open vent → reach hidden tunnel.

---

## 4. Spawn budget summary

| Zone | Nodes | Expected items / run (approx) |
|---|---|---|
| Corridors + cells | 14 | 5–7 |
| Cafeteria | 6 | 2–4 |
| Yard | 10 | 4–6 |
| Kitchen | 7 | 3–5 |
| Workshop | 8 | 3–5 |
| Laundry | 6 | 3–4 |
| Hidden | 8 | 1–3 rare |
| **Total** | **~59** | **~22–34 pickups per day** |

Tuning knob: raise/lower `spawnChance` globally if prison feels too sparse or too generous.

---

## 5. Code & data tasks (cross-cutting)

| Task | Phase | Priority |
|---|---|---|
| Wire `yard` + `rollCallArea` on registry | C | P0 |
| Create loot table assets | A | P0 |
| World pickup prefab + assign `worldPrefab` | A | P0 |
| Place all `ItemSpawnNode` objects | B–I | P0 |
| Rebake NavMesh | C, D, E, F | P0 after each wing |
| Fix `CraftingSystem` full-inventory bug | A | P1 |
| `PrisonLightingController` | H | P2 |
| Extend `ZoneType` with Kitchen/Workshop/Laundry | Optional | P3 — only if HUD should show sub-zone names |
| `EscapeManager` + win trigger | I+ | P1 (game goal) |

**Note:** Kitchen/workshop/laundry do **not** need new schedule phases for v1. They are **free-exploration zones** during FreeTime (and physically reachable paths during other phases if you leave doors open).

---

## 6. PR breakdown (recommended)

| PR | Title | Contents |
|---|---|---|
| **13a** | `feat(items): world pickup prefabs, loot tables, item cleanup` | Phase A |
| **13b** | `feat(spawns): ItemSpawnNode prefab + corridor/cafeteria/cell spawns` | Phase B + I.4 partial |
| **13c** | `feat(level): yard, roll call zone, fence, yard spawns` | Phase C |
| **13d** | `feat(level): kitchen wing geometry, lights, spawns` | Phase D |
| **13e** | `feat(level): workshop wing + maintenance tunnel start` | Phase E |
| **13f** | `feat(level): laundry wing + showers` | Phase F |
| **13g** | `feat(level): hidden vent network + rare spawns` | Phase G |
| **13h** | `feat(level): lighting pass + lightmap rebake` | Phase H |
| **13i** | `feat(level): prop polish + escape route markers` | Phase I |

Each PR: open `PrisonLevel1`, run NavMesh bake, run `PrisonNavMeshValidator`, screenshot before/after.

---

## 7. Unity editor checklist (every session)

1. Open **`Assets/Scenes/PrisonLevel1.unity`**
2. Edit geometry under clear parent (`Kitchen`, `Workshop`, etc.)
3. Place spawn nodes → assign loot table + chance
4. **Window → AI → Navigation → Bake**
5. Select `DebugPrisonnavMeshValidator` → Validate Now
6. Play Mode: confirm `GameManager` spawns pickups + schedule compliance for yard/cafeteria
7. **`Prison → Fix Cell Doors & UI`** if cell doors break after edits

---

## 8. Definition of done (whole plan)

- [ ] All items in master catalog have `worldPrefab` + unique `networkId`
- [ ] 6 loot tables created and assigned
- [ ] ~59 spawn nodes placed; `PopulateWorldSpawns` fills prison each run
- [ ] Yard, roll call, kitchen, workshop, laundry built and navigable
- [ ] ≥3 hidden areas with rare loot
- [ ] Lighting readable in every zone; lightmaps rebaked
- [ ] NavMesh valid for player, prisoners, guards in all zones
- [ ] One full gameplay loop tested: loot → craft screwdriver → vent → hidden tunnel
- [ ] Docs updated: this file + `Items_Crafting_And_Loot.md` spawn counts if tuned

---

## 9. What to build first (recommended order)

If you want the **fastest path to "feels like a game"**:

1. **Phase A + B** (items actually spawn in existing corridor/cafeteria) — 2 days
2. **Phase C** (yard — fixes FreeTime compliance + outdoor space) — 2 days
3. **Phase D** (kitchen — first new wing, high loot value) — 2 days
4. **Phase G partial** (one vent hidden route — escape fantasy) — 1 day
5. **Phase H** (lighting pass on what exists) — 1 day
6. Then E, F, full G, I

---

_When starting a phase, tick boxes in this doc and note the PR number. Update spawn budget table if tuning changes._
