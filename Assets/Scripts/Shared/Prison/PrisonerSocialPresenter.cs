using System.Text;
using Prison.Visuals;
using TMPro;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// World label (name, affinity) + [F] greet via existing <see cref="PlayerInteractor"/> / <see cref="IInteractable"/> ray.
    /// Add to the same GameObject as <see cref="PrisonerAI"/>. Use a <see cref="SocialManager"/> in the scene.
    /// </summary>
    [RequireComponent(typeof(PrisonerAI))]
    public class PrisonerSocialPresenter : MonoBehaviour, IInteractable
    {
        [Header("Display")]
        [SerializeField] private string displayName = "Inmate";
        [SerializeField] [TextArea(1, 2)] private string personalitySubtitle = "";

        [Header("Label layout")]
        [SerializeField] private Vector3 labelLocalOffset = new Vector3(0f, CharacterVisualConstants.SocialLabelHeight, 0f);
        [Tooltip("Scales the whole world label. Smaller = smaller text in world space.")]
        [SerializeField] private float labelRootUniformScale = 0.05f;
        [SerializeField] private float lineFontSize = 3.2f;

        [Header("Raycast (F interaction)")]
        [Tooltip("Physics.Raycast does not hit CharacterController. If no other Collider exists, add a character-sized capsule (non-trigger) so the player can target this NPC.")]
        [SerializeField] private bool addInteractionCapsuleIfMissing = true;
        [SerializeField] private Vector3 capsuleCenter = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private float capsuleRadius = 0.4f;
        [SerializeField] private float capsuleHeight = 1.8f;

        private PrisonerAI _ai;
        private Transform _labelRoot;
        private TextMeshPro _text;
        private Camera _mainCamera;

        public InteractionInputType InputType => InteractionInputType.Press;
        public float HoldDuration => 0f;

        public void SetRuntimeLabel(string name, string subtitle = null)
        {
            if (!string.IsNullOrEmpty(name))
                displayName = name;

            var nameLabel = GetComponent<CharacterNameLabel>();
            if (nameLabel != null)
                nameLabel.SetDisplayName(displayName);

            personalitySubtitle = subtitle ?? "";
            RefreshText();
        }

        private void Awake()
        {
            _ai = GetComponent<PrisonerAI>();
            if (addInteractionCapsuleIfMissing && !HasUsableBodyCollider())
                AddInteractionCapsule();
            CreateLabel();
        }

        private void OnEnable()
        {
            if (SocialManager.Instance != null)
                SocialManager.Instance.OnAffinityChanged += OnAffinityChanged;
        }

        private void OnDisable()
        {
            if (SocialManager.Instance != null)
                SocialManager.Instance.OnAffinityChanged -= OnAffinityChanged;
        }

        private void Start()
        {
            RefreshText();
        }

        private void LateUpdate()
        {
            if (_labelRoot == null) return;
            if (_mainCamera == null)
                _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Vector3 toCamera = _labelRoot.position - _mainCamera.transform.position;
            if (toCamera.sqrMagnitude < 0.0001f)
                return;

            _labelRoot.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
        }

        private void OnAffinityChanged(int cellIndex, float newValue, float delta)
        {
            if (_ai != null && cellIndex == _ai.cellIndex)
                RefreshText();
        }

        private void CreateLabel()
        {
            _labelRoot = new GameObject("WorldSocialLabel").transform;
            _labelRoot.SetParent(transform, false);
            _labelRoot.localPosition = labelLocalOffset;
            _labelRoot.localRotation = Quaternion.identity;
            _labelRoot.localScale = Vector3.one * labelRootUniformScale;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_labelRoot, false);
            _text = textGo.AddComponent<TextMeshPro>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            _text.fontSize = lineFontSize;
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.raycastTarget = false;
            _text.outlineWidth = 0.2f;
            _text.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        }

        private void RefreshText()
        {
            if (_text == null) return;

            var pers = SocialManager.Instance != null
                ? SocialManager.Instance.GetPersonality(_ai.cellIndex)
                : null;
            float aff = SocialManager.Instance != null
                ? SocialManager.Instance.GetAffinity(_ai.cellIndex)
                : 0f;

            string secondLine = "";
            if (!string.IsNullOrWhiteSpace(personalitySubtitle))
                secondLine = $"<size=70%><color=#aaaaaa>{personalitySubtitle}</color></size>\n";
            else if (pers != null && !string.IsNullOrWhiteSpace(pers.personalityName))
                secondLine = $"<size=70%><color=#aaaaaa>{pers.personalityName}</color></size>\n";

            string bar = BuildAffinityBar(aff);
            string sign = aff >= 0f ? "+" : "";

            bool hasNameLabel = GetComponent<CharacterNameLabel>() != null;
            var sb = new StringBuilder();
            if (!hasNameLabel)
                sb.AppendLine($"<b>{displayName}</b>");

            if (!string.IsNullOrEmpty(secondLine))
                sb.Append(secondLine);

            sb.Append($"<size=90%>Affinity: {sign}{aff:0}</size>\n");
            sb.Append($"<size=75%><color=#99ccff>{bar}</color></size>");
            _text.text = sb.ToString();
        }

        private static string BuildAffinityBar(float affinity)
        {
            const int segments = 10;
            int filled = Mathf.RoundToInt(Mathf.InverseLerp(SocialMath.MinAffinity, SocialMath.MaxAffinity, affinity) * segments);
            filled = Mathf.Clamp(filled, 0, segments);
            var sb = new StringBuilder(segments);
            for (int i = 0; i < segments; i++)
                sb.Append(i < filled ? "█" : "░");
            return sb.ToString();
        }

        private bool HasUsableBodyCollider()
        {
            var colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                var c = colliders[i];
                if (c == null || !c.enabled) continue;
                if (c is CharacterController) continue;
                if (c.isTrigger) continue;
                return true;
            }
            return false;
        }

        private void AddInteractionCapsule()
        {
            var proxy = new GameObject("InteractionBody");
            proxy.transform.SetParent(transform, false);
            proxy.layer = gameObject.layer;
            var cap = proxy.AddComponent<CapsuleCollider>();
            cap.center = capsuleCenter;
            cap.radius = capsuleRadius;
            cap.height = capsuleHeight;
            cap.isTrigger = false;
        }

        public string GetInteractionPrompt(PlayerInventory inventory)
        {
            if (SocialManager.Instance == null)
                return displayName;

            int cell = _ai.cellIndex;
            var favor = SocialManager.Instance.GetActiveFavorInfo(cell);
            if (favor.HasFavor && favor.Definition != null && favor.Definition.requiredItem != null)
            {
                if (inventory != null && inventory.HasItem(favor.Definition.requiredItem, 1))
                    return $"[F] Deliver {favor.Definition.requiredItem.itemName}";
                return $"[F] Needs {favor.Definition.requiredItem.itemName}";
            }

            if (SocialManager.Instance.IsGreetingBlockedByPhaseCooldown(cell))
                return "[F] Busy...";

            return "[F] Greet (+2)";
        }

        public bool CanInteract(PlayerInventory inventory)
        {
            if (SocialManager.Instance == null) return false;

            int cell = _ai.cellIndex;
            var favor = SocialManager.Instance.GetActiveFavorInfo(cell);
            if (favor.HasFavor && favor.Definition != null && favor.Definition.requiredItem != null)
                return inventory != null && inventory.HasItem(favor.Definition.requiredItem, 1);
            if (SocialManager.Instance.IsGreetingBlockedByPhaseCooldown(cell))
                return false;
            return true;
        }

        public void Interact(PlayerInventory inventory)
        {
            if (SocialManager.Instance == null)
            {
                Debug.LogWarning("[PrisonerSocialPresenter] No SocialManager in scene.");
                return;
            }

            int cell = _ai.cellIndex;
            var favor = SocialManager.Instance.GetActiveFavorInfo(cell);
            if (favor.HasFavor && favor.Definition != null && favor.Definition.requiredItem != null)
            {
                if (SocialManager.Instance.TryCompleteFavor(cell, inventory, out _))
                {
                    RefreshText();
                    return;
                }
            }

            if (SocialManager.Instance.IsGreetingBlockedByPhaseCooldown(cell))
                return;

            SocialManager.Instance.ChangeAffinity(cell, SocialActionType.Greeting);
        }
    }
}
