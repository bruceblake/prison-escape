using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>
    /// Objective guidance via a fixed bottom HUD panel (readable at all times).
    /// </summary>
    public class ObjectiveWaypointUI : MonoBehaviour
    {
        private const int SortOrder = 130;
        private const float DistanceSmoothTime = 0.18f;

        private static ObjectiveWaypointUI _instance;

        private CanvasGroup _group;
        private RectTransform _bottomHud;
        private TMP_Text _hudTitle;
        private TMP_Text _hudDetail;
        private Image _compassArrow;
        private PrisonerController _prisoner;
        private Camera _cam;

        private float _smoothedDistance;
        private float _distanceVel;
        private bool _hasSmoothedState;

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

            _bottomHud = CreateRect("BottomHud", transform);
            _bottomHud.anchorMin = new Vector2(0.5f, 0f);
            _bottomHud.anchorMax = new Vector2(0.5f, 0f);
            _bottomHud.pivot = new Vector2(0.5f, 0f);
            _bottomHud.anchoredPosition = new Vector2(0f, 118f);
            _bottomHud.sizeDelta = new Vector2(560f, 80f);

            CreateImage(_bottomHud, "Bg", Vector2.zero, new Vector2(560f, 80f),
                new Color(0.04f, 0.06f, 0.08f, 0.92f));

            _compassArrow = CreateImage(_bottomHud, "Arrow", new Vector2(-220f, 0f), new Vector2(28f, 28f),
                PrisonUITheme.CautionYellow);
            _compassArrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            _hudTitle = CreateText(_bottomHud, "Title", new Vector2(0f, 16f), 22f, FontStyles.Bold);
            _hudDetail = CreateText(_bottomHud, "Detail", new Vector2(0f, -14f), 18f, FontStyles.Normal);

            _bottomHud.gameObject.SetActive(false);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 pos, float fontSize, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(520f, 32f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = PrisonUITheme.CautionYellow;
            return tmp;
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
                SetVisible(false);
                return;
            }

            if (!_hasSmoothedState)
            {
                _smoothedDistance = Vector3.Distance(_prisoner.transform.position, worldPos);
                _hasSmoothedState = true;
            }
            else
            {
                float rawDistance = Vector3.Distance(_prisoner.transform.position, worldPos);
                _smoothedDistance = Mathf.SmoothDamp(_smoothedDistance, rawDistance, ref _distanceVel, DistanceSmoothTime);
            }

            string destName = FormatDestinationName(label);
            string bearing = GetCompassBearing(worldPos);
            _hudTitle.text = $"GO TO {destName}";
            _hudDetail.text = string.IsNullOrEmpty(bearing)
                ? $"{_smoothedDistance:F0} meters away"
                : $"{_smoothedDistance:F0}m — head {bearing}";

            UpdateCompassArrow(worldPos);
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (_group != null) _group.alpha = visible ? 1f : 0f;
            if (_bottomHud != null) _bottomHud.gameObject.SetActive(visible);
        }

        private void UpdateCompassArrow(Vector3 worldPos)
        {
            if (_compassArrow == null || _prisoner == null) return;

            Vector3 delta = worldPos - _prisoner.transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.25f)
            {
                _compassArrow.color = new Color(1f, 0.82f, 0.12f, 0.35f);
                return;
            }

            Vector3 camFwd = _cam.transform.forward;
            camFwd.y = 0f;
            if (camFwd.sqrMagnitude < 0.01f) return;
            camFwd.Normalize();

            Vector3 camRight = _cam.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector2 screenDir = new Vector2(Vector3.Dot(delta.normalized, camRight), Vector3.Dot(delta.normalized, camFwd));
            float angle = Mathf.Atan2(screenDir.x, screenDir.y) * Mathf.Rad2Deg;
            _compassArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            _compassArrow.color = PrisonUITheme.CautionYellow;
        }

        private static string FormatDestinationName(string label)
        {
            if (string.IsNullOrEmpty(label))
                return "OBJECTIVE";
            int pipe = label.IndexOf('|');
            if (pipe >= 0)
                label = label.Substring(0, pipe).Trim();
            if (label.StartsWith("GO TO:", System.StringComparison.OrdinalIgnoreCase))
                label = label.Substring(6).Trim();
            if (label.Length > 32)
                label = label.Substring(0, 32);
            return label.ToUpperInvariant();
        }

        private string GetCompassBearing(Vector3 worldPos)
        {
            if (_prisoner == null) return string.Empty;
            Vector3 delta = worldPos - _prisoner.transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.5f) return string.Empty;

            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            if (angle < 22.5f || angle >= 337.5f) return "NORTH";
            if (angle < 67.5f) return "NORTHEAST";
            if (angle < 112.5f) return "EAST";
            if (angle < 157.5f) return "SOUTHEAST";
            if (angle < 202.5f) return "SOUTH";
            if (angle < 247.5f) return "SOUTHWEST";
            if (angle < 292.5f) return "WEST";
            return "NORTHWEST";
        }
    }
}
