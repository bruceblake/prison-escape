using UnityEngine;
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
        ItemSpawnNode[] nodes = Object.FindObjectsOfType<ItemSpawnNode>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            ItemSpawnNode node = nodes[i];
            if (node == null) continue;
            if (UnityEngine.Random.value > node.spawnChance) continue;
            if (node.lootTable == null) continue;

            ItemData pick = node.lootTable.GetRandomItem();
            if (pick == null) continue;
            if (pick.worldPrefab == null)
            {
                Debug.LogWarning($"[GameManager] Item '{pick.itemName}' has no worldPrefab; skip spawn at {node.gameObject.name}.", node);
                continue;
            }

            Transform t = node.transform;
            GameObject instance = Object.Instantiate(pick.worldPrefab, t.position, t.rotation);

            WorldItemPickup pickup = instance.GetComponent<WorldItemPickup>();
            if (pickup == null)
                pickup = instance.AddComponent<WorldItemPickup>();
            pickup.itemData = pick;
        }
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

        var player = Instantiate(playerPrefab, pos, rot);
        var prisonerCtrl = player.GetComponent<PrisonerController>();
        if (prisonerCtrl != null)
            prisonerCtrl.cellIndex = playerCellIndex;

        if (locationRegistry != null)
            locationRegistry.TryRegisterCellOccupant(playerCellIndex, player);

        Debug.Log("Player spawned into cell.");
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

                var prisoner = Instantiate(prisonerPrefab, cell.SpawnPosition, cell.SpawnRotation);
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

                var prisoner = Instantiate(prisonerPrefab, cell.SpawnPosition, cell.SpawnRotation);
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
                var go = Instantiate(guardPrefab, t.position, t.rotation);
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

            var guard = Instantiate(guardPrefab, pos, rot);
            var shift = guard.GetComponent<GuardShiftController>();
            if (shift == null)
                shift = guard.AddComponent<GuardShiftController>();
            shift.Initialize(GuardSpawnRole.StandardPatrol, null);
        }
    }
}