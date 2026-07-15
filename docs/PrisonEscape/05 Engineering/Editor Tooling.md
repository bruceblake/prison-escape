# Editor Tooling

Repeatable scene generation and balance tools. **Rule: change the tool and re-run — never hand-edit generated objects** (they're wiped on rebuild).

## Prison level layout (`Assets/Editor/PrisonLevelLayoutRunner.cs`)

Menu **Prison → Layout →**:

| Item | Action |
|---|---|
| 0 — Apply Connected Diagram Layout | Tile the 18 floor plates from [[Prison Layout — Minimum Security]] (deletes legacy floors, repositions cell blocks) |
| 1 — Rename East Cells (09-16) | East block cell renaming + number labels |
| 2 — Build Walls + Roofs | 6 m walls with **BoxCollider** on structural segments; doorway lintels + jambs; roof overhang + exterior soffits |
| 3 — Build All Lighting | Light grids per plate + per-cell lights (~370 after density pass) |
| 4 — Furnish Rooms (Scratch Build) | Cube-built furniture (cafeteria, showers, workshop, security, courtyard) |
| 5 — Wire Registry | `PrisonLocationRegistry` cells + zones |
| 6 — Build Escape Systems | Solitary block (4 cells), escape boundary ring, restricted zones, `EscapeManager` wiring |
| **Run Full Build** | All steps (0→6) + save scene |

Key constants: hub (-26, -98) · corridor width 4 m · wall height sampled 6 m · floor surface Y synced to `JailCell_01` spawn (~0.82 m) · edge tolerance 0.35 · doorway 3.5×3 m.

**Wall rules (7/14/2026):**
- `CreateWallBlock` — structural walls/partitions keep physics colliders (walkable blocking).
- `CreateBlock` — visual-only props/roofs/lights (collider stripped).
- **Cell wing keep-out:** on `CellWingFloor_West` / `CellWingFloor_East`, skip wall/lintel/jamb segments that intersect jail cell zone bounds (cells supply their own geometry).

Layout truth lives in `BuildDiagramPlates()` — keep in sync with [[Prison Layout — Minimum Security]].

## Character visuals (`CharacterVisualSetupRunner.cs`)

**Prison → Setup Character Visuals** — builds all role materials, rigs, colliders, labels ([[Character Visuals]]).

## Other tools

| Tool | Menu / usage |
|---|---|
| `PrisonOverhaulRunner` | **Prison → Fix Cell Doors & UI** (legacy door replacement) |
| `SocialBalanceSimulatorWindow` | **Tools → Prison → Social Balance Simulator** — preview affinity math |
| `PrisonNavMeshValidator` | Component context menu **Validate Prison NavMesh Now** — checks stand points sit on NavMesh |

## Workflow with Unity MCP

Cursor can run these menu items directly in the live editor, read console output, and query the scene to verify results — see [[Development Workflow]]. Remember Unity must **recompile** after script edits before the new code runs.

## Pattern for new tools

1. Static class in `Assets/Editor/` with `[MenuItem("Prison/...")]` entries
2. Idempotent: clear generated roots (`LayoutFloors`, `LayoutWalls`, …) before rebuilding
3. Numbered steps + a "run everything" item
4. Log a `[PrisonLayout]`-style summary line for MCP verification
5. Save the scene at the end of full builds

Related: [[Prison Layout — Minimum Security]] · [[Codebase Map]] · [[Development Workflow]]
