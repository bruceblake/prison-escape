# Character Visuals

In-game characters are currently **procedurally built low-poly humanoids** (rebuild via **Prison → Setup Character Visuals**). As of 7/14/2026 there are also **rigged + animated Blender characters** in the asset kit — see [[#Rigged Blender characters (asset kit)]] below; migrating the prefabs to them is an open task.

## Dimensions (`CharacterVisualConstants`)

| Constant | Value |
|---|---|
| Visual scale | 1.0× (base 2.0 m → **2.0 m tall**) |
| Capsule height | **2.0 m** |
| Capsule radius | **0.38 m** (~0.76 m wide — fits 1.2 m cell doors) |
| Eye height | ~1.66 m |
| Name label height | ~2.15 m |
| Social label height | ~1.75 m |

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
- **Animation wiring (7/15/2026):** per-role controllers built from each rig's **own** clips — `Char_Locomotion_Prisoner.controller` / `Char_Locomotion_Guard.controller`. Import uses full FBX take names (`SM_Char_*|Walk`, etc.) so real clips exist (short names left only `__preview__` stubs → frozen mid-stride).
- **Runtime (7/15 evening):** `BlenderKitLocomotionAnimator` **prefers procedural bone walk** (disables Mecanim when Hips/legs bind). Standing rest is sampled from Idle when possible, otherwise L/R legs are averaged so idle is not a frozen mid-stride bind pose. Speed still uses max(agent velocity, transform delta).

Related: [[Player & Interaction]] · [[Editor Tooling]] · [[Content Inventory]] · [[Blender Asset Kit]]
