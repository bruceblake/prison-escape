using System;
using NUnit.Framework;
using UnityEngine;
using Prison;

namespace Prison.Tests
{
    /// <summary>
    /// Strict EditMode unit tests for <see cref="CellDoorController"/> — the cell-door
    /// open/close feature. Covers every schedule phase, the open/closed position math,
    /// initialization, the deterministic slide step, and design contracts.
    /// </summary>
    public class CellDoorControllerTests
    {
        private GameObject _go;
        private CellDoorController _door;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestDoor");
            _door = _go.AddComponent<CellDoorController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        private static void AssertVec(Vector3 expected, Vector3 actual, float tol = 1e-4f)
        {
            Assert.Less(Vector3.Distance(expected, actual), tol,
                $"Expected {expected} but was {actual}");
        }

        // ---------------------------------------------------------------
        // IsOpenPhase — open phases (movement blocks only)
        // ---------------------------------------------------------------
        [TestCase(PrisonEventType.Breakfast)]
        [TestCase(PrisonEventType.Lunch)]
        [TestCase(PrisonEventType.Dinner)]
        [TestCase(PrisonEventType.FreeTime)]
        [TestCase(PrisonEventType.WorkProgram)]
        public void IsOpenPhase_OpenPhases_ReturnTrue(PrisonEventType evt)
        {
            Assert.IsTrue(CellDoorController.IsOpenPhase(evt), $"{evt} should be an OPEN phase");
        }

        // ---------------------------------------------------------------
        // IsOpenPhase — closed phases (night + cell counts)
        // ---------------------------------------------------------------
        [TestCase(PrisonEventType.LightsOut)]
        [TestCase(PrisonEventType.NightRollCall)]
        [TestCase(PrisonEventType.MorningRollCall)]
        [TestCase(PrisonEventType.RollCall)]
        [TestCase(PrisonEventType.MiddayCount)]
        [TestCase(PrisonEventType.EveningCount)]
        public void IsOpenPhase_ClosedPhases_ReturnFalse(PrisonEventType evt)
        {
            Assert.IsFalse(CellDoorController.IsOpenPhase(evt), $"{evt} should be a CLOSED phase");
        }

        [Test]
        public void IsOpenPhase_CoversEveryEnumValue_NoUnhandledPhase()
        {
            foreach (PrisonEventType evt in Enum.GetValues(typeof(PrisonEventType)))
            {
                bool expectedOpen =
                    evt == PrisonEventType.Breakfast ||
                    evt == PrisonEventType.Lunch ||
                    evt == PrisonEventType.Dinner ||
                    evt == PrisonEventType.FreeTime ||
                    evt == PrisonEventType.WorkProgram;

                Assert.AreEqual(expectedOpen, CellDoorController.IsOpenPhase(evt),
                    $"IsOpenPhase mismatch for {evt}");
            }
        }

        [Test]
        public void IsOpenPhase_NightPhases_AreAlwaysClosed()
        {
            Assert.IsFalse(CellDoorController.IsOpenPhase(PrisonEventType.LightsOut));
            Assert.IsFalse(CellDoorController.IsOpenPhase(PrisonEventType.NightRollCall));
        }

        [Test]
        public void IsOpenPhase_MorningRollCall_StaysClosedUntilBreakfast()
        {
            Assert.IsFalse(CellDoorController.IsOpenPhase(PrisonEventType.MorningRollCall));
            Assert.IsTrue(CellDoorController.IsOpenPhase(PrisonEventType.Breakfast));
        }

        // ---------------------------------------------------------------
        // Open / closed position math
        // ---------------------------------------------------------------
        [Test]
        public void OpenLocalPosition_EqualsClosedPlusOffset()
        {
            _door.closedLocalPosition = new Vector3(16.9f, 0f, 2.04f);
            _door.openOffset = new Vector3(0f, 0f, 1.35f);
            AssertVec(new Vector3(16.9f, 0f, 3.39f), _door.OpenLocalPosition);
        }

        [Test]
        public void GetTargetLocalPosition_OpenPhase_ReturnsOpenPosition()
        {
            _door.closedLocalPosition = new Vector3(1f, 2f, 3f);
            _door.openOffset = new Vector3(0f, 0f, 1.35f);
            AssertVec(_door.OpenLocalPosition, _door.GetTargetLocalPosition(PrisonEventType.Lunch));
        }

