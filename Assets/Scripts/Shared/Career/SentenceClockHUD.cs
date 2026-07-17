using Prison;
using TMPro;
using UnityEngine;

namespace Prison.Career
{
    /// <summary>
    /// "Days served: N / 7" line for facilities with a sentence clock (County), parked in the
    /// routine-bar area at the top of the screen. Runtime-constructed; no scene wiring.
    /// </summary>
    public class SentenceClockHUD : MonoBehaviour
    {
        private TMP_Text _text;

        public static SentenceClockHUD Ensure()
        {
            var existing = FindAnyObjectByType<SentenceClockHUD>();
            if (existing != null) return existing;

            var root = new GameObject("SentenceClockHUD");
            var hud = root.AddComponent<SentenceClockHUD>();
            hud.Build();
            return hud;
        }

        public void SetLine(string line)
        {
            if (_text != null) _text.text = line;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var go = new GameObject("DaysServed", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -86f); // just under the routine now/next bar
            rt.sizeDelta = new Vector2(560f, 34f);

            _text = go.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 22f;
            _text.fontStyle = FontStyles.Bold;
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = PrisonUITheme.CautionYellow;
            _text.text = "";
        }
    }
}
