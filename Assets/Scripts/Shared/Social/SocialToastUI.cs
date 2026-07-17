using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison.Social
{
    /// <summary>
    /// Lightweight bottom-center toast line for social feedback (warn-offs, lookout warnings,
    /// favor updates, deliveries). Programmatic, no scene wiring; queues messages.
    /// </summary>
    public class SocialToastUI : MonoBehaviour
    {
        private const int SortOrder = 128;
        private const float ShowSeconds = 3.2f;

        private static SocialToastUI _instance;

        private TMP_Text _label;
        private CanvasGroup _group;
        private readonly Queue<string> _queue = new Queue<string>();
        private Coroutine _runner;

        public static void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            EnsureInstance()._queue.Enqueue(message);
            EnsureInstance().Pump();
        }

        public static SocialToastUI EnsureInstance()
        {
            if (_instance != null) return _instance;
            var existing = FindAnyObjectByType<SocialToastUI>();
            if (existing != null) { _instance = existing; return _instance; }

            var root = new GameObject("SocialToastUI");
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<SocialToastUI>();
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
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var panel = new GameObject("Toast", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 180f);
            rt.sizeDelta = new Vector2(640f, 44f);
            panel.GetComponent<Image>().color = Prison.PrisonUITheme.CommandStripBackdrop;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(panel.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(14f, 4f);
            textRt.offsetMax = new Vector2(-14f, -4f);
            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 21f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = Prison.PrisonUITheme.ConcreteGrey;
        }

        private void Pump()
        {
            if (_runner == null && _queue.Count > 0)
                _runner = StartCoroutine(RunQueue());
        }

        private IEnumerator RunQueue()
        {
            while (_queue.Count > 0)
            {
                string message = _queue.Dequeue();
                if (_label != null) _label.text = message;

                float t = 0f;
                while (t < ShowSeconds)
                {
                    t += Time.deltaTime;
                    if (_group != null)
                    {
                        float fadeIn = Mathf.Clamp01(t / 0.25f);
                        float fadeOut = Mathf.Clamp01((ShowSeconds - t) / 0.5f);
                        _group.alpha = Mathf.Min(fadeIn, fadeOut);
                    }
                    yield return null;
                }
            }
            if (_group != null) _group.alpha = 0f;
            _runner = null;
        }
    }
}