        [Test]
        public void GetTargetLocalPosition_ClosedPhase_ReturnsClosedPosition()
        {
            _door.closedLocalPosition = new Vector3(1f, 2f, 3f);
            _door.openOffset = new Vector3(0f, 0f, 1.35f);
            AssertVec(_door.closedLocalPosition, _door.GetTargetLocalPosition(PrisonEventType.LightsOut));
        }

        [Test]
        public void GetTargetLocalPosition_AllPhases_MatchIsOpenPhase()
        {
            _door.closedLocalPosition = new Vector3(5f, 0f, 1f);
            _door.openOffset = new Vector3(0f, 0f, 1.35f);
            foreach (PrisonEventType evt in Enum.GetValues(typeof(PrisonEventType)))
            {
                Vector3 expected = CellDoorController.IsOpenPhase(evt)
                    ? _door.OpenLocalPosition
                    : _door.closedLocalPosition;
                AssertVec(expected, _door.GetTargetLocalPosition(evt));
            }
        }

        [Test]
        public void ZeroOffset_OpenEqualsClosed()
        {
            _door.closedLocalPosition = new Vector3(2f, 3f, 4f);
            _door.openOffset = Vector3.zero;
            AssertVec(_door.closedLocalPosition, _door.OpenLocalPosition);
            AssertVec(_door.closedLocalPosition, _door.GetTargetLocalPosition(PrisonEventType.FreeTime));
        }

        [Test]
        public void NegativeOffset_OpenPositionRespectsSign()
        {
            _door.closedLocalPosition = new Vector3(0f, 0f, 2f);
            _door.openOffset = new Vector3(0f, 0f, -5f);
            AssertVec(new Vector3(0f, 0f, -3f), _door.OpenLocalPosition);
        }

        // ---------------------------------------------------------------
        // Initialization
        // ---------------------------------------------------------------
        [Test]
        public void IsInitialized_FalseBeforeInitialize()
        {
            Assert.IsFalse(_door.IsInitialized);
        }

        [Test]
        public void InitializeClosedPosition_CapturesTransformLocalPosition()
        {
            _go.transform.localPosition = new Vector3(16.9f, 0f, 2.04f);
            _door.InitializeClosedPosition();
            Assert.IsTrue(_door.IsInitialized);
            AssertVec(new Vector3(16.9f, 0f, 2.04f), _door.closedLocalPosition);
        }

        [Test]
        public void InitializeClosedPosition_MarksAuthoredClosed_SoLaterPoseDoesNotReplaceIt()
        {
            _go.transform.localPosition = new Vector3(10f, 0f, 20f);
            _door.InitializeClosedPosition();
            AssertVec(new Vector3(10f, 0f, 20f), _door.closedLocalPosition);

            // Simulate a door left slid open in the scene (the bug that blocked cell exits).
            _go.transform.localPosition = new Vector3(10f, 0f, 21.35f);
            // Start path: when authored closed exists, closed must stay at the captured pose.
            var so = new UnityEditor.SerializedObject(_door);
            var authored = so.FindProperty("hasAuthoredClosedPosition");
            Assert.IsNotNull(authored);
            Assert.IsTrue(authored.boolValue);
            AssertVec(new Vector3(10f, 0f, 20f), _door.closedLocalPosition);
            AssertVec(new Vector3(10f, 0f, 20f) + _door.openOffset, _door.OpenLocalPosition);
        }

        // ---------------------------------------------------------------
        // StepToward — deterministic slide behaviour
        // ---------------------------------------------------------------
        [Test]
        public void StepToward_MovesPartwayTowardTarget()
        {
            Vector3 next = CellDoorController.StepToward(
                Vector3.zero, new Vector3(0f, 0f, 6f), 3.0f, 0.1f); // t = 0.3
            AssertVec(new Vector3(0f, 0f, 1.8f), next);
        }

        [Test]
        public void StepToward_LargeStep_ClampsToTarget_NoOvershoot()
        {
            Vector3 target = new Vector3(0f, 0f, 6f);
            Vector3 next = CellDoorController.StepToward(Vector3.zero, target, 100f, 1f);
            AssertVec(target, next);
        }

        [Test]
        public void StepToward_ZeroDelta_DoesNotMove()
        {
            Vector3 start = new Vector3(1f, 1f, 1f);
            AssertVec(start, CellDoorController.StepToward(start, new Vector3(9f, 9f, 9f), 5f, 0f));
        }

