using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fullscreen solitary-confinement overlay: shows Mental Health, Physical Health, and Strength ticking down,
/// then fades out and releases the player. Runs on unscaled time.
/// </summary>
public class SolitaryScreenUI : MonoBehaviour
{
    private const float CountDownSeconds = 2.4f;
    private const float HoldSeconds = 1.2f;
    private const float FadeSeconds = 0.8f;

    private CanvasGroup _group;
    private TMP_Text _mentalText;
    private TMP_Text _physicalText;
    private TMP_Text _strengthText;
    private Action _onComplete;

    public static void Show(float mentalFrom, float mentalTo, float physicalFrom, float physicalTo,
        float strengthFrom, float strengthTo, Action onComplete)
    {
        var existing = FindAnyObjectByType<SolitaryScreenUI>();
        if (existing != null)
            Destroy(existing.gameObject);

        var root = new GameObject("SolitaryScreen");
        var ui = root.AddComponent<SolitaryScreenUI>();
        ui._onComplete = onComplete;
        ui.Build();
        ui.StartCoroutine(ui.Run(mentalFrom, mentalTo, physicalFrom, physicalTo, strengthFrom, strengthTo));
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
            new Color(0.75f, 0.2f, 0.16f), new Vector2(0.5f, 0.78f), FontStyles.Bold);

        EscapeEndScreenUI.CreateText(transform, "Caption", "You were caught trying to escape.\nYour belongings were confiscated.", 30f,
            new Color(0.8f, 0.78f, 0.75f), new Vector2(0.5f, 0.64f), FontStyles.Normal);

        _mentalText = EscapeEndScreenUI.CreateText(transform, "Mental", "", 36f,
            new Color(0.55f, 0.7f, 0.95f), new Vector2(0.5f, 0.5f), FontStyles.Bold);

        _physicalText = EscapeEndScreenUI.CreateText(transform, "Physical", "", 36f,
            new Color(0.45f, 0.85f, 0.55f), new Vector2(0.5f, 0.42f), FontStyles.Bold);

        _strengthText = EscapeEndScreenUI.CreateText(transform, "Strength", "", 36f,
            new Color(0.95f, 0.6f, 0.35f), new Vector2(0.5f, 0.34f), FontStyles.Bold);
    }

    private IEnumerator Run(float mentalFrom, float mentalTo, float physicalFrom, float physicalTo,
        float strengthFrom, float strengthTo)
    {
        float t = 0f;
        while (t < CountDownSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / CountDownSeconds);
            _mentalText.text = $"MENTAL HEALTH  {Mathf.Lerp(mentalFrom, mentalTo, k):F0} / 100";
            _physicalText.text = $"PHYSICAL HEALTH  {Mathf.Lerp(physicalFrom, physicalTo, k):F0} / 100";
            _strengthText.text = $"STRENGTH  {Mathf.Lerp(strengthFrom, strengthTo, k):F0} / 100";
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
