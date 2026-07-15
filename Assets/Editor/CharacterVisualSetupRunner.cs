using Prison.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Wires BlenderKit rigged characters into player, guard, and NPC prefabs.
/// Menu: Prison / Setup Character Visuals
/// </summary>
public static class CharacterVisualSetupRunner
{
    private const string MaterialFolder = "Assets/Materials/Characters";

    private struct RolePalette
    {
        public Color Clothing;
        public Color Accent;
        public Color Boots;
        public string DefaultName;
    }

    [MenuItem("Prison/Setup Character Visuals")]
    public static void Run()
    {
        EnsureFolders();
        BlenderKitCharacterSetup.GenerateCharacterPrefabs();

        Material skin = EnsureMaterial("Char_Skin", new Color(0.82f, 0.62f, 0.48f), 0.05f);

        var palettes = new[]
        {
            (CharacterVisualRole.Player, new RolePalette
            {
                Clothing = new Color(0.92f, 0.48f, 0.18f),
                Accent = new Color(0.95f, 0.95f, 0.92f),
                Boots = new Color(0.55f, 0.55f, 0.58f),
                DefaultName = "You"
            }),
            (CharacterVisualRole.Guard, new RolePalette
            {
                Clothing = new Color(0.14f, 0.24f, 0.42f),
                Accent = new Color(0.78f, 0.62f, 0.18f),
                Boots = new Color(0.08f, 0.08f, 0.1f),
                DefaultName = "Guard"
            }),
            (CharacterVisualRole.Prisoner, new RolePalette
            {
                Clothing = new Color(0.92f, 0.48f, 0.18f),
                Accent = new Color(0.95f, 0.95f, 0.92f),
                Boots = new Color(0.55f, 0.55f, 0.58f),
                DefaultName = "Inmate"
            })
        };

        foreach (var (role, palette) in palettes)
        {
            Material clothing = EnsureMaterial($"Char_Clothing_{role}", palette.Clothing, 0.08f);
            Material roleBoots = EnsureMaterial($"Char_Boots_{role}", palette.Boots, 0.12f);
            Material roleAccent = EnsureMaterial($"Char_Accent_{role}", palette.Accent, 0.1f);
            ApplyToPrefabs(role, skin, clothing, roleBoots, roleAccent, palette.DefaultName);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CharacterVisualSetup] BlenderKit characters wired (~{CharacterVisualConstants.ColliderHeight:0.0#}m tall).");
    }

    private static void EnsureFolders()
    {
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

    private static void ApplyToPrefabs(
        CharacterVisualRole role,
        Material skin,
        Material clothing,
        Material boots,
        Material accent,
        string defaultName)
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

        GameObject kitSource = role == CharacterVisualRole.Guard
            ? BlenderKitCharacterSetup.LoadGuardVisual()
            : BlenderKitCharacterSetup.LoadPrisonerVisual();

        if (kitSource == null)
        {
            Debug.LogError("[CharacterVisualSetup] Missing BlenderKit character prefab — run Prison → Assets → Setup BlenderKit first.");
            return;
        }

        foreach (string path in prefabPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[CharacterVisualSetup] Missing prefab: {path}");
                continue;
            }

            GameObject instance = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RemoveLegacyRootVisuals(instance.transform);
                FixCharacterPhysics(instance.transform);

                string visualName = role == CharacterVisualRole.Player ? "Model" : "BodyVisual";
                Transform visualRoot = FindOrCreateVisualRoot(instance.transform, visualName);
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one;

                ClearVisualChildren(visualRoot);

                var kitInstance = (GameObject)PrefabUtility.InstantiatePrefab(kitSource, visualRoot);
                kitInstance.name = "KitVisual";
                kitInstance.transform.localPosition = Vector3.zero;
                kitInstance.transform.localRotation = Quaternion.identity;
                kitInstance.transform.localScale = Vector3.one;

                RemapKitMaterials(kitInstance, skin, clothing, boots, accent);

                Object.DestroyImmediate(visualRoot.GetComponent<LowPolyLocomotionAnimator>());
                Object.DestroyImmediate(visualRoot.GetComponent<BlenderKitLocomotionAnimator>());

                EnsureKitRig(kitInstance, role);

                EnsureNameLabel(instance.transform, defaultName);
                FixRoleAttachments(instance.transform, role);
                RemoveLegacyRootAccessories(instance.transform, role);

                PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(instance);
            }
        }
    }

