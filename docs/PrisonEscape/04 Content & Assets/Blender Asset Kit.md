# Blender Asset Kit

The **canonical source of truth for 3D assets**. The modular kit + assembled prison live in `ArtSource/PrisonKit.blend`; Unity consumes FBX exports from `Assets/Models/BlenderKit/`. Built 2026-07-14 to the specs in [[Prison Layout — Minimum Security]].

## Files & folders

| Path | Contents |
|---|---|
| `ArtSource/PrisonKit.blend` | **Master file** — all kit pieces + full prison assembly (~1,600 objects) |
| `ArtSource/review/` | Review renders (room-by-room + final shots) |
| `Assets/Models/BlenderKit/` | 61 kit-piece FBX + `PrisonFacility.fbx` (whole prison, 1.3 MB) |
| `Assets/Models/BlenderKit/Items/` | 25 item-pickup FBX |
| `Assets/Models/BlenderKit/Characters/` | `SM_Char_Guard.fbx` + `SM_Char_Prisoner.fbx` — rigged, with Idle/Walk/Run/Jump takes (see [[Character Visuals]]) |
| `Assets/Models/BlenderKit/Textures/` | 8 tileable PNGs (procedurally generated) |

## Modularity rules (follow these for every new piece)

1. **Scale:** 1 Blender unit = 1 m. Characters are **2.0 m** tall in-game (`VisualScale` 1.0); doorway clearance is 3 m; cell sliding doors are ~1.2 m wide.
2. **Snap grid:** all footprints are multiples of **0.5 m**; fine detail on 0.1 m. Wall thickness **0.2 m**, wall height **6 m**.
3. **Origins:** wall/floor modules → **bottom-back-left on the grid** (walls: start of run on the centerline, thickness ±0.1). Furniture/props → **bottom-center**. Wall-mounted props (signs, vents, posters, pipes) → **on the wall plane**, facing −Y.
4. **Shared walls are half-thickness:** cell shells use 0.1 m side/back partitions so two adjacent cells butt to exactly 0.2 m. Any repeating room module should do the same.
5. **Transforms applied:** rotation/scale identity before saving. Instances in assemblies are **linked duplicates** (shared mesh) — edit the kit mesh once, every copy updates.
6. **Naming:** `SM_<Category>_<Name>_<Variant>` (e.g. `SM_Wall_Doorway_4m`, `SM_Item_Screwdriver`). Materials `M_*`, textures `T_*`.
7. **Interactables are separate objects:** vent cover has **4 parented screw objects** (Unity animates each), the pillow is separate from the bed (stash), cell door is separate from the shell (schedule slide).
8. **Doorway module:** 4 m run with a 3.5 × 3 m opening (0.25 m jambs, lintel above 3 m) — drop into any wall run.

## Collections in PrisonKit.blend

| Collection | Contents |
|---|---|
| `Kit_Architecture` | Walls 1/2/4 m, corner, T-filler, doorway, floors, roofs + soffit edge, light fixture, pillar, barred window, **all props** (lockers, signs, trash cans, extinguishers, clocks, barrels, pallets, machine, drains, floodlight, cell desk/stool, poster, kitchen counter, pipe runs) |
| `Kit_Cell` | Cell shell 4×5.5 m, sliding barred door, bed, pillow, toilet, sink, shelf, solitary shell 3×4 m, vent cover + 4 screws, duct |
| `Kit_Furniture_{Cafeteria,Showers,Workshop,Security,Courtyard}` | Room sets: table-bench, serving line, tray · stall, sink row, bench · workbench, shelf unit, tool board, crate · desk, monitor bank, chair · pull-up bar, weight bench, hoop |
| `Kit_Escape` | Fence panel 4 m (+ **pre-cut variant**), post, corner post, gate |
| `Kit_Items` | All 25 pickups from [[Item Catalog]] — every crafting part + tool (screwdriver, fake bed dummy, shovel, wire cutters, ladder, grappling hook, molotov…) |
| `Kit_Characters` | Rigged Guard + Prisoner (17-bone armature, rigid skinning, shared `Idle/Walk/Run/Jump` actions — animate in place, character faces −Y). Character FBX exports use `bake_space_transform=False` (unlike static pieces) to keep bone axes intact |
| `Assembly_*` | The assembled prison: `Floors`, `Roofs` (hide to see inside), `Walls`, `Cells`, `Furniture`, `Fence`, `Lights` |
| `Support` | Sun + review camera (never export) |

