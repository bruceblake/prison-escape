using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Prison.Tests
{
    /// <summary>
    /// EditMode tests for prison layout spec, anchors, and scene validation.
    /// Scene tests load PrisonLevel1 when present.
    /// </summary>
    public class PrisonLayoutValidationTests
    {
        // ---------------------------------------------------------------
        // Tier A — pure spec (no scene load)
        // ---------------------------------------------------------------

        [Test]
        public void Spec_ExpectedCellCount_IsEight()
        {
            Assert.AreEqual(8, PrisonLayoutSpec.ExpectedCellCount);
        }

        [Test]
        public void Spec_WallHeight_MatchesAnchors()
        {
            Assert.AreEqual(PrisonLayoutSpec.StandardWallHeight, PrisonLayoutAnchors.WallHeight);
        }

        [Test]
        public void Spec_FloorY_MatchesAnchors()
        {
            Assert.AreEqual(PrisonLayoutSpec.FloorY, PrisonLayoutAnchors.FloorY, 1e-4f);
        }

        [Test]
        public void Spec_InnerWalls_BoundCellBlock()
        {
            Assert.Less(PrisonLayoutAnchors.InnerLeftWallX, PrisonLayoutAnchors.InnerRightWallX);
            Assert.Less(PrisonLayoutAnchors.InnerLeftWallX, PrisonLayoutAnchors.CellBlockCenter.x);
            Assert.Greater(PrisonLayoutAnchors.InnerRightWallX, PrisonLayoutAnchors.CellBlockCenter.x);
        }

        [Test]
        public void Spec_WingCenters_AreOutsideCellBlock()
        {
            Bounds cellBounds = PrisonLayoutSpec.CellBlockBounds;
            Assert.IsFalse(cellBounds.Contains(PrisonLayoutAnchors.WorkshopCenter));
            Assert.IsFalse(cellBounds.Contains(PrisonLayoutAnchors.LaundryCenter));
        }

        [Test]
        public void Spec_Yard_IsNorthOfCellBlock()
        {
            Assert.Greater(PrisonLayoutAnchors.YardCenter.z, PrisonLayoutAnchors.CellBlockCenter.z);
            Assert.Greater(PrisonLayoutAnchors.YardGateCenter.z, PrisonLayoutAnchors.MainCorridorCenter.z);
        }

        [Test]
        public void Spec_CorridorWidth_IsWalkable()
        {
            Assert.GreaterOrEqual(PrisonLayoutSpec.MinCorridorWidth, 2f);
        }

        [TestCase("LeftPrisonWall_Segment", true)]
        [TestCase("RightPrisonWall_01", true)]
        [TestCase("JailCells", false)]
        [TestCase("Cell_0", false)]
        public void Spec_LegacyPerimeterNameDetection(string name, bool expected)
        {
            Assert.AreEqual(expected, PrisonLayoutSpec.IsLegacyPerimeterName(name));
        }

        [Test]
        public void Spec_SpawnFloorBand_IsBelowRoofReject()
        {
            Assert.Less(PrisonLayoutSpec.SpawnFloorMaxY, PrisonLayoutSpec.RoofRaycastRejectMinY);
        }

        [Test]
        public void Spec_RequiredZoneNames_AreNonEmpty()
        {
            foreach (string zoneName in PrisonLayoutSpec.RequiredZoneObjectNames)
                Assert.IsFalse(string.IsNullOrWhiteSpace(zoneName));
        }

        // ---------------------------------------------------------------
        // Tier A — validator report structure
        // ---------------------------------------------------------------

        [Test]
        public void Validator_EmptyIssueList_Passes()
        {
            var report = new PrisonLayoutValidator.ValidationReport
            {
                Issues = new List<PrisonLayoutValidator.ValidationIssue>()
            };
            Assert.IsTrue(report.Passed);
            Assert.AreEqual(0, report.ErrorCount);
        }

        [Test]
        public void Validator_ErrorIssue_FailsReport()
        {
            var issues = new List<PrisonLayoutValidator.ValidationIssue>();
            PrisonLayoutValidator.ValidateSpecConstants(issues);
            var report = new PrisonLayoutValidator.ValidationReport { Issues = issues };
            Assert.IsTrue(report.Passed, "Spec constants should pass with current anchors.");
        }

        // ---------------------------------------------------------------
        // Tier B — scene validation (requires PrisonLevel1 loaded)
        // ---------------------------------------------------------------

        [Test]
        [Category("Scene")]
        public void Scene_PrisonLevel1_JailCellsExist()
        {
            if (!TryLoadPrisonLevel1()) return;

            var jailCells = GameObject.Find(PrisonLayoutSpec.JailCellsRootName);
            Assert.IsNotNull(jailCells, "JailCells root missing from PrisonLevel1.");
        }

        [Test]
        [Category("Scene")]
        public void Scene_PrisonLevel1_ValidationReport_HasNoErrors()
        {
            if (!TryLoadPrisonLevel1()) return;

            PrisonLayoutValidator.ValidationReport report = PrisonLayoutValidator.ValidateScene();
            foreach (PrisonLayoutValidator.ValidationIssue issue in report.Issues)
            {
                if (issue.IsError)
                    Assert.Fail($"[{issue.Code}] {issue.Message}");
            }
        }

        [Test]
        [Category("Scene")]
        public void Scene_PrisonLevel1_AllSpawnsHitFloorNotRoof()
        {
            if (!TryLoadPrisonLevel1()) return;

            var jailCells = GameObject.Find(PrisonLayoutSpec.JailCellsRootName);
            Assert.IsNotNull(jailCells);

            foreach (Transform cell in jailCells.transform)
            {
                Transform spawn = cell.Find("SpawnPoint");
                if (spawn == null) continue;

                bool ok = PrisonLayoutValidator.RaycastHitsFloorNotRoof(spawn.position, out RaycastHit hit);
                Assert.IsTrue(ok,
                    $"{cell.name}: expected floor raycast, got {(hit.collider != null ? hit.collider.name : "none")} @ y={hit.point.y:0.###}");
            }
        }

        private static bool TryLoadPrisonLevel1()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PrisonLevel1")
                return true;

            var scene = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(
                "Assets/Scenes/PrisonLevel1.unity");
            if (scene == null)
            {
                Assert.Ignore("PrisonLevel1 scene asset not found.");
                return false;
            }

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/PrisonLevel1.unity");
            return true;
        }
    }
}
