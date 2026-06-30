using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>Top-right cash readout with rolling numbers and contraband tint.</summary>
    public class CashUIController : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text balanceText;
        public Image background;
        public TMP_FontAsset monospacedFont;

        [Header("Format")]
        public string balanceFormat = "${0:0.00}";

        [Header("Rolling")]
        [Min(0.05f)] public float rollDuration = 0.5f;

        [Header("Contraband")]
        public Color normalTextColor = new Color(0.92f, 0.96f, 1f, 1f);
        public Color contrabandTextColor = new Color(1f, 0.42f, 0.38f, 1f);
        public Color normalBackgroundColor = new Color(0.04f, 0.06f, 0.08f, 0.75f);
        public Color contrabandBackgroundColor = new Color(0.22f, 0.04f, 0.04f, 0.82f);

        private float _displayedBalance;
        private float _targetBalance;
        private Coroutine _rollRoutine;
        private bool _contraband;

        private void Awake()
        {
            if (balanceText != null && monospacedFont != null)
                balanceText.font = monospacedFont;
        }

        private void OnEnable()
        {
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnBalanceChanged += OnWalletBalanceChanged;
                _targetBalance = PlayerWallet.Instance.Balance;
                _displayedBalance = _targetBalance;
                SetContrabandState(PlayerWallet.Instance.HoldsContrabandCash);
                RefreshLabel();
            }
        }

        private void OnDisable()
        {
            if (PlayerWallet.Instance != null)
                PlayerWallet.Instance.OnBalanceChanged -= OnWalletBalanceChanged;
        }

        private void Start()
        {
            if (PlayerWallet.Instance == null)
                return;
            _targetBalance = PlayerWallet.Instance.Balance;
            _displayedBalance = _targetBalance;
            RefreshLabel();
        }

        public void SetBalanceImmediate(float amount)
        {
            _targetBalance = amount;
            _displayedBalance = amount;
            RefreshLabel();
        }

        public void SetContrabandState(bool isDirty)
        {
            _contraband = isDirty;
            ApplyContrabandColors();
        }

        private void OnWalletBalanceChanged(float previous, float current)
        {
            _targetBalance = current;
            if (_rollRoutine != null)
                StopCoroutine(_rollRoutine);
            _rollRoutine = StartCoroutine(RollToTarget());
        }

        private IEnumerator RollToTarget()
        {
            float start = _displayedBalance;
            float elapsed = 0f;
            while (elapsed < rollDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = rollDuration > 0.001f ? Mathf.Clamp01(elapsed / rollDuration) : 1f;
                t = t * t * (3f - 2f * t);
                _displayedBalance = Mathf.Lerp(start, _targetBalance, t);
                RefreshLabel();
                yield return null;
            }

            _displayedBalance = _targetBalance;
            RefreshLabel();
            _rollRoutine = null;
        }

        private void RefreshLabel()
        {
            if (balanceText != null)
                balanceText.text = string.Format(balanceFormat, _displayedBalance);
        }

        private void ApplyContrabandColors()
        {
            if (balanceText != null)
                balanceText.color = _contraband ? contrabandTextColor : normalTextColor;
            if (background != null)
                background.color = _contraband ? contrabandBackgroundColor : normalBackgroundColor;
        }

#if UNITY_EDITOR
        [ContextMenu("Build Top-Right Cash Panel")]
        private void BuildTopRightPanelEditor()
        {
            var rt = transform as RectTransform;
            if (rt == null) return;

            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -16f);
            rt.sizeDelta = new Vector2(200f, 44f);

            if (background == null)
            {
                var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(transform, false);
                background = bgGo.GetComponent<Image>();
                background.color = normalBackgroundColor;
                var bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
            }

            if (balanceText == null)
            {
                var tGo = new GameObject("BalanceText", typeof(RectTransform));
                tGo.transform.SetParent(transform, false);
                balanceText = tGo.AddComponent<TextMeshProUGUI>();
                balanceText.alignment = TextAlignmentOptions.MidlineRight;
                balanceText.fontSize = 32f;
                var tRt = balanceText.rectTransform;
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = new Vector2(10f, 4f);
                tRt.offsetMax = new Vector2(-12f, -4f);
            }

            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
#endif
    }
}
