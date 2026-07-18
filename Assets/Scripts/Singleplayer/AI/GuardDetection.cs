using UnityEngine;
using Prison;

public class GuardDetection : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 10f;
    [Tooltip("Cone angle in degrees (180 = hemisphere in front)")]
    public float coneAngle = 90f;
    [Tooltip("Within this distance, non-compliant prisoners are spotted without the cone check (fixes guards facing waypoint direction and ignoring you beside them). 0 = cone only.")]
    public float proximitySpotDistance = 6f;
    public LayerMask prisonerLayer = -1;

    [Header("References")]
    public Transform eyeTransform;

    [Header("Debug")]
    [Tooltip("Log detection scans (throttled) and every acquisition of a non-compliant prisoner.")]
    public bool debugLogs;
    [Tooltip("Minimum seconds between full scan summary logs while in Patrol.")]
    public float debugScanIntervalSeconds = 1.25f;

    private Transform _transform;
    private float _nextScanLogTime;
    private float _nextNearMissLogTime;

    private void Awake()
    {
        _transform = transform;
        if (eyeTransform == null) eyeTransform = _transform;
    }

    public IPrisoner FindNonCompliantPrisoner()
    {
        // Blind-eye bribe (Social Ecosystem v3 §8): this guard's detection is off for the phase.
        var socialProfile = GetComponent<Prison.Social.GuardSocialProfile>();
        if (socialProfile != null && socialProfile.BlindEyeActive)
            return null;

        var playerPrisoners = FindObjectsByType<PrisonerController>(FindObjectsSortMode.None);
        var npcPrisoners = FindObjectsByType<PrisonerAI>(FindObjectsSortMode.None);
        Vector3 eyePos = eyeTransform.position;
        Vector3 forward = eyeTransform.forward;

        bool logScan = debugLogs && Time.unscaledTime >= _nextScanLogTime;
        if (logScan)
            _nextScanLogTime = Time.unscaledTime + debugScanIntervalSeconds;

        if (logScan)
            Debug.Log($"[GuardDetection][{gameObject.name}] Scan: eye={eyePos} fwd={forward} range={detectionRange} cone={coneAngle}° | players={playerPrisoners.Length} npcs={npcPrisoners.Length}", this);

        // Suspicion after a caught escape widens detection against the player for a few days.
        float suspicionMult = PrisonSuspicion.GlobalDetectionRangeMultiplier;

        // Per-player guard Trust (Social v3 §2, M6): trusted guards watch the player less
        // closely and sit a moment on a fresh schedule lapse; distrusted guards watch harder.
        float guardTrust = socialProfile != null ? socialProfile.TrustTowardPlayer : 0f;
        float trustMult = Prison.Social.GuardTrustMath.DetectionRangeMultiplier(guardTrust);

        foreach (var p in playerPrisoners)
        {
            if (p.MovementBlocked)
            {
                if (logScan) Debug.Log($"[GuardDetection] Player {p.name}: skip (MovementBlocked)", this);
                continue;
            }

            if (p.HasPostEscortImmunity)
            {
                if (logScan) Debug.Log($"[GuardDetection] Player {p.name}: skip (post-escort immunity)", this);
                continue;
            }

            // Crouching shrinks the guard's effective spotting ranges (stealth).
            var crouchController = p.GetComponent<PlayerController>();
            float crouchMult = crouchController != null && crouchController.IsCrouched ? 0.6f : 1f;

            float dist = Vector3.Distance(eyePos, p.transform.position);
            bool inCone = IsInSight(p.transform.position, eyePos, forward, suspicionMult * crouchMult * trustMult);
            bool inProximity = proximitySpotDistance > 0.01f && dist <= proximitySpotDistance * suspicionMult * crouchMult * trustMult;
            bool spotted = inCone || inProximity;

            if (logScan)
                Debug.Log($"[GuardDetection] Player {p.name}: compliant={p.IsCompliant} dist={dist:F2} cone={inCone} proximity={inProximity} crouched={crouchMult < 1f} trustMult={trustMult:F2} eyeFwd={forward}", this);

            if (!p.IsCompliant && spotted)
            {
                // Trust grace covers schedule lapses only — never a restricted-zone escape attempt.
                float grace = Prison.Social.GuardTrustMath.ComplianceGraceSeconds(guardTrust);
                if (grace > 0f && !p.IsInActiveRestrictedZone && p.NonCompliantSeconds < grace)
                {
                    if (logScan)
                        Debug.Log($"[GuardDetection] Player {p.name}: within trust grace ({p.NonCompliantSeconds:F1}s < {grace:F1}s) — holding off.", this);
                    continue;
                }
                if (debugLogs)
                    Debug.Log($"[GuardDetection] *** TARGET player {p.name} (non-compliant) via {(inProximity && !inCone ? "PROXIMITY" : "CONE/PROX")}", this);
                return p;
            }

            LogNearMissNonCompliant(p.IsCompliant, dist, inCone, inProximity, p.name, p.transform.position, "Player");
        }

        foreach (var p in npcPrisoners)
        {
            if (p.MovementBlocked)
            {
                if (logScan) Debug.Log($"[GuardDetection] NPC {p.name}: skip (MovementBlocked)", this);
                continue;
            }

            if (p.HasPostEscortImmunity)
            {
                if (logScan) Debug.Log($"[GuardDetection] NPC {p.name}: skip (post-escort immunity)", this);
                continue;
            }

            float dist = Vector3.Distance(eyePos, p.transform.position);
            bool inCone = IsInSight(p.transform.position, eyePos, forward);
            bool inProximity = proximitySpotDistance > 0.01f && dist <= proximitySpotDistance;
            bool spotted = inCone || inProximity;

            if (logScan)
                Debug.Log($"[GuardDetection] NPC {p.name}: cell={p.CellIndex} compliant={p.IsCompliant} dist={dist:F2} cone={inCone} proximity={inProximity}", this);

            if (!p.IsCompliant && spotted)
            {
                if (debugLogs)
                    Debug.Log($"[GuardDetection] *** TARGET NPC {p.name} (non-compliant) via {(inProximity && !inCone ? "PROXIMITY" : "CONE/PROX")}", this);
                return p;
            }

            LogNearMissNonCompliant(p.IsCompliant, dist, inCone, inProximity, p.name, p.transform.position, "NPC");
        }

        if (logScan)
            Debug.Log($"[GuardDetection] Scan complete: no non-compliant prisoner in sight.", this);

        return null;
    }

    /// <summary>
    /// True if the position is in this guard’s sight cone or proximity spot (same geometry as a non‑compliance spot check).
    /// Used for “heat” UI even when the prisoner is still compliant.
    /// </summary>
    public bool IsPositionInAttentionZone(Vector3 worldPos)
    {
        Vector3 eyePos = eyeTransform != null ? eyeTransform.position : _transform.position;
        Vector3 forward = eyeTransform != null ? eyeTransform.forward : _transform.forward;

        float dist = Vector3.Distance(eyePos, worldPos);
        bool inCone = IsInSight(worldPos, eyePos, forward);
        bool inProximity = proximitySpotDistance > 0.01f && dist <= proximitySpotDistance;
        return inCone || inProximity;
    }

    /// <summary>
    /// Night roll call: true if a <see cref="PrisonerController"/> or <see cref="FakeBedDummy"/> for this cell is inside the cell interior sphere.
    /// </summary>
    public bool VerifyCellBedPresence(int cellIndex)
    {
        var reg = PrisonLocationRegistry.Instance;
        var cell = reg?.GetCell(cellIndex);
        if (cell == null) return false;

        Vector3 c = cell.BedPresenceWorldCenter;
        float r = cell.InteriorRadius;
        var cols = Physics.OverlapSphere(c, r, ~0, QueryTriggerInteraction.Collide);
        foreach (var col in cols)
        {
            var dummy = col.GetComponentInParent<FakeBedDummy>();
            if (dummy != null && dummy.CellIndex == cellIndex)
                return true;

            var pc = col.GetComponentInParent<PrisonerController>();
            if (pc != null && pc.CellIndex == cellIndex)
                return true;

            var npc = col.GetComponentInParent<PrisonerAI>();
            if (npc != null && npc.CellIndex == cellIndex)
                return true;
        }

        return false;
    }

    private void LogNearMissNonCompliant(bool compliant, float dist, bool inCone, bool inProximity, string name, Vector3 targetPos, string tag)
    {
        if (!debugLogs || compliant) return;
        if (inCone || inProximity) return;
        if (dist > detectionRange + 2f) return;
        if (Time.unscaledTime < _nextNearMissLogTime) return;
        _nextNearMissLogTime = Time.unscaledTime + 0.75f;
        Vector3 to = (targetPos - eyeTransform.position).normalized;
        if (to.sqrMagnitude < 0.0001f) return;
        float ang = Vector3.Angle(eyeTransform.forward, to);
        Debug.Log($"[GuardDetection][{gameObject.name}] Near miss: {tag} {name} NON-COMPLIANT not spotted — dist={dist:F2} angle={ang:F0}° (cone half={coneAngle * 0.5f:F0}°) proxSpot≤{proximitySpotDistance} maxRange={detectionRange}. eyeFwd={eyeTransform.forward}", this);
    }

    private bool IsInSight(Vector3 targetPos, Vector3 eyePos, Vector3 forward, float rangeMultiplier = 1f)
    {
        Vector3 toTarget = targetPos - eyePos;
        float dist = toTarget.magnitude;
        // Career difficulty scales the base sight cone (ADX guards see farther); suspicion
        // multiplier stacks on top (Prison Career Ladder § Difficulty & pacing curves).
        float facilityMult = Prison.Career.CareerSession.DetectionRangeMult;
        if (dist > detectionRange * facilityMult * rangeMultiplier) return false;

        float angle = Vector3.Angle(forward, toTarget.normalized);
        return angle <= coneAngle * 0.5f;
    }
}
