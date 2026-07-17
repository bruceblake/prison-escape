using Prison;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Spawns runtime HUD widgets that do not require scene wiring.</summary>
public static class HudBootstrap
{
    private static bool _booted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootAfterSceneLoad() => EnsureHud();

    public static void EnsureHud()
    {
        if (_booted) return;
        _booted = true;

        DeduplicateEventSystems();
        PlayerStats.EnsureInstance();
        EnsureWallet();
        PlayerVitalsHUD.EnsureInstance();
        CurrentLocationHUD.EnsureInstance();
        ObjectiveWaypointUI.EnsureInstance();
    }

    /// <summary>
    /// Duplicate EventSystems log a warning every frame and can tank Editor play-mode FPS.
    /// </summary>
    private static void DeduplicateEventSystems()
    {
        var systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (systems.Length <= 1) return;

        EventSystem keep = null;
        foreach (var es in systems)
        {
            if (es != null && es.name == "EventSystem")
            {
                keep = es;
                break;
            }
        }
        if (keep == null)
            keep = systems[0];

        foreach (var es in systems)
        {
            if (es == null || es == keep) continue;
            Object.Destroy(es.gameObject);
        }
    }

    private static void EnsureWallet()
    {
        if (PlayerWallet.Instance != null) return;
        var go = new GameObject("PlayerWallet");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<PlayerWallet>();
    }
}
