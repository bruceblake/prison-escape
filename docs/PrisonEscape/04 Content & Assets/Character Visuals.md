# Character Visuals

In-game characters are currently **procedurally built low-poly humanoids** (rebuild via **Prison → Setup Character Visuals**). As of 7/14/2026 there are also **rigged + animated Blender characters** in the asset kit — see [[#Rigged Blender characters (asset kit)]] below; migrating the prefabs to them is an open task.

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

## Rigged Blender characters (asset kit)

Skinned, animated characters built in `ArtSource/PrisonKit.blend` (`Kit_Characters` collection) and exported to `Assets/Models/BlenderKit/Characters/`:

| FBX | Look (doc palettes) |
|---|---|
| `SM_Char_Guard.fbx` | Navy uniform, dark vest, belt + gold buckle, gold epaulettes/badge, navy cap (2.66 m with cap) |
| `SM_Char_Prisoner.fbx` | Orange jumpsuit, 3 white stripe bands + chest pocket (2.56 m) |

- **Rig:** 17 bones, Unity-friendly names (`Root/Hips/Spine/Chest/Head`, `UpperLeg/LowerLeg/Foot`, `UpperArm/LowerArm/Hand` L/R), rigid per-part skinning (one bone per cube). Import as **Generic** rig.
- **Animations** (baked into each FBX as takes, 24 fps): `Idle` (2 s breathing bob), `Walk` (1 s cycle — leg swing 38°, arm 28°, knee bend, matching the procedural animator constants), `Run` (0.67 s, bigger swing + forward lean), `Jump` (1 s non-looping: crouch → takeoff → tuck → land). Locomotion clips loop; animate in place (no root motion) — code drives movement, same as the procedural pipeline.
- Character faces −Y in Blender → +Z forward in Unity. Shared `M_Char_*` materials (skin 0.82/0.62/0.48 + role palettes).
- **Migration (7/14/2026):** complete — `BlenderKitLocomotionAnimator` drives the rigs. Player is the prisoner mesh (inmate).
- **Animation wiring (7/15/2026):** per-role controllers built from each rig's **own** clips — `Char_Locomotion_Prisoner.controller` / `Char_Locomotion_Guard.controller` (no cross-rig retargeting; legacy shared `Char_Locomotion.controller` kept only as load fallback). Each has the `Speed` 1D blend (idle 0 / walk 1.6 / run 4.2) **plus a `Jump` state** (any-state entry on the `Jump` trigger, exit-time return). `BlenderKitLocomotionAnimator` now feeds **continuous smoothed speed** (smooth idle↔walk↔run instead of threshold snapping) and fires `Jump` on CharacterController takeoff. Prefab builds strip the FBX importer's redundant child Animator — exactly one Animator per character. All four clips (Idle/Walk/Run/Jump) are in use.

Related: [[Player & Interaction]] · [[Editor Tooling]] · [[Content Inventory]] · [[Blender Asset Kit]]
