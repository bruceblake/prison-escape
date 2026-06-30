# Prison Game — Feature & Test Coverage Documentation

_Last updated: 2026-06-30_

This document is a single-source overview of **what exists in the game today** and **what is covered by automated tests**. It is meant to be read top-to-bottom to understand the systems, then used as a reference (see the Coverage Matrix in §4).

Legend used throughout:

| Symbol | Meaning |
|---|---|
| ✅ Full | Core logic has automated EditMode tests that pass |
| 🟡 Partial | Some pure logic tested; runtime/integration behavior not yet |
| 🧪 Planned (Tier 2) | Logic is testable after a small "extract pure helper" refactor |
| 🎬 Planned (PlayMode) | Needs a live scene / `Update` loop / physics / NavMesh to test |
| ⚪ Untested | No tests yet, and not currently scheduled |

---

## 1. What the game is

A **single-player prison-escape simulation** built in **Unity 6000.4.4f1 (URP)**, with a secondary **multiplayer FPS** layer.

> **The main goal of the game is to ESCAPE the prison through various escape routes.** Escaping is the intended primary objective — though the player is not *forced* to escape and can keep living the prison life. Every other system (schedule compliance, social standing, crafting, contraband) exists primarily to **enable, fund, or cover for an escape attempt**. See **§1.1** for the dedicated escape breakdown.

The single-player core loop is driven by a **daily routine/schedule**: the prison moves through timed phases (roll call, meals, free time, lights out, night/morning checks). The player is an inmate who must **stay compliant** with the schedule (be in the right place at the right time) — or *appear* compliant — while secretly **preparing and executing an escape**. Supporting goals include **building social standing**, **crafting tools/weapons**, and **collecting/hiding contraband**, all of which feed into escape. **AI guards** patrol, detect non-compliance, run shakedowns, and verify bed presence at night (the main thing an escapee must defeat). **AI prisoners** follow the same routine. A **social/reputation system** lets the player greet, gift, do favors for, or betray other inmates to climb reputation tiers.

The project also contains a **multiplayer FPS** layer (custom Riptide-style `NetworkManager`, lobby, weapons, client-side prediction/reconciliation) that shares the inventory/item code with single-player.

### Project layout

```
3dgame/Assets/Scripts/
├── Shared/        # Used by both single- and multiplayer (Prison sim, Crafting, UI, Interaction, Items)
├── Singleplayer/  # AI, prisoner/player controllers, items, security
├── Multiplayer/   # Networking, FPS player/camera/weapons
└── Editor/        # Editor-only tooling (world overhaul runner, social balance simulator)

3dgame/Assets/Tests/Editor/   # All automated EditMode tests
3dgame/Assets/Docs/           # This document + design docs
```

---

## 1.1 The main goal: ESCAPE 🔑

Escape is the **central objective**. The intended fantasy: study the routine, acquire/craft the right tools, defeat the guards' detection (especially the **night bed check**), and slip out through an **escape route** to freedom — without getting caught.

### Escape-enabling mechanics that exist today

| Mechanic | Scripts | What it does for an escape | Status |
|---|---|---|---|
| **Vent route** | `VentCover`, `InteractableScrew` | Unscrew all screws on a vent cover (requires the correct **tool**); when the last screw is out, the cover slides open and its **passage collider** is enabled — a physical way out of a room. | 🧪/🎬 (logic extractable; slide is PlayMode) |
| **Fake bed dummy** | `CellBed`, `FakeBedDummy` | Place a dummy in your bed so the **night bed-presence check** passes while you're elsewhere preparing/escaping. The dummy is "discovered" at morning line-up and raises suspicion. | 🧪/🎬 |
| **Contraband stash** | `PillowStash` | Hide tools/contraband so the **morning shakedown** doesn't confiscate them. | 🧪/🎬 |
| **Crafting** | `CraftingSystem`, `CraftingRecipe` | Build the tools/weapons needed to open routes or deal with guards. | ✅ tested |
| **Tools / contraband items** | `ItemData`, `ToolData`, `WeaponData` (`ItemCategory.Tool/Weapon/Contraband`) | The gear an escape depends on; these are exactly what guards confiscate. | ✅ classification tested |
| **Guard detection (the obstacle)** | `GuardDetection`, `GuardFSM`, `MorningShakedownSweeper` | The opposition the escapee must avoid: vision cones, non-compliance detection, bed checks, shakedowns. | 🧪/🎬 |

