using System.Collections.Generic;
using System.Linq;
using Prison.Visuals;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds rigged BlenderKit character prefabs + locomotion animator controller.
/// </summary>
public static class BlenderKitCharacterSetup
{
    public const string PrisonerFbx = "Assets/Models/BlenderKit/Characters/SM_Char_Prisoner.fbx";
    public const string GuardFbx = "Assets/Models/BlenderKit/Characters/SM_Char_Guard.fbx";
    public const string PrisonerPrefab = "Assets/Prefabs/BlenderKit/Char_Prisoner.prefab";
    public const string GuardPrefab = "Assets/Prefabs/BlenderKit/Char_Guard.prefab";
    /// <summary>Legacy shared controller path (pre per-role split); kept as a load fallback.</summary>
    public const string ControllerPath = "Assets/Animations/Characters/Char_Locomotion.controller";
    public const string PrisonerControllerPath = "Assets/Animations/Characters/Char_Locomotion_Prisoner.controller";
    public const string GuardControllerPath = "Assets/Animations/Characters/Char_Locomotion_Guard.controller";

    public static int GenerateCharacterPrefabs()
    {
        EnsureFolders();
        ConfigureCharacterImporter(PrisonerFbx);
        ConfigureCharacterImporter(GuardFbx);

        // Per-role controllers built from each rig's own clips — no cross-rig retargeting.
        var prisonerController = EnsureLocomotionController(PrisonerFbx, PrisonerControllerPath);
        var guardController = EnsureLocomotionController(GuardFbx, GuardControllerPath);
        int count = 0;
        if (CreateCharacterPrefab(PrisonerFbx, PrisonerPrefab, prisonerController)) count++;
        if (CreateCharacterPrefab(GuardFbx, GuardPrefab, guardController)) count++;
        return count;
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder("Assets/Animations/Characters"))
            AssetDatabase.CreateFolder("Assets/Animations", "Characters");
    }

    static void ConfigureCharacterImporter(string fbxPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.autoGenerateAvatarMappingIfUnspecified = true;
        importer.importAnimation = true;
        importer.bakeAxisConversion = false;
        importer.globalScale = 1f;

        // Without explicit frame ranges Unity imports each clip as frames 0-0 —
        // a single-frame pose, so characters never play the actual cycles.
        // Pull the real ranges from the FBX takes.
        var takeInfos = importer.importedTakeInfos;
        var clips = new List<ModelImporterClipAnimation>();
        foreach (var take in new[] { "Idle", "Walk", "Run", "Jump" })
        {
            var clip = new ModelImporterClipAnimation
            {
                name = take,
                takeName = take,
                loopTime = take != "Jump",
                loopPose = take != "Jump",
            };

            if (takeInfos != null)
            {
                foreach (var info in takeInfos)
                {
                    if (info.name != take && !info.name.EndsWith("|" + take, System.StringComparison.Ordinal))
                        continue;
                    float rate = info.sampleRate > 0f ? info.sampleRate : 24f;
                    clip.firstFrame = info.bakeStartTime * rate;
                    clip.lastFrame = info.bakeStopTime * rate;
                    break;
                }
            }

            if (clip.lastFrame <= clip.firstFrame)
                Debug.LogWarning($"[BlenderKitCharacter] Take '{take}' in {fbxPath} has no frame range — clip will be a static pose.");

            clips.Add(clip);
        }

        importer.clipAnimations = clips.ToArray();
        importer.SaveAndReimport();
    }

    static AnimatorController EnsureLocomotionController(string fbxPath, string controllerPath)
    {
        var idle = LoadClip(fbxPath, "Idle");
        var walk = LoadClip(fbxPath, "Walk");
        var run = LoadClip(fbxPath, "Run");
        var jump = LoadClip(fbxPath, "Jump");

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            AssetDatabase.DeleteAsset(controllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var rootStateMachine = controller.layers[0].stateMachine;
        var blendTree = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false,
        };
        blendTree.AddChild(idle, 0f);
        blendTree.AddChild(walk ?? idle, 1.6f);
        blendTree.AddChild(run ?? walk ?? idle, 4.2f);
        AssetDatabase.AddObjectToAsset(blendTree, controller);

        var locomotion = rootStateMachine.AddState("Locomotion", new Vector3(300f, 0f, 0f));
        locomotion.motion = blendTree;
        rootStateMachine.defaultState = locomotion;

        if (jump != null)
        {
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            var jumpState = rootStateMachine.AddState("Jump", new Vector3(300f, 130f, 0f));
            jumpState.motion = jump;

            var toJump = rootStateMachine.AddAnyStateTransition(jumpState);
            toJump.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Jump");
            toJump.hasExitTime = false;
            toJump.duration = 0.05f;
            toJump.canTransitionToSelf = false;

            var backToLocomotion = jumpState.AddTransition(locomotion);
            backToLocomotion.hasExitTime = true;
            backToLocomotion.exitTime = 0.95f;
            backToLocomotion.duration = 0.1f;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    static Transform FindSkeletonRoot(Transform modelRoot)
    {
        if (modelRoot.name.StartsWith("SM_Char", System.StringComparison.Ordinal))
            return modelRoot;

        foreach (var t in modelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("SM_Char", System.StringComparison.Ordinal))
                return t;
            if (t.name == "Root")
                return t;
        }

        return modelRoot;
    }

    static AnimationClip LoadClip(string fbxPath, string clipName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => c.name == clipName
                || c.name.Contains(clipName)
                || c.name.EndsWith("|" + clipName, System.StringComparison.Ordinal));
    }

    static bool CreateCharacterPrefab(string fbxPath, string prefabPath, RuntimeAnimatorController controller)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (source == null)
        {
            Debug.LogError($"[BlenderKitCharacter] Missing FBX: {fbxPath}");
            return false;
        }

        string prefabName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
        var root = new GameObject(prefabName);

        var model = Object.Instantiate(source);
        model.name = "Mesh";
        model.transform.SetParent(root.transform, false);
        BlenderKitAssetSetup.RemapMaterialsPublic(model);

        // The FBX import adds its own Animator to the model — remove it so the
        // prefab has exactly one Animator (on the root, wired below).
        foreach (var redundant in model.GetComponentsInChildren<Animator>(true))
            Object.DestroyImmediate(redundant);

        float height = MeasureHeight(model);
        float targetHeight = CharacterVisualConstants.ColliderHeight;
        if (height > 0.01f)
        {
            float scale = targetHeight / height;
            model.transform.localScale = Vector3.one * scale;
        }

        Transform skeletonRoot = FindSkeletonRoot(model.transform);
        Avatar avatar = LoadAvatar(fbxPath);
        string avatarAssetPath = prefabPath.Replace(".prefab", "_Avatar.asset");
        if (avatar == null && skeletonRoot != null)
        {
            avatar = AvatarBuilder.BuildGenericAvatar(skeletonRoot.gameObject, "");
            if (avatar != null)
            {
                avatar.name = $"{prefabName}_Avatar";
                var existing = AssetDatabase.LoadAssetAtPath<Avatar>(avatarAssetPath);
                if (existing != null)
                    AssetDatabase.DeleteAsset(avatarAssetPath);
                AssetDatabase.CreateAsset(avatar, avatarAssetPath);
                avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarAssetPath);
            }
        }
        if (avatar == null)
            Debug.LogWarning($"[BlenderKitCharacter] Could not build avatar for {fbxPath}");

        var animator = root.AddComponent<Animator>();
        animator.avatar = avatar;
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        if (avatar == null)
            animator.enabled = false;

        root.AddComponent<BlenderKitLocomotionAnimator>();

        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(prefabPath)))
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath)!);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        if (prefab == null)
            Debug.LogError($"[BlenderKitCharacter] Failed to save prefab: {prefabPath}");
        return prefab != null;
    }

    static float MeasureHeight(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 0f;
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b.size.y;
    }

    static Avatar LoadAvatar(string fbxPath)
    {
        var avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
        if (avatar != null) return avatar;

        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return null;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
    }

    public static Avatar LoadAvatarPublic(string fbxPath) => LoadAvatar(fbxPath);

    public static GameObject LoadPrisonerVisual()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrisonerPrefab);
        if (prefab != null) return prefab;
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BlenderKit/SM_Char_Prisoner.prefab");
    }

    public static GameObject LoadGuardVisual()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuardPrefab);
        if (prefab != null) return prefab;
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BlenderKit/SM_Char_Guard.prefab");
    }
}
