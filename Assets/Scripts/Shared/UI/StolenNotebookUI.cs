using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>Tab + CanvasGroup “Stolen Notebook”: map, social, crafting workbench. Toggle with same key as <see cref="toggleKey"/> (default Tab).</summary>
public class StolenNotebookUI : MonoBehaviour
{
    [Header("Wiring")]
    public PlayerInventory inventory;
    public KeyCode toggleKey = KeyCode.Tab;
    [Tooltip("Closes the old full-screen bag if you still use it")]
    public InventoryUI optionalLegacyInventory;
    [Tooltip("Root to fade (add CanvasGroup if missing)")]
    public GameObject notebookRoot;
    public CanvasGroupFader fader;
    [Min(0.01f)] public float fadeSeconds = 0.2f;

    [Header("Tabs")]
    public GameObject mapPanel;
    public GameObject socialPanel;
    public GameObject workbenchPanel;
    [Tooltip("Optional: full day schedule from PrisonSchedule (hand-drawn page can be behind this text)")]
    public GameObject scheduleDetailPanel;
    public Button mapTabButton;
    public Button socialTabButton;
    public Button workbenchTabButton;
    public Button scheduleTabButton;
    [Header("Notebook — routine detail page")]
    public TMP_Text scheduleDetailText;

    [Header("Scrap map")]
    [Tooltip("Optional: static sketch image or minimap you paint in the editor")]
    public Image mapImage;

    [Header("Social stack")]
    public Transform socialListParent;
    [Tooltip("Each row: add PrisonSocialRowUI, or a TMP text named for auto-fill.")]
    public GameObject socialRowPrefab;

    [Header("Workbench (craft)")]
    public CraftingRecipe[] recipes;
    public Button craftButton;
    public TMP_Text craftButtonText;
    [Tooltip("Optional: list of recipe name + can-craft (built from StolenNotebookUI)")]
    public RectTransform recipeHintParent;
    public GameObject recipeLinePrefab;

    public bool IsOpen { get; private set; }
    private int _tab;
    private readonly List<GameObject> _socialRows = new List<GameObject>();
    private readonly List<GameObject> _recipeLines = new List<GameObject>();
    private CraftingRecipe _activeCraft;
    private bool _socialBuilt;
    private CanvasGroup _rootCg;

    private int MaxTabIndex => scheduleDetailPanel != null ? 3 : 2;

    private void Awake()
    {
        if (notebookRoot != null)
        {
            _rootCg = notebookRoot.GetComponent<CanvasGroup>();
            if (_rootCg == null) _rootCg = notebookRoot.AddComponent<CanvasGroup>();
        }
        if (fader == null && notebookRoot != null)
        {
            fader = notebookRoot.GetComponent<CanvasGroupFader>();
            if (fader == null) fader = notebookRoot.AddComponent<CanvasGroupFader>();
        }
        if (mapTabButton != null) mapTabButton.onClick.AddListener(() => SetTab(0));
        if (socialTabButton != null) socialTabButton.onClick.AddListener(() => SetTab(1));
        if (workbenchTabButton != null) workbenchTabButton.onClick.AddListener(() => SetTab(2));
        if (scheduleTabButton != null) scheduleTabButton.onClick.AddListener(() => SetTab(3));
        if (craftButton != null) craftButton.onClick.AddListener(OnCraftClicked);
    }

    private void Start()
    {
        if (notebookRoot != null) notebookRoot.SetActive(false);
        if (fader != null) fader.SetImmediate(false, false);
        if (_rootCg == null) return;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (IsOpen) Close();
            else Open();
        }