    static void EnsureKitRig(GameObject kitRoot, CharacterVisualRole role)
    {
        bool isGuard = role == CharacterVisualRole.Guard;

        var animator = kitRoot.GetComponent<Animator>();
        if (animator == null)
            animator = kitRoot.GetComponentInChildren<Animator>();
        if (animator == null)
            animator = kitRoot.AddComponent<Animator>();

        // Exactly one Animator per character — strip importer leftovers on children.
        foreach (var extra in kitRoot.GetComponentsInChildren<Animator>(true))
            if (extra != animator)
                Object.DestroyImmediate(extra);

        if (animator.avatar == null)
        {
            string avatarPath = isGuard
                ? BlenderKitCharacterSetup.GuardPrefab.Replace(".prefab", "_Avatar.asset")
                : BlenderKitCharacterSetup.PrisonerPrefab.Replace(".prefab", "_Avatar.asset");
            animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath)
                ?? BlenderKitCharacterSetup.LoadAvatarPublic(isGuard
                    ? BlenderKitCharacterSetup.GuardFbx
                    : BlenderKitCharacterSetup.PrisonerFbx);
        }

        string controllerPath = isGuard
            ? BlenderKitCharacterSetup.GuardControllerPath
            : BlenderKitCharacterSetup.PrisonerControllerPath;
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath)
            ?? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BlenderKitCharacterSetup.ControllerPath);
        if (controller != null)
            animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        if (animator.avatar == null)
            animator.enabled = false;

        if (kitRoot.GetComponent<BlenderKitLocomotionAnimator>() == null)
            kitRoot.AddComponent<BlenderKitLocomotionAnimator>();
    }

    static void RemapKitMaterials(GameObject kitRoot, Material skin, Material clothing, Material boots, Material accent)
    {
        foreach (var renderer in kitRoot.GetComponentsInChildren<Renderer>(true))
        {
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                string n = mats[i]?.name ?? "";
                if (n.Contains("Skin")) mats[i] = skin;
                else if (n.Contains("Boot")) mats[i] = boots;
                else if (n.Contains("Accent") || n.Contains("Stripe") || n.Contains("Band")) mats[i] = accent;
                else if (n.Contains("Clothing") || n.Contains("Prisoner") || n.Contains("Guard") || n.Contains("Uniform"))
                    mats[i] = clothing;
            }

            renderer.sharedMaterials = mats;
        }
    }

    private static void EnsureNameLabel(Transform root, string defaultName)
    {
        var label = root.GetComponent<CharacterNameLabel>();
        if (label == null)
            label = root.gameObject.AddComponent<CharacterNameLabel>();

        label.ApplyScaledLayout();
        if (string.IsNullOrWhiteSpace(label.DisplayName) || label.DisplayName == "Character")
            label.SetDisplayName(defaultName);
    }

    private static void FixRoleAttachments(Transform root, CharacterVisualRole role)
    {
        float scale = CharacterVisualConstants.VisualScale;

        switch (role)
        {
            case CharacterVisualRole.Guard:
            {
                Transform eyes = root.Find("Eyes");
                if (eyes != null)
                    eyes.localPosition = new Vector3(0f, CharacterVisualConstants.EyeHeight, 0.473f * scale);
                break;
            }
            case CharacterVisualRole.Player:
            {
                Vector3 cameraPos = new Vector3(0f, CharacterVisualConstants.EyeHeight, CharacterVisualConstants.CameraForwardOffset);
                Transform camProxy = root.Find("CamProxy");
                if (camProxy != null)
                    camProxy.localPosition = cameraPos;

                Transform camera = root.Find("Camera");
                if (camera != null)
                    camera.localPosition = cameraPos;
                break;
            }
            case CharacterVisualRole.Prisoner:
            {
                var social = root.GetComponent<Prison.PrisonerSocialPresenter>();
                if (social != null)
                {
                    var so = new SerializedObject(social);
                    so.FindProperty("labelLocalOffset").vector3Value =
                        new Vector3(0f, CharacterVisualConstants.SocialLabelHeight, 0f);
                    so.FindProperty("capsuleCenter").vector3Value =
                        new Vector3(0f, CharacterVisualConstants.ColliderCenterY, 0f);
                    so.FindProperty("capsuleHeight").floatValue = CharacterVisualConstants.ColliderHeight;
                    so.FindProperty("capsuleRadius").floatValue = CharacterVisualConstants.ColliderRadius;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                break;
            }
        }
    }

    private static void RemoveLegacyRootVisuals(Transform root)
    {
        Object.DestroyImmediate(root.GetComponent<MeshFilter>());
        Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
    }

    private static void FixCharacterPhysics(Transform root)
    {
        float height = CharacterVisualConstants.ColliderHeight;
        float radius = CharacterVisualConstants.ColliderRadius;
        float centerY = CharacterVisualConstants.ColliderCenterY;

        var capsule = root.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.height = height;
            capsule.radius = radius;
            capsule.center = new Vector3(0f, centerY, 0f);
        }

        var agent = root.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.height = height;
            agent.radius = radius;
            agent.baseOffset = 0f;
        }

        var controller = root.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.height = height;
            controller.radius = radius;
            controller.center = new Vector3(0f, centerY, 0f);
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
        Object.DestroyImmediate(visualRoot.GetComponent<LowPolyLocomotionAnimator>());
        Object.DestroyImmediate(visualRoot.GetComponent<BlenderKitLocomotionAnimator>());
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
