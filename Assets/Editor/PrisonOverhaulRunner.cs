using UnityEngine;
using UnityEditor;
using Prison;

public class PrisonOverhaulRunner : MonoBehaviour
{
    [MenuItem("Prison/Fix Cell Doors & UI")]
    public static void FixCellDoors()
    {
        int deletedBars = 0;
        int createdDoors = 0;

        // 1. Find all JailCells
        var jailCells = GameObject.Find("JailCells");
        if (jailCells == null)
        {
            Debug.LogError("Could not find 'JailCells' object in the scene.");
            return;
        }

        // Try to find the cell door prefab
        GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Modular/CellDoor_Modular.prefab");
        if (doorPrefab == null)
        {
            doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/PrisonCellDoor.prefab");
        }

        for (int i = 0; i < jailCells.transform.childCount; i++)
        {
            var cell = jailCells.transform.GetChild(i);

            // 2. Delete the custom CellDoorBars we made earlier
            Transform oldBars = cell.Find("CellDoorBars");
            if (oldBars != null)
            {
                DestroyImmediate(oldBars.gameObject);
                deletedBars++;
            }

            // 3. Instantiate the real door if we haven't already
            Transform existingDoor = cell.Find("PrisonCellDoor");
            if (existingDoor == null && doorPrefab != null)
            {
                var newDoor = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab);
                newDoor.name = "PrisonCellDoor";
                newDoor.transform.SetParent(cell);
                
                // Position it in the opening (roughly x=16.9, z=2 based on our previous bars)
                newDoor.transform.localPosition = new Vector3(16.9f, 0f, 2f);
                
                // Add our new automation script
                var controller = newDoor.AddComponent<CellDoorController>();
                // Adjust open offset based on the doorway width
                controller.openOffset = new Vector3(0f, 0f, 4.5f); 
                
                createdDoors++;
            }
        }

        Debug.Log($"[PrisonOverhaul] Deleted {deletedBars} old bar groups. Created {createdDoors} proper sliding doors.");
    }
}
