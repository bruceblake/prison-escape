using Prison;
using Prison.Career;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The transfer ceremony — the rewrite of the v1 "YOU ESCAPED" end screen. Crossing the boundary
/// means you were caught outside the wall and sentenced to the next facility; County's clock ends
/// in "SENTENCE COMPLETE"; escaping the top Federal facility is "CAREER CLEARED". Runtime-built,
/// styled per the UI theme (dark institutional chrome, caution-yellow accents).
/// Spec: docs/PrisonEscape/02 Features/Facility Transfer & Graduation.md § The transfer ceremony.
/// </summary>
public class EscapeEndScreenUI : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    // ------------------------------------------------------------------
    // Entry points
    // ------------------------------------------------------------------

    /// <summary>Ceremony for a career transfer (caught / sentence complete / career win).</summary>
    public static void ShowTransfer(TransferResult result, EscapeRunStats stats)
    {
        var ui = CreateInstance();
        if (ui == null) return;

        string headline;
        Color headlineColor;
        string subtitle;
        var from = FacilityDirectory.Get(result.fromFacilityId);
        string fromTitle = from != null ? from.title.ToUpperInvariant() : result.fromFacilityId;

        switch (result.kind)
        {
            case TransferKind.CareerWin:
                headline = "CAREER CLEARED";
                headlineColor = PrisonUITheme.CautionYellow;
                subtitle = $"{fromTitle} — NOBODY HAD EVER LEFT IT EARLY";
                break;
            case TransferKind.SentenceServed:
                headline = "SENTENCE COMPLETE";
                headlineColor = PrisonUITheme.InkGreen;
                subtitle = $"{fromTitle} — TIME SERVED, PAPERWORK CLEAN";
                break;
            default:
                headline = "CAUGHT — TRANSFERRED";
                headlineColor = PrisonUITheme.HazardRed;
                subtitle = $"{fromTitle} — PICKED UP OUTSIDE THE WALL";
                break;
        }

        ui.BuildCeremony(headline, headlineColor, subtitle, stats, result);
    }

    /// <summary>Dev sandbox / non-career escape: no world writes, no transfer.</summary>
    public static void ShowSandboxEscape(EscapeRunStats stats, bool backToHub)
    {
        var ui = CreateInstance();
        if (ui == null) return;
        ui._backToHub = backToHub;
        ui.BuildCeremony("YOU ESCAPED", PrisonUITheme.CautionYellow,
            "DEVELOPMENT PRISON — SANDBOX RUN (NOT ON THE CAREER LADDER)", stats, null);
    }

    private static EscapeEndScreenUI CreateInstance()
    {
        var existing = FindAnyObjectByType<EscapeEndScreenUI>();
        if (existing != null) return null;

        var root = new GameObject("TransferCeremonyScreen");
        DontDestroyOnLoad(root);
        return root.AddComponent<EscapeEndScreenUI>();
    }

    private bool _backToHub;

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------

    private void BuildCeremony(string headline, Color headlineColor, string subtitle,
        EscapeRunStats stats, TransferResult result)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        var bg = CreatePanel(transform, "Backdrop", new Color(0.02f, 0.02f, 0.03f, 0.97f));
        Stretch(bg.rectTransform);

        // Caution-stripe accent under the headline.
        var stripe = CreatePanel(transform, "Stripe", PrisonUITheme.CautionYellow);
        var srt = stripe.rectTransform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.795f);
        srt.sizeDelta = new Vector2(760f, 4f);

        CreateText(transform, "Title", headline, 84f, headlineColor, new Vector2(0.5f, 0.865f), FontStyles.Bold);
        CreateText(transform, "Subtitle", subtitle, 26f, new Color(0.8f, 0.8f, 0.82f), new Vector2(0.5f, 0.755f), FontStyles.Normal);

        // Run stats — label/value rows.
        float y = 0.665f;
        const float rowStep = 0.048f;
        AddRow(ref y, rowStep, "Days inside", stats.daysInside.ToString());
        AddRow(ref y, rowStep, "Play time", stats.playTime);
        AddRow(ref y, rowStep, "Solitary stays", stats.timesInSolitary.ToString());
        AddRow(ref y, rowStep, "Items crafted", stats.itemsCrafted.ToString());
        AddRow(ref y, rowStep, "Reputation", stats.reputation);

        if (result != null)
        {
            y -= 0.02f;
            AddRow(ref y, rowStep, "Cash carried", $"${result.cashCarried:n0}", PrisonUITheme.CautionYellow);
            AddRow(ref y, rowStep, "Career respect", $"+{result.respectAwarded:0.#}  (now {result.respectAfter:0})", PrisonUITheme.CautionYellow);
            AddRow(ref y, rowStep, "Inventory confiscated", result.itemsConfiscated > 0 ? $"{result.itemsConfiscated} items" : "nothing to take", PrisonUITheme.HazardRed);

            if (result.kind == TransferKind.CareerWin)
            {
                CreateText(transform, "NextStop", "THE LADDER IS CLEARED — THE WORLD STAYS OPEN.\nEvery facility remains yours to revisit.",
                    24f, PrisonUITheme.CautionYellow, new Vector2(0.5f, y - 0.05f), FontStyles.Italic);
            }
            else
            {
                BuildNextStopCard(result, new Vector2(0.5f, y - 0.055f));
            }
        }

        BuildButtons(result);
    }

    private void AddRow(ref float y, float step, string label, string value) =>
        AddRow(ref y, step, label, value, Color.white);

    private void AddRow(ref float y, float step, string label, string value, Color valueColor)
    {
        var l = CreateText(transform, "Row_" + label, label.ToUpperInvariant(), 24f,
            new Color(0.62f, 0.65f, 0.68f), new Vector2(0.395f, y), FontStyles.Normal);
        l.alignment = TextAlignmentOptions.MidlineRight;
        l.rectTransform.sizeDelta = new Vector2(420f, 40f);

        var v = CreateText(transform, "Val_" + label, value, 26f, valueColor, new Vector2(0.62f, y), FontStyles.Bold);
        v.alignment = TextAlignmentOptions.MidlineLeft;
        v.rectTransform.sizeDelta = new Vector2(560f, 40f);

        y -= step;
    }

    /// <summary>The next facility's silhouette "flips" to its unlocked identity.</summary>
    private void BuildNextStopCard(TransferResult result, Vector2 anchor)
    {
        var next = FacilityDirectory.Get(result.nextFacilityId);
        string nextTitle = next != null ? next.title : result.nextFacilityId;

        var card = CreatePanel(transform, "NextStopCard", new Color(0.07f, 0.09f, 0.11f, 0.95f));
        var rt = card.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(680f, 96f);
        card.gameObject.AddComponent<Outline>().effectColor = PrisonUITheme.CautionYellow;

        string flag = result.unlockedNewFacility ? "NEXT STOP — NEWLY UNLOCKED" : "NEXT STOP";
        var tag = CreateText(card.transform, "Tag", flag, 18f, new Color(0.62f, 0.65f, 0.68f), new Vector2(0.5f, 0.76f), FontStyles.Normal);
        tag.rectTransform.sizeDelta = new Vector2(640f, 26f);

        string construction = next != null && !next.HasScene ? "   [UNDER CONSTRUCTION]" : "";
        var title = CreateText(card.transform, "Facility", nextTitle.ToUpperInvariant() + construction, 30f,
            PrisonUITheme.CautionYellow, new Vector2(0.5f, 0.34f), FontStyles.Bold);
        title.rectTransform.sizeDelta = new Vector2(640f, 44f);
    }

    private void BuildButtons(TransferResult result)
    {
        if (result == null)
        {
            CreateButton(transform, _backToHub ? "PRISON SELECT" : "RETURN TO MENU",
                new Vector2(0.5f, 0.115f), OnPrisonSelect);
            return;
        }

        if (result.kind == TransferKind.CareerWin)
        {
            CreateButton(transform, "KEEP PLAYING", new Vector2(0.5f, 0.115f), OnPrisonSelect);
            return;
        }

        var next = FacilityDirectory.Get(result.nextFacilityId);
        bool canEnterNext = next != null && next.HasScene;
        if (canEnterNext)
        {
            string nextId = result.nextFacilityId;
            CreateButton(transform, $"ENTER {next.title.ToUpperInvariant()}", new Vector2(0.35f, 0.115f),
                () => OnEnterNext(nextId));
            CreateButton(transform, "PRISON SELECT", new Vector2(0.65f, 0.115f), OnPrisonSelect);
        }
        else
        {
            CreateButton(transform, "PRISON SELECT", new Vector2(0.5f, 0.115f), OnPrisonSelect);
        }
    }

    // ------------------------------------------------------------------
    // Button handlers
    // ------------------------------------------------------------------

    private void OnEnterNext(string facilityId)
    {
        var world = CareerSession.ActiveWorld;
        Time.timeScale = 1f;
        Destroy(gameObject);
        if (world == null || !CareerSession.EnterFacility(world, facilityId))
            SceneTransitionScreen.Load(MainMenuSceneName, "Prison Select");
    }

    private void OnPrisonSelect()
    {
        Time.timeScale = 1f;
        Destroy(gameObject);
        if (CareerSession.ActiveWorld != null || _backToHub)
            CareerSession.EndRunAndReturnToHub();
        else
            SceneTransitionScreen.Load(MainMenuSceneName, "Main Menu");
    }

    // ------------------------------------------------------------------
    // UI helpers (shared with other runtime-built screens)
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

    internal static void CreateButton(Transform parent, string label, Vector2 anchorCenter,
        UnityEngine.Events.UnityAction onClick, Vector2? size = null)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchorCenter;
        rt.sizeDelta = size ?? new Vector2(460f, 84f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.18f, 0.22f, 0.95f);
        go.AddComponent<Outline>().effectColor = new Color(0.35f, 0.38f, 0.42f);

        var button = go.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var text = CreateText(go.transform, "Label", label, 28f, Color.white, new Vector2(0.5f, 0.5f), FontStyles.Bold);
        Stretch(text.rectTransform);
    }
}
