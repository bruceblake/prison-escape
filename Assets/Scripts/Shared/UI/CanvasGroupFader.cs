using System.Collections;
using UnityEngine;

/// <summary>Smooth show/hide using CanvasGroup alpha + interactable/raycast flags.</summary>
[RequireComponent(typeof(CanvasGroup))]
public class CanvasGroupFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private float defaultDuration = 0.18f;

    private Coroutine _co;

    private void Reset()
    {
        group = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
    }

    public void SetImmediate(bool visible, bool interactable)
    {
        if (group == null) return;
        if (_co != null) { StopCoroutine(_co); _co = null; }
        group.alpha = visible ? 1f : 0f;
        group.interactable = interactable && visible;
        group.blocksRaycasts = interactable && visible;
    }

    public void FadeTo(bool visible, bool allowInteraction, float duration = -1f, System.Action onComplete = null)
    {
        if (group == null) return;
        float d = duration >= 0f ? duration : defaultDuration;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(FadeCo(visible, allowInteraction, d, onComplete));
    }

    private IEnumerator FadeCo(bool visible, bool allowInteraction, float duration, System.Action onComplete)
    {
        float start = group.alpha;
        float end = visible ? 1f : 0f;
        if (duration < 0.0001f)
        {
            group.alpha = end;
        }
        else
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, end, t / duration);
                yield return null;
            }
        }

        group.alpha = end;
        group.interactable = allowInteraction && visible;
        group.blocksRaycasts = allowInteraction && visible;
        _co = null;
        onComplete?.Invoke();
    }
}