### ⚠️ Known gap: there is no escape *completion* / win state yet

There is currently **no "you escaped / you reached freedom" system** in the codebase — no exit/escape-zone trigger, no win or game-over state, and no escape objective/progress tracking. Searching the project for an escape/win condition returns only an unrelated string helper.

**What this means:** the *ingredients* of escape exist (a vent you can open, a way to fake the bed check, tools to craft, guards to evade), but the **keystone that makes escape the actual goal is missing**. To deliver the intended main goal, the next gameplay feature to build is an **escape-completion system**, e.g.:

- An **EscapeZone / ExitTrigger** the player reaches to win.
- An **EscapeManager** tracking objective state (prerequisites met → route opened → reached exit → escaped) and raising a win event.
- Optional: tie it to the existing `PrisonSecurityAlerts` (a botched attempt → lockdown/recapture) and a "heat" level (`PrisonHeatUI` already exists in the UI).

This system would also be **highly testable** (objective state machine = pure EditMode logic; the trigger/flow = PlayMode), so it fits the testing roadmap cleanly.

---

## 2. Systems / features

Each subsection lists the feature's purpose, the key scripts, and its current test status.

### A. Time & Daily Schedule — 🟡 Partial
The heartbeat of the single-player game.

- **`PrisonTimeManager`** (singleton MonoBehaviour) — advances in-game minutes, drives phase transitions, exposes progress fractions, compliance grace windows, morning roll-call gating, and the `OnEventChanged` event.
- **`PrisonSchedule`** (ScriptableObject) — the data: an ordered list of `ScheduleEntry { eventType, startTimeMinutes, durationMinutes }` plus `minutesPerRealSecond`.
- **`PrisonEventType`** (enum) — `RollCall, Breakfast, Lunch, Dinner, FreeTime, LightsOut, MorningRollCall, NightRollCall`.
- **`PrisonEventRules`** — mandatory vs. flexible phase classification + "high-stakes upcoming" handoff detection.
- **`PrisonEventExtensions`** — `IsMorningLineUp`, `IsNightBedPhase` helpers.
- **`PrisonRoutineLabels`** — display strings for HUD (phase titles, "go to" destinations, player-location formatting).

**Tested:** ✅ `PrisonEventRules` (mandatory/flexible/high-stakes across all phases), ✅ `PrisonEventExtensions` (both helpers across all phases), ✅ `PrisonRoutineLabels.FormatPhaseTitle` (all phases, upper/title case) + `FormatPlayerLocation` (null/empty/prefix strip) + registry-fallback `GetGoToLabel`.
**Not yet:** 🧪/🎬 `PrisonTimeManager` tick/advance, progress math, grace windows (time-dependent → extract pure helpers for EditMode, full clock in PlayMode).

### B. Locations, Zones & Cells — 🟡 Partial
- **`PrisonLocationRegistry`** (singleton) — array of `CellData`, the cafeteria/yard/roll-call zones, and per-cell occupancy tracking. Resolves "where should an inmate stand for this event."
- **`PrisonLocationZone`** — a tagged area (Cell / Cafeteria / Yard / RollCallArea) with stand points and a HUD label; trigger volume registers prisoners entering/leaving.
- **`CellData`** (plain serializable class) — per-cell transforms (spawn, roll-call stand, night-check approach, bed-presence center, shakedown sweep center) + interior radius.
- **`PrisonNavMeshValidator`** — editor/runtime check that stand points sit on the baked NavMesh.

