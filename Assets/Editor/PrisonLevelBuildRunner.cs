using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds prison wings using shared layout anchors. Prefer <see cref="PrisonLayoutRebuildRunner"/> for full layout fixes.
/// Menu: Prison / Build Level Wings
/// </summary>
public static class PrisonLevelBuildRunner
{
    [MenuItem("Prison/Build Level Wings")]
    public static void Run()
    {
        PrisonLayoutRebuildRunner.Run();
    }
}
