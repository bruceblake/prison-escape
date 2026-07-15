using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Single batchmode entry point: opens the prison scene, regenerates character
/// visuals/animations, runs the polish pass (props, colliders, textures,
/// lighting, NavMesh, patrol routes), then the door/waypoint fixer, and saves.
/// Usage: Unity.exe -batchmode -quit -executeMethod PrisonBatchRunner.RunFullSetup
/// </summary>
public static class PrisonBatchRunner
{
    public static void RunFullSetup()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PrisonLevel1.unity", OpenSceneMode.Single);

        CharacterVisualSetupRunner.Run();
        PrisonPolishPass.RunAll();
        PrisonDoorAndWaypointFixer.FixAll(); // realigns doors, snaps waypoints, wires registry, saves scene

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }
}
