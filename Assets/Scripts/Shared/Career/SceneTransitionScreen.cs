using Prison;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prison.Career
{
    /// <summary>
    /// Fullscreen loading screen for facility/hub scene changes: black institutional backdrop,
    /// destination title, and a caution-yellow progress bar driven by LoadSceneAsync. Replaces
    /// bare SceneManager.LoadScene calls in the career flow so heavy prison scenes never hard-hitch.
    /// Runtime-built, survives the load, removes itself once the new scene is up.
    /// </summary>
    public class SceneTransitionScreen : MonoBehaviour
    {
        private AsyncOperation _op;
        private Image _barFill;
        private TMP_Text _percent;
        private float _shownProgress;
        private float _doneAtRealtime = -1f;

        /// <summary>Loads a scene behind a loading screen. Falls back to a plain load if one is already running.</summary>
        public static void Load(string sceneName, string title, string subtitle = "")
        {
            if (FindAnyObjectByType<SceneTransitionScreen>() != null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            var root = new GameObject("SceneTransitionScreen");
            DontDestroyOnLoad(root);
            var screen = root.AddComponent<SceneTransitionScreen>();
            screen.Build(title, subtitle);

            Time.timeScale = 1f;
            screen._op = SceneManager.LoadSceneAsync(sceneName);
            if (screen._op == null)
            {
                Debug.LogWarning($"[SceneTransitionScreen] Scene '{sceneName}' could not be loaded.");
                Destroy(root);
            }
        }

        private void Update()
        {
            if (_op == null) return;

            // AsyncOperation progress parks at 0.9 until activation; remap to a full bar.
            float target = Mathf.Clamp01(_op.progress / 0.9f);
            _shownProgress = Mathf.MoveTowards(_shownProgress, target, Time.unscaledDeltaTime * 1.5f);
            if (_barFill != null)
                _barFill.fillAmount = _shownProgress;
            if (_percent != null)
                _percent.text = $"{Mathf.RoundToInt(_shownProgress * 100f)}%";

            if (_op.isDone && _doneAtRealtime < 0f)
                _doneAtRealtime = Time.realtimeSinceStartup;

            // Hold one beat after the scene is up so the bar visibly completes.
            if (_doneAtRealtime > 0f && Time.realtimeSinceStartup - _doneAtRealtime > 0.25f)
                Destroy(gameObject);
        }

        private void Build(string title, string subtitle)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var bg = EscapeEndScreenUI.CreatePanel(transform, "Backdrop", new Color(0.02f, 0.025f, 0.03f, 1f));
            EscapeEndScreenUI.Stretch(bg.rectTransform);

            var stripe = EscapeEndScreenUI.CreatePanel(transform, "Stripe", PrisonUITheme.CautionYellow);
            stripe.rectTransform.anchorMin = new Vector2(0f, 0.615f);
            stripe.rectTransform.anchorMax = new Vector2(1f, 0.618f);
            stripe.rectTransform.offsetMin = stripe.rectTransform.offsetMax = Vector2.zero;

            EscapeEndScreenUI.CreateText(transform, "Kicker", "TRANSPORT IN PROGRESS", 20f,
                new Color(0.55f, 0.58f, 0.62f), new Vector2(0.5f, 0.7f), FontStyles.Normal);
            EscapeEndScreenUI.CreateText(transform, "Title", title.ToUpperInvariant(), 62f,
                Color.white, new Vector2(0.5f, 0.55f), FontStyles.Bold);
            if (!string.IsNullOrEmpty(subtitle))
                EscapeEndScreenUI.CreateText(transform, "Subtitle", subtitle, 22f,
                    PrisonUITheme.CautionYellow, new Vector2(0.5f, 0.47f), FontStyles.Italic);

            var barBack = EscapeEndScreenUI.CreatePanel(transform, "BarBack", new Color(0.1f, 0.12f, 0.14f, 1f));
            barBack.rectTransform.anchorMin = barBack.rectTransform.anchorMax = new Vector2(0.5f, 0.36f);
            barBack.rectTransform.sizeDelta = new Vector2(720f, 22f);
            barBack.gameObject.AddComponent<Outline>().effectColor = new Color(0.3f, 0.33f, 0.36f);

            _barFill = EscapeEndScreenUI.CreatePanel(barBack.transform, "BarFill", PrisonUITheme.CautionYellow);
            EscapeEndScreenUI.Stretch(_barFill.rectTransform);
            _barFill.type = Image.Type.Filled;
            _barFill.fillMethod = Image.FillMethod.Horizontal;
            _barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _barFill.fillAmount = 0f;

            _percent = EscapeEndScreenUI.CreateText(transform, "Percent", "0%", 20f,
                new Color(0.55f, 0.58f, 0.62f), new Vector2(0.5f, 0.315f), FontStyles.Normal);
        }
    }
}