**Tested:** ✅ `PrisonLocationZone.GetHudLabel` (custom + all default types), ✅ `CellData.InteriorRadius` (fallback threshold).
**Not yet:** 🧪 registry occupancy dict + `GetStandPointForEvent` switch; 🎬 zone triggers, NavMesh validation.

### C. Cell Doors — ✅ Full
- **`CellDoorController`** — custom barred sliding doors that open/close based on the current schedule phase (open during day/active phases, closed at lights-out/night roll call). Deterministic slide math, initialization from the closed transform position.

**Tested:** ✅ Complete (25 tests): `IsOpenPhase` for every enum value, open/closed target position math, offset sign handling, initialization capture, `StepToward` slide (no overshoot, convergence, zero-delta), and design contracts (default offset clears doorway width, positive slide speed).

### D. Roll Call & Morning Shakedown — 🟡 Partial
- **`MorningRollCallTracker`** (singleton) — tracks which cells have completed shakedown during morning line-up; raises completion events; gates inmate release from the stand.
- **`MorningShakedownSweeper`** — a guard coroutine that walks each cell during morning line-up, confiscates contraband, and marks cells cleared.

**Tested:** ✅ `MorningShakedownSweeper.ShouldConfiscate` (Contraband/Tool/Weapon confiscated; CraftingPart/Consumable kept; null safe).
**Not yet:** 🧪 tracker HashSet/“all complete” logic (inject prisoner list); 🎬 the sweep coroutine + physics overlap confiscation.

### E. AI — Guards — ⚪/🧪/🎬 Untested (planned)
- **`GuardFSM`** — patrol / escort / night-cell-verification state machine; syncs to schedule.
- **`GuardDetection`** — vision cone + range + proximity spotting; finds non-compliant prisoners; verifies bed presence via overlap.
- **`GuardShiftController`** — spawns/enables guards on duty for given phases (roles: standard patrol, morning shakedown, etc.).

**Tested:** none yet.
**Plan:** 🧪 extract `GuardDetection.IsInSight` (cone/range/proximity geometry) and `GuardShiftController.IsOnDutyFor` (role × phase) for EditMode; 🎬 full FSM transitions in PlayMode (NavMesh + time).

### F. AI — Prisoners — ⚪/🧪/🎬 Untested (planned)
- **`PrisonerAI`** / **`PrisonerController`** (both implement **`IPrisoner`**) — navigate to the expected location for the current phase, report compliance, handle roll-call release timing.
- **`PrisonerPresence`** — scene-wide registry of prisoners.

**Tested:** none yet.
**Plan:** 🧪 extract the `IsZoneCompliantForEvent` (event × zone) matrix for EditMode; 🎬 NavMesh travel + compliance flips in PlayMode.

### G. Social & Reputation — 🟡 Partial
See also `Prison_Social_And_Reputation_System.md` for design depth.

- **`SocialMath`** (static, pure) — the model: base affinity deltas per action, favored-gift doubling, same-category gift penalty, the **positive-only soft cap** (`delta × (1 − affinity/100) × gainMultiplier`), clamping to [-100, 100], reputation-tier thresholds, and average affinity.
- **`SocialManager`** (singleton) — orchestrates per-prisoner affinity, greeting phase cooldown, favor rolls per schedule phase, favor completion (consumes an inventory item), personality perk unlock at affinity ≥ 50, reputation-tier-change events.
- **`SocialActionType`** (enum), **`ReputationTier`** (enum: Outsider/Associate/Respected/Kingpin).
- **`NPCPersonalityData`**, **`FavorOfferDefinition`** (ScriptableObjects) — personality tuning + favor offers with phase/personality validity filters.
- **`PrisonerSocialPresenter`** — the interactable that shows an inmate's affinity bar and routes social actions.

