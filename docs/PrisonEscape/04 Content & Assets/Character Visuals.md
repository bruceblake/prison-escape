# Character Visuals

Characters are **procedurally built low-poly humanoids** — no imported models. Rebuild everything via **Prison → Setup Character Visuals**.

## Dimensions (`CharacterVisualConstants`)

| Constant | Value |
|---|---|
| Visual scale | 1.3× (base 2.0 m → **2.6 m tall**) |
| Collider radius / center Y | 0.65 / 1.3 m |
| Eye height | ~2.16 m |
| Name label height | ~2.8 m |
| Social label height | ~2.28 m |

## Pipeline

1. `CharacterVisualRole` — Player / Guard / Prisoner
2. `LowPolyCharacterRigBuilder` — hierarchy of cubes with pivots (`AnimRoot`, hips, knees, arms, elbows, torso, head)
3. Role extras: Guard = vest, belt, epaulettes, cap + badge · Player = teal vest + bandana · Prisoner = chest pocket + 3 stripe bands
4. `LowPolyLocomotionAnimator` — procedural walk/idle (leg swing 38°, arm 28°, knee 24°, cycle speed 8, idle bob 2.2); reads NavMeshAgent or CharacterController speed
5. `CharacterNameLabel` — billboard TMP (font 5, white + dark outline)
6. `CharacterVisualSetupRunner` (editor) — generates URP materials, rebuilds prefab visuals, sizes colliders/agents, places camera/eye anchors

## Role → prefab mapping

| Role | Prefabs |
|---|---|
| Player | `Prefabs/Player`, `Prefabs/LocalPlayer` |
| Guard | `Prefabs/AIPrefabs/Guard` |
| Prisoner | `Prefabs/NPCs/Prisoner_NPC`, `Prefabs/AIPrefabs/Prisoner` |

Palettes are in [[Content Inventory]]. Cached meshes: `Assets/Meshes/Characters/LowPolyHumanoid_{Player,Guard,Prisoner}.asset`.

## Rules

- Character eye line (~2.16 m) is what the camera height must match — don't tune the camera independently
- New roles (e.g. Warden, Cook) = new `CharacterVisualRole` entry + palette + extras in the rig builder + runner mapping

Key files: `Assets/Scripts/Shared/Visuals/` + `Assets/Editor/CharacterVisualSetupRunner.cs`

Related: [[Player & Interaction]] · [[Editor Tooling]] · [[Content Inventory]]
