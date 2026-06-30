using System.Collections;
using TMPro;
using UnityEngine;

namespace Prison
{
    /// <summary>Shows a short +affinity popup when <see cref="SocialManager"/>'s OnAffinityChanged fires.</summary>
    public class AffinityFloatPopup : MonoBehaviour
    {
        [SerializeField] private GameObject floatPrefab;
        [SerializeField] private RectTransform spawnAt;
        [SerializeField] private float floatSeconds = 1.1f;
        [SerializeField] private float floatDistance = 60f;
        [SerializeField] private Color defaultColor = new Color(0.75f, 0.9f, 0.6f, 1f);
        [Tooltip("-1 = show for all cells; else only that cell index (e.g. 2 for Sly)")]
        public int filterToCell = -1;

        private void OnEnable()
        {
            if (SocialManager.Instance != null)
                SocialManager.Instance.OnAffinityChanged += OnAffinity;
        }

        private void OnDisable()
        {
            if (SocialManager.Instance != null)
                SocialManager.Instance.OnAffinityChanged -= OnAffinity;
        }

        private void OnAffinity(int cell, float newVal, float delta)
        {
            if (filterToCell >= 0 && cell != filterToCell) return;
            if (Mathf.Approximately(delta, 0f)) return;
            if (floatPrefab == null) return;
            var go = Instantiate(floatPrefab, spawnAt != null ? spawnAt : (RectTransform)transform);
            go.SetActive(true);
            var t = go.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.text = delta > 0f ? $"+{delta:F0}" : $"{delta:F0}";
                t.color = defaultColor;
            }
            var rt = go.transform as RectTransform;
            if (rt == null) return;
            StartCoroutine(Animate(rt));
        }

        private IEnumerator Animate(RectTransform rt)
        {
            Vector2 start = rt.anchoredPosition;
            float el = 0f;
            Color c = defaultColor;
            var tmp = rt.GetComponent<TMP_Text>();
            while (el < floatSeconds)
            {
                el += Time.deltaTime;
                if (rt == null) yield break;
                float u = el / floatSeconds;
                rt.anchoredPosition = start + new Vector2(0f, floatDistance * u);
                c.a = 1f - u;
                if (tmp != null) tmp.color = c;
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }
    }
}
