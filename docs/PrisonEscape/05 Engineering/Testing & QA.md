# Testing & QA

All automated tests are **EditMode NUnit** tests in `Assets/Tests/Editor/`, namespace `Prison.Tests`. No `.asmdef` files exist — gameplay code lives in the default `Assembly-CSharp`, which is why EditMode tests need zero setup (and why PlayMode tests are deferred until an asmdef migration).

**Run them:** Unity → Window → General → Test Runner → EditMode → Run All. Pure logic, completes in under a second.

**Last known result: 136 passed / 0 failed.**

## Test suites

| Test file | Covers |
|---|---|
| `CellDoorControllerTests.cs` | Door open/close phases, slide math, initialization, design contracts (25 tests) |
| `SocialMathTests.cs` | Affinity model: base deltas, gift doubling/penalty, soft cap, clamps, tiers, averages |
| `PrisonRulesAndLabelsTests.cs` | Event rules/extensions, routine labels, zone labels, cell radius, favor validity, enum pinning |
| `CraftingInventoryLootTests.cs` | Inventory stacking, crafting, recipe descriptions, loot weights, confiscation rules, security alerts |

## Coverage status (summary)

**✅ Fully tested:** cell doors, social math, inventory core, crafting, security alerts, event rules, labels, favor validity, contraband classification, loot rarity weights.

**🧪 Testable after extracting pure helpers:** time-manager progress math, registry occupancy, roll-call tracker, guard detection geometry, guard shifts, prisoner compliance matrix, wallet clamp, unscrew math, UI formatting.

**🎬 Needs PlayMode (blocked on asmdef migration):** schedule tick, live doors, NavMesh AI behavior, shakedown coroutine, social manager flow, interaction side effects, UI widgets, player movement.

**⚪ Not scheduled:** networking (integration-only), editor tooling (validated visually).

## Testing rules for new features

1. Any new **pure logic** (math, state machines, classification) gets an EditMode test in the same PR
2. Design specs should name their test targets up front (see [[Feature Spec Template]])
3. Keep pure logic separable from MonoBehaviour lifecycles so it stays EditMode-testable
4. The escape-completion system ([[Roadmap & Priorities]]) is explicitly designed to be testable: objective state machine in EditMode, trigger flow in PlayMode

Technical companion doc: `Assets/Docs/Game_Features_And_Test_Coverage.md` (per-system test detail; must not contradict this vault).
