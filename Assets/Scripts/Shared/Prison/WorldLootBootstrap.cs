using System.Collections;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Creates layout-based <see cref="ItemSpawnNode"/> markers at runtime when the scene
    /// has too few (e.g. before Prison → Setup Items & World Loot has been saved).
    /// </summary>
    public static class WorldLootBootstrap
    {
        public static void EnsureSpawnNodes()
        {
            var oldRoot = GameObject.Find("WorldSpawns");
            if (oldRoot != null)
                Object.Destroy(oldRoot);

            var common = Resources.Load<LootTable>("LootTables/LootTable_Prison_Common");
            var yard = Resources.Load<LootTable>("LootTables/LootTable_Yard");
            var kitchen = Resources.Load<LootTable>("LootTables/LootTable_Kitchen");
            var workshop = Resources.Load<LootTable>("LootTables/LootTable_Workshop");
            var laundry = Resources.Load<LootTable>("LootTables/LootTable_Laundry");
            var rare = Resources.Load<LootTable>("LootTables/LootTable_RareHidden");

            if (common == null || yard == null)
            {
                Debug.LogWarning("[WorldLootBootstrap] Loot tables missing under Resources/LootTables — world spawns skipped.");
                return;
            }

            var root = new GameObject("WorldSpawns");

            Vector3 hub = PrisonLayoutAnchors.MainCorridorCenter;
            Vector3 connector = PrisonLayoutAnchors.CellSouthConnectorCenter;
            Vector3 cells = PrisonLayoutAnchors.CellBlockCenter;
            Vector3 yardC = PrisonLayoutAnchors.YardCenter;
            Vector3 workshopC = PrisonLayoutAnchors.WorkshopCenter;
            Vector3 laundryC = PrisonLayoutAnchors.LaundryCenter;
            Vector3 cafeteria = PrisonLayoutAnchors.CafeteriaCenter;
            Vector3 servingCounter = new Vector3(-16f, 1.3f, -71f);
            LootTable kitchenTable = kitchen ?? common;

            var corridor = CreateGroup(root.transform, "Spawn_Corridor");
            FillGrid(corridor, hub, 6, 4, 9f, 7f, common, 0.98f, "C");
            FillGrid(corridor, connector, 5, 3, 8f, 6f, common, 0.97f, "CC");
            Place(corridor, "C_Hub", hub, common, 0.99f);
            Place(corridor, "C_CafJoin", PrisonLayoutAnchors.CafeteriaConnectorCenter, common, 0.98f);
            // Guaranteed cluster outside the cell block so Play starts with visible pickups nearby.
            Place(corridor, "C_NearCellsA", connector + new Vector3(-2f, 0f, 2f), yard, 1f);
            Place(corridor, "C_NearCellsB", connector + new Vector3(2f, 0f, 2f), kitchenTable, 1f);
            Place(corridor, "C_NearCellsC", connector + new Vector3(0f, 0f, 4f), workshop ?? common, 1f);

            var cellsRoot = CreateGroup(root.transform, "Spawn_Cells");
            FillGrid(cellsRoot, cells, 5, 4, 10f, 8f, common, 0.98f, "L");
            FillGrid(cellsRoot, connector, 4, 3, 7f, 5f, common, 0.97f, "LC");
            Place(cellsRoot, "L_RareA", cells + new Vector3(-20f, 0f, 8f), rare ?? common, 0.85f);
            Place(cellsRoot, "L_RareB", cells + new Vector3(20f, 0f, -8f), rare ?? common, 0.82f);

            var cafeteriaRoot = CreateGroup(root.transform, "Spawn_Cafeteria");
            FillGrid(cafeteriaRoot, servingCounter, 5, 4, 6f, 5f, kitchenTable, 0.96f, "F");
            FillGrid(cafeteriaRoot, cafeteria, 4, 3, 7f, 6f, common, 0.95f, "FC");

            var yardRoot = CreateGroup(root.transform, "Spawn_Yard");
            FillGrid(yardRoot, yardC, 6, 5, 11f, 9f, yard, 0.96f, "Y");
            Place(yardRoot, "Y_RollCall", PrisonLayoutAnchors.RollCallCenter, yard, 0.94f);
            Place(yardRoot, "Y_Rare", yardC + new Vector3(-28f, 0f, -14f), rare ?? common, 0.82f);

            if (workshop != null)
            {
                var workshopRoot = CreateGroup(root.transform, "Spawn_Workshop");
                FillGrid(workshopRoot, workshopC, 4, 4, 5f, 5f, workshop, 0.96f, "W");
            }

            if (laundry != null)
            {
                var laundryRoot = CreateGroup(root.transform, "Spawn_Laundry");
                FillGrid(laundryRoot, laundryC, 4, 3, 5f, 5f, laundry, 0.95f, "LA");
            }

            var kitchenRoot = CreateGroup(root.transform, "Spawn_Kitchen");
            FillGrid(kitchenRoot, cafeteria, 4, 3, 6f, 5f, kitchenTable, 0.94f, "K");

            if (rare != null)
            {
                var hiddenRoot = CreateGroup(root.transform, "Spawn_Hidden");
                Place(hiddenRoot, "H01", hub + new Vector3(0f, 0f, -6f), rare, 0.88f);
                Place(hiddenRoot, "H02", workshopC + new Vector3(7f, 0f, 5f), rare, 0.85f);
                Place(hiddenRoot, "H03", laundryC + new Vector3(-6f, 0f, 5f), rare, 0.85f);
                Place(hiddenRoot, "H04", cafeteria + new Vector3(8f, 0f, -4f), rare, 0.82f);
                Place(hiddenRoot, "H05", yardC + new Vector3(18f, 0f, 14f), rare, 0.80f);
                Place(hiddenRoot, "H06", PrisonLayoutAnchors.CafeteriaConnectorCenter + new Vector3(4f, 0f, -8f), rare, 0.78f);
            }

            int placed = root.GetComponentsInChildren<ItemSpawnNode>(true).Length;
            Debug.Log($"[WorldLootBootstrap] Created {placed} world loot spawn nodes.");
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void FillGrid(Transform parent, Vector3 center, int cols, int rows, float spacingX, float spacingZ,
            LootTable table, float chance, string prefix)
        {
            float startX = -(cols - 1) * spacingX * 0.5f;
            float startZ = -(rows - 1) * spacingZ * 0.5f;
            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Vector3 pos = center + new Vector3(startX + c * spacingX, 0f, startZ + r * spacingZ);
                    Place(parent, $"{prefix}{index:D2}", pos, table, chance);
                    index++;
                }
            }
        }

        private static void Place(Transform parent, string name, Vector3 worldPos, LootTable table, float chance)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, true);
            // Keep markers on the walkable floor plane — Y=0 grids bury under slabs after snap.
            worldPos.y = PrisonLayoutAnchors.FloorY;
            go.transform.position = worldPos;
            var node = go.AddComponent<ItemSpawnNode>();
            node.lootTable = table;
            node.spawnChance = chance;
            node.spawnRolls = 2;
        }
    }
}