## Materials & textures

- Palette matches [[Content Inventory]] (concrete 0.28/0.27/0.25, warm emissive fixtures, accent yellow/white/red/blue).
- 8 textured materials use tileable PNGs wired straight into Base Color (FBX-friendly): concrete wall/ceiling, floor tile, cafeteria checker, metal shelf/bars, wood, porcelain. Everything else is flat color — intended for the low-poly style.
- UVs are **box-projected at 1 UV per meter** on every mesh; new pieces get the same treatment so textures tile seamlessly across modules.

## How to iterate

### Add a new prop/module
1. Model it in the right `Kit_*` collection following the rules above (grid footprint, origin, naming, existing `M_*` materials).
2. Box-project UVs (1 UV/m) if it uses a textured material.
3. Export FBX to `Assets/Models/BlenderKit/` — settings: **selected only, apply unit scale, FBX_SCALE_ALL, forward −Z, up +Y, bake space transform, no anim**. Move the object to origin for export, restore after.

### Build a new room or prison (Medium Security etc.)
1. Spec the plate table + topology in a new layout note first (copy the format of [[Prison Layout — Minimum Security]]) — Unity Z = Blender Y (north).
2. Assemble in a new `Assembly_<PrisonName>` collection using **linked duplicates** of kit pieces; wall runs = 4/2/1 m pieces + doorway modules on shared edges; floors are one slab per plate, top surface at z=0.
3. Re-use the cell shell / solitary shell for blocks; keep the 2 m plumbing corridor behind cell rows (vent route).
4. Export the assembly as one FBX (same settings, selected only).

### Iterate on an existing piece
Edit the mesh in `Kit_*` — every linked duplicate in every assembly updates automatically. Then re-export **both** the piece FBX and any assembly FBX that contains it.

## Unity import notes

- Imported materials arrive as Standard shader: select the FBX → **Extract Materials**, then run the **Render Pipeline Converter** (Window → Rendering) so URP doesn't render pink.
- **`Prison/Assets/Setup BlenderKit`** assigns tileable PNGs to `Materials/Prison/` URP mats, remaps `M_*` slots, generates prefabs under `Assets/Prefabs/BlenderKit/`, character prefabs, and wires `ItemData.worldPrefab`.
- `PrisonLevelLayoutRunner` instantiates kit prefabs on **Run Full Build** — structural pieces keep colliders, props do not (see [[Editor Tooling]]).
- `PrisonFacility.fbx` is the **assembled prison** used by `PrisonFacilityInstaller` — the layout runner's **Run Full Build** installs this monolith and wires gameplay (cells, doors, vents, zones). Per-piece kit FBX remain for isolated edits/re-export.
- **Cell gameplay anchors (7/15/2026):** `PrisonFacilityInstaller` derives spawn/roll-call from `Cell_XX_Bed` (interior floor), night-check from outside `Cell_XX_Door`; doors keep authored FBX poses and slide ~1.35 m on a local wall axis (not shell-center align). Rebake NavMesh after anchor changes.
- Kit FBX pivots match the snap conventions, so Unity grid snapping at 0.5 m works out of the box.

## Change policy

This note is the **source of truth for the asset kit**. To change assets: update this note (and [[Prison Layout — Minimum Security]] if the map changes) → edit `PrisonKit.blend` → re-export FBX → re-import in Unity. Never edit the FBX output or extracted materials by hand expecting it to survive a re-export.

Related: [[Prison Layout — Minimum Security]] · [[Content Inventory]] · [[Item Catalog]] · [[Editor Tooling]]
