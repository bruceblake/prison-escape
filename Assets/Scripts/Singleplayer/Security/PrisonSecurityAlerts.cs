using System;
using UnityEngine;

/// <summary>
/// Global hooks for lockdown / suspicion (wire warden UI, alarms, or chase FSM from listeners).
/// </summary>
public static class PrisonSecurityAlerts
{
    public static event Action<string> OnLockdown;
    public static event Action<string> OnSuspicion;

    public static void RaiseLockdown(string reason)
    {
        Debug.LogWarning($"[PrisonSecurity] LOCKDOWN: {reason}");
        OnLockdown?.Invoke(reason);
    }

    public static void RaiseSuspicion(string reason)
    {
        Debug.Log($"[PrisonSecurity] Suspicion: {reason}");
        OnSuspicion?.Invoke(reason);
    }
}
