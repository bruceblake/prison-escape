using System.Collections.Generic;
using Prison;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prison.Career
{
    /// <summary>
    /// The career hub that takes over the MainMenu scene: title screen (CONTINUE / NEW WORLD /
    /// LOAD WORLDS / QUIT), the named-worlds list with destructive delete, and the Prison Select
    /// grid where the whole ladder is visible but locked facilities are black silhouettes.
    /// Runtime-constructed like the other screens; dark institutional chrome, caution-yellow
    /// accents. Spec: docs/PrisonEscape/02 Features/World Saves & Start Screen.md
    /// </summary>
    public class CareerMainMenuUI : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

        private GameObject _titlePanel;
        private GameObject _worldsPanel;
        private GameObject _prisonSelectPanel;
        private GameObject _namePromptPanel;
        private string _pendingName = "";
        private TMP_Text _nameText;

        // ------------------------------------------------------------------
        // Bootstrap — no scene wiring; the hub rebuilds itself whenever MainMenu loads.
        // ------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                if (scene.name == MainMenuSceneName && mode == LoadSceneMode.Single)
                    Build();
            };
            if (SceneManager.GetActiveScene().name == MainMenuSceneName)
                Build();
        }

        private static void Build()
        {
            if (FindAnyObjectByType<CareerMainMenuUI>() != null) return;

            var root = new GameObject("CareerMainMenu");
            var ui = root.AddComponent<CareerMainMenuUI>();
            ui.BuildAll();
        }

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        private void BuildAll()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            EnsureEventSystem();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();

            // Opaque institutional backdrop — visually replaces the legacy menu beneath.
            var bg = EscapeEndScreenUI.CreatePanel(transform, "Backdrop", new Color(0.03f, 0.04f, 0.05f, 1f));
            EscapeEndScreenUI.Stretch(bg.rectTransform);
            var stripe = EscapeEndScreenUI.CreatePanel(transform, "Stripe", PrisonUITheme.CautionYellow);
            stripe.rectTransform.anchorMin = new Vector2(0f, 0.925f);
            stripe.rectTransform.anchorMax = new Vector2(1f, 0.932f);
            stripe.rectTransform.offsetMin = stripe.rectTransform.offsetMax = Vector2.zero;

            _titlePanel = BuildTitlePanel();
            _worldsPanel = BuildPanelShell("WorldsPanel");
            _prisonSelectPanel = BuildPanelShell("PrisonSelectPanel");
            _namePromptPanel = BuildNamePrompt();

            if (CareerSession.ReopenPrisonSelect && CareerSession.ActiveWorld != null)
            {
                CareerSession.ReopenPrisonSelect = false;
                OpenPrisonSelect(CareerSession.ActiveWorld);
            }
            else
            {
                ShowOnly(_titlePanel);
                RefreshTitlePanel();
            }
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        private GameObject BuildPanelShell(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            EscapeEndScreenUI.Stretch((RectTransform)go.transform);
            go.SetActive(false);
            return go;
        }

        private void ShowOnly(GameObject panel)
        {
            _titlePanel.SetActive(panel == _titlePanel);
            _worldsPanel.SetActive(panel == _worldsPanel);
            _prisonSelectPanel.SetActive(panel == _prisonSelectPanel);
            _namePromptPanel.SetActive(panel == _namePromptPanel);
        }

        // ------------------------------------------------------------------
        // Title screen
        // ------------------------------------------------------------------

        private TMP_Text _continueSubtitle;
        private GameObject _continueButton;

        private GameObject BuildTitlePanel()
        {
            var panel = BuildPanelShell("TitlePanel");

            EscapeEndScreenUI.CreateText(panel.transform, "Logo", "P R I S O N   E S C A P E", 92f,
                Color.white, new Vector2(0.5f, 0.8f), FontStyles.Bold);
            EscapeEndScreenUI.CreateText(panel.transform, "Tagline", "EVERY FACILITY IS A PUZZLE BOX. SOLVING IT ONLY MOVES YOU UP THE LADDER.",
                20f, new Color(0.55f, 0.58f, 0.62f), new Vector2(0.5f, 0.71f), FontStyles.Normal);

            var size = new Vector2(520f, 76f);
            _continueButton = CreateMenuButton(panel.transform, "CONTINUE", new Vector2(0.5f, 0.55f), OnContinue, size);
            _continueSubtitle = EscapeEndScreenUI.CreateText(panel.transform, "ContinueSub", "", 20f,
                PrisonUITheme.CautionYellow, new Vector2(0.5f, 0.505f), FontStyles.Italic);

            CreateMenuButton(panel.transform, "NEW WORLD", new Vector2(0.5f, 0.44f), OnNewWorld, size);
            CreateMenuButton(panel.transform, "LOAD WORLDS", new Vector2(0.5f, 0.35f), OnLoadWorlds, size);
            CreateMenuButton(panel.transform, "QUIT", new Vector2(0.5f, 0.26f), OnQuit, size);

            return panel;
        }

        private void RefreshTitlePanel()
        {
            var recent = CareerWorldStore.MostRecentlyPlayed(CareerWorldStore.List());
            bool hasWorlds = recent != null;
            _continueButton.SetActive(hasWorlds);
            _continueSubtitle.gameObject.SetActive(hasWorlds);
            if (hasWorlds)
                _continueSubtitle.text = DescribeWorldShort(recent);
        }

        private static string DescribeWorldShort(CareerWorld w)
        {
            var def = FacilityDirectory.Get(w.currentFacilityId);
            string facility = def != null ? def.title : w.currentFacilityId;
            int day = w.activeRun != null && w.activeRun.IsActive ? w.activeRun.day : 1;
            return $"\"{w.displayName}\" — {facility}, Day {day}";
        }

        private void OnContinue()
        {
            var recent = CareerWorldStore.MostRecentlyPlayed(CareerWorldStore.List());
            if (recent != null)
                OpenPrisonSelect(recent);
        }

        private void OnNewWorld()
        {
            _pendingName = "";
            ShowOnly(_namePromptPanel);
            RefreshNameText();
        }

        private void OnLoadWorlds()
        {
            RebuildWorldsPanel();
            ShowOnly(_worldsPanel);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------
        // New-world name prompt (keyboard-driven; 1–24 chars, guid identity)
        // ------------------------------------------------------------------

        private GameObject BuildNamePrompt()
        {
            var panel = BuildPanelShell("NamePromptPanel");

            EscapeEndScreenUI.CreateText(panel.transform, "Title", "NAME YOUR WORLD", 48f,
                Color.white, new Vector2(0.5f, 0.66f), FontStyles.Bold);

            var field = EscapeEndScreenUI.CreatePanel(panel.transform, "Field", new Color(0.09f, 0.11f, 0.13f, 1f));
            field.rectTransform.anchorMin = field.rectTransform.anchorMax = new Vector2(0.5f, 0.55f);
            field.rectTransform.sizeDelta = new Vector2(640f, 74f);
            field.gameObject.AddComponent<Outline>().effectColor = PrisonUITheme.CautionYellow;

            _nameText = EscapeEndScreenUI.CreateText(field.transform, "Name", "_", 34f,
                Color.white, new Vector2(0.5f, 0.5f), FontStyles.Normal);
            EscapeEndScreenUI.Stretch(_nameText.rectTransform);

            EscapeEndScreenUI.CreateText(panel.transform, "Hint", "Type a name (1–24 characters). ENTER to create, ESC to cancel.",
                18f, new Color(0.55f, 0.58f, 0.62f), new Vector2(0.5f, 0.485f), FontStyles.Normal);

            CreateMenuButton(panel.transform, "CREATE", new Vector2(0.42f, 0.37f), ConfirmCreateWorld, new Vector2(280f, 66f));
            CreateMenuButton(panel.transform, "CANCEL", new Vector2(0.58f, 0.37f), () =>
            {
                ShowOnly(_titlePanel);
                RefreshTitlePanel();
            }, new Vector2(280f, 66f));

            return panel;
        }

        private void OnEnable()
        {
            if (Keyboard.current != null)
                Keyboard.current.onTextInput += OnTextInput;
        }

        private void OnDisable()
        {
            if (Keyboard.current != null)
                Keyboard.current.onTextInput -= OnTextInput;
        }

        private void OnTextInput(char c)
        {
            if (_namePromptPanel == null || !_namePromptPanel.activeSelf) return;
            if (char.IsControl(c)) return;
            if (_pendingName.Length >= CareerWorld.DisplayNameMaxLength) return;
            _pendingName += c;
            RefreshNameText();
        }

        private void Update()
        {
            if (_namePromptPanel == null || !_namePromptPanel.activeSelf || Keyboard.current == null) return;

            if (Keyboard.current.backspaceKey.wasPressedThisFrame && _pendingName.Length > 0)
            {
                _pendingName = _pendingName.Substring(0, _pendingName.Length - 1);
                RefreshNameText();
            }
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                ConfirmCreateWorld();
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ShowOnly(_titlePanel);
                RefreshTitlePanel();
            }
        }

        private void RefreshNameText()
        {
            if (_nameText != null)
                _nameText.text = _pendingName + "<color=#F4D03F>_</color>";
        }

        private void ConfirmCreateWorld()
        {
            if (string.IsNullOrWhiteSpace(_pendingName)) return;
            var world = CareerWorldStore.Create(_pendingName);
            OpenPrisonSelect(world);
        }

        // ------------------------------------------------------------------
        // Load Worlds
        // ------------------------------------------------------------------

        private void RebuildWorldsPanel()
        {
            foreach (Transform child in _worldsPanel.transform)
                Destroy(child.gameObject);

            EscapeEndScreenUI.CreateText(_worldsPanel.transform, "Title", "LOAD WORLDS", 48f,
                Color.white, new Vector2(0.5f, 0.87f), FontStyles.Bold);

            var worlds = CareerWorldStore.List();
            if (worlds.Count == 0)
                EscapeEndScreenUI.CreateText(_worldsPanel.transform, "Empty", "No worlds yet — start a NEW WORLD.",
                    24f, new Color(0.55f, 0.58f, 0.62f), new Vector2(0.5f, 0.5f), FontStyles.Italic);

            float y = 0.76f;
            foreach (var world in worlds)
            {
                if (y < 0.2f) break; // v1: no scrolling; a screenful of careers is plenty
                BuildWorldRow(world, y);
                y -= 0.085f;
            }

            CreateMenuButton(_worldsPanel.transform, "BACK", new Vector2(0.5f, 0.1f), () =>
            {
                ShowOnly(_titlePanel);
                RefreshTitlePanel();
            }, new Vector2(280f, 62f));
        }

        private void BuildWorldRow(CareerWorld world, float y)
        {
            var row = EscapeEndScreenUI.CreatePanel(_worldsPanel.transform, "Row_" + world.displayName,
                new Color(0.07f, 0.09f, 0.11f, 0.96f));
            row.rectTransform.anchorMin = row.rectTransform.anchorMax = new Vector2(0.5f, y);
            row.rectTransform.sizeDelta = new Vector2(1180f, 74f);
            row.gameObject.AddComponent<Outline>().effectColor = new Color(0.2f, 0.23f, 0.26f);

            var name = EscapeEndScreenUI.CreateText(row.transform, "Name", world.displayName, 28f,
                Color.white, new Vector2(0.14f, 0.5f), FontStyles.Bold);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.rectTransform.sizeDelta = new Vector2(300f, 60f);

            var def = FacilityDirectory.Get(world.currentFacilityId);
            string facility = def != null ? def.title : world.currentFacilityId;
            int day = world.activeRun != null && world.activeRun.IsActive ? world.activeRun.day : 1;
            string played = CareerWorld.ParseUtc(world.lastPlayedUtc).ToLocalTime().ToString("M/d h:mm tt");
            string detail = $"{facility} · Day {day} · ${world.global.cash:n0} · R{world.global.respect:0}" +
                            (world.global.careerWon ? " · <color=#F4D03F>CLEARED</color>" : "") + $" · {played}";

            var info = EscapeEndScreenUI.CreateText(row.transform, "Detail", detail, 20f,
                new Color(0.68f, 0.71f, 0.74f), new Vector2(0.52f, 0.5f), FontStyles.Normal);
            info.alignment = TextAlignmentOptions.MidlineLeft;
            info.rectTransform.sizeDelta = new Vector2(620f, 60f);

            EscapeEndScreenUI.CreateButton(row.transform, "SELECT", new Vector2(0.85f, 0.5f),
                () => OpenPrisonSelect(world), new Vector2(150f, 54f));
            EscapeEndScreenUI.CreateButton(row.transform, "DELETE", new Vector2(0.955f, 0.5f),
                () => ConfirmDeleteWorld(world), new Vector2(120f, 54f));
        }

        private void ConfirmDeleteWorld(CareerWorld world)
        {
            var overlay = EscapeEndScreenUI.CreatePanel(transform, "DeleteConfirm", new Color(0f, 0f, 0f, 0.78f));
            EscapeEndScreenUI.Stretch(overlay.rectTransform);

            var panel = EscapeEndScreenUI.CreatePanel(overlay.transform, "Panel", new Color(0.07f, 0.09f, 0.11f, 0.98f));
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(780f, 300f);
            panel.gameObject.AddComponent<Outline>().effectColor = PrisonUITheme.HazardRed;

            var t = EscapeEndScreenUI.CreateText(panel.transform, "Title", $"DELETE \"{world.displayName}\"?", 34f,
                PrisonUITheme.HazardRed, new Vector2(0.5f, 0.78f), FontStyles.Bold);
            t.rectTransform.sizeDelta = new Vector2(720f, 50f);
            var b = EscapeEndScreenUI.CreateText(panel.transform, "Body",
                "Transfer paperwork shredded — this world is gone forever.", 22f,
                new Color(0.8f, 0.8f, 0.82f), new Vector2(0.5f, 0.56f), FontStyles.Normal);
            b.rectTransform.sizeDelta = new Vector2(720f, 60f);

            var size = new Vector2(300f, 64f);
            EscapeEndScreenUI.CreateButton(panel.transform, "DELETE FOREVER", new Vector2(0.27f, 0.2f), () =>
            {
                CareerWorldStore.Delete(world.id);
                if (CareerSession.ActiveWorld == world)
                    CareerSession.SelectWorld(null);
                Destroy(overlay.gameObject);
                RebuildWorldsPanel();
            }, size);
            EscapeEndScreenUI.CreateButton(panel.transform, "CANCEL", new Vector2(0.73f, 0.2f),
                () => Destroy(overlay.gameObject), size);
        }

        // ------------------------------------------------------------------
        // Prison Select hub
        // ------------------------------------------------------------------

        private void OpenPrisonSelect(CareerWorld world)
        {
            CareerSession.SelectWorld(world);
            RebuildPrisonSelect(world);
            ShowOnly(_prisonSelectPanel);
        }

        private void RebuildPrisonSelect(CareerWorld world)
        {
            foreach (Transform child in _prisonSelectPanel.transform)
                Destroy(child.gameObject);

            var header = EscapeEndScreenUI.CreateText(_prisonSelectPanel.transform, "Header",
                $"PRISON SELECT — \"{world.displayName}\"", 42f, Color.white, new Vector2(0.36f, 0.885f), FontStyles.Bold);
            header.alignment = TextAlignmentOptions.MidlineLeft;
            header.rectTransform.sizeDelta = new Vector2(1000f, 60f);

            var carry = EscapeEndScreenUI.CreateText(_prisonSelectPanel.transform, "Carry",
                $"${world.global.cash:n0} · RESPECT {world.global.respect:0}" +
                (world.global.careerWon ? " · <color=#F4D03F>CAREER CLEARED</color>" : ""),
                26f, PrisonUITheme.CautionYellow, new Vector2(0.82f, 0.885f), FontStyles.Bold);
            carry.alignment = TextAlignmentOptions.MidlineRight;
            carry.rectTransform.sizeDelta = new Vector2(500f, 60f);

            // Grid: the whole ladder is always visible — locked slots are the long-term goal
            // rendered as black silhouettes. Dev sandbox slot appears in dev builds only.
            var grid = new GameObject("Grid", typeof(RectTransform));
            grid.transform.SetParent(_prisonSelectPanel.transform, false);
            var grt = (RectTransform)grid.transform;
            grt.anchorMin = new Vector2(0.5f, 0.5f);
            grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.anchoredPosition = new Vector2(0f, -20f);
            grt.sizeDelta = new Vector2(1740f, 620f);
            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(330f, 290f);
            layout.spacing = new Vector2(18f, 22f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;

            bool devBuild = Application.isEditor || Debug.isDebugBuild;
            foreach (string id in FacilityIds.LadderOrder)
                BuildFacilityCard(grid.transform, world, FacilityDirectory.Get(id));
            if (devBuild && world.IsUnlocked(FacilityIds.DevSandbox))
                BuildFacilityCard(grid.transform, world, FacilityDirectory.Get(FacilityIds.DevSandbox));

            CreateMenuButton(_prisonSelectPanel.transform, "BACK", new Vector2(0.5f, 0.07f), () =>
            {
                ShowOnly(_titlePanel);
                RefreshTitlePanel();
            }, new Vector2(280f, 62f));
        }

        private void BuildFacilityCard(Transform grid, CareerWorld world, FacilityDefinition def)
        {
            if (def == null) return;
            bool unlocked = world.IsUnlocked(def.id);
            bool current = world.currentFacilityId == def.id;
            bool buildable = def.HasScene;

            var card = EscapeEndScreenUI.CreatePanel(grid, "Card_" + def.id,
                unlocked ? new Color(0.07f, 0.09f, 0.11f, 0.97f) : new Color(0.015f, 0.015f, 0.02f, 1f));
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = current ? PrisonUITheme.CautionYellow : new Color(0.16f, 0.18f, 0.21f);
            outline.effectDistance = current ? new Vector2(3f, 3f) : new Vector2(1f, 1f);

            if (!unlocked)
            {
                // Black silhouette, greyed title only — no lore spoilers.
                var sil = EscapeEndScreenUI.CreatePanel(card.transform, "Silhouette", Color.black);
                sil.rectTransform.anchorMin = new Vector2(0.12f, 0.38f);
                sil.rectTransform.anchorMax = new Vector2(0.88f, 0.88f);
                sil.rectTransform.offsetMin = sil.rectTransform.offsetMax = Vector2.zero;
                if (def.silhouette != null) sil.sprite = def.silhouette;

                var lockedTitle = EscapeEndScreenUI.CreateText(card.transform, "Title", def.title.ToUpperInvariant(),
                    20f, new Color(0.28f, 0.3f, 0.33f), new Vector2(0.5f, 0.22f), FontStyles.Bold);
                lockedTitle.rectTransform.sizeDelta = new Vector2(300f, 54f);
                return;
            }

            var tier = EscapeEndScreenUI.CreateText(card.transform, "Tier",
                def.IsDevSandbox ? "DEV" : $"{def.system.ToUpperInvariant()}{(string.IsNullOrEmpty(def.securityLabel) ? "" : " · " + def.securityLabel.ToUpperInvariant())}",
                15f, new Color(0.55f, 0.58f, 0.62f), new Vector2(0.5f, 0.93f), FontStyles.Normal);
            tier.rectTransform.sizeDelta = new Vector2(300f, 22f);

            var title = EscapeEndScreenUI.CreateText(card.transform, "Title", def.title.ToUpperInvariant(),
                22f, current ? PrisonUITheme.CautionYellow : Color.white, new Vector2(0.5f, 0.82f), FontStyles.Bold);
            title.rectTransform.sizeDelta = new Vector2(305f, 56f);

            if (buildable)
            {
                var desc = EscapeEndScreenUI.CreateText(card.transform, "Desc", def.description, 15f,
                    new Color(0.66f, 0.69f, 0.72f), new Vector2(0.5f, 0.56f), FontStyles.Normal);
                desc.rectTransform.sizeDelta = new Vector2(295f, 110f);

                var stay = EscapeEndScreenUI.CreateText(card.transform, "Stay",
                    def.IsDevSandbox ? "Sandbox — never writes to your career" : def.RecommendedStayHint,
                    14f, new Color(0.5f, 0.53f, 0.56f), new Vector2(0.5f, 0.315f), FontStyles.Italic);
                stay.rectTransform.sizeDelta = new Vector2(300f, 26f);

                string facilityId = def.id;
                EscapeEndScreenUI.CreateButton(card.transform, current ? "ENTER ▸" : "ENTER",
                    new Vector2(0.5f, 0.13f), () => CareerSession.EnterFacility(world, facilityId),
                    new Vector2(220f, 52f));
            }
            else
            {
                // Unlocked but not built yet — acknowledge progress without lying about content.
                var sil = EscapeEndScreenUI.CreatePanel(card.transform, "Silhouette", new Color(0.03f, 0.03f, 0.04f, 1f));
                sil.rectTransform.anchorMin = new Vector2(0.12f, 0.34f);
                sil.rectTransform.anchorMax = new Vector2(0.88f, 0.72f);
                sil.rectTransform.offsetMin = sil.rectTransform.offsetMax = Vector2.zero;

                var uc = EscapeEndScreenUI.CreateText(card.transform, "UnderConstruction", "UNDER CONSTRUCTION",
                    17f, PrisonUITheme.CautionYellow, new Vector2(0.5f, 0.18f), FontStyles.Bold);
                uc.rectTransform.sizeDelta = new Vector2(300f, 30f);
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private GameObject CreateMenuButton(Transform parent, string label, Vector2 anchor,
            UnityEngine.Events.UnityAction onClick, Vector2 size)
        {
            EscapeEndScreenUI.CreateButton(parent, label, anchor, onClick, size);
            return parent.Find("Button_" + label).gameObject;
        }
    }
}
