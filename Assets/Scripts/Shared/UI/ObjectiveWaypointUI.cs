using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>
    /// Minimal screen-space objective guide. Shows a single pulsing destination marker when the
    /// target is on-screen, an edge arrow pointing toward it when it is off-screen, and a distance
    /// readout. No NavMesh path, breadcrumbs, or world-space geometry — just a clean HUD pointer.
    /// </summary>
    public class ObjectiveWaypointUI : MonoBehaviour
    {
        private const int SortOrder = 130;
        private const float DistanceSmoothTime = 0.18f;
        private const float BeaconHeight = 1.6f;
        private const float EdgeMargin = 48f;
        private const float DestMarkerSize = 42f;
        private const float EdgeArrowSize = 52f;

        private static ObjectiveWaypointUI _instance;
        private static Sprite _uiSprite;

        private PrisonerController _prisoner;
        private Camera _cam;
        private RectTransform _canvasRt;

        private float _smoothedDistance;
        private float _distanceVel;
        private bool _hasSmoothedState;

        private CanvasGroup _group;
        private RectTransform _guideRoot;
        private Image _destMarker;
        private Image _edgeArrow;
        private TMP_Text _hintLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawnAfterSceneLoad()
        {
            if (PrisonTimeManager.Instance != null)
                EnsureInstance();
        }

        public static ObjectiveWaypointUI EnsureInstance()
        {
            if (_instance != null) return _instance;
            var existing = FindAnyObjectByType<ObjectiveWaypointUI>();
            if (existing != null)
            {
                _instance = existing;
                return _instance;
            }

            var root = new GameObject("ObjectiveWaypointUI");
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<ObjectiveWaypointUI>();
            _instance.Build();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortOrder;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _canvasRt = (RectTransform)transform;

            BuildGuideUi();
            SetGuideVisible(false);
        }

        private void BuildGuideUi()
        {
            var rootGo = new GameObject("GuideRoot", typeof(RectTransform));
            rootGo.transform.SetParent(transform, false);
            _guideRoot = (RectTransform)rootGo.transform;
            Stretch(_guideRoot);

            _destMarker = CreateMarkerImage("DestMarker", PrisonUITheme.CautionYellow, DestMarkerSize);
            _edgeArrow = CreateMarkerImage("EdgeArrow", PrisonUITheme.CautionYellow, EdgeArrowSize);

            var labelGo = new GameObject("HintLabel", typeof(RectTransform));
            labelGo.transform.SetParent(_guideRoot, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.anchoredPosition = new Vector2(0f, 108f);
            labelRt.sizeDelta = new Vector2(520f, 36f);
            _hintLabel = labelGo.AddComponent<TextMeshProUGUI>();
            _hintLabel.alignment = TextAlignmentOptions.Center;
            _hintLabel.fontSize = 22f;
            _hintLabel.color = PrisonUITheme.CautionYellow;
            _hintLabel.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                _hintLabel.font = TMP_Settings.defaultFontAsset;
        }

        private Image CreateMarkerImage(string name, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_guideRoot, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = Vector2.one * size;
            var img = go.GetComponent<Image>();
            img.sprite = GetUiSprite();
            img.color = color;
            img.raycastTarget = false;
            go.SetActive(false);
            return img;
        }

        private static Sprite GetUiSprite()
        {
            if (_uiSprite != null) return _uiSprite;
            var tex = Texture2D.whiteTexture;
            _uiSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _uiSprite;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (_cam == null)
                _cam = Camera.main;

            var tm = PrisonTimeManager.Instance;
            if (_prisoner == null)
                _prisoner = FindAnyObjectByType<PrisonerController>(FindObjectsInactive.Exclude);

            bool showBase = !UIMenuFocus.IsAnyMenuOpen
                && tm != null
                && _prisoner != null
                && _cam != null
                && PrisonRoutineDestination.ShouldShowWaypoint(tm, _prisoner);

            Vector3 worldPos = Vector3.zero;
            string label = string.Empty;
            bool show = showBase && PrisonRoutineDestination.TryGetDestination(tm, _prisoner, out worldPos, out label);

            if (!show)
            {
                _hasSmoothedState = false;
                SetGuideVisible(false);
                return;
            }

            float rawDistance = Vector3.Distance(_prisoner.transform.position, worldPos);
            if (!_hasSmoothedState)
            {
                _smoothedDistance = rawDistance;
                _hasSmoothedState = true;
            }
            else
            {
                _smoothedDistance = Mathf.SmoothDamp(_smoothedDistance, rawDistance, ref _distanceVel, DistanceSmoothTime);
            }

            UpdateGuide(worldPos, label);
        }

        private void UpdateGuide(Vector3 dest, string label)
        {
            if (_guideRoot == null || _prisoner == null || _cam == null)
                return;

            SetGuideVisible(true);
            UpdateHintLabel(label);

            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 3.5f);
            Vector3 destWorld = dest + Vector3.up * BeaconHeight;

            bool destOnScreen = TryWorldToCanvas(destWorld, out Vector2 destCanvas);
            if (destOnScreen)
            {
                _destMarker.rectTransform.anchoredPosition = destCanvas;
                _destMarker.rectTransform.sizeDelta = Vector2.one * (DestMarkerSize * pulse);
                _destMarker.gameObject.SetActive(true);
            }
            else
            {
                _destMarker.gameObject.SetActive(false);
            }

            UpdateEdgeArrow(destWorld, destOnScreen);
        }

        private void UpdateHintLabel(string venueLabel)
        {
            if (_hintLabel == null) return;
            string dist = _smoothedDistance >= 10f
                ? $"{_smoothedDistance:F0} m"
                : $"{_smoothedDistance:F1} m";
            _hintLabel.text = string.IsNullOrEmpty(venueLabel) ? dist : $"{venueLabel}  ·  {dist}";
            _hintLabel.gameObject.SetActive(true);
        }

        private void UpdateEdgeArrow(Vector3 worldTarget, bool targetVisibleOnScreen)
        {
            if (_edgeArrow == null) return;

            // Hide the edge arrow when the destination marker itself is on-screen.
            if (targetVisibleOnScreen)
            {
                _edgeArrow.gameObject.SetActive(false);
                return;
            }

            Vector3 screen = _cam.WorldToScreenPoint(worldTarget);
            if (screen.z < 0f)
                screen *= -1f;

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = new Vector2(screen.x, screen.y) - center;
            if (dir.sqrMagnitude < 1f)
            {
                _edgeArrow.gameObject.SetActive(false);
                return;
            }

            dir.Normalize();
            float halfW = Screen.width * 0.5f - EdgeMargin;
            float halfH = Screen.height * 0.5f - EdgeMargin;
            float scale = 1f / Mathf.Max(Mathf.Abs(dir.x) / halfW, Mathf.Abs(dir.y) / halfH);
            Vector2 edgeScreen = center + dir * scale;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, edgeScreen, null, out Vector2 local))
            {
                _edgeArrow.gameObject.SetActive(false);
                return;
            }

            _edgeArrow.rectTransform.anchoredPosition = local;
            _edgeArrow.rectTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            _edgeArrow.gameObject.SetActive(true);
        }

        private bool TryWorldToCanvas(Vector3 worldPos, out Vector2 canvasLocal)
        {
            canvasLocal = default;
            if (_cam == null || _canvasRt == null) return false;

            Vector3 screen = _cam.WorldToScreenPoint(worldPos);
            if (screen.z <= 0f) return false;

            const float margin = 8f;
            if (screen.x < margin || screen.x > Screen.width - margin
                || screen.y < margin || screen.y > Screen.height - margin)
                return false;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRt, screen, null, out canvasLocal);
        }

        private void SetGuideVisible(bool visible)
        {
            if (_group != null)
                _group.alpha = visible ? 1f : 0f;
            if (!visible)
            {
                if (_destMarker != null) _destMarker.gameObject.SetActive(false);
                if (_edgeArrow != null) _edgeArrow.gameObject.SetActive(false);
                if (_hintLabel != null) _hintLabel.gameObject.SetActive(false);
            }
        }
    }
}
