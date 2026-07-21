using System.Collections.Generic;
using UnityEngine;
using Prison;

public class PrisonerController : MonoBehaviour, Prison.IPrisoner
{
    [Header("Prisoner")]
    [Tooltip("Cell index for this prisoner (0 = player)")]
    public int cellIndex;

    [Header("Compliance")]
    [Tooltip("Distance to stand point to count as compliant (meters)")]
    public float compliantDistance = 3f;
    [Tooltip("After a guard escorts you to your cell, ignore schedule non-compliance for this many real seconds so they do not re-arrest you in a loop.")]
    [Min(5f)]
    public float postEscortImmunitySeconds = 40f;

    private HashSet<PrisonLocationZone> _zonesIn = new HashSet<PrisonLocationZone>();
    private PlayerController _playerController;
    private CharacterController _characterController;
    private bool _movementBlocked;
    private bool _isLocalPlayer;
    private float _postEscortImmunityUntil = float.NegativeInfinity;

    public bool IsCompliant { get; private set; }
    public bool IsAtRequiredLocation { get; private set; }
    /// <summary>Inside a currently-active <see cref="RestrictedZone"/> — counts as an escape attempt.</summary>
    public bool IsInActiveRestrictedZone { get; private set; }
    public bool IsRollCallShakedownComplete =>
        MorningRollCallTracker.Instance != null && MorningRollCallTracker.Instance.IsInmateShakedownComplete(this);
    public bool MovementBlocked => _movementBlocked;
    public bool HasPostEscortImmunity => Time.unscaledTime < _postEscortImmunityUntil;
    public int CellIndex => cellIndex;

    /// <summary>Time.time when compliance was last lost; -1 while compliant.</summary>
    public float NonCompliantSince { get; private set; } = -1f;
    /// <summary>Seconds since compliance was lost (0 while compliant). High-trust guards grace short lapses (Social v3 §2).</summary>
    public float NonCompliantSeconds => NonCompliantSince < 0f ? 0f : Time.time - NonCompliantSince;

    /// <summary>Roll-call early release (shakedown) ignored until this time — avoids pre-sweep before spawn.</summary>
    public float RollCallReleaseAllowedAfter { get; private set; }

