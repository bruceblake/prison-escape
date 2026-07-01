using UnityEngine;

namespace Prison.Visuals
{
    /// <summary>
    /// Builds a hierarchical low-poly character rig from primitive boxes (for procedural animation).
    /// </summary>
    public static class LowPolyCharacterRigBuilder
    {
        public struct RigResult
        {
            public Transform AnimRoot;
            public Transform Torso;
            public Transform Head;
            public Transform LeftLegPivot;
            public Transform RightLegPivot;
            public Transform LeftKneePivot;
            public Transform RightKneePivot;
            public Transform LeftArmPivot;
            public Transform RightArmPivot;
            public Transform LeftElbowPivot;
            public Transform RightElbowPivot;
        }

        public static RigResult Build(Transform bodyVisual, CharacterVisualRole role, Material skin, Material clothing, Material boots, Material accent)
        {
            var animRoot = CreateEmpty(bodyVisual, "AnimRoot", Vector3.zero);

            var leftLegPivot = CreateEmpty(animRoot, "LeftLegPivot", new Vector3(-0.13f, -0.05f, 0f));
            var leftKneePivot = CreateEmpty(leftLegPivot, "LeftKneePivot", new Vector3(0f, -0.22f, 0f));
            CreateBox(leftLegPivot, "LeftUpperLeg", Vector3.zero, new Vector3(0.17f, 0.44f, 0.17f), clothing);
            CreateBox(leftKneePivot, "LeftLowerLeg", new Vector3(0f, -0.22f, 0f), new Vector3(0.16f, 0.44f, 0.16f), clothing);
            CreateBox(leftKneePivot, "LeftFoot", new Vector3(0f, -0.46f, 0.03f), new Vector3(0.2f, 0.12f, 0.3f), boots);

            var rightLegPivot = CreateEmpty(animRoot, "RightLegPivot", new Vector3(0.13f, -0.05f, 0f));
            var rightKneePivot = CreateEmpty(rightLegPivot, "RightKneePivot", new Vector3(0f, -0.22f, 0f));
            CreateBox(rightLegPivot, "RightUpperLeg", Vector3.zero, new Vector3(0.17f, 0.44f, 0.17f), clothing);
            CreateBox(rightKneePivot, "RightLowerLeg", new Vector3(0f, -0.22f, 0f), new Vector3(0.16f, 0.44f, 0.16f), clothing);
            CreateBox(rightKneePivot, "RightFoot", new Vector3(0f, -0.46f, 0.03f), new Vector3(0.2f, 0.12f, 0.3f), boots);

            var torso = CreateBox(animRoot, "Torso", new Vector3(0f, 0.34f, 0f), new Vector3(0.46f, 0.52f, 0.26f), clothing);
            CreateBox(animRoot, "Pelvis", new Vector3(0f, -0.02f, 0f), new Vector3(0.38f, 0.22f, 0.24f), clothing);
            var head = CreateBox(animRoot, "Head", new Vector3(0f, 0.82f, 0.02f), new Vector3(0.3f, 0.32f, 0.28f), skin);
            CreateBox(animRoot, "Neck", new Vector3(0f, 0.62f, 0f), new Vector3(0.14f, 0.1f, 0.14f), skin);

            var leftArmPivot = CreateEmpty(animRoot, "LeftArmPivot", new Vector3(-0.36f, 0.38f, 0f));
            var leftElbowPivot = CreateEmpty(leftArmPivot, "LeftElbowPivot", new Vector3(0f, -0.18f, 0f));
            CreateBox(leftArmPivot, "LeftUpperArm", Vector3.zero, new Vector3(0.14f, 0.36f, 0.14f), clothing);
            CreateBox(leftElbowPivot, "LeftLowerArm", new Vector3(0f, -0.17f, 0f), new Vector3(0.12f, 0.34f, 0.12f), clothing);
            CreateBox(leftElbowPivot, "LeftHand", new Vector3(0f, -0.36f, 0f), new Vector3(0.11f, 0.12f, 0.1f), skin);

            var rightArmPivot = CreateEmpty(animRoot, "RightArmPivot", new Vector3(0.36f, 0.38f, 0f));
            var rightElbowPivot = CreateEmpty(rightArmPivot, "RightElbowPivot", new Vector3(0f, -0.18f, 0f));
            CreateBox(rightArmPivot, "RightUpperArm", Vector3.zero, new Vector3(0.14f, 0.36f, 0.14f), clothing);
            CreateBox(rightElbowPivot, "RightLowerArm", new Vector3(0f, -0.17f, 0f), new Vector3(0.12f, 0.34f, 0.12f), clothing);
            CreateBox(rightElbowPivot, "RightHand", new Vector3(0f, -0.36f, 0f), new Vector3(0.11f, 0.12f, 0.1f), skin);

            AddRoleExtras(role, animRoot, torso, clothing, boots, accent);

            rig.AnimRoot.localScale = Vector3.one * CharacterVisualConstants.VisualScale;

            return new RigResult
            {
                AnimRoot = animRoot,
                Torso = torso,
                Head = head,
                LeftLegPivot = leftLegPivot,
                RightLegPivot = rightLegPivot,
                LeftKneePivot = leftKneePivot,
                RightKneePivot = rightKneePivot,
                LeftArmPivot = leftArmPivot,
                RightArmPivot = rightArmPivot,
                LeftElbowPivot = leftElbowPivot,
                RightElbowPivot = rightElbowPivot
            };
        }

