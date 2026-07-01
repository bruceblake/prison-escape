#!/usr/bin/env bash
# License-free QA guard for the EditMode test suite.
#
# Verifies the required EditMode test files exist and that the discovered NUnit test
# count has not regressed below a floor. Runs anywhere (locally or in CI) without a
# Unity license. Wire it into CI with a minimal workflow (see docs/QA_AUDIT.md), e.g.:
#
#   - uses: actions/checkout@v4
#     with: { lfs: false }
#   - run: bash ci/test-suite-audit.sh
#
set -euo pipefail

MIN_TESTS="${MIN_TESTS:-100}"

required=(
  "Assets/Tests/Editor/CellDoorControllerTests.cs"
  "Assets/Tests/Editor/SocialMathTests.cs"
  "Assets/Tests/Editor/PrisonRulesAndLabelsTests.cs"
  "Assets/Tests/Editor/CraftingInventoryLootTests.cs"
)

missing=0
for f in "${required[@]}"; do
  if [ ! -f "$f" ]; then
    echo "ERROR: missing required test file: $f" >&2
    missing=1
  else
    echo "found: $f"
  fi
done
[ "$missing" -eq 0 ] || { echo "ERROR: one or more required test files are missing" >&2; exit 1; }

count=$(grep -rhoE "\[(Test|TestCase|TestCaseSource|UnityTest)" Assets/Tests | wc -l | tr -d ' ')
echo "Discovered $count NUnit test attributes across Assets/Tests"

if [ "$count" -lt "$MIN_TESTS" ]; then
  echo "ERROR: test count $count fell below the expected floor of $MIN_TESTS" >&2
  exit 1
fi

echo "QA audit passed: $count tests (floor $MIN_TESTS)."
