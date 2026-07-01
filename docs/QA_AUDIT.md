# QA & Testing Audit

This document is the quality-assurance capstone for the feature-by-feature history seeding
of **Prison Escape**. It summarizes what is automatically tested, how CI guards it, and the
known coverage gaps.

## Automated test suite (EditMode)

The project ships **~136 EditMode unit tests** (1,025 lines) under `Assets/Tests/Editor`:

| Test file | System under test | Focus |
| --- | --- | --- |
| `CellDoorControllerTests.cs` | `CellDoorController` (PR #22) | Open/close offset math, slide lerp, state transitions (25 tests) |
| `SocialMathTests.cs` | `SocialMath` (PR #13) | Affinity deltas, positive-only soft cap, clamping, reputation tiers |
| `PrisonRulesAndLabelsTests.cs` | `PrisonEventRules` / `PrisonRoutineLabels` (PR #3) | Schedule-phase rules and label mapping |
| `CraftingInventoryLootTests.cs` | `CraftingSystem`, `PlayerInventory`, `LootTable`, security sweeps (PR #9/#11/#12/#18) | Recipe matching, inventory ops, weighted loot, shakedown detection |

The suite deliberately targets **pure, stateless seams** (static math + data rules), which is
why the highest-blast-radius systems in the codebase are also the best covered. This validates
the "pure logic + thin MonoBehaviour" architecture used throughout the stack.

### Why the tests live in `Assembly-CSharp` (no `.asmdef`)

`NetworkManager` (PR #23) is an in-project `Assembly-CSharp` type. Introducing test `.asmdef`
files would force an assembly migration that ripples through the multiplayer netcode. To keep
the risk low and the tests fast to author, the suite stays in the default assembly. Migrating
to dedicated assemblies is a prerequisite for PlayMode/integration tests (see gaps below).

## Continuous integration

`ci/test-suite-audit.sh` is a **license-free** guard that:

1. Asserts the four required EditMode test files are present.
2. Counts NUnit attributes (`[Test]`/`[TestCase]`/`[TestCaseSource]`/`[UnityTest]`) and fails
   if the total regresses below 100 (override with `MIN_TESTS`).

Run it locally with `bash ci/test-suite-audit.sh`. This catches accidental deletion or
gutting of the test suite without requiring a Unity license.

### Wiring it into GitHub Actions

Add the following workflow (requires a token/actor with the `workflow` scope to push files
under `.github/workflows/`):

```yaml
name: QA - EditMode Test Suite Audit
on:
  push: { branches: [ main ] }
  pull_request:
jobs:
  audit:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with: { lfs: false }
      - run: bash ci/test-suite-audit.sh
```

### Enabling full Unity test execution (optional)

To actually execute the tests in CI, add a [GameCI](https://game.ci) job using
`game-ci/unity-test-runner` with `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD` repo
secrets. That job can run alongside the audit above.

## Known coverage gaps (tracked, not defects)

- **No escape-completion / win-state.** The game's stated main goal is to escape, but no
  win-condition system exists yet (see `docs/ARCHITECTURE_NOTES.md` and
  `Assets/Docs/Game_Features_And_Test_Coverage.md`). This is the top recommended next feature.
- **No PlayMode / integration tests.** Requires the `.asmdef` migration above.
- **No shared `IEscapeRoute` abstraction.** The escape props (stash #19, vent #20, fake-bed
  #21, doors #22) each reimplement interaction; a common contract would make them testable as
  a group.
- **Multiplayer netcode is unit-test-light**, as it depends on runtime transport/session state.
