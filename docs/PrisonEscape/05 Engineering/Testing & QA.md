# Testing & QA

All automated tests are **EditMode NUnit** tests in `Assets/Tests/Editor/`, namespace `Prison.Tests`. No `.asmdef` files exist — gameplay code lives in the default `Assembly-CSharp`, which is why EditMode tests need zero setup (and why PlayMode tests are deferred until an asmdef migration).

**Run them:** Unity → Window → General → Test Runner → EditMode → Run All. Pure logic, completes in under a second.

**Menu runners:**
- **Tools → Prison → Career → Run Career EditMode Tests** (`CareerTestRunner`)
- **Tools → Prison → Social → Run All EditMode Tests** (`SocialTestRunner` — full EditMode suite)

**Inventory on tip:** **12** suites · **216** `[Test]` methods · **61** `[TestCase]` expansions ≈ **277** runnable cases. Re-run Test Runner for the current pass/fail count (do not treat an old “146 passed” line as live).

## Test suites

| Test file | Covers |
|---|---|
| `CellDoorControllerTests.cs` | Door open/close phases, slide math, initialization, design contracts (~21 `[Test]` + cases) |
| `PrisonRulesAndLabelsTests.cs` | Event rules/extensions, routine labels, zone labels, cell radius, enum pinning |
| `CraftingInventoryLootTests.cs` | Inventory stacking, crafting, recipe descriptions, loot weights, confiscation, security alerts |
| `EscapeSystemTests.cs` | Escape boundary, restricted zones, solitary flow, stats/suspicion math |
| `PrisonLayoutValidationTests.cs` | Layout anchors, room bounds, spec alignment (`PrisonLayoutSpec` / `PrisonLayoutAnchors`) |
| `SocialRelationshipTests.cs` | `RelationshipMath`: deltas, soft cap, standing bands, tiers |
| `SocialMemoryGossipTests.cs` | Memory decay/eviction, gossip hop/weight |
| `SocialRosterSnitchTests.cs` | Roster/identity helpers, snitch propensity gates |
| `SocialGangTradeTests.cs` | Gang membership/propagation, trade price math |
| `CareerWorldStoreTests.cs` | World JSON IO, migration, New/Load/Delete |
| `CareerTransferTests.cs` | Transfer / gates / sentence / ceremony math |
| `GuardDetectionPerfTests.cs` | `PrisonerRegistry` membership, guard scan throttle / cache invalidation (registers by hand — `OnEnable` wiring is uncovered) |

> **Deleted:** `SocialMathTests.cs` (v1 affinity). Do not resurrect.

## Coverage status (summary)

**✅ Fully tested (EditMode):** cell doors, relationship/standing/gang/trade/roster/snitch math, career world store + transfer math, inventory core, crafting, security alerts, event rules, labels, loot rarity weights, escape/solitary/suspicion math, layout validation.

**🧪 Testable after extracting pure helpers:** time-manager progress math, registry occupancy, roll-call tracker, guard detection geometry, guard shifts, prisoner compliance matrix, wallet clamp, unscrew math, UI formatting.

**🎬 Needs PlayMode (blocked on asmdef migration):** schedule tick, live doors, NavMesh AI, shakedown coroutine, **`SocialWorld` MonoBehaviour orchestration**, Talk/Dossier widgets, interaction side effects, player movement, career hub UI flow.

**⚪ Not scheduled:** networking (integration-only), editor tooling (validated visually / batch logs).

## Testing rules for new features

1. Any new **pure logic** (math, state machines, classification) gets an EditMode test in the same PR
2. Design specs should name their test targets up front (see [[Feature Spec Template]])
3. Keep pure logic separable from MonoBehaviour lifecycles so it stays EditMode-testable
4. Career and Social pure seams already follow this pattern — prefer extending those suites over new MonoBehaviour-only paths

Technical companion doc: `Assets/Docs/Game_Features_And_Test_Coverage.md` (per-system test detail; must not contradict this vault — companion may still mention deleted v1 social until updated).

Related: [[Codebase Map]] · [[Social & Reputation]] · [[Prison Career Ladder]]