**Tested:** ✅ `SocialMath` end-to-end (base deltas for every action, favored-gift, same-category penalty, soft cap at 0/50/100, negative bypass, gain multiplier, min/max clamp, tier boundaries, averages, full gift scenario), ✅ `FavorOfferDefinition.IsValidFor` (phase + personality filters), ✅ `ReputationTier` int-value pinning.
**Not yet:** 🧪/🎬 `SocialManager` orchestration (singleton + time + RNG favor roll + inventory mutation).

### H. Inventory — ✅ Full (core logic)
- **`PlayerInventory`** (+ `InventorySlot`) — slot-based inventory: only `CraftingPart` items stack; identity matches by reference **or** `itemName`; add respects `maxSlots`; remove decrements and clears empty slots; selected/equipped slot lookup; tool lookup.

**Tested:** ✅ stacking vs. non-stacking, full-inventory rejection, `HasItem` required amounts, name-based `CountItem` across instances, remove/clear, equipped-slot retrieval.
**Not yet:** 🎬 raycast hover state (UI/physics).

### I. Crafting — ✅ Full
- **`CraftingSystem`** (static) — `CanCraft` (all ingredients present) and `TryCraft` (consume ingredients, produce result).
- **`CraftingRecipe`** (+ `CraftingIngredient`) (ScriptableObject) — ingredients, result, result amount.
- **`CraftingRecipeDescription`** (static) — rich TMP requirement lines / "have/need" paragraph with colors.

**Tested:** ✅ `CanCraft` (null args, sufficient/insufficient), `TryCraft` (consumes + produces + not re-craftable, fails when unable), `IngredientRequirementLines` (formatting, zero-amount clamp, null recipe), `IngredientsRichParagraph` (no-ingredients message, have-enough check mark + counts, missing bullet + counts).

### J. Items & Loot — 🟡 Partial
- **`ItemData`** (+ `ItemCategory`: CraftingPart/Tool/Weapon/Consumable/Contraband, `ItemRarity`: Common/Uncommon/Rare/Legendary), **`ToolData`**, **`WeaponData`** (ScriptableObjects).
- **`LootTable`** (ScriptableObject) — weighted random selection; base weight by rarity (Common 60 / Uncommon 25 / Rare 10 / Legendary 5) × item weight multiplier.
- **`ItemSpawnNode`**, **`WorldContainer`**, **`PickupItem`** — world spawning & lootable containers.

**Tested:** ✅ `LootTable.GetRarityBaseWeight` (all rarities).
**Not yet:** 🎬/🧪 `GetRandomItem` weighted roll (seed RNG or inject), container/spawn-node population.

### K. Interaction & Escape mechanics — ⚪/🧪/🎬 Untested (planned)
**These are the building blocks of the game's main goal (see §1.1).**
- **`IInteractable`** (+ `InteractionInputType`) — interaction contract.
- **`PlayerInteractor`** — raycast targeting + hold-to-interact.
- **`WorldItemPickup`**, **`PillowStash`** (hide contraband), **`VentCover`** + **`InteractableScrew`** (unscrew to open a vent — an **escape route**), **`CellBed`** + **`FakeBedDummy`** (place a dummy to fool the **night bed check**).

**Tested:** none yet.
**Plan:** 🧪 prompt/`CanInteract` rules (with a mock inventory), `InteractableScrew.UpdateUnscrewing` math, `VentCover` screw-count→open threshold; 🎬 actual `Interact` side effects (spawn/destroy/animation).
**⚠️ Missing keystone:** no escape *completion*/win system exists yet (see §1.1) — this is the main-goal feature still to be built.

### L. Economy — ⚪/🧪 Untested (planned)
- **`PlayerWallet`** (singleton) — balance with clamp-at-zero, contraband-cash flag, change events.

**Tested:** none yet.
**Plan:** 🧪 extract `SetBalance`/`Add` clamp logic for EditMode.

### M. Security Alerts — ✅ Full
- **`PrisonSecurityAlerts`** (static) — global `OnLockdown` / `OnSuspicion` event hooks for alarms/UI/chase logic.

