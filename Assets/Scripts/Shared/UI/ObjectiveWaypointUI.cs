using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>
    /// Screen-space objective marker during mandatory non-compliance: label + distance,
    /// on-screen dot or off-screen edge arrow toward the required stand point.
    /// </summary>
    public class ObjectiveWaypointUI : MonoBehaviour
    {
        private const int SortOrder = 130;
        private const float EdgePadding = 48f;

        private static ObjectiveWaypointUI _instance;

        private CanvasGroup _group;
        private RectTransform _markerRoot;
        private TMP_Text _labelText;
        private Image _onScreenDot;
        private Image _offScreenArrow;
        private PrisonerController _prisoner;
        private Camera _cam;

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

            var rootGo = new GameObject("Marker", typeof(RectTransform));
            rootGo.transform.SetParent(transform, false);
            _markerRoot = (RectTransform)rootGo.transform;
            _markerRoot.sizeDelta = new Vector2(200f, 48f);

            _onScreenDot = CreateImage(rootGo.transform, "Dot", 14f, PrisonUITheme.CautionYellow);
            var dotRt = _onScreenDot.rectTransform;
            dotRt.anchorMin = dotRt.anchorMax = new Vector2(0.5f, 0.5f);
            dotRt.anchoredPosition = new Vector2(0f, 18f);

            _offScreenArrow = CreateImage(rootGo.transform, "Arrow", 28f, PrisonUITheme.CautionYellow);
            var arrowRt = _offScreenArrow.rectTransform;
            arrowRt.anchorMin = arrowRt.anchorMax = new Vector2(0.5f, 0.5f);
            _offScreenArrow.gameObject.SetActive(false);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(rootGo.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0f);
            labelRt.sizeDelta = new Vector2(320f, 28f);
            labelRt.anchoredPosition = new Vector2(0f, -8f);
            _labelText = labelGo.AddComponent<TextMeshProUGUI>();
            _labelText.fontSize = 20f;
            _labelText.fontStyle = FontStyles.Bold;
            _labelText.alignment = TextAlignmentOptions.Center;
            _labelText.color = PrisonUITheme.CautionYellow;

            _markerRoot.gameObject.SetActive(false);
        }

        private static Image CreateImage(Transform parent, string name, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private void LateUpdate()
        {
            if (_cam == null)
                _cam = Camera.main;

            var tm = PrisonTimeManager.Instance;
            if (_prisoner == null)
                _prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);

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
                if (_markerRoot != null)
                    _markerRoot.gameObject.SetActive(false);
                return;
            }

            float distance = Vector3.Distance(_prisoner.transform.position, worldPos);
            string shortLabel = ShortenLabel(label);
            _labelText.text = $"{shortLabel} — {distance:F0}m";

            Vector3 screen = _cam.WorldToScreenPoint(worldPos);
            bool behind = screen.z < 0f;
            float w = Screen.width;
            float h = Screen.height;
            bool onScreen = !behind
                && screen.x >= EdgePadding && screen.x <= w - EdgePadding
                && screen.y >= EdgePadding && screen.y <= h - EdgePadding;

            _markerRoot.gameObject.SetActive(true);

            if (onScreen)
            {
                _onScreenDot.gameObject.SetActive(true);
                _offScreenArrow.gameObject.SetActive(false);
                _markerRoot.position = screen;
            }
            else
            {
                _onScreenDot.gameObject.SetActive(false);
                _offScreenArrow.gameObject.SetActive(true);

                Vector3 dir = (worldPos - _prisoner.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f)
                    dir = _cam.transform.forward;
                dir.Normalize();

                Vector3 camFwd = _cam.transform.forward;
                Vector3 camRight = _cam.transform.right;
                camFwd.y = 0f;
                camRight.y = 0f;
                camFwd.Normalize();
                camRight.Normalize();

                float dx = Vector3.Dot(dir, camRight);
                float dy = Vector3.Dot(dir, camFwd);
                Vector2 edgeDir = new Vector2(dx, dy).normalized;
                if (edgeDir.sqrMagnitude < 0.01f)
                    edgeDir = Vector2.up;

                float padX = EdgePadding;
                float padY = EdgePadding;
                float sx = w * 0.5f + edgeDir.x * (w * 0.5f - padX);
                float sy = h * 0.5f + edgeDir.y * (h * 0.5f - padY);
                _markerRoot.position = new Vector3(sx, sy, 0f);

                float angle = Mathf.Atan2(edgeDir.x, edgeDir.y) * Mathf.Rad2Deg;
                _offScreenArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }
        }

        private static string ShortenLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return "OBJECTIVE";
            int pipe = label.IndexOf('|');
            if (pipe >= 0)
                label = label.Substring(0, pipe).Trim();
            if (label.Length > 28)
                label = label.Substring(0, 28);
            return label.ToUpperInvariant();
        }
    }
}