    /// <summary>Crouch state, cached so guard detection does not GetComponent per scan.</summary>
    public bool IsCrouched => _playerController != null && _playerController.IsCrouched;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        PrisonerRegistry.Register(this);
    }

    private void OnDisable()
    {
        PrisonerRegistry.Unregister(this);
    }

    private void Start()
    {
        _isLocalPlayer = GetComponent<Player>()?.IsLocal ?? true;

        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged += OnScheduleEventChanged;

        MorningRollCallTracker.EnsureInstance();
        FormalCountMonitor.EnsureInstance();
        if (MorningRollCallTracker.Instance != null)
            MorningRollCallTracker.Instance.OnCellShakedownComplete += OnRollCallCellShakedownComplete;

        if (PrisonLocationRegistry.Instance != null)
            PrisonLocationRegistry.Instance.TryRegisterCellOccupant(cellIndex, gameObject);

        if (MorningRollCallTracker.Instance != null
            && MorningRollCallTracker.Instance.IsInmateShakedownComplete(this))
            RollCallReleaseAllowedAfter = Time.unscaledTime + 2f;

        UpdateCompliance();
    }

    private void OnDestroy()
    {
        PrisonLocationRegistry.Instance?.UnregisterCellOccupant(gameObject);

        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged -= OnScheduleEventChanged;

        if (MorningRollCallTracker.Instance != null)
            MorningRollCallTracker.Instance.OnCellShakedownComplete -= OnRollCallCellShakedownComplete;
    }

    private void OnRollCallCellShakedownComplete(int completedCell)
    {
        if (completedCell != cellIndex)
            return;
        RollCallReleaseAllowedAfter = 0f;
        UpdateCompliance();
    }

    private void OnScheduleEventChanged(PrisonEventType evt)
    {
        RollCallReleaseAllowedAfter = 0f;
        UpdateCompliance();
    }

    private void Update()
    {
        UpdateCompliance();
    }

    /// <summary>Raised when the local player enters (true) or exits (false) a location zone. Used by the social layer (gang territory warn-offs, job tracking).</summary>
    public static event System.Action<PrisonLocationZone, bool> OnPlayerZoneChanged;

    public void EnterZone(PrisonLocationZone zone)
    {
        if (zone != null)
        {
            _zonesIn.Add(zone);
            OnPlayerZoneChanged?.Invoke(zone, true);
        }
    }

    public void ExitZone(PrisonLocationZone zone)
    {
        if (zone != null)
        {
            _zonesIn.Remove(zone);
            OnPlayerZoneChanged?.Invoke(zone, false);
        }
    }

    /// <summary>True while the player overlaps any zone of this type.</summary>
    public bool IsInZoneType(Prison.ZoneType type)
    {
        foreach (var z in _zonesIn)
            if (z != null && z.zoneType == type) return true;
        return false;
    }

    /// <summary>Short label for the compliance HUD, e.g. "LOC: CELL BLOCK B" from overlapping zones. Fill <see cref="PrisonLocationZone.hudDisplayName"/> in the scene for best results.</summary>
    public string GetCurrentLocationLabel()
    {
        if (_zonesIn == null || _zonesIn.Count == 0) return "LOC: —";
        foreach (var z in _zonesIn)
        {
            if (z != null) return "LOC: " + z.GetHudLabel();
        }
        return "LOC: —";
    }

    private void UpdateCompliance()
    {
        UpdateComplianceCore();
        MorningRollCallTracker.TryOpenDoorWhenInmateAtStand(this);
        if (IsCompliant) NonCompliantSince = -1f;
        else if (NonCompliantSince < 0f) NonCompliantSince = Time.time;
    }

    private void UpdateComplianceCore()
    {
        // Restricted zones override everything (grace, release, flexible phases):
        // being inside one is an escape attempt, not schedule non-compliance.
        IsInActiveRestrictedZone = RestrictedZone.IsPrisonerInActiveRestrictedZone(this);
        if (IsInActiveRestrictedZone)
        {
            IsAtRequiredLocation = false;
            IsCompliant = false;
            return;
        }

        if (PrisonTimeManager.Instance == null || PrisonLocationRegistry.Instance == null)
        {
            IsAtRequiredLocation = true;
            IsCompliant = true;
            return;
        }

        var tm = PrisonTimeManager.Instance;
        var evt = tm.CurrentEvent;

        // Post-escort: still report where you are for HUD, but count as compliant so
        // the same guard does not walk away and immediately re-arrest you in your cell.
        if (HasPostEscortImmunity)
        {
            IsAtRequiredLocation = IsPhysicallyAtRequiredLocation(evt);
            IsCompliant = true;
            return;
        }

        if (MorningRollCallTracker.IsInmateReleasedFromRollCallStand(this))
        {
            tm.GetNextEventInfo(out PrisonEventType nextEvt, out _);
            IsAtRequiredLocation = IsPhysicallyAtRequiredLocation(nextEvt)
                || IsPhysicallyAtRequiredLocation(evt);
            IsCompliant = true;
            return;
        }

        IsAtRequiredLocation = IsPhysicallyAtRequiredLocation(evt);

        if (PrisonTimeManager.Instance.IsMandatoryTravelGraceActive)
        {
            IsCompliant = true;
            return;
        }

        IsCompliant = IsAtRequiredLocation;
    }

    private bool IsPhysicallyAtRequiredLocation(PrisonEventType evt)
    {
        var registry = PrisonLocationRegistry.Instance;
        if (registry == null) return true;

        foreach (var zone in _zonesIn)
        {
            if (zone != null && IsZoneCompliantForEvent(zone, evt))
                return true;
        }

        var standPoint = registry.GetStandPointForEvent(evt, cellIndex);
        if (standPoint != null && _characterController != null)
        {
            float dist = Vector3.Distance(transform.position, standPoint.position);
            if (dist <= compliantDistance)
                return true;
        }

        if (PrisonEventExtensions.IsMorningLineUp(evt) || PrisonEventExtensions.IsCellCountPhase(evt))
        {
            var cellData = registry.GetCell(cellIndex);
            if (cellData != null && _characterController != null
                && Vector3.Distance(transform.position, cellData.ShakedownSweepWorldCenter)
                <= cellData.InteriorRadius)
                return true;
        }

        return false;
    }

    private bool IsZoneCompliantForEvent(PrisonLocationZone zone, PrisonEventType evt)
    {
        switch (evt)
        {
            case PrisonEventType.RollCall:
            case PrisonEventType.MorningRollCall:
                // Use a cell volume (PrisonLocationZone, ZoneType.Cell) for "be in your cell" roll call;
                // stand-point distance is still checked in IsPhysicallyAtRequiredLocation as fallback.
                return zone.zoneType == ZoneType.Cell && zone.cellIndex == cellIndex;
            case PrisonEventType.NightRollCall:
            case PrisonEventType.LightsOut:
            case PrisonEventType.MiddayCount:
            case PrisonEventType.EveningCount:
                return zone.zoneType == ZoneType.Cell && zone.cellIndex == cellIndex;
            case PrisonEventType.Breakfast:
            case PrisonEventType.Lunch:
            case PrisonEventType.Dinner:
                return zone.zoneType == ZoneType.Cafeteria;
            case PrisonEventType.FreeTime:
                return zone.zoneType == ZoneType.Yard || zone.zoneType == ZoneType.Cafeteria;
            case PrisonEventType.WorkProgram:
                return zone.zoneType == ZoneType.Workshop;
        }
        return false;
    }

    public void SendToCell(int targetCellIndex = -1)
    {
        int idx = targetCellIndex >= 0 ? targetCellIndex : cellIndex;
        var cell = PrisonLocationRegistry.Instance?.GetCell(idx);
        if (cell == null) return;

        SetMovementBlocked(true);

        if (_playerController != null)
        {
            _playerController.ForceTeleport(cell.SpawnPosition);
        }
        else if (_characterController != null)
        {
            _characterController.enabled = false;
            transform.position = cell.SpawnPosition;
            transform.rotation = cell.SpawnRotation;
            _characterController.enabled = true;
        }
        else
        {
            transform.position = cell.SpawnPosition;
            transform.rotation = cell.SpawnRotation;
        }

        GrantPostEscortImmunity(postEscortImmunitySeconds);
        Invoke(nameof(ReleaseMovement), 1.25f);
    }

    /// <summary>Starts the post-escort window; also usable after other soft punishments.</summary>
    public void GrantPostEscortImmunity(float seconds)
    {
        float duration = Mathf.Max(5f, seconds);
        _postEscortImmunityUntil = Time.unscaledTime + duration;
        NonCompliantSince = -1f;
        IsCompliant = true;

        if (_isLocalPlayer)
        {
            string tip = PrisonRoutineLabels.GetGoToLabel(
                PrisonTimeManager.Instance != null ? PrisonTimeManager.Instance.CurrentEvent : PrisonEventType.FreeTime,
                cellIndex);
            if (string.IsNullOrEmpty(tip))
                tip = "your next destination";
            Prison.Social.SocialToastUI.Show($"Back in your cell. Get to {tip} — guards will give you a minute.");
        }
    }

    public void SetMovementBlocked(bool blocked)
    {
        _movementBlocked = blocked;
        if (_playerController != null)
            _playerController.enabled = !blocked;
    }

    private void ReleaseMovement()
    {
        SetMovementBlocked(false);
    }
}