        if (IsOpen)
        {
            if (_tab == 1) RefreshSocial();
            if (_tab == 2) RefreshWorkbench();
            if (_tab == 3) RefreshScheduleDetail();
        }
    }

    public void SetInventory(PlayerInventory inv)
    {
        inventory = inv;
        _socialBuilt = false;
    }

    public void Open()
    {
        if (inventory == null)
        {
            Debug.LogWarning("[StolenNotebookUI] No inventory.");
            return;
        }

        if (optionalLegacyInventory != null && optionalLegacyInventory.IsOpen) optionalLegacyInventory.Close();

        IsOpen = true;
        if (notebookRoot != null) notebookRoot.SetActive(true);
        if (fader != null) fader.FadeTo(true, true, fadeSeconds);
        else if (_rootCg != null) { _rootCg.alpha = 1f; _rootCg.interactable = true; _rootCg.blocksRaycasts = true; }

        SetTab(_tab);
        if (_tab == 3) RefreshScheduleDetail();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        IsOpen = false;
        if (fader != null) fader.FadeTo(false, false, fadeSeconds, () => { if (notebookRoot != null) notebookRoot.SetActive(false); });
        else
        {
            if (_rootCg != null) { _rootCg.alpha = 0f; _rootCg.interactable = false; _rootCg.blocksRaycasts = false; }
            if (notebookRoot != null) notebookRoot.SetActive(false);
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetTab(int t)
    {
        _tab = Mathf.Clamp(t, 0, MaxTabIndex);
        if (mapPanel != null) mapPanel.SetActive(_tab == 0);
        if (socialPanel != null) socialPanel.SetActive(_tab == 1);
        if (workbenchPanel != null) workbenchPanel.SetActive(_tab == 2);
        if (scheduleDetailPanel != null) scheduleDetailPanel.SetActive(_tab == 3);
        if (_tab == 3) RefreshScheduleDetail();
    }

    private void RefreshSocial()
    {
        if (Prison.SocialManager.Instance == null) return;
        if (socialListParent == null) return;

        if (!_socialBuilt)
        {
            foreach (var g in _socialRows) { if (g != null) Destroy(g); }
            _socialRows.Clear();
            var dict = Prison.SocialManager.Instance.PrisonerAffinity;
            foreach (var kvp in dict)
            {
                int cell = kvp.Key;
                float aff = kvp.Value;
                var p = Prison.SocialManager.Instance.GetPersonality(cell);
                string name = p != null && !string.IsNullOrEmpty(p.personalityName) ? p.personalityName : $"Cell {cell}";

                if (socialRowPrefab == null) continue;
                var row = Instantiate(socialRowPrefab, socialListParent);
                var rowUI = row.GetComponent<PrisonSocialRowUI>();
                if (rowUI != null) rowUI.SetRow(name, aff, p);
                else
                {
                    var t = row.GetComponentInChildren<TMP_Text>();
                    if (t != null) t.text = $"{name} — {aff:F0}";
                }
                _socialRows.Add(row);
            }
            _socialBuilt = true;
        }
        else
        {
            int i = 0;
            foreach (var kvp in Prison.SocialManager.Instance.PrisonerAffinity)
            {
                if (i < _socialRows.Count && _socialRows[i] != null)
                {
                    var p = Prison.SocialManager.Instance.GetPersonality(kvp.Key);
                    string name = p != null && !string.IsNullOrEmpty(p.personalityName) ? p.personalityName : $"Cell {kvp.Key}";
                    var rowUI = _socialRows[i].GetComponent<PrisonSocialRowUI>();
                    if (rowUI != null) rowUI.SetRow(name, kvp.Value, p);
                    else
                    {
                        var t = _socialRows[i].GetComponentInChildren<TMP_Text>();
                        if (t != null) t.text = $"{name} — {kvp.Value:F0}";
                    }
                }
                i++;
            }
        }
    }

    private void RefreshWorkbench()
    {
        if (inventory == null) return;

        _activeCraft = null;
        if (recipes == null) return;
        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] != null && CraftingSystem.CanCraft(recipes[i], inventory))
            {
                _activeCraft = recipes[i];
                break;
            }
        }

        if (craftButton != null) craftButton.interactable = _activeCraft != null;
        if (craftButtonText != null)
        {
            craftButtonText.text = _activeCraft != null
                ? $"Craft {_activeCraft.result.itemName} (ready)"
                : "Nothing you can make yet";
        }

        if (recipeHintParent != null && recipeLinePrefab != null)
        {
            foreach (var l in _recipeLines) { if (l != null) Destroy(l); }
            _recipeLines.Clear();
            for (int i = 0; i < recipes.Length; i++)
            {
                if (recipes[i] == null) continue;
                var line = Instantiate(recipeLinePrefab, recipeHintParent);
                _recipeLines.Add(line);
                var t = line.GetComponentInChildren<TMP_Text>();
                if (t != null)
                {
                    bool can = CraftingSystem.CanCraft(recipes[i], inventory);
                    t.text = $"{recipes[i].result.itemName} {(can ? "· READY" : "")}";
                    t.color = can ? new Color(0.6f, 0.85f, 0.5f) : new Color(0.55f, 0.55f, 0.55f);
                }
            }
        }
    }

    private void OnCraftClicked()
    {
        if (_activeCraft == null || inventory == null) return;
        CraftingSystem.TryCraft(_activeCraft, inventory);
        RefreshWorkbench();
    }

    private void RefreshScheduleDetail()
    {
        if (scheduleDetailText == null) return;
        if (Prison.PrisonTimeManager.Instance == null || Prison.PrisonTimeManager.Instance.schedule == null)
        {
            scheduleDetailText.text = "";
            return;
        }
        var sch = Prison.PrisonTimeManager.Instance.schedule;
        if (sch.entries == null || sch.entries.Length == 0)
        {
            scheduleDetailText.text = "";
            return;
        }
        var sb = new StringBuilder();
        for (int i = 0; i < sch.entries.Length; i++)
        {
            var e = sch.entries[i];
            int h = Mathf.FloorToInt(e.startTimeMinutes / 60f) % 24;
            int m = Mathf.FloorToInt(e.startTimeMinutes % 60f);
            sb.AppendFormat("{0:D2}:{1:D2}  {2}  ({3} min game)\n", h, m, e.eventType, e.durationMinutes);
        }
        scheduleDetailText.text = sb.ToString().TrimEnd();
    }
}
