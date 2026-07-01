using System.IO;
using Prison.Visuals;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates low-poly humanoid meshes/materials and wires them into player, guard, and NPC prefabs.
/// Menu: Prison / Setup Character Visuals
/// </summary>
public static class CharacterVisualSetupRunner
{
    private const string MeshFolder = "Assets/Meshes/Characters";
    private const string MaterialFolder = "Assets/Materials/Characters";

    private struct RolePalette
    {
        public Color Clothing;
        public Color Accent;
        public Color Boots;
    }

    [MenuItem("Prison/Setup Character Visuals")]
    public static void Run()
    {
        EnsureFolders();

        Material skin = EnsureMaterial("Char_Skin", new Color(0.82f, 0.62f, 0.48f), 0.05f);
        Material boots = EnsureMaterial("Char_Boots", new Color(0.12f, 0.12f, 0.14f), 0.15f);
        Material accent = EnsureMaterial("Char_Accent", new Color(0.92f, 0.92f, 0.88f), 0.1f);

        var palettes = new[]
        {
            (CharacterVisualRole.Player, new RolePalette
            {
                Clothing = new Color(0.18f, 0.35f, 0.52f),
                Accent = new Color(0.28f, 0.78f, 0.72f),
                Boots = new Color(0.15f, 0.16f, 0.2f)
            }),
            (CharacterVisualRole.Guard, new RolePalette
            {
                Clothing = new Color(0.14f, 0.24f, 0.42f),
                Accent = new Color(0.78f, 0.62f, 0.18f),
                Boots = new Color(0.08f, 0.08f, 0.1f)
            }),
            (CharacterVisualRole.Prisoner, new RolePalette
            {
                Clothing = new Color(0.92f, 0.48f, 0.18f),
                Accent = new Color(0.95f, 0.95f, 0.92f),
                Boots = new Color(0.55f, 0.55f, 0.58f)
            })
        };

        foreach (var (role, palette) in palettes)
        {
            Material clothing = EnsureMaterial($"Char_Clothing_{role}", palette.Clothing, 0.08f);
            Material roleBoots = EnsureMaterial($"Char_Boots_{role}", palette.Boots, 0.12f);
            Material roleAccent = EnsureMaterial($"Char_Accent_{role}", palette.Accent, 0.1f);
            Mesh mesh = EnsureMesh(role);
            ApplyToPrefabs(role, mesh, skin, clothing, roleBoots, roleAccent);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CharacterVisualSetup] Low-poly character visuals updated for Player, Guard, and Prisoner.");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Meshes"))
            AssetDatabase.CreateFolder("Assets", "Meshes");
        if (!AssetDatabase.IsValidFolder(MeshFolder))
            AssetDatabase.CreateFolder("Assets/Meshes", "Characters");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Characters"))
            AssetDatabase.CreateFolder("Assets/Materials", "Characters");
    }

    private static Material EnsureMaterial(string name, Color color, float smoothness)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.shader = shader;
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Glossiness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Mesh EnsureMesh(CharacterVisualRole role)
    {
        string path = $"{MeshFolder}/LowPolyHumanoid_{role}.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        Mesh mesh = LowPolyCharacterMeshBuilder.BuildHumanoidMesh(role);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        existing.Clear(false);
        existing.vertices = mesh.vertices;
        existing.normals = mesh.normals;
        existing.subMeshCount = mesh.subMeshCount;
        for (int i = 0; i < mesh.subMeshCount; i++)
            existing.SetTriangles(mesh.GetTriangles(i), i);
        existing.RecalculateBounds();
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(mesh);
        return existing;
    }

    private static void ApplyToPrefabs(
        CharacterVisualRole role,
        Mesh mesh,
        Material skin,
        Material clothing,
        Material boots,
        Material accent)
    {
        string[] prefabPaths = role switch
        {
            CharacterVisualRole.Player => new[] { "Assets/Prefabs/Player.prefab", "Assets/Prefabs/LocalPlayer.prefab" },
            CharacterVisualRole.Guard => new[] { "Assets/Prefabs/AIPrefabs/Guard.prefab" },
            CharacterVisualRole.Prisoner => new[]
            {
                "Assets/Prefabs/NPCs/Prisoner_NPC.prefab",
                "Assets/Prefabs/AIPrefabs/Prisoner.prefab"
            },
            _ => System.Array.Empty<string>()
        };

        Material[] materials = { skin, clothing, boots };

        foreach (string path in prefabPaths)
        {
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"[CharacterVisualSetup] Missing prefab: {path}");
                continue;
            }

            GameObject instance = PrefabUtility.LoadPrefabContents(path);
            try
            {
                string visualName = role == CharacterVisualRole.Player ? "Model" : "BodyVisual";
                Transform visualRoot = FindOrCreateVisualRoot(instance.transform, visualName);
                visualRoot.localPosition = new Vector3(0f, 1f, 0f);
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one;

                ClearVisualChildren(visualRoot);
                RemoveLegacyPrimitiveColliders(visualRoot);

                GameObject body = new GameObject("LowPolyBody");
                body.transform.SetParent(visualRoot, false);
                var filter = body.AddComponent<MeshFilter>();
                var renderer = body.AddComponent<MeshRenderer>();
                filter.sharedMesh = mesh;
                renderer.sharedMaterials = materials;

                AddRoleAccessories(role, visualRoot, accent, clothing, boots);
                RemoveLegacyRootAccessories(instance.transform, role);

                PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(instance);
            }
        }
    }

    private static Transform FindOrCreateVisualRoot(Transform root, string visualName)
    {
        Transform existing = root.Find(visualName);
        if (existing != null)
            return existing;

        var go = new GameObject(visualName);
        go.transform.SetParent(root, false);
        return go.transform;
    }

    private static void ClearVisualChildren(Transform visualRoot)
    {
        for (int i = visualRoot.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(visualRoot.GetChild(i).gameObject);

        Object.DestroyImmediate(visualRoot.GetComponent<MeshFilter>());
        Object.DestroyImmediate(visualRoot.GetComponent<MeshRenderer>());
    }

    private static void RemoveLegacyPrimitiveColliders(Transform visualRoot)
    {
        foreach (var collider in visualRoot.GetComponents<Collider>())
            Object.DestroyImmediate(collider);
    }

    private static void AddRoleAccessories(
        CharacterVisualRole role,
        Transform visualRoot,
        Material accent,
        Material clothing,
        Material boots)
    {
        switch (role)
        {
            case CharacterVisualRole.Guard:
                BuildGuardCap(visualRoot, clothing, boots, accent);
                break;
            case CharacterVisualRole.Player:
                BuildPlayerBandana(visualRoot, accent);
                break;
            case CharacterVisualRole.Prisoner:
                BuildPrisonerIdTag(visualRoot, accent);
                break;
        }
    }

    private static void BuildGuardCap(Transform parent, Material clothing, Material boots, Material accent)
    {
        var capRoot = new GameObject("Cap");
        capRoot.transform.SetParent(parent, false);
        capRoot.transform.localPosition = new Vector3(0f, 0.86f, 0.02f);

        CreateAccessoryBox(capRoot.transform, "CapTop", new Vector3(0f, 0.06f, 0f), new Vector3(0.34f, 0.12f, 0.32f), clothing);
        CreateAccessoryBox(capRoot.transform, "CapBrim", new Vector3(0f, -0.02f, 0.12f), new Vector3(0.38f, 0.04f, 0.22f), boots);
        CreateAccessoryBox(capRoot.transform, "Badge", new Vector3(0f, -0.12f, 0.15f), new Vector3(0.08f, 0.1f, 0.02f), accent);
    }

    private static void BuildPlayerBandana(Transform parent, Material accent)
    {
        var bandana = new GameObject("Bandana");
        bandana.transform.SetParent(parent, false);
        bandana.transform.localPosition = new Vector3(0f, 0.78f, 0.12f);
        CreateAccessoryBox(bandana.transform, "BandanaFold", Vector3.zero, new Vector3(0.26f, 0.08f, 0.06f), accent);
    }

    private static void BuildPrisonerIdTag(Transform parent, Material accent)
    {
        var tag = new GameObject("IdTag");
        tag.transform.SetParent(parent, false);
        tag.transform.localPosition = new Vector3(0f, 0.42f, 0.16f);
        CreateAccessoryBox(tag.transform, "Tag", Vector3.zero, new Vector3(0.12f, 0.16f, 0.02f), accent);
    }

    private static void CreateAccessoryBox(Transform parent, string name, Vector3 localPos, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void RemoveLegacyRootAccessories(Transform root, CharacterVisualRole role)
    {
        if (role != CharacterVisualRole.Guard)
            return;

        Transform legacyHat = root.Find("Hat");
        if (legacyHat != null)
            Object.DestroyImmediate(legacyHat.gameObject);
    }
}
