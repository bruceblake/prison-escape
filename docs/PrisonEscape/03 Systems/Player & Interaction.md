# Player & Interaction

First-person inmate: movement, camera, and the raycast interaction framework everything tactile runs through.

## Movement & camera

Singleplayer reuses the multiplayer `PlayerController` (prediction-capable) + `PrisonerController` for compliance.

| Parameter | Value |
|---|---|
| Move speed | 5 (sprint ×2 with Left Shift) |
| Jump height | 2 |
| Gravity | -9.81 |
| Mouse sensitivity | 20 (LocalPlayer prefab) |
| Pitch clamp | ±85° |
| Character height | 2.6 m (1.3× visual scale — see [[Character Visuals]]) |
| Eye height | ~2.16 m |

Camera only rotates while the cursor is locked (inventory/notebook unlock it). Movement is disabled while arrested.

## Controls

| Input | Action |
|---|---|
| WASD / Space / LShift | Move / jump / sprint |
| **F** | Press-interact |
| **LMB (hold)** | Hold-interact (e.g. unscrewing); suppresses gun fire |
| **E** | Inventory bag |
| **Tab** | Stolen notebook (map / social / workbench / schedule) |
| **1–6** / wheel | Hotbar select |

## Interaction framework

`PlayerInteractor` raycasts from the camera (range **5** on LocalPlayer; script default 3) against `IInteractable` targets. Two input types: `Press` and `Hold` (hold duration per-interactable, reduced by tool `interactionSpeedModifier`).

| Interactable | Input | Behavior |
|---|---|---|
| `WorldItemPickup` / `PickupItem` | Press | Add to inventory (destroy only on success) |
| `InteractableScrew` | Hold 2 s (~1.33 s with Screwdriver) | Requires equipped tool; notifies vent cover |
| `PillowStash` | Press | Hide/retrieve 1 item ([[Escape Routes & Mechanics]]) |
| `CellBed` | Press | Place fake bed dummy (consumes item) |
| `WorldContainer` | Press | Browse loot (no auto-transfer yet) |

Reticle (`InteractionReticleView`): 6 px idle → 20 px on target.

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Multiplayer/Player/PlayerController.cs` | Movement (+prediction) |
| `Assets/Scripts/Multiplayer/Player/CameraController.cs` | Look |
| `Assets/Scripts/Singleplayer/Player/PrisonerController.cs` | Compliance / arrest |
| `Assets/Scripts/Singleplayer/Player/PlayerInteractor.cs` | Raycast + hold logic |
| `Assets/Scripts/Shared/Interaction/IInteractable.cs` | Contract |
| `Assets/Prefabs/LocalPlayer.prefab` | The tuned local player |

Related: [[Inventory & Items]] · [[Escape Routes & Mechanics]] · [[Prisoner AI & NPCs]] · [[Multiplayer & Networking]]
