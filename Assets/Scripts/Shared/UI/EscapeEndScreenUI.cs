using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fullscreen "YOU ESCAPED" end screen with run stats and the prison-ladder framing.
/// Built entirely at runtime so no scene wiring is required.
/// </summary>
public class EscapeEndScreenUI : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    public static void Show(string statsText)
    {
        var existing = FindAnyObjectByType<EscapeEndScreenUI>();
        if (existing != null) return;

        var root = new GameObject("EscapeEndScreen");
        DontDestroyOnLoad(root);
        var ui = root.AddComponent<EscapeEndScreenUI>();
        ui.Build(statsText);
    }

    private void Build(string statsText)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        var bg = CreatePanel(transform, "Backdrop", new Color(0.02f, 0.02f, 0.03f, 0.96f));
        Stretch(bg.rectTransform);

        CreateText(transform, "Title", "YOU ESCAPED", 92f, new Color(0.95f, 0.85f, 0.4f),
            new Vector2(0.5f, 0.82f), FontStyles.Bold);

        CreateText(transform, "Subtitle", "MINIMUM SECURITY PRISON — CLEARED", 30f, new Color(0.8f, 0.8f, 0.82f),
            new Vector2(0.5f, 0.72f), FontStyles.Normal);

        CreateText(transform, "Stats", statsText, 34f, Color.white,
            new Vector2(0.5f, 0.5f), FontStyles.Normal);

        CreateText(transform, "Ladder", "Next stop: MEDIUM SECURITY", 28f, new Color(0.75f, 0.35f, 0.3f),
            new Vector2(0.5f, 0.3f), FontStyles.Italic);

        CreateButton(transform, "Return to Main Menu", new Vector2(0.5f, 0.18f), OnReturnToMenu);
    }

    private void OnReturnToMenu()
    {
        Time.timeScale = 1f;
        Destroy(gameObject);
        SceneManager.LoadScene(MainMenuSceneName);
    }

    // ------------------------------------------------------------------
    // UI helpers
    // ------------------------------------------------------------------

    internal static Image CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    internal static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    internal static TMP_Text CreateText(Transform parent, string name, string text, float size, Color color,
        Vector2 anchorCenter, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchorCenter;
        rt.sizeDelta = new Vector2(1500f, 400f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static void CreateButton(Transform parent, string label, Vector2 anchorCenter, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchorCenter;
        rt.sizeDelta = new Vector2(420f, 84f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.18f, 0.22f, 0.95f);

        var button = go.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var text = CreateText(go.transform, "Label", label, 30f, Color.white, new Vector2(0.5f, 0.5f), FontStyles.Bold);
        Stretch(text.rectTransform);
    }
}
