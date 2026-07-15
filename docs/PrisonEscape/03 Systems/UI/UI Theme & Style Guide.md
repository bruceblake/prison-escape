# UI Theme & Style Guide

Shared visual language for every UI surface. Code source of truth: `Assets/Scripts/Shared/Prison/PrisonUITheme.cs`.

## Palette

| Token | Hex | Use |
|---|---|---|
| Caution yellow | `#F4D03F` | Travel grace, warnings, "awaiting" states |
| Hazard red | `#C0392B` | Enforcement, non-compliance, contraband |
| Concrete grey | `#95A5A6` | Neutral chrome |
| Ink green | `#5A8A50` | Satisfied requirements, "ready to craft" (muted — never neon green) |
| Command strip | `rgba(0.04, 0.06, 0.08, 0.88)` | Dark translucent HUD backdrops |

## Rules

- **Dark translucent backdrops** behind HUD text; never raw text over the 3D scene.
- **Warm light fixtures** in-world; UI stays cool/desaturated so caution yellow and hazard red pop.
- State colors always come from `PrisonUITheme` — no hand-typed near-duplicates in widget scripts.
- Menus are **diegetic where possible** (stolen notebook = paper, ink, sketches). HUD is non-diegetic but institutional: stencil-feeling caps, `[ BRACKETS ]` around phase titles.
- **Menu focus**: while any fullscreen/paper menu is open, ambient HUD (routine strip, hotbar, heat eye) fades to near-zero via `UIMenuFocus` so surfaces never overlap.
- Typography: TMP, bold for the "now" layer, normal for preview/next, ~32–40 pt HUD range at 1080p reference.

## Key files

`Assets/Scripts/Shared/Prison/PrisonUITheme.cs` · `Assets/Scripts/Shared/UI/UIMenuFocus.cs` · `Assets/Scripts/Shared/UI/CanvasGroupFader.cs`

Related: [[UI & HUD]]
