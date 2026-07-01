using UnityEngine;
using UnityEngine.AI;
using Prison;

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    [Header("World random")]
    [Tooltip("Drives UnityEngine.Random after InitState. If Use Random Seed is on, a new value is chosen each run.")]
    public string worldSeed;

    [Tooltip("If true, worldSeed is replaced with a random 0..999999 before InitState. If false, use worldSeed as entered (e.g. for reproducible runs).")]
    public bool useRandomSeed = true;

    [Header("Social system")]
    [Tooltip("Narc, Broker, etc. If empty, SocialManager still registers affinities with no personality data.")]
    public NPCPersonalityData[] availablePersonalities;

    [Header("Spawn Settings")]
    [Tooltip("Drag your Player Prefab here")]
    public GameObject playerPrefab;

    [Tooltip("Prefab for NPC prisoners")]
    public GameObject prisonerPrefab;

    [Tooltip("Prefab for guards")]
    public GameObject guardPrefab;

    [Header("Registry")]
    [Tooltip("Prison location registry (holds cells). Auto-found if null.")]
    public PrisonLocationRegistry locationRegistry;

    [Header("Prison")]
    [Tooltip("Cell index for the player (0 = first cell). Schedule cell IDs 101–108 map to indices 0–7 when firstScheduleCellId is 101.")]
    public int playerCellIndex;

    [Tooltip("When enabled, spawn one NPC in each free cell for IDs firstScheduleCellId…lastScheduleCellId (skips player cell and occupied cells).")]
    public bool useDynamicCellPopulation = true;

    [Tooltip("Inclusive schedule cell id for first cell slot (array index 0).")]
    public int firstScheduleCellId = 101;

    [Tooltip("Inclusive schedule cell id for last cell slot (array index = last - first).")]
    public int lastScheduleCellId = 108;

    [Tooltip("Legacy: when useDynamicCellPopulation is off, spawn this many NPCs into rotating cells.")]
    public int npcPrisonerCount;

    [Header("Guards")]
    [Tooltip("If this list has any entries, each entry spawns one guard with its own spawn point, role, waypoints, and shift. Same prefab can be used for every row — settings differ per instance.")]
    public GuardSpawnEntry[] guardSpawnTable;

    [Tooltip("Legacy: used only when guardSpawnTable is null or empty.")]
    public int guardCount;

    [Tooltip("Legacy: random spawn from these when guardSpawnTable is empty")]
    public Transform[] guardSpawnPoints;

    [Tooltip("Fallback: single spawn point for player if no registry")]
    public Transform cellSpawnPoint;

    void Awake()
    {
        if (useRandomSeed)
            worldSeed = UnityEngine.Random.Range(0, 1_000_000).ToString();
        else if (string.IsNullOrEmpty(worldSeed))
            worldSeed = "0";

        UnityEngine.Random.InitState(worldSeed.GetHashCode());
    }

    void Start()
    {
        if (locationRegistry == null)
            locationRegistry = FindFirstObjectByType<PrisonLocationRegistry>();

        SpawnPlayer();
        SpawnNpcPrisoners();
        SpawnGuards();
        PopulateWorldSpawns();
    }

    /// <summary>
    /// Spawns world pickups at each <see cref="ItemSpawnNode"/> that passes <see cref="ItemSpawnNode.spawnChance"/>.
    /// </summary>
    public void PopulateWorldSpawns()
    {
        ItemSpawnNode[] nodes = Object.FindObjectsByType<ItemSpawnNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int spawned = 0;
        int skipped = 0;

        for (int i = 0; i < nodes.Length; i++)
        {
            ItemSpawnNode node = nodes[i];
            if (node == null) continue;
            if (UnityEngine.Random.value > node.spawnChance)
            {
                skipped++;
                continue;
            }

            if (node.lootTable == null)
            {
                skipped++;
                continue;
            }

            ItemData pick = null;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                ItemData candidate = node.lootTable.GetRandomItem();
                if (candidate != null && candidate.worldPrefab != null)
                {
                    pick = candidate;
                    break;
                }
            }

            if (pick == null)
            {
                skipped++;
                continue;
            }

            Vector3 spawnPos = SpawnPlacementUtility.SnapPickupPosition(node.transform.position);
            GameObject instance = Object.Instantiate(pick.worldPrefab, spawnPos, node.transform.rotation);
            instance.transform.Rotate(0f, UnityEngine.Random.Range(0f, 360f), 0f, Space.World);

            WorldItemPickup pickup = instance.GetComponent<WorldItemPickup>();
            if (pickup == null)
                pickup = instance.AddComponent<WorldItemPickup>();
            pickup.itemData = pick;
            spawned++;
        }

        Debug.Log($"[GameManager] World spawns: {spawned} placed, {skipped} skipped ({nodes.Length} nodes).");
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null) return;

        Vector3 pos;
        Quaternion rot;

        if (locationRegistry != null && locationRegistry.CellCount > 0)
        {
            var cell = locationRegistry.GetCell(Mathf.Clamp(playerCellIndex, 0, locationRegistry.CellCount - 1));
            if (cell != null)
            {
                pos = cell.SpawnPosition;
                rot = cell.SpawnRotation;
            }
            else
            {
                pos = cellSpawnPoint != null ? cellSpawnPoint.position : Vector3.zero;
                rot = cellSpawnPoint != null ? cellSpawnPoint.rotation : Quaternion.identity;
            }
        }
        else if (cellSpawnPoint != null)
        {
            pos = cellSpawnPoint.position;
            rot = cellSpawnPoint.rotation;
        }
        else
        {
            Debug.LogWarning("Missing location registry or cell spawn point!");
            return;
        }

        // Cell spawn points are editor-placed on the floor. Do not raycast-snap — that hits the roof deck (~Y7.5).
        var player = Instantiate(playerPrefab, pos, rot);
        var playerAgent = player.GetComponent<NavMeshAgent>();
        if (playerAgent != null)
            SpawnPlacementUtility.WarpNavMeshAgent(playerAgent, pos);
        var prisonerCtrl = player.GetComponent<PrisonerController>();
        if (prisonerCtrl != null)
            prisonerCtrl.cellIndex = playerCellIndex;

        if (locationRegistry != null)
            locationRegistry.TryRegisterCellOccupant(playerCellIndex, player);

        Debug.Log($"Player spawned into cell at {pos}.");
    }

    void SpawnNpcPrisoners()
    {
        if (prisonerPrefab == null || locationRegistry == null || locationRegistry.CellCount <= 0) return;

        if (useDynamicCellPopulation)
        {
            if (lastScheduleCellId < firstScheduleCellId) return;

            for (int cellId = firstScheduleCellId; cellId <= lastScheduleCellId; cellId++)
            {
                int cellIdx = cellId - firstScheduleCellId;
                if (cellIdx < 0 || cellIdx >= locationRegistry.CellCount) continue;
                if (cellIdx == playerCellIndex) continue;
                if (locationRegistry.IsCellOccupied(cellIdx)) continue;

                var cell = locationRegistry.GetCell(cellIdx);
                if (cell == null) continue;

                Vector3 npcPos = cell.SpawnPosition;
                var prisoner = Instantiate(prisonerPrefab, npcPos, cell.SpawnRotation);
                var npcAgent = prisoner.GetComponent<NavMeshAgent>();
                if (npcAgent != null)
                    SpawnPlacementUtility.WarpNavMeshAgent(npcAgent, npcPos);
                var ai = prisoner.GetComponent<PrisonerAI>();
                if (ai != null)
                {
                    ai.cellIndex = cellIdx;
                    RegisterPrisonerSocial(prisoner, cellId, cellIdx, ai);
                }
            }
        }
        else
        {
            int spawnCount = Mathf.Min(npcPrisonerCount, Mathf.Max(0, locationRegistry.CellCount - 1));
            for (int i = 0; i < spawnCount; i++)
            {
                int cellIdx = (playerCellIndex + 1 + i) % locationRegistry.CellCount;
                if (cellIdx == playerCellIndex) continue;
                if (locationRegistry.IsCellOccupied(cellIdx)) continue;
                var cell = locationRegistry.GetCell(cellIdx);
                if (cell == null) continue;

                Vector3 npcPos = cell.SpawnPosition;
                var prisoner = Instantiate(prisonerPrefab, npcPos, cell.SpawnRotation);
                var npcAgent = prisoner.GetComponent<NavMeshAgent>();
                if (npcAgent != null)
                    SpawnPlacementUtility.WarpNavMeshAgent(npcAgent, npcPos);
                var ai = prisoner.GetComponent<PrisonerAI>();
                if (ai != null)
                {
                    ai.cellIndex = cellIdx;
                    int scheduleCellId = firstScheduleCellId + cellIdx;
                    RegisterPrisonerSocial(prisoner, scheduleCellId, cellIdx, ai);
                }
            }
        }
    }

    void RegisterPrisonerSocial(GameObject prisonerInstance, int scheduleCellId, int cellIndex, PrisonerAI ai)
    {
        if (prisonerInstance == null || ai == null) return;

        NPCPersonalityData assigned = null;
        if (availablePersonalities != null && availablePersonalities.Length > 0)
            assigned = availablePersonalities[Random.Range(0, availablePersonalities.Length)];

        if (SocialManager.Instance == null)
            Debug.LogWarning("[GameManager] SocialManager not in scene. Affinity UI will not update correctly.");
        else
            SocialManager.Instance.RegisterPrisoner(cellIndex, assigned, 0f);

        var label = prisonerInstance.GetComponent<PrisonerSocialPresenter>();
        if (label != null)
        {
            string title = $"Inmate {scheduleCellId}";
            string sub = (assigned != null && !string.IsNullOrWhiteSpace(assigned.personalityName))
                ? assigned.personalityName
                : null;
            label.SetRuntimeLabel(title, sub);
        }
    }

    void SpawnGuards()
    {
        if (guardPrefab == null) return;

        if (guardSpawnTable != null && guardSpawnTable.Length > 0)
        {
            for (int i = 0; i < guardSpawnTable.Length; i++)
            {
                var entry = guardSpawnTable[i];
                if (entry == null || entry.spawnPoint == null)
                {
                    Debug.LogWarning($"[GameManager] guardSpawnTable[{i}] missing spawnPoint — skipped.");
                    continue;
                }

                var t = entry.spawnPoint;
                Vector3 guardPos = SpawnPlacementUtility.SnapCharacterPosition(t.position);
                var go = Instantiate(guardPrefab, guardPos, t.rotation);
                var guardAgent = go.GetComponent<NavMeshAgent>();
                if (guardAgent != null)
                    SpawnPlacementUtility.WarpNavMeshAgent(guardAgent, guardPos);
                if (!string.IsNullOrWhiteSpace(entry.displayName))
                    go.name = entry.displayName.Trim();

                var fsm = go.GetComponent<GuardFSM>();
                if (fsm != null)
                {
                    fsm.duty = entry.role == GuardSpawnRole.NightCellVerifier
                        ? GuardFSM.GuardDuty.NightCellVerifier
                        : GuardFSM.GuardDuty.StandardPatrol;
                    if (entry.patrolWaypoints != null && entry.patrolWaypoints.Length > 0)
                        fsm.patrolWaypoints = entry.patrolWaypoints;
                }

                var shift = go.GetComponent<GuardShiftController>();
                if (shift == null)
                    shift = go.AddComponent<GuardShiftController>();
                shift.Initialize(entry.role, entry.onDutyDuring);
            }

            return;
        }

        for (int i = 0; i < guardCount; i++)
        {
            Vector3 pos;
            Quaternion rot = Quaternion.identity;
            if (guardSpawnPoints != null && guardSpawnPoints.Length > 0)
            {
                var pt = guardSpawnPoints[Random.Range(0, guardSpawnPoints.Length)];
                pos = pt.position;
                rot = pt.rotation;
            }
            else
            {
                pos = locationRegistry != null && locationRegistry.CellCount > 0
                    ? locationRegistry.GetCell(0).SpawnPosition + Vector3.forward * 5f
                    : Vector3.zero;
            }

            pos = SpawnPlacementUtility.SnapCharacterPosition(pos);
            var guard = Instantiate(guardPrefab, pos, rot);
            var guardAgent = guard.GetComponent<NavMeshAgent>();
            if (guardAgent != null)
                SpawnPlacementUtility.WarpNavMeshAgent(guardAgent, pos);
            var shift = guard.GetComponent<GuardShiftController>();
            if (shift == null)
                shift = guard.AddComponent<GuardShiftController>();
            shift.Initialize(GuardSpawnRole.StandardPatrol, null);
        }
    }
}
