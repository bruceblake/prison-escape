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

    private HashSet<PrisonLocationZone> _zonesIn = new HashSet<PrisonLocationZone>();
    private PlayerController _playerController;
    private CharacterController _characterController;
    private bool _movementBlocked;
    private bool _isLocalPlayer;

    public bool IsCompliant { get; private set; }
    public bool IsAtRequiredLocation { get; private set; }
    /// <summary>Inside a currently-active <see cref="RestrictedZone"/> — counts as an escape attempt.</summary>
    public bool IsInActiveRestrictedZone { get; private set; }
    public bool IsRollCallShakedownComplete =>
        MorningRollCallTracker.Instance != null && MorningRollCallTracker.Instance.IsInmateShakedownComplete(this);
    public bool MovementBlocked => _movementBlocked;
    public int CellIndex => cellIndex;

    /// <summary>Roll-call early release (shakedown) ignored until this time — avoids pre-sweep before spawn.</summary>
    public float RollCallReleaseAllowedAfter { get; private set; }

    private void Start()
    {
        _playerController = GetComponent<PlayerController>();
        _characterController = GetComponent<CharacterController>();
        _isLocalPlayer = GetComponent<Player>()?.IsLocal ?? true;

        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged += OnScheduleEventChanged;

        MorningRollCallTracker.EnsureInstance();
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

    public void EnterZone(PrisonLocationZone zone)
    {
        if (zone != null) _zonesIn.Add(zone);
    }

    public void ExitZone(PrisonLocationZone zone)
    {
        if (zone != null) _zonesIn.Remove(zone);
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

        if (evt == PrisonEventType.MorningRollCall || evt == PrisonEventType.RollCall)
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
                return zone.zoneType == ZoneType.Cell && zone.cellIndex == cellIndex;
            case PrisonEventType.Breakfast:
            case PrisonEventType.Lunch:
            case PrisonEventType.Dinner:
                return zone.zoneType == ZoneType.Cafeteria;
            case PrisonEventType.FreeTime:
                return zone.zoneType == ZoneType.Yard || zone.zoneType == ZoneType.Cafeteria;
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

        Invoke(nameof(ReleaseMovement), 1f);
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
