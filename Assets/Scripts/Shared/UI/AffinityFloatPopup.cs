using System.Collections;
using Prison.Social;
using TMPro;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Floating Trust/Respect delta popups (retitled from v1 affinity popups per the
    /// Social Ecosystem v3 teardown table). Subscribes to player-facing relationship
    /// changes on <see cref="SocialWorld"/>.
    /// </summary>
    public class AffinityFloatPopup : MonoBehaviour
    {
        [SerializeField] private GameObject floatPrefab;
        [SerializeField] private RectTransform spawnAt;
        [SerializeField] private float floatSeconds = 1.1f;
        [SerializeField] private float floatDistance = 60f;
        [SerializeField] private Color trustColor = new Color(0.55f, 0.85f, 0.95f, 1f);
        [SerializeField] private Color respectColor = new Color(0.95f, 0.75f, 0.4f, 1f);
        [SerializeField] private Color lossColor = new Color(0.9f, 0.4f, 0.35f, 1f);

        private void OnEnable()
        {
            if (SocialWorld.Instance != null)
                SocialWorld.Instance.OnPlayerRelationshipChanged += OnRelationshipChanged;
        }

        private void OnDisable()
        {
            if (SocialWorld.Instance != null)
                SocialWorld.Instance.OnPlayerRelationshipChanged -= OnRelationshipChanged;
        }

        private void OnRelationshipChanged(int observer, float trustDelta, float respectDelta, RelationshipRecord record)
        {
            // Only pop deltas for the NPC you're actually engaging with.
            if (!SocialInteractionMenu.IsOpenFor(observer)) return;
            if (Mathf.Abs(trustDelta) >= 0.5f)
                Spawn($"Trust {trustDelta:+0;-0}", trustDelta < 0f ? lossColor : trustColor);
            if (Mathf.Abs(respectDelta) >= 0.5f)
                Spawn($"Respect {respectDelta:+0;-0}", respectDelta < 0f ? lossColor : respectColor);
        }

        private void Spawn(string message, Color color)
        {
            if (floatPrefab == null) return;
            var go = Instantiate(floatPrefab, spawnAt != null ? spawnAt : (RectTransform)transform);
            go.SetActive(true);
            var t = go.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.text = message;
                t.color = color;
            }
            var rt = go.transform as RectTransform;
            if (rt == null) return;
            StartCoroutine(Animate(rt, color));
        }

        private IEnumerator Animate(RectTransform rt, Color color)
        {
            Vector2 start = rt.anchoredPosition;
            float el = 0f;
            Color c = color;
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
