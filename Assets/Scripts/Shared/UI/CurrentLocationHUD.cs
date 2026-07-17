using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>Top-right current location label (e.g. "CELL 5", "CAFETERIA").</summary>
    public class CurrentLocationHUD : MonoBehaviour
    {
        private const int SortOrder = 121;

        private static CurrentLocationHUD _instance;

        private CanvasGroup _group;
        private TMP_Text _label;
        private PrisonerController _prisoner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawnAfterSceneLoad()
        {
            if (PrisonTimeManager.Instance != null)
                EnsureInstance();
        }

        public static CurrentLocationHUD EnsureInstance()
        {
            if (_instance != null) return _instance;
            var existing = FindAnyObjectByType<CurrentLocationHUD>();
            if (existing != null)
            {
                _instance = existing;
                return _instance;
            }

            var root = new GameObject("CurrentLocationHUD");
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<CurrentLocationHUD>();
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

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var panelRt = (RectTransform)panel.transform;
            // Top-right — clears the bottom hotbar and sits below the routine strip (top-center).
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.anchoredPosition = new Vector2(-16f, -72f);
            panelRt.sizeDelta = new Vector2(280f, 40f);
            panel.GetComponent<Image>().color = PrisonUITheme.CommandStripBackdrop;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(panel.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 4f);
            textRt.offsetMax = new Vector2(-12f, -4f);
            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 22f;
            _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.MidlineRight;
            _label.color = PrisonUITheme.ConcreteGrey;
            _label.text = "—";
        }

        private void LateUpdate()
        {
            if (_group != null)
            {
                float target = UIMenuFocus.IsAnyMenuOpen ? 0.04f : 1f;
                _group.alpha = Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime * 10f);
            }

            if (_prisoner == null)
                _prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
            if (_label == null || _prisoner == null)
                return;

            _label.text = PrisonRoutineLabels.FormatPlayerLocation(_prisoner.GetCurrentLocationLabel()).ToUpperInvariant();
        }
    }
}
