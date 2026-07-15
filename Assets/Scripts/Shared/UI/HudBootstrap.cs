using Prison;
using UnityEngine;

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

        PlayerStats.EnsureInstance();
        EnsureWallet();
        PlayerVitalsHUD.EnsureInstance();
        CurrentLocationHUD.EnsureInstance();
        ObjectiveWaypointUI.EnsureInstance();
    }

    private static void EnsureWallet()
    {
        if (PlayerWallet.Instance != null) return;
        var go = new GameObject("PlayerWallet");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<PlayerWallet>();
    }
}