        private static void AddRoleExtras(CharacterVisualRole role, Transform animRoot, Transform torso, Material clothing, Material boots, Material accent)
        {
            switch (role)
            {
                case CharacterVisualRole.Guard:
                    CreateBox(animRoot, "GuardVest", new Vector3(0f, 0.36f, 0.02f), new Vector3(0.48f, 0.46f, 0.28f), clothing);
                    CreateBox(animRoot, "GuardBelt", new Vector3(0f, -0.02f, 0.14f), new Vector3(0.4f, 0.1f, 0.06f), boots);
                    CreateBox(animRoot, "LeftEpaulette", new Vector3(-0.28f, 0.52f, 0f), new Vector3(0.12f, 0.06f, 0.18f), accent);
                    CreateBox(animRoot, "RightEpaulette", new Vector3(0.28f, 0.52f, 0f), new Vector3(0.12f, 0.06f, 0.18f), accent);

                    var capRoot = CreateEmpty(animRoot, "Cap", new Vector3(0f, 0.86f, 0.02f));
                    CreateBox(capRoot, "CapTop", new Vector3(0f, 0.06f, 0f), new Vector3(0.34f, 0.12f, 0.32f), clothing);
                    CreateBox(capRoot, "CapBrim", new Vector3(0f, -0.02f, 0.12f), new Vector3(0.38f, 0.04f, 0.22f), boots);
                    CreateBox(capRoot, "Badge", new Vector3(0f, -0.12f, 0.15f), new Vector3(0.08f, 0.1f, 0.02f), accent);
                    break;

                case CharacterVisualRole.Player:
                    CreateBox(torso, "PlayerVest", new Vector3(0f, 0f, 0.15f), new Vector3(0.34f, 0.4f, 0.05f), accent);
                    CreateBox(animRoot, "Bandana", new Vector3(0f, 0.78f, 0.12f), new Vector3(0.26f, 0.08f, 0.06f), accent);
                    break;

                case CharacterVisualRole.Prisoner:
                    CreateBox(torso, "ChestPocket", new Vector3(-0.12f, -0.06f, 0.14f), new Vector3(0.1f, 0.12f, 0.03f), accent);
                    CreateBox(torso, "StripeTop", new Vector3(0f, 0.04f, 0.14f), new Vector3(0.44f, 0.05f, 0.02f), accent);
                    CreateBox(torso, "StripeMid", new Vector3(0f, -0.1f, 0.14f), new Vector3(0.44f, 0.05f, 0.02f), accent);
                    CreateBox(torso, "StripeBottom", new Vector3(0f, -0.24f, 0.14f), new Vector3(0.44f, 0.05f, 0.02f), accent);
                    break;
            }
        }

        private static Transform CreateEmpty(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private static Transform CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            if (material != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go.transform;
        }
    }
}
