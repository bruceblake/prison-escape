using Prison.Social;
using Prison.Visuals;
using TMPro;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Talk Menu entry point for any social actor (inmates and guards) — reworked from the
    /// v1 greet chain (Social Ecosystem &amp; Gangs v3). [F] opens <see cref="SocialInteractionMenu"/>.
    /// Also drives the nameplate Standing-band tint and the Escapists-style overhead markers
    /// (green ! = open favor, coin = trade stock today).
    /// </summary>
    public class PrisonerSocialPresenter : MonoBehaviour, IInteractable
    {
        [Header("Label layout")]
        [Tooltip("Kept for editor tooling compatibility; overhead marker anchor.")]
        [SerializeField] private Vector3 labelLocalOffset = new Vector3(0f, CharacterVisualConstants.SocialLabelHeight, 0f);

        [Header("Raycast (F interaction)")]
        [Tooltip("Physics.Raycast does not hit CharacterController. If no other Collider exists, add a character-sized capsule (non-trigger) so the player can target this NPC.")]
        [SerializeField] private bool addInteractionCapsuleIfMissing = true;
        [SerializeField] private Vector3 capsuleCenter = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private float capsuleRadius = 0.4f;
        [SerializeField] private float capsuleHeight = 1.8f;

        private Transform _markerRoot;
        private TextMeshPro _markerText;
        private Camera _mainCamera;
        private float _nextRefresh;

        public InteractionInputType InputType => InteractionInputType.Press;
        public float HoldDuration => 0f;

        private int ActorId
        {
            get
            {
                var world = SocialWorld.Instance;
                return world != null ? world.GetActorId(gameObject) : SocialTuning.NoActor;
            }
        }

        private void Awake()
        {
            if (addInteractionCapsuleIfMissing && !HasUsableBodyCollider())
                AddInteractionCapsule();
            CreateMarker();
        }

        private void OnEnable()
        {
            if (SocialWorld.Instance != null)
                SocialWorld.Instance.OnPlayerRelationshipChanged += OnRelationshipChanged;
        }

        private void OnDisable()
        {
            if (SocialWorld.Instance != null)
                SocialWorld.Instance.OnPlayerRelationshipChanged -= OnRelationshipChanged;
        }

        private void LateUpdate()
        {
            if (Time.time >= _nextRefresh)
            {
                _nextRefresh = Time.time + 0.75f;
                RefreshBandTint();
                RefreshMarker();
            }

            if (_markerRoot == null) return;
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;
            Vector3 toCamera = _markerRoot.position - _mainCamera.transform.position;
            if (toCamera.sqrMagnitude < 0.0001f) return;
            _markerRoot.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
        }

        private void OnRelationshipChanged(int observer, float trustDelta, float respectDelta, RelationshipRecord record)
        {
            if (observer == ActorId)
                RefreshBandTint();
        }

        private void RefreshBandTint()
        {
            var world = SocialWorld.Instance;
            int actorId = ActorId;
            if (world == null || !world.IsBuilt || actorId == SocialTuning.NoActor) return;

            var label = GetComponent<CharacterNameLabel>();
            if (label == null) return;
            var band = world.Relationships.GetBand(actorId, SocialTuning.PlayerActorId);
            label.SetTint(StandingBandUI.ColorOf(band));
        }

        private void RefreshMarker()
        {
            var world = SocialWorld.Instance;
            int actorId = ActorId;
            if (_markerText == null || world == null || !world.IsBuilt || actorId == SocialTuning.NoActor)
                return;

            var identity = world.GetIdentity(actorId);
            if (identity == null) { _markerText.text = ""; return; }

            bool hasFavor = !identity.isGuard && world.Favors.OpenOfferFor(actorId) != null;
            bool hasStock = !identity.isGuard && world.Trading.HasStockToday(actorId);

            if (hasFavor) _markerText.text = "<color=#4CD24C>!</color>";
            else if (hasStock) _markerText.text = "<color=#F4D03F>$</color>";
            else _markerText.text = "";
        }

        private void CreateMarker()
        {
            _markerRoot = new GameObject("SocialOverheadMarker").transform;
            _markerRoot.SetParent(transform, false);
            _markerRoot.localPosition = labelLocalOffset + new Vector3(0f, 0.55f, 0f);
            _markerRoot.localScale = Vector3.one * 0.06f;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_markerRoot, false);
            _markerText = textGo.AddComponent<TextMeshPro>();
            _markerText.alignment = TextAlignmentOptions.Center;
            _markerText.fontSize = 9f;
            _markerText.fontStyle = FontStyles.Bold;
            _markerText.raycastTarget = false;
            _markerText.outlineWidth = 0.22f;
            _markerText.outlineColor = new Color(0f, 0f, 0f, 0.85f);
            _markerText.text = "";
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

        // ------------------------------------------------------------------ IInteractable

        public string GetInteractionPrompt(PlayerInventory inventory)
        {
            var world = SocialWorld.Instance;
            int actorId = ActorId;
            if (world == null || !world.IsBuilt || actorId == SocialTuning.NoActor)
                return "";

            var identity = world.GetIdentity(actorId);
            if (identity == null) return "";
            string name = world.HasMet(actorId) || identity.isGuard ? identity.DisplayName : "the inmate";
            return $"[F] Talk to {name}";
        }

        public bool CanInteract(PlayerInventory inventory)
        {
            var world = SocialWorld.Instance;
            return world != null && world.IsBuilt && ActorId != SocialTuning.NoActor;
        }

        public void Interact(PlayerInventory inventory)
        {
            int actorId = ActorId;
            if (actorId == SocialTuning.NoActor) return;

            if (SocialTalkGate.TryGetRefusal(gameObject, out string refusal))
            {
                SocialToastUI.Show(refusal);
                return;
            }

            SocialInteractionMenu.Open(actorId);
        }
    }
}
