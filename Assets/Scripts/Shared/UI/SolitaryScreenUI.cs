using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fullscreen solitary-confinement overlay: shows Mental Health and Strength ticking down,
/// then fades out and releases the player. Runs on unscaled time.
/// </summary>
public class SolitaryScreenUI : MonoBehaviour
{
    private const float CountDownSeconds = 2.4f;
    private const float HoldSeconds = 1.2f;
    private const float FadeSeconds = 0.8f;

    private CanvasGroup _group;
    private TMP_Text _mentalText;
    private TMP_Text _strengthText;
    private Action _onComplete;

    public static void Show(float mentalFrom, float mentalTo, float strengthFrom, float strengthTo, Action onComplete)
    {
        var existing = FindAnyObjectByType<SolitaryScreenUI>();
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        var root = new GameObject("SolitaryScreen");
        var ui = root.AddComponent<SolitaryScreenUI>();
        ui._onComplete = onComplete;
        ui.Build();
        ui.StartCoroutine(ui.Run(mentalFrom, mentalTo, strengthFrom, strengthTo));
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4900;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();
        _group = gameObject.AddComponent<CanvasGroup>();

        var bg = EscapeEndScreenUI.CreatePanel(transform, "Backdrop", new Color(0.03f, 0.02f, 0.02f, 0.97f));
        EscapeEndScreenUI.Stretch(bg.rectTransform);

        EscapeEndScreenUI.CreateText(transform, "Title", "SOLITARY CONFINEMENT", 72f,
            new Color(0.75f, 0.2f, 0.16f), new Vector2(0.5f, 0.72f), FontStyles.Bold);

        EscapeEndScreenUI.CreateText(transform, "Caption", "You were caught trying to escape.\nYour belongings were confiscated.", 30f,
            new Color(0.8f, 0.78f, 0.75f), new Vector2(0.5f, 0.6f), FontStyles.Normal);

        _mentalText = EscapeEndScreenUI.CreateText(transform, "Mental", "", 40f,
            new Color(0.55f, 0.7f, 0.95f), new Vector2(0.5f, 0.44f), FontStyles.Bold);

        _strengthText = EscapeEndScreenUI.CreateText(transform, "Strength", "", 40f,
            new Color(0.95f, 0.6f, 0.35f), new Vector2(0.5f, 0.36f), FontStyles.Bold);
    }

    private IEnumerator Run(float mentalFrom, float mentalTo, float strengthFrom, float strengthTo)
    {
        float t = 0f;
        while (t < CountDownSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / CountDownSeconds);
            float mh = Mathf.Lerp(mentalFrom, mentalTo, k);
            float st = Mathf.Lerp(strengthFrom, strengthTo, k);
            _mentalText.text = $"MENTAL HEALTH  {mh:F0} / 100";
            _strengthText.text = $"STRENGTH  {st:F0} / 100";
            yield return null;
        }

        yield return new WaitForSecondsRealtime(HoldSeconds);

        t = 0f;
        while (t < FadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = 1f - Mathf.Clamp01(t / FadeSeconds);
            yield return null;
        }

        _onComplete?.Invoke();
        Destroy(gameObject);
    }
}