        [Test]
        public void StepToward_ConvergesToTarget_OverManySteps()
        {
            Vector3 pos = Vector3.zero;
            Vector3 target = new Vector3(0f, 0f, 6f);
            for (int i = 0; i < 1000; i++)
                pos = CellDoorController.StepToward(pos, target, 3.0f, 0.05f);
            Assert.Less(Vector3.Distance(pos, target), 1e-3f);
        }

        [Test]
        public void StepToward_FromOpenToClosed_ReturnsTowardClosed()
        {
            Vector3 open = new Vector3(0f, 0f, 8.04f);
            Vector3 closed = new Vector3(0f, 0f, 2.04f);
            Vector3 next = CellDoorController.StepToward(open, closed, 3.0f, 0.1f);
            // Should move from 8.04 toward 2.04 (smaller z), but not past it.
            Assert.Less(next.z, open.z);
            Assert.Greater(next.z, closed.z);
        }

        // ---------------------------------------------------------------
        // Design contract: default open slide must clear a standard doorway (~2 m)
        // without sliding into the neighboring cell (~4 m cell pitch).
        // ---------------------------------------------------------------
        [Test]
        public void DefaultOpenOffset_ClearsStandardDoorwayWidth()
        {
            const float minClearance = 1.2f;
            const float maxClearance = 1.7f;
            var fresh = new GameObject("freshDoor").AddComponent<CellDoorController>();
            try
            {
                Assert.GreaterOrEqual(fresh.openOffset.magnitude, minClearance,
                    "Default open offset must clear the doorway opening.");
                Assert.LessOrEqual(fresh.openOffset.magnitude, maxClearance,
                    "Default open offset must not slide into the neighboring cell doorway.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fresh.gameObject);
            }
        }

        [Test]
        public void DefaultSlideSpeed_IsPositive()
        {
            var fresh = new GameObject("freshDoor").AddComponent<CellDoorController>();
            try
            {
                Assert.Greater(fresh.slideSpeed, 0f, "Slide speed must be positive or the door never moves.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fresh.gameObject);
            }
        }

        [Test]
        public void ComputeDoorOpenOffsetLocal_IsHorizontalLocalAxis()
        {
            var doorGo = new GameObject("door");
            doorGo.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            var bedGo = new GameObject("bed");
            bedGo.transform.position = doorGo.transform.position + doorGo.transform.forward * 3f;

            Vector3 offset = PrisonFacilityInstaller.ComputeDoorOpenOffsetLocal(doorGo.transform, bedGo.transform);

            Assert.AreEqual(0f, offset.y, 1e-4f, "Door slide offset must stay horizontal in local space.");
            Assert.GreaterOrEqual(offset.magnitude, 1.2f, "Door must slide far enough to clear the doorway.");
            Assert.LessOrEqual(offset.magnitude, 1.7f, "Door must not slide into the neighboring cell.");
            float dominant = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.z));
            Assert.AreEqual(offset.magnitude, dominant, 1e-4f, "Slide must be along a single local horizontal axis.");
        }

        [Test]
        public void AlignDoorToCellWall_PlacesDoorOnCorridorFace()
        {
            var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shell.name = "Cell_09";
            shell.transform.position = new Vector3(-55f, 3.82f, -90.3f);
            shell.transform.localScale = new Vector3(4f, 6f, 5.6f);

            var bed = new GameObject("Cell_09_Bed");
            bed.transform.position = new Vector3(-51.6f, 0.82f, -88.7f);

            var door = new GameObject("Cell_09_Door");
            door.transform.position = new Vector3(-61.73f, 0.82f, -94.04f);

            try
            {
                PrisonFacilityInstaller.AlignDoorToCellWall(door.transform, shell.transform, bed.transform);

                float distToSouthWall = Mathf.Abs(door.transform.position.z - (-93.1f));
                Assert.Less(distToSouthWall, 1.0f, "Door should sit on the corridor-facing south wall, not float in the hallway.");
                Assert.Less(Mathf.Abs(door.transform.position.x - (-55f)), 1.5f, "Door should be centered on the cell opening in X (shell center).");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(door);
                UnityEngine.Object.DestroyImmediate(bed);
                UnityEngine.Object.DestroyImmediate(shell);
            }
        }
    }
}
