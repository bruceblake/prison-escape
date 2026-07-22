using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Prison;

namespace Prison.Tests
{
    /// <summary>
    /// EditMode tests for the guard-detection hot path: <see cref="PrisonerRegistry"/> replacing
    /// per-frame <c>FindObjectsByType</c> scans, and the per-guard scan throttle in
    /// <see cref="GuardDetection"/>.
    ///
    /// Note: MonoBehaviour OnEnable does not fire in EditMode, so these tests register with the
    /// registry explicitly. The OnEnable/OnDisable wiring itself is therefore NOT covered here,
    /// and has no automated coverage — PlayMode tests are blocked on the asmdef migration
    /// (see the vault's Testing &amp; QA note). Verify that wiring by playtest until then.
    /// </summary>
    public class GuardDetectionPerfTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            ClearRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            ClearRegistry();
        }

        private static void ClearRegistry()
        {
            foreach (var p in new List<PrisonerController>(PrisonerRegistry.Players))
                PrisonerRegistry.Unregister(p);
            foreach (var n in new List<PrisonerAI>(PrisonerRegistry.Npcs))
                PrisonerRegistry.Unregister(n);
        }

        private GameObject NewGo(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        /// <summary>Guard looking down +Z from the origin, with Awake's wiring done by hand.</summary>
        private GuardDetection NewGuard(float scanInterval)
        {
            var go = NewGo("Guard");
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var d = go.AddComponent<GuardDetection>();
            d.eyeTransform = go.transform;
            d.scanIntervalSeconds = scanInterval;
            return d;
        }

        private PrisonerController NewPlayerPrisoner(Vector3 pos)
        {
            var go = NewGo("PlayerPrisoner");
            go.transform.position = pos;
            var pc = go.AddComponent<PrisonerController>();
            PrisonerRegistry.Register(pc);
            return pc;
        }

        // ---------------------------------------------------------------
        // Registry
        // ---------------------------------------------------------------

        [Test]
        public void Registry_RegisterThenUnregister_TracksMembership()
        {
            var pc = NewPlayerPrisoner(new Vector3(0f, 0f, 3f));
            Assert.Contains(pc, new List<PrisonerController>(PrisonerRegistry.Players));

            PrisonerRegistry.Unregister(pc);
            CollectionAssert.DoesNotContain(new List<PrisonerController>(PrisonerRegistry.Players), pc);
        }

        [Test]
        public void Registry_RegisterTwice_DoesNotDuplicate()
        {
            var pc = NewPlayerPrisoner(new Vector3(0f, 0f, 3f));
            PrisonerRegistry.Register(pc);

            int count = 0;
            foreach (var p in PrisonerRegistry.Players)
            {
                if (p == pc) count++;
            }
            Assert.AreEqual(1, count, "Double registration must not duplicate the entry.");
        }

        // ---------------------------------------------------------------
        // Detection still works off the registry
        // ---------------------------------------------------------------

        [Test]
        public void Detection_NonCompliantPrisonerInFrontOfGuard_IsFound()
        {
            var guard = NewGuard(0f); // no throttle
            var pc = NewPlayerPrisoner(new Vector3(0f, 0f, 3f));

            Assert.IsFalse(pc.IsCompliant, "Fresh PrisonerController should start non-compliant.");
            Assert.AreSame(pc, guard.FindNonCompliantPrisoner());
        }

        [Test]
        public void Detection_PrisonerBeyondRange_IsNotFound()
        {
            var guard = NewGuard(0f);
            guard.detectionRange = 10f;
            guard.proximitySpotDistance = 6f;
            NewPlayerPrisoner(new Vector3(0f, 0f, 500f));

            Assert.IsNull(guard.FindNonCompliantPrisoner());
        }

        [Test]
        public void Detection_PrisonerBehindGuardButWithinProximity_IsFound()
        {
            var guard = NewGuard(0f);
            guard.coneAngle = 90f;
            guard.proximitySpotDistance = 6f;
            var pc = NewPlayerPrisoner(new Vector3(0f, 0f, -3f)); // behind, inside proximity

            Assert.AreSame(pc, guard.FindNonCompliantPrisoner(),
                "Proximity spot should catch a prisoner outside the cone.");
        }

        [Test]
        public void Detection_UnregisteredPrisoner_IsIgnored()
        {
            var guard = NewGuard(0f);
            var pc = NewPlayerPrisoner(new Vector3(0f, 0f, 3f));
            Assert.AreSame(pc, guard.FindNonCompliantPrisoner());

            // Simulates the prisoner being disabled/destroyed (OnDisable unregisters).
            PrisonerRegistry.Unregister(pc);
            Assert.IsNull(guard.FindNonCompliantPrisoner(),
                "Detection must read the registry, not the scene.");
        }

        // ---------------------------------------------------------------
        // Throttle
        // ---------------------------------------------------------------

        [Test]
        public void Throttle_SecondCallWithinInterval_ReusesCachedTarget()
        {
            // Time.time does not advance in EditMode, so any positive interval keeps the cache warm.
            var guard = NewGuard(0.2f);
            var pc = NewPlayerPrisoner(new Vector3(0f, 0f, 3f));

            Assert.AreSame(pc, guard.FindNonCompliantPrisoner(), "First call should scan.");

            // Move the prisoner far away; the throttled call should still return the cached hit.
            pc.transform.position = new Vector3(0f, 0f, 500f);
            Assert.AreSame(pc, guard.FindNonCompliantPrisoner(),
                "Within the interval the previous result is reused.");
        }

        [Test]
        public void Throttle_CachedTargetThatBecomesInvalid_IsDroppedImmediately()
        {
            var guard = NewGuard(0.2f);
            var pc = NewPlayerPrisoner(new Vector3(0f, 0f, 3f));
            Assert.AreSame(pc, guard.FindNonCompliantPrisoner());

            // Arrest freezes the prisoner - the guard must not keep re-acquiring them.
            pc.SetMovementBlocked(true);
            Assert.IsNull(guard.FindNonCompliantPrisoner(),
                "A cached target that is now movement-blocked must be dropped without waiting for the next scan.");
        }

        [Test]
        public void Throttle_InvalidateScanCache_ForcesFreshScan()
        {
            var guard = NewGuard(0.2f);
            var pc = NewPlayerPrisoner(new Vector3(0f, 0f, 3f));
            Assert.AreSame(pc, guard.FindNonCompliantPrisoner());

            pc.transform.position = new Vector3(0f, 0f, 500f);
            guard.InvalidateScanCache();

            Assert.IsNull(guard.FindNonCompliantPrisoner(),
                "InvalidateScanCache should force a real scan, which now sees the prisoner out of range.");
        }

        [Test]
        public void Throttle_ZeroInterval_ScansEveryCall()
        {
            var guard = NewGuard(0f);
            var pc = NewPlayerPrisoner(new Vector3(0f, 0f, 3f));
            Assert.AreSame(pc, guard.FindNonCompliantPrisoner());

            pc.transform.position = new Vector3(0f, 0f, 500f);
            Assert.IsNull(guard.FindNonCompliantPrisoner(),
                "With throttling off every call must re-scan.");
        }

        [Test]
        public void Throttle_DefaultIntervalIsResponsive()
        {
            var guard = NewGuard(0.2f);
            Assert.LessOrEqual(guard.scanIntervalSeconds, 0.25f,
                "Worst-case spotting latency should stay at or under a quarter second.");
        }
    }
}
