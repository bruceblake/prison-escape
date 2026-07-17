using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>
    /// Screen-space objective guide: projects destination / path corners onto the HUD
    /// (no 3D primitives). Edge arrow when the target is off-screen; breadcrumb dots along
    /// the NavMesh path when visible.
    /// </summary>
    public class ObjectiveWaypointUI : MonoBehaviour
    {
        private const int SortOrder = 130;
        private const float DistanceSmoothTime = 0.18f;
        private const float BeaconHeight = 1.6f;
        private const float BreadcrumbSpacing = 3.5f;
        private const int MaxBreadcrumbs = 12;
        private const float EdgeMargin = 48f;
        private const float DestMarkerSize = 42f;
        private const float CornerMarkerSize = 26f;
        private const float CrumbMarkerSize = 10f;
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
        private Image _nextCornerMarker;
        private Image _edgeArrow;
        private TMP_Text _hintLabel;
        private readonly List<Image> _breadcrumbs = new List<Image>();

        private NavMeshPath _path;
        private Vector3 _lastDest;
        private float _pathRebuildTimer;
        private WaypointWorldGuide _worldGuide;

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

            _path = new NavMeshPath();
            BuildGuideUi();
            _worldGuide = WaypointWorldGuide.Create();
            SetGuideVisible(false);
        }

        private void BuildGuideUi()
        {
            var rootGo = new GameObject("GuideRoot", typeof(RectTransform));
            rootGo.transform.SetParent(transform, false);
            _guideRoot = (RectTransform)rootGo.transform;
            Stretch(_guideRoot);

            _destMarker = CreateMarkerImage("DestMarker", PrisonUITheme.CautionYellow, DestMarkerSize);
            _nextCornerMarker = CreateMarkerImage("NextCorner", new Color(1f, 0.55f, 0.1f, 0.92f), CornerMarkerSize);
            _edgeArrow = CreateMarkerImage("EdgeArrow", PrisonUITheme.CautionYellow, EdgeArrowSize);

            for (int i = 0; i < MaxBreadcrumbs; i++)
            {
                var crumb = CreateMarkerImage($"Breadcrumb_{i}", new Color(1f, 0.82f, 0.2f, 0.55f), CrumbMarkerSize);
                crumb.gameObject.SetActive(false);
                _breadcrumbs.Add(crumb);
            }

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

            bool inCell = PrisonRoutineDestination.ResolveActiveDestination(tm, _prisoner).InCell;
            UpdateGuide(worldPos, label, inCell);
        }

        private void UpdateGuide(Vector3 dest, string label, bool inCell)
        {
            if (_guideRoot == null || _prisoner == null || _cam == null)
                return;

            SetGuideVisible(true);

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

            UpdateHintLabel(label);

            // In-cell or very close: edge arrow toward stand point, beacon only, no path trail.
            if (inCell || _smoothedDistance < 1.25f)
            {
                _nextCornerMarker.gameObject.SetActive(false);
                HideBreadcrumbs();
                _worldGuide?.UpdateGuide(null, dest);
                UpdateEdgeArrow(destWorld, destOnScreen);
                return;
            }

            _pathRebuildTimer -= Time.deltaTime;
            bool destMoved = (_lastDest - dest).sqrMagnitude > 0.25f;
            if (_path == null)
                _path = new NavMeshPath();
            if (_pathRebuildTimer <= 0f || destMoved || _path.status == NavMeshPathStatus.PathInvalid)
            {
                _pathRebuildTimer = 0.75f;
                _lastDest = dest;
                Vector3 from = _prisoner.transform.position;
                if (NavMesh.SamplePosition(from, out NavMeshHit startHit, 2f, NavMesh.AllAreas))
                    from = startHit.position;
                NavMesh.CalculatePath(from, dest, NavMesh.AllAreas, _path);
            }

            Vector3 guideWorld;
            if (_path.status == NavMeshPathStatus.PathInvalid || _path.corners == null || _path.corners.Length < 2)
            {
                guideWorld = Vector3.Lerp(_prisoner.transform.position, dest, 0.25f);
                guideWorld.y = dest.y;
                guideWorld += Vector3.up * 0.15f;
            }
            else
            {
                guideWorld = _path.corners[Mathf.Min(1, _path.corners.Length - 1)];
                for (int i = 1; i < _path.corners.Length; i++)
                {
                    if (Vector3.Distance(_prisoner.transform.position, _path.corners[i]) > 1.1f)
                    {
                        guideWorld = _path.corners[i];
                        break;
                    }
                }
                float bob = 0.05f * Mathf.Sin(Time.time * 5f);
                guideWorld += Vector3.up * (0.2f + bob);
            }

            bool cornerOnScreen = TryWorldToCanvas(guideWorld, out Vector2 cornerCanvas);
            if (cornerOnScreen)
            {
                _nextCornerMarker.rectTransform.anchoredPosition = cornerCanvas;
                _nextCornerMarker.gameObject.SetActive(true);
                Vector3 face = guideWorld - _prisoner.transform.position;
                face.y = 0f;
                if (face.sqrMagnitude > 0.01f)
                    _nextCornerMarker.rectTransform.rotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg);
            }
            else
            {
                _nextCornerMarker.gameObject.SetActive(false);
            }

            UpdateEdgeArrow(guideWorld, cornerOnScreen || destOnScreen);
            PlaceBreadcrumbUi();

            // Physical guidance: route line on the floor + beacon at the destination.
            bool pathValid = _path.status != NavMeshPathStatus.PathInvalid
                && _path.corners != null && _path.corners.Length >= 2;
            _worldGuide?.UpdateGuide(pathValid ? _path.corners : null, dest);
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

            // Hide edge arrow when the on-screen markers are already visible.
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

        private void PlaceBreadcrumbUi()
        {
            if (_path == null || _path.corners == null || _path.corners.Length < 2)
            {
                HideBreadcrumbs();
                return;
            }

            int cornerCount = _path.corners.Length;
            var cum = new float[cornerCount];
            cum[0] = 0f;
            for (int i = 1; i < cornerCount; i++)
                cum[i] = cum[i - 1] + Vector3.Distance(_path.corners[i - 1], _path.corners[i]);

            float total = cum[cornerCount - 1];
            int crumbIndex = 0;
            for (float d = BreadcrumbSpacing; d < total - 1.5f && crumbIndex < _breadcrumbs.Count; d += BreadcrumbSpacing)
            {
                if (d < 1.5f) continue;

                int seg = 1;
                while (seg < cornerCount && cum[seg] < d) seg++;
                if (seg >= cornerCount) break;

                float segStart = cum[seg - 1];
                float segLen = cum[seg] - segStart;
                float t = segLen > 0.001f ? (d - segStart) / segLen : 0f;
                Vector3 p = Vector3.Lerp(_path.corners[seg - 1], _path.corners[seg], t) + Vector3.up * 0.12f;

                if (TryWorldToCanvas(p, out Vector2 local))
                {
                    var crumb = _breadcrumbs[crumbIndex];
                    crumb.rectTransform.anchoredPosition = local;
                    crumb.gameObject.SetActive(true);
                    crumbIndex++;
                }
            }

            for (int i = crumbIndex; i < _breadcrumbs.Count; i++)
                _breadcrumbs[i].gameObject.SetActive(false);
        }

        private void HideBreadcrumbs()
        {
            for (int i = 0; i < _breadcrumbs.Count; i++)
                _breadcrumbs[i].gameObject.SetActive(false);
        }

        private void SetGuideVisible(bool visible)
        {
            if (_group != null)
                _group.alpha = visible ? 1f : 0f;
            if (!visible)
            {
                if (_destMarker != null) _destMarker.gameObject.SetActive(false);
                if (_nextCornerMarker != null) _nextCornerMarker.gameObject.SetActive(false);
                if (_edgeArrow != null) _edgeArrow.gameObject.SetActive(false);
                if (_hintLabel != null) _hintLabel.gameObject.SetActive(false);
                _worldGuide?.SetVisible(false);
                HideBreadcrumbs();
            }
        }
    }
}
