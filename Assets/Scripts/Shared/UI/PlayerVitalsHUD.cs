using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>
    /// Bottom-left vitals panel: cash, mental health, physical health, strength.
    /// Runtime-built; always visible during gameplay.
    /// </summary>
    public class PlayerVitalsHUD : MonoBehaviour
    {
        private const int SortOrder = 120;

        private static PlayerVitalsHUD _instance;

        private CanvasGroup _group;
        private TMP_Text _cashText;
        private Image _mentalFill;
        private Image _physicalFill;
        private Image _strengthFill;
        private TMP_Text _mentalValue;
        private TMP_Text _physicalValue;
        private TMP_Text _strengthValue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawnAfterSceneLoad()
        {
            if (PrisonTimeManager.Instance != null)
                EnsureInstance();
        }

        public static PlayerVitalsHUD EnsureInstance()
        {
            if (_instance != null) return _instance;
            var existing = FindAnyObjectByType<PlayerVitalsHUD>();
            if (existing != null)
            {
                _instance = existing;
                return _instance;
            }

            var root = new GameObject("PlayerVitalsHUD");
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<PlayerVitalsHUD>();
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
            Unsubscribe();
            if (_instance == this)
                _instance = null;
        }

        private void Start()
        {
            Subscribe();
            RefreshAll();
        }

        private void LateUpdate()
        {
            TrySubscribeWallet();

            if (_group != null)
            {
                float target = UIMenuFocus.IsAnyMenuOpen ? 0.04f : 1f;
                _group.alpha = Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime * 10f);
            }
        }

        private void Subscribe()
        {
            PlayerStats.EnsureInstance();
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.StatsChanged += OnStatsChanged;

            TrySubscribeWallet();
        }

        private bool _walletSubscribed;

        private void TrySubscribeWallet()
        {
            if (_walletSubscribed || PlayerWallet.Instance == null)
                return;
            PlayerWallet.Instance.OnBalanceChanged += OnWalletChanged;
            _walletSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.StatsChanged -= OnStatsChanged;
            if (_walletSubscribed && PlayerWallet.Instance != null)
                PlayerWallet.Instance.OnBalanceChanged -= OnWalletChanged;
            _walletSubscribed = false;
        }

        private void OnStatsChanged(float _, float __, float ___) => RefreshBars();
        private void OnWalletChanged(float _, float __) => RefreshCash();

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
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0f, 0f);
            panelRt.pivot = new Vector2(0f, 0f);
            panelRt.anchoredPosition = new Vector2(16f, 16f);
            panelRt.sizeDelta = new Vector2(260f, 168f);
            panel.GetComponent<Image>().color = PrisonUITheme.CommandStripBackdrop;

            _cashText = CreateLabel(panel.transform, "Cash", "$0.00", 26f, new Vector2(12f, -10f), 236f, 32f,
                TextAlignmentOptions.MidlineLeft, new Color(0.92f, 0.96f, 1f));

            _mentalFill = CreateBar(panel.transform, "Mental", "MENTAL", new Color(0.45f, 0.65f, 0.95f), -48f, out _mentalValue);
            _physicalFill = CreateBar(panel.transform, "Physical", "PHYSICAL", new Color(0.55f, 0.85f, 0.55f), -88f, out _physicalValue);
            _strengthFill = CreateBar(panel.transform, "Strength", "STRENGTH", new Color(0.95f, 0.6f, 0.35f), -128f, out _strengthValue);
        }

        private static TMP_Text CreateLabel(Transform parent, string name, string text, float size, Vector2 pos, float width, float height,
            TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, height);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

        private static Image CreateBar(Transform parent, string id, string title, Color fillColor, float y, out TMP_Text valueText)
        {
            CreateLabel(parent, id + "Title", title, 14f, new Vector2(12f, y), 120f, 18f,
                TextAlignmentOptions.MidlineLeft, new Color(0.75f, 0.78f, 0.82f));

            var trackGo = new GameObject(id + "Track", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(parent, false);
            var trackRt = (RectTransform)trackGo.transform;
            trackRt.anchorMin = trackRt.anchorMax = new Vector2(0f, 1f);
            trackRt.pivot = new Vector2(0f, 1f);
            trackRt.anchoredPosition = new Vector2(12f, y - 22f);
            trackRt.sizeDelta = new Vector2(180f, 10f);
            trackGo.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.12f, 0.9f);

            var fillGo = new GameObject(id + "Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fill = fillGo.GetComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            valueText = CreateLabel(parent, id + "Value", "100", 13f, new Vector2(198f, y - 20f), 50f, 18f,
                TextAlignmentOptions.MidlineRight, new Color(0.85f, 0.88f, 0.9f));
            return fill;
        }

        private void RefreshAll()
        {
            RefreshCash();
            RefreshBars();
        }

        private void RefreshCash()
        {
            if (_cashText == null) return;
            float balance = PlayerWallet.Instance != null ? PlayerWallet.Instance.Balance : 0f;
            bool dirty = PlayerWallet.Instance != null && PlayerWallet.Instance.HoldsContrabandCash;
            _cashText.text = $"${balance:0.00}";
            _cashText.color = dirty ? new Color(1f, 0.42f, 0.38f) : new Color(0.92f, 0.96f, 1f);
        }

        private void RefreshBars()
        {
            var stats = PlayerStats.Instance;
            float mh = stats != null ? stats.MentalHealth : PlayerStatsMath.MaxStat;
            float ph = stats != null ? stats.PhysicalHealth : PlayerStatsMath.MaxStat;
            float st = stats != null ? stats.Strength : PlayerStatsMath.MaxStat;

            SetBar(_mentalFill, _mentalValue, mh);
            SetBar(_physicalFill, _physicalValue, ph);
            SetBar(_strengthFill, _strengthValue, st);
        }

        private static void SetBar(Image fill, TMP_Text value, float stat)
        {
            if (fill != null)
                fill.fillAmount = stat / PlayerStatsMath.MaxStat;
            if (value != null)
                value.text = $"{stat:F0}";
        }
    }
}
