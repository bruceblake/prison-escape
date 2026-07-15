using UnityEngine;

/// <summary>
/// Tracks open fullscreen/paper menus so ambient HUD can fade out.
/// Call <see cref="RegisterOpen"/> / <see cref="RegisterClosed"/> from menu Open/Close paths.
/// </summary>
public static class UIMenuFocus
{
    private static int _openMenuCount;

    public static bool IsAnyMenuOpen => _openMenuCount > 0;

    public static void RegisterOpen() => _openMenuCount = Mathf.Max(0, _openMenuCount) + 1;

    public static void RegisterClosed() => _openMenuCount = Mathf.Max(0, _openMenuCount - 1);

    public static void Reset() => _openMenuCount = 0;
}
