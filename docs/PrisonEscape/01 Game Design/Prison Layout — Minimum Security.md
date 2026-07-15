# Prison Layout — Minimum Security (MVP)

The canonical map spec. Implemented by `Assets/Editor/PrisonLevelLayoutRunner.cs` (menu: **Prison → Layout → Run Full Build**) in scene `PrisonLevel1.unity`.

Reference diagram:
![[Minimum Security Prison Layout.png|469]]

## Topology (top-down, +Z = north)

```
              ┌──────────[ Corridor: Loop North ]──────────┐
              │            ┌───────────────┐               │
              │            │   COURTYARD   │ (open air)    │
              │            └───────┬───────┘               │
              │        [ Corridor: North ]                 │
┌─────────┐ ┌┴────────┐ ┌────────┐ ┌────────┐ ┌──────────┐ │
│  West   │ │  MAIN   │ │ CELLS  │ │ CELLS  │ │          │ │ East
│  Spine  │←│SECURITY │←│  1–8   │←│CAFETER.│→│  9–16   │→│WORKSHOP│ Spine
└─────────┘ └┬────────┘ └────────┘ └────────┘ └──────────┘ │
              │        [ Corridor: South ]                 │
              │            ┌───────┬───────┐               │
              │            │    SHOWERS    │               │
              │            └───────────────┘               │
              └──────────[ Corridor: Loop South ]──────────┘
```

**Central spine (west → east):** Main Security → Left cell block (cells 1–8) → Cafeteria/Mess Hall → Right cell block (cells 9–16) → Workshop. All five plates share edges — no gaps.

**North wing:** inner corridor above the spine → Courtyard (open air, no roof).
**South wing:** inner corridor below the spine → Showers.
**Outer perimeter loop:** corridors surround the entire inner facility, connected to Main Security on the west side.

## Cell blocks

- **Left block (`JailCells`):** cells 01–08. Bottom row 1–4, top row 5–8 (west to east).
- **Right block (`JailCells_East`):** cells 09–16, mirroring the left. Bottom row 9–12, top row 13–16.
- Behind each block: **plumbing/electrical corridor** connected to the cells, traversable via vents (designed; see [[Escape Routes & Mechanics]]).

## Implemented dimensions (world units ≈ meters)

Hub (cafeteria center) at world `(-26, -98)`. All floor plates at Y = 0.6, thickness 0.2.

| Plate | Center (X,Z) | Size (W×D) |
|---|---|---|
| MainSecurityFloor | (-95, -98) | 28 × 56 |
| CellWingFloor_West | (-59, -98) | 44 × 56 |
| CafeteriaFloor | (-26, -98) | 22 × 56 |
| CellWingFloor_East | (7, -98) | 44 × 56 |
| WorkshopFloor | (43, -98) | 28 × 56 |
| Corridor_North / South | (-26, -68) / (-26, -128) | 166 × 4 |
| CourtyardFloor | (-26, -48) | 94 × 36 |
| ShowerFloor | (-26, -148) | 94 × 36 |
| Corridor_LoopNorth / LoopSouth | (-26, -28) / (-26, -168) | 178 × 4 |
| Corridor_WestSpine / EastSpine | (-115, -98) / (63, -98) | 4 × 144 |
| + security loop link and 4 corner connectors | | 4 wide |

## Build rules

- **Walls:** 6 m tall (sampled from `JailCell_01`), 0.2 m thick, only on **exterior edges** — no walls where plates touch
- **Roofs:** every plate **except the Courtyard** (open air for the fence escape)
- **Lighting:** grid of ceiling fixtures per room/corridor + one light per cell (~635 point lights); warm color (1, 0.95, 0.85)
- **Furniture:** scratch-built from cubes + prison materials (no prefabs) — cafeteria tables/serving line, shower stalls/sinks/benches, workshop benches/shelves, security desk/monitor bank, courtyard exercise equipment

## Solitary confinement block

A section of **4 solitary cells** at the **south end of Main Security** (scratch-built interior partitions, each cell ~3×4 m with a spawn point and barred front). Players caught escaping are brought here — see [[Escape Completion System]].

## Escape boundary & restricted zones

- **Escape boundary:** a perimeter volume encircling the whole facility beyond the loop corridors/courtyard fence — crossing it = escaped.
- **Restricted zones:** vent/plumbing corridors, beyond the fence line, and the outer band between loop corridors and the boundary are always restricted; some interior rooms are restricted per phase ([[Escape Completion System]]).

## Escape-relevant geography

| Feature | Location | Escape relevance |
|---|---|---|
| Main Security | far west | gates the **front entrance** route |
| Courtyard | north, open air | **barbed-wire fence** cut route |
| Vent corridors | behind cell rows | hidden traversal (planned in geometry) |
| Perimeter loop | outermost corridors | patrol route; also the last ring before outside |

## Change policy

This note is the **source of truth for the map**. To change the layout:
1. Update this note (and the diagram if topology changes)
2. Edit the plate table in `PrisonLevelLayoutRunner.BuildDiagramPlates()`
3. Re-run **Prison → Layout → Run Full Build**
4. Never hand-move generated floors/walls — they are wiped on rebuild

Related: [[Game Vision & Core Loop]] · [[Escape Routes & Mechanics]] · [[Editor Tooling]]