**Tested:** ✅ both raise paths invoke subscribers with the correct reason string.

### N. Player Controllers & Camera (FPS / multiplayer) — 🎬 Untested (planned)
- **`PlayerController`** (CharacterController movement + client prediction/reconciliation), **`CameraController`** (mouse look + clamp), **`WeaponController`**, **`WeaponSway`**.

**Tested:** none yet.
**Plan:** 🎬 PlayMode (Input, CharacterController, networking); 🧪 reconciliation buffer math could be extracted.

### O. Networking (multiplayer) — ⚪ Untested
- **`NetworkManager`** (custom, Riptide-style singleton), **`LobbyManager`**, **`SinglePlayerLauncher`**.

**Tested:** none. Integration/PlayMode only; not currently scheduled.

### P. UI / HUD — 🟡 Partial (a few pure seams), mostly 🧪/🎬
HUD and menus: `InventoryUI`, `InventorySlotUI`, `HotbarUI`, `ItemTooltipUI`, `PauseManager`, `CanvasGroupFader`, `AffinityFloatPopup`, `PrisonSocialRowUI`, `PrisonScheduleUI`, `RoutineNowNextBarUI`, `CashUIController`, `ComplianceStatusHUD`, `DailyRoutineBarUI`, `PrisonHeatUI`, `StolenNotebookUI`, `RecipeRequirementSlotUI`, `NotebookRecipeIndexEntry`, `HeldItemDisplay`, `InteractionReticleView`, `PillowStashProximityUI`, plus theming in `PrisonUITheme`.

**Tested:** Indirectly, the crafting requirement text (`CraftingRecipeDescription`) used by recipe UI is ✅.
**Plan:** 🧪 EditMode for pure formatting/state seams (`PrisonSocialRowUI` fill math, `CashUIController` format, `RoutineNowNextBarUI` state machine helpers, `PrisonScheduleUI.FormatEvent`, `ItemTooltipUI` string builders, `InventorySlotUI.IsIllegalCategory`); 🎬 PlayMode for canvas widgets (open/close, drag-and-drop, fades, HUD reacting to simulated state).

### Q. World building & editor tooling — ⚪ Untested (by design)
- **`GameManager`** — world setup, NPC spawning, social-system bootstrapping, world spawn population.
- **`PrisonOverhaulRunner`**, **`SocialBalanceSimulatorWindow`** (Editor) — authoring tools.
- Recent additions: the **prison decor/lighting pass** (`PrisonDecor` object: corridor/cafeteria lights, accent materials, signage, guideline stripes).

**Tested:** none (editor tooling / scene authoring; validated visually, not via unit tests).

---

## 3. Automated test suite

All tests are **EditMode** NUnit tests in `3dgame/Assets/Tests/Editor/`, namespace `Prison.Tests`. They run against the game's default `Assembly-CSharp` (no `.asmdef`), so no assembly setup is required.

| Test file | Area covered |
|---|---|
| `CellDoorControllerTests.cs` | Cell door open/close, slide math, init, design contracts |
| `SocialMathTests.cs` | Affinity/reputation model (base deltas, gifts, soft cap, clamp, tiers, averages) |
| `PrisonRulesAndLabelsTests.cs` | Event rules/extensions, routine labels, zone labels, cell radius, favor validity, enum pinning |
| `CraftingInventoryLootTests.cs` | Inventory, crafting, recipe descriptions, loot weights, confiscation, security alerts |

### Last run result

```
Passed = 136    Failed = 0    Skipped = 0    Inconclusive = 0
```

- **25** pre-existing cell-door tests + **111** new Tier-1 tests = **136 total, all green.**

### How to run the tests

- In the Unity Editor: **Window → General → Test Runner → EditMode → Run All**.
- The suite is pure logic (no scene required) and completes in well under a second.

---

## 4. Coverage matrix (quick reference)

