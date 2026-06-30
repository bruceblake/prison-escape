using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory inventory;
    public GameObject panel;
    public Transform slotContainer;
    public GameObject slotPrefab;

    [Header("Notebook — shell")]
    public CanvasGroup notebookCanvasGroup;
    public RectTransform leftPanelPockets;
    public RectTransform rightPanelNotebook;

    [Header("Crafting recipes")]
    public CraftingRecipe[] recipes;

    [Header("Notebook — recipe index")]
    public ScrollRect recipeIndexScrollRect;
    public GameObject recipeIndexRowPrefab;
    public float recipeIndexRowHeight = 70f;
    public Sprite recipeRowBackground;

    [Header("Notebook — recipe card")]
public RecipeRequirementSlotUI[] requirementSlots = new RecipeRequirementSlotUI[3];
    public TMP_Text selectedRecipeTitleText;
    public TMP_Text selectedRecipeResultHintText;
    public TMP_Text recipeOverflowNoteText;
    public Button assembleButton;

    [Header("Legacy crafting (fallback) ")]
    public Button craftButton;
    public TMP_Text craftButtonText;

    [Header("Layout polish")]
    public GameObject hotbarHiddenWhileOpen;
    public bool hideHotbarWhileInventoryOpen = true;
    public GameObject bagDimOverlay;
    public GameObject crosshair;

    public bool IsOpen { get; private set; }

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private CraftingRecipe activeCraftableRecipe;
    private CraftingRecipe _selectedRecipe;
    private readonly List<NotebookRecipeIndexEntry> _recipeIndexEntries = new List<NotebookRecipeIndexEntry>();
    private bool isBuilt;
    private bool craftingListDirty = true;
    private bool notebookVisualsDirty;
    private Coroutine fadeCoroutine;

    [Header("Professional Layout")]
    public Transform categoryTabParent;
    public ItemCategory selectedCategory = ItemCategory.Tool;

    void OnEnable() { if (inventory != null) inventory.SlotsChanged += OnInventorySlotsChanged; }
    void OnDisable() { if (inventory != null) inventory.SlotsChanged -= OnInventorySlotsChanged; }

    private void OnInventorySlotsChanged()
    {
        if (IsOpen) { RefreshSlots(); if (UsesNotebookCrafting()) notebookVisualsDirty = true; else craftingListDirty = true; }
    }

    private Color assembleButtonBaseColor = new Color(0.2f, 0.15f, 0.1f, 1f);

    void Start()
    {
        if (inventory == null) { var p = GameObject.FindAnyObjectByType<PlayerInventory>(); if (p != null) SetInventory(p); }
        if (panel != null) panel.SetActive(false);
        if (assembleButton != null) 
        {
            assembleButton.onClick.AddListener(OnAssembleClicked);
            var img = assembleButton.GetComponent<UnityEngine.UI.Image>();
            if (img != null) assembleButtonBaseColor = img.color;
        }
        
        // Auto-find hotbar if reference is missing
if (hotbarHiddenWhileOpen == null)
        {
            var hotbar = GameObject.FindAnyObjectByType<HotbarUI>();
            if (hotbar != null) hotbarHiddenWhileOpen = hotbar.gameObject;
        }

        // Setup category buttons
        if (categoryTabParent != null)
        {
            Button[] tabs = categoryTabParent.GetComponentsInChildren<Button>();
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                if (i < tabs.Length) tabs[i].onClick.AddListener(() => SetCategory(index));
            }
        }
    }

    void Update()
    {
        if (!IsOpen || inventory == null) return;

        // Safety check to ensure hotbar stays hidden while inventory is open
        if (hideHotbarWhileInventoryOpen && hotbarHiddenWhileOpen != null && hotbarHiddenWhileOpen.activeSelf)
        {
            hotbarHiddenWhileOpen.SetActive(false);
        }

        RefreshSlots();
        if (UsesNotebookCrafting())
        {
            if (craftingListDirty) RebuildRecipeNotebookIndex();
            else if (notebookVisualsDirty) RefreshNotebookCraftingVisuals();
            UpdateRecipeIndexCraftabilityVisuals();
            craftingListDirty = false; notebookVisualsDirty = false;
        }
        }

        private void UpdateRecipeIndexCraftabilityVisuals()
        {
            if (inventory == null || _recipeIndexEntries == null) return;
            foreach (var entry in _recipeIndexEntries)
            {
                if (entry == null || entry.recipe == null || entry.rowBackground == null) continue;
                
                bool canCraft = CraftingSystem.CanCraft(entry.recipe, inventory);
                bool isSelected = (entry.recipe == _selectedRecipe);
                
                // Base background color - darker for better icon visibility
                Color bgColor = new Color(0, 0, 0, 0.5f); 
                
                // If it's craftable, give it a vibrant green look
                if (canCraft)
                {
                    bgColor = new Color(0.1f, 0.9f, 0.1f, 0.95f);
                }
                else if (isSelected)
                {
                    // Selection highlight (Blue-ish) if not craftable
                    bgColor = new Color(0.2f, 0.5f, 0.9f, 0.8f);
                }
                
                entry.rowBackground.color = bgColor;
                
                // Add/Update outline for selection
                UnityEngine.UI.Outline outline = entry.GetComponent<UnityEngine.UI.Outline>();
                if (isSelected)
                {
                    if (outline == null) outline = entry.gameObject.AddComponent<UnityEngine.UI.Outline>();
                    // Bright outline for selection
                    outline.effectColor = canCraft ? Color.white : new Color(1f, 1f, 1f, 0.8f);
                    outline.effectDistance = new Vector2(3, -3);
                    outline.enabled = true;
                }
                else if (outline != null)
                {
                    outline.enabled = false;
                }
            }
        }

    private bool UsesNotebookCrafting()
    {
        return recipeIndexScrollRect != null && assembleButton != null && requirementSlots != null && requirementSlots.Length >= 3 && requirementSlots[0] != null;
    }

    public void SetInventory(PlayerInventory playerInventory)
    {
        if (inventory != null) inventory.SlotsChanged -= OnInventorySlotsChanged;
        inventory = playerInventory;
        if (inventory != null) inventory.SlotsChanged += OnInventorySlotsChanged;
        isBuilt = false; craftingListDirty = true;
        BuildSlots();
    }

    private void BuildSlots()
    {
        if (isBuilt || inventory == null || slotPrefab == null || slotContainer == null) return;
        isBuilt = true;
        foreach (InventorySlotUI existing in slotUIs) if (existing != null) Destroy(existing.gameObject);
        slotUIs.Clear();
        for (int i = 0; i < inventory.maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
            slotUI.Configure(i, inventory, dragSwapEnabled: true);
            slotUI.ClearSlot();
            slotUIs.Add(slotUI);
        }
    }

    public void Toggle() { if (IsOpen) Close(); else Open(); }

    public void Open()
    {
        if (inventory == null) return;
        BuildSlots();
        IsOpen = true;
        
        if (panel != null) panel.SetActive(true);
        if (bagDimOverlay != null) bagDimOverlay.SetActive(true);
        if (crosshair != null) crosshair.SetActive(false);
        craftingListDirty = true;
        RefreshSlots();
        
        if (notebookCanvasGroup != null) 
        { 
            notebookCanvasGroup.interactable = true; 
            notebookCanvasGroup.blocksRaycasts = true; 
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(notebookCanvasGroup, 0f, 1f, 0.2f));
        }
        
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public void Close()
    {
        IsOpen = false;
        
        if (bagDimOverlay != null) bagDimOverlay.SetActive(false);
        if (crosshair != null) crosshair.SetActive(true);
        
        if (notebookCanvasGroup != null) 
        { 
            notebookCanvasGroup.interactable = false; 
            notebookCanvasGroup.blocksRaycasts = false; 
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(notebookCanvasGroup, notebookCanvasGroup.alpha, 0f, 0.15f, () => {
                if (panel != null) panel.SetActive(false);
            }));
        }
        else if (panel != null) panel.SetActive(false);
        
        // Re-enable hotbar GameObject when inventory closes
        if (hotbarHiddenWhileOpen != null && !hotbarHiddenWhileOpen.activeSelf)
        {
            hotbarHiddenWhileOpen.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }
    
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t / duration);
            
            // Add a slight scale pop effect if scaling up
            if (endAlpha > startAlpha)
            {
                float scale = Mathf.Lerp(0.95f, 1f, t / duration);
                cg.transform.localScale = new Vector3(scale, scale, 1f);
            }
            
            yield return null;
        }
        cg.alpha = endAlpha;
        if (endAlpha > startAlpha) cg.transform.localScale = Vector3.one;
        onComplete?.Invoke();
    }

    private void RefreshSlots()
    {
        if (inventory == null) return;
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (i < inventory.inventorySlots.Count)
            {
                var s = inventory.inventorySlots[i];
                if (s != null && !s.IsEmpty) slotUIs[i].SetSlot(s); else slotUIs[i].ClearSlot();
            }
            else slotUIs[i].ClearSlot();
            slotUIs[i].SetSelected(i == inventory.selectedSlotIndex);
        }
    }

    public void SetCategory(int categoryIndex)
    {
        ItemCategory[] categories = { ItemCategory.Tool, ItemCategory.Weapon, ItemCategory.CraftingPart, ItemCategory.Consumable };
        if (categoryIndex >= 0 && categoryIndex < categories.Length)
        {
            selectedCategory = categories[categoryIndex];
            craftingListDirty = true;
            UpdateCategoryTabVisuals(categoryIndex);
        }
    }

    private void UpdateCategoryTabVisuals(int selectedIndex)
    {
        if (categoryTabParent == null) return;
        Button[] tabs = categoryTabParent.GetComponentsInChildren<Button>();
        for (int i = 0; i < tabs.Length; i++)
        {
            UnityEngine.UI.Image img = tabs[i].GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                // Highlight selected tab with a bright tint, dim others
                img.color = (i == selectedIndex) 
                    ? new Color(0.4f, 0.35f, 0.25f, 1f) 
                    : new Color(0.15f, 0.12f, 0.1f, 0.8f);
            }
        }
    }

    private void RebuildRecipeNotebookIndex()
    {
        craftingListDirty = false;
        if (!UsesNotebookCrafting() || inventory == null) return;
        _recipeIndexEntries.Clear();
        RectTransform content = recipeIndexScrollRect.content;
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
        
        CraftingRecipe firstEligible = null;
        if (recipes != null)
        {
            foreach (var recipe in recipes)
            {
                if (recipe == null || recipe.result == null) continue;
                if (recipe.result.category != selectedCategory) continue;
                InstantiateRecipeIconButton(recipe);
                if (firstEligible == null) firstEligible = recipe;
            }
        }
        if (firstEligible != null && _selectedRecipe == null) UpdateRecipeDetailsInternal(firstEligible);
        else if (_selectedRecipe != null) UpdateRecipeDetailsInternal(_selectedRecipe);
        else ClearNotebookRecipeDetails();
    }

    private void RefreshNotebookCraftingVisuals()
    {
        if (!UsesNotebookCrafting() || inventory == null) return;
        if (_selectedRecipe != null) UpdateRecipeDetailsInternal(_selectedRecipe);
    }

    public void UpdateRecipeDetails(CraftingRecipe selected) { UpdateRecipeDetailsInternal(selected); }

    private void UpdateRecipeDetailsInternal(CraftingRecipe selected)
    {
        _selectedRecipe = selected;
        if (selected == null || selected.result == null) { ClearNotebookRecipeDetails(); return; }
        
        if (selectedRecipeTitleText != null) 
        {
            selectedRecipeTitleText.text = (selected.recipeName ?? selected.result.itemName).ToUpper();
            selectedRecipeTitleText.color = Color.white; // Changed from dark blue
            selectedRecipeTitleText.enableWordWrapping = false;
            selectedRecipeTitleText.alignment = TextAlignmentOptions.Center;
            selectedRecipeTitleText.fontStyle = FontStyles.Bold; // Ensure it's bold
            selectedRecipeTitleText.fontSize = 32; // Increased size
            
            // Add shadow/outline for extra readability
            selectedRecipeTitleText.outlineColor = Color.black;
            selectedRecipeTitleText.outlineWidth = 0.15f;
// Force width to prevent vertical wrap
            RectTransform rt = selectedRecipeTitleText.rectTransform;
            rt.sizeDelta = new Vector2(400, rt.sizeDelta.y); 
        }
        
        if (selectedRecipeResultHintText != null) 
        {
            selectedRecipeResultHintText.text = $"RESULT: {selected.result.itemName} x{selected.resultAmount}";
            selectedRecipeResultHintText.color = new Color(0.8f, 0.8f, 0.85f); // Changed from dark grey
            selectedRecipeResultHintText.enableWordWrapping = false;
            selectedRecipeResultHintText.alignment = TextAlignmentOptions.Center;
            RectTransform rt = selectedRecipeResultHintText.rectTransform;
            rt.sizeDelta = new Vector2(400, rt.sizeDelta.y);
        }

        for (int i = 0; i < requirementSlots.Length; i++)
        {
            if (requirementSlots[i] == null) continue;
            if (i < selected.ingredients.Length) 
            {
                requirementSlots[i].gameObject.SetActive(true);
                requirementSlots[i].SetIngredient(selected.ingredients[i], inventory);
                
                RectTransform slotRT = requirementSlots[i].GetComponent<RectTransform>();
                if (slotRT != null) {
                    slotRT.localRotation = Quaternion.identity;
                    // Removed hardcoded sizeDelta that was fighting LayoutGroups
                }
            }
            else 
            {
                requirementSlots[i].gameObject.SetActive(false);
                requirementSlots[i].ClearSlot();
            }
        }
        
        if (assembleButton != null)
        {
            assembleButton.interactable = CraftingSystem.CanCraft(selected, inventory);
            // Ensure button has a reasonable width if not set
            RectTransform btnRT = assembleButton.GetComponent<RectTransform>();
            if (btnRT != null && btnRT.sizeDelta.x < 100) 
            {
                btnRT.sizeDelta = new Vector2(400, btnRT.sizeDelta.y);
            }

            UnityEngine.UI.Image btnImg = assembleButton.GetComponent<UnityEngine.UI.Image>();
            if (btnImg != null)
            {
                Color c = assembleButtonBaseColor;
                if (!assembleButton.interactable) c.a *= 0.5f;
                btnImg.color = c;
            }
// Ensure button text is horizontal
            TMP_Text btnTxt = assembleButton.GetComponentInChildren<TMP_Text>();
            if (btnTxt != null)
            {
                btnTxt.rectTransform.localRotation = Quaternion.identity;
                btnTxt.enableWordWrapping = false;
                btnTxt.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    private void ClearNotebookRecipeDetails()
    {
        _selectedRecipe = null;
        if (selectedRecipeTitleText != null) selectedRecipeTitleText.text = "";
        if (selectedRecipeResultHintText != null) selectedRecipeResultHintText.text = "";
        foreach (var s in requirementSlots) if (s != null) s.ClearSlot();
        if (assembleButton != null) assembleButton.interactable = false;
    }

    private void OnAssembleClicked()
    {
        if (_selectedRecipe == null || inventory == null) return;
        if (CraftingSystem.TryCraft(_selectedRecipe, inventory)) { RefreshSlots(); UpdateRecipeDetailsInternal(_selectedRecipe); RefreshNotebookCraftingVisuals(); }
    }

    private void InstantiateRecipeIconButton(CraftingRecipe recipe)
    {
        RectTransform content = recipeIndexScrollRect.content;
        GameObject go = new GameObject($"RecipeIcon_{recipe.name}", typeof(RectTransform));
        go.transform.SetParent(content, false);
        
        UnityEngine.UI.Image bg = go.AddComponent<UnityEngine.UI.Image>(); 
        if (recipeRowBackground != null) { bg.sprite = recipeRowBackground; bg.type = UnityEngine.UI.Image.Type.Sliced; }
        // Much darker background for high-contrast buttons
        bg.color = new Color(0, 0, 0, 0.75f);

        UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>(); 
        btn.targetGraphic = bg;

        // Container for icon and label
        GameObject inner = CreateUIObj("Container", go.transform);
        RectTransform innerRT = inner.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero; innerRT.anchorMax = Vector2.one; innerRT.sizeDelta = Vector2.zero;
        
        UnityEngine.UI.VerticalLayoutGroup vlg = inner.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = true; vlg.childControlWidth = true;
        vlg.padding = new RectOffset(5, 5, 5, 5);

        // Icon
        GameObject iconObj = CreateUIObj("Icon", inner.transform);
        UnityEngine.UI.Image iconImg = iconObj.AddComponent<UnityEngine.UI.Image>();
        iconImg.sprite = recipe.result != null ? recipe.result.icon : null;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Short Name Label
        GameObject nameObj = CreateUIObj("Name", inner.transform);
        TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
        nameTxt.text = (recipe.recipeName ?? (recipe.result != null ? recipe.result.itemName : "??")).ToUpper();
        nameTxt.fontSize = 14; // Larger font
        nameTxt.fontStyle = FontStyles.Bold; // Bold
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.color = Color.white; // White text on dark background
        nameTxt.enableWordWrapping = false;
        
        // Add shadow for "better fonts" look
        nameTxt.extraPadding = true;
        nameTxt.outlineColor = new Color(0, 0, 0, 0.5f);
        nameTxt.outlineWidth = 0.1f;

        btn.onClick.AddListener(() => UpdateRecipeDetails(recipe));

        var entry = go.AddComponent<NotebookRecipeIndexEntry>();
        entry.recipe = recipe; 
        entry.rowBackground = bg;
        _recipeIndexEntries.Add(entry);
    }

    private GameObject CreateUIObj(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public void OnCraftRecipe(CraftingRecipe recipe)
    {
        if (recipe == null || inventory == null) return;
        if (CraftingSystem.TryCraft(recipe, inventory)) { RefreshSlots(); craftingListDirty = true; notebookVisualsDirty = true; }
    }
    }
