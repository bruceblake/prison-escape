using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Prison;
using Prison.Visuals;

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    [Header("World random")]
    [Tooltip("Drives UnityEngine.Random after InitState. If Use Random Seed is on, a new value is chosen each run.")]
    public string worldSeed;

    [Tooltip("If true, worldSeed is replaced with a random 0..999999 before InitState. If false, use worldSeed as entered (e.g. for reproducible runs).")]
    public bool useRandomSeed = true;

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

    private GameObject _spawnedPlayer;
    private readonly List<(GameObject go, int cellIndex)> _spawnedInmates = new List<(GameObject, int)>();
    private readonly List<GameObject> _spawnedGuards = new List<GameObject>();

    void Start()
    {
        if (locationRegistry == null)
            locationRegistry = FindFirstObjectByType<PrisonLocationRegistry>();

        SpawnPlayer();
        SpawnNpcPrisoners();
        SpawnGuards();
        WorldLootBootstrap.EnsureSpawnNodes();
        StartCoroutine(PopulateWorldSpawnsDeferred());
        BuildSocialWorld();
    }

    private IEnumerator PopulateWorldSpawnsDeferred()
    {
        // Let floors / NavMesh / physics finish so pickup snaps land on walkable surfaces.
        yield return null;
        yield return null;
        PopulateWorldSpawns();
    }

    /// <summary>
    /// Deterministic social ecosystem boot (Social Ecosystem & Gangs v3): every spawned
    /// prisoner and guard gets an identity, archetype, traits, and gang from worldSeed.
    /// Career Respect seeds the arrival Standing band, never individual history.
    /// </summary>
    void BuildSocialWorld()
    {
        var social = Prison.Social.SocialWorld.EnsureInstance();
        if (_spawnedPlayer != null)
            social.RegisterPlayer(_spawnedPlayer);

        float arrivalSeed = 0f;
        var careerWorld = Prison.Career.CareerSession.ActiveWorld;
        if (careerWorld != null && Prison.Career.CareerSession.HasActiveRun)
            arrivalSeed = Prison.Career.CareerRespectMath.ArrivalAffinitySeed(careerWorld.global.respect);

        social.BuildWorld(worldSeed.GetHashCode(), _spawnedInmates, _spawnedGuards, arrivalSeed);

        if (careerWorld != null && Prison.Career.CareerSession.HasActiveRun)
            social.ApplyCareerGangTag(careerWorld.global.gangId);

        // Runtime labels come from generated identities now.
        foreach (var (go, _) in _spawnedInmates)
        {
            if (go == null) continue;
            int actorId = social.GetActorId(go);
            var identity = social.GetIdentity(actorId);
            if (identity == null) continue;

            var nameLabel = go.GetComponent<Prison.Visuals.CharacterNameLabel>();
            if (nameLabel != null)
                nameLabel.SetDisplayName(identity.DisplayName);
        }
    }

    /// <summary>
    /// Spawns world pickups at each <see cref="ItemSpawnNode"/> that passes <see cref="ItemSpawnNode.spawnChance"/>.
    /// </summary>
    public void PopulateWorldSpawns()
    {
        // Career difficulty: County is littered with parts, ADX is bare (lootAbundance curve).
        float lootAbundance = Prison.Career.CareerSession.LootAbundance;

        ItemSpawnNode[] nodes = Object.FindObjectsOfType<ItemSpawnNode>(true);
        int spawned = 0;
        for (int i = 0; i < nodes.Length; i++)
        {
            ItemSpawnNode node = nodes[i];
            if (node == null) continue;
            if (node.lootTable == null) continue;

            int rolls = Mathf.Max(1, node.spawnRolls);
            for (int r = 0; r < rolls; r++)
            {
                if (UnityEngine.Random.value > node.spawnChance * lootAbundance) continue;

                ItemData pick = node.lootTable.GetRandomItem();
                if (pick == null) continue;
                if (pick.worldPrefab == null)
                {
                    Debug.LogWarning($"[GameManager] Item '{pick.itemName}' has no worldPrefab; skip spawn at {node.gameObject.name}.", node);
                    continue;
                }

                Transform t = node.transform;
                Vector3 spawnPos = SpawnPlacementUtility.SnapPickupPosition(t.position);
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.45f, 0.45f),
                    0f,
                    UnityEngine.Random.Range(-0.45f, 0.45f));
                GameObject instance = Object.Instantiate(pick.worldPrefab, spawnPos + offset, t.rotation);
                SpawnPlacementUtility.FitWorldPickupOnFloor(instance, spawnPos.y);

                WorldItemPickup pickup = instance.GetComponent<WorldItemPickup>();
                if (pickup == null)
                    pickup = instance.AddComponent<WorldItemPickup>();
                pickup.itemData = pick;
                spawned++;
            }
        }

        Debug.Log($"[GameManager] World loot: spawned {spawned} pickups from {nodes.Length} nodes (abundance {lootAbundance:0.00}).");
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
        _spawnedPlayer = player;
        var prisonerCtrl = player.GetComponent<PrisonerController>();
        if (prisonerCtrl != null)
            prisonerCtrl.cellIndex = playerCellIndex;

        var playerLabel = player.GetComponent<CharacterNameLabel>();
        if (playerLabel != null)
            playerLabel.SetDisplayName("You");

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
                    _spawnedInmates.Add((prisoner, cellIdx));
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
                    _spawnedInmates.Add((prisoner, cellIdx));
                }
            }
        }
    }

    void SpawnGuards()
    {
        if (guardPrefab == null)
        {
            Debug.LogError("[GameManager] guardPrefab is null — no guards will spawn.");
            return;
        }

        int spawned = 0;
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
                Vector3 pos = t.position;
                Quaternion rot = t.rotation;
                if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var navHit, 6f, UnityEngine.AI.NavMesh.AllAreas))
                    pos = navHit.position;
                else
                    Debug.LogWarning($"[GameManager] Guard spawn '{t.name}' at {t.position} is off NavMesh — spawning raw.", t);

                var go = Instantiate(guardPrefab, pos, rot);
                go.SetActive(true);
                _spawnedGuards.Add(go);
                if (!string.IsNullOrWhiteSpace(entry.displayName))
                    go.name = entry.displayName.Trim();

                var guardLabel = go.GetComponent<CharacterNameLabel>();
                if (guardLabel != null)
                {
                    string label = !string.IsNullOrWhiteSpace(entry.displayName)
                        ? entry.displayName.Trim()
                        : go.name;
                    guardLabel.SetDisplayName(label);
                }

                var fsm = go.GetComponent<GuardFSM>();
                if (fsm != null)
                {
                    fsm.duty = entry.role == GuardSpawnRole.NightCellVerifier
                        ? GuardFSM.GuardDuty.NightCellVerifier
                        : GuardFSM.GuardDuty.StandardPatrol;
                    if (entry.patrolWaypoints != null && entry.patrolWaypoints.Length > 0)
                        fsm.patrolWaypoints = entry.patrolWaypoints;
                }

                var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.enabled && !agent.isOnNavMesh)
                {
                    if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var warpHit, 6f, UnityEngine.AI.NavMesh.AllAreas))
                        agent.Warp(warpHit.position);
                }

                var shift = go.GetComponent<GuardShiftController>();
                if (shift == null)
                    shift = go.AddComponent<GuardShiftController>();
                // Empty array from the inspector means "always on" — normalize to null.
                var dutyWindow = entry.onDutyDuring != null && entry.onDutyDuring.Length > 0
                    ? entry.onDutyDuring
                    : null;
                shift.Initialize(entry.role, dutyWindow);
                spawned++;
                Debug.Log($"[GameManager] Spawned guard '{go.name}' role={entry.role} at {pos}", go);
            }

            Debug.Log($"[GameManager] Guard spawn complete: {spawned}/{guardSpawnTable.Length} from spawn table.");
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

            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var navHit, 6f, UnityEngine.AI.NavMesh.AllAreas))
                pos = navHit.position;

            var guard = Instantiate(guardPrefab, pos, rot);
            guard.SetActive(true);
            _spawnedGuards.Add(guard);
            var shift = guard.GetComponent<GuardShiftController>();
            if (shift == null)
                shift = guard.AddComponent<GuardShiftController>();
            shift.Initialize(GuardSpawnRole.StandardPatrol, null);
            spawned++;
        }

        Debug.Log($"[GameManager] Guard spawn complete (legacy): {spawned} guards.");
    }
}