| Feature | Status | Where tested / next step |
|---|---|---|
| Cell doors | ✅ Full | `CellDoorControllerTests` |
| Social/reputation math | ✅ Full | `SocialMathTests` |
| Inventory core | ✅ Full | `CraftingInventoryLootTests` |
| Crafting | ✅ Full | `CraftingInventoryLootTests` |
| Security alerts | ✅ Full | `CraftingInventoryLootTests` |
| Event rules / phase classification | ✅ Full | `PrisonRulesAndLabelsTests` |
| Routine / zone / cell labels & radius | ✅ Full | `PrisonRulesAndLabelsTests` |
| Favor offer validity | ✅ Full | `PrisonRulesAndLabelsTests` |
| Contraband classification | ✅ Full | `CraftingInventoryLootTests` |
| Loot rarity weights | ✅ Full | `CraftingInventoryLootTests` |
| Time manager (clock/progress/grace) | 🧪/🎬 | Extract progress math; PlayMode for tick |
| Location registry occupancy | 🧪 | Extract dict + event→location switch |
| Roll-call tracker | 🧪 | Extract set/“all complete” logic |
| Morning shakedown sweep | 🎬 | PlayMode coroutine (confiscate-rule already ✅) |
| Guard detection (vision) | 🧪 | Extract `IsInSight` geometry |
| Guard shifts | 🧪 | Extract `IsOnDutyFor` |
| Guard FSM | 🎬 | PlayMode |
| Prisoner compliance | 🧪/🎬 | Extract `IsZoneCompliantForEvent`; PlayMode nav |
| Social manager orchestration | 🧪/🎬 | Extract favor roll / affinity apply |
| Items & loot rolls | 🎬/🧪 | Seed/inject RNG |
| Interaction & escape mechanics | 🧪/🎬 | Extract prompt/can-interact + unscrew math |
| **Escape completion / win condition** | ⚪ **not built** | **Build `EscapeZone`+`EscapeManager` (the main-goal feature, see §1.1)** |
| Economy (wallet) | 🧪 | Extract clamp logic |
| Player/camera/weapons (FPS) | 🎬 | PlayMode |
| Networking | ⚪ | Integration only |
| UI / HUD | 🧪/🎬 | Extract formatting; PlayMode widgets |
| World/editor tooling | ⚪ | Not scheduled |

---

## 5. Roadmap

0. **(Gameplay, highest priority for the main goal) Build the escape-completion system.** See §1.1: an `EscapeZone`/`ExitTrigger` + `EscapeManager` objective state machine + win event (and optional recapture-on-failure via `PrisonSecurityAlerts`/heat). Without this, the game's stated main goal — escape — has no actual completion. The objective state machine is pure logic (EditMode-testable); the trigger/flow is PlayMode.

### Remaining test work

1. **Tier 2 — extract pure seams (small, behavior-preserving refactors of shipping code), then EditMode-test them.** Targets: `PrisonTimeManager` progress/phase math, `PrisonLocationRegistry` occupancy + stand-point switch, `MorningRollCallTracker` set logic, `GuardDetection.IsInSight`, `GuardShiftController.IsOnDutyFor`, `PrisonerController.IsZoneCompliantForEvent`, `PlayerWallet` clamp, interaction prompts + `InteractableScrew.UpdateUnscrewing` + `VentCover` threshold, and UI formatting/state seams.
2. **PlayMode integration tests.** Requires migrating game code into an `.asmdef` so a PlayMode test assembly can reference it (the one riskier infra step — done in isolation and verified). Then: schedule tick/phase transitions, door behavior live, guard/prisoner NavMesh behavior, shakedown coroutine, social manager flow, loot rolls, interaction side effects, UI widgets, player movement.

> **Note on assembly definitions:** the game currently has **no `.asmdef` files**; all gameplay code is in the predefined `Assembly-CSharp`. This is exactly why EditMode tests work with zero setup, and why PlayMode (which needs a referencing test assembly) is deferred to its own carefully validated migration.
