using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison.Social
{
    /// <summary>
    /// Real-time Talk Menu overlay (design of record: Talk Menu &amp; NPC Profile).
    /// Bottom-third panel, tabbed: Profile / Talk / Gift / Trade / Favors / Threat (inmates),
    /// Bribe / Snitch (guards). No pause — the world keeps running; player movement is locked.
    /// Unavailable tabs are hidden, not greyed. Fully programmatic (no scene wiring).
    /// </summary>
    public class SocialInteractionMenu : MonoBehaviour
    {
        private const int SortOrder = 140;
        private const int LayoutVersion = 4;

        private static SocialInteractionMenu _instance;

        public static bool IsOpenFor(int actorId) =>
            _instance != null && _instance._open && _instance._actorId == actorId;

        public static bool IsAnyOpen => _instance != null && _instance._open;

        private bool _open;
        private int _layoutVersion;
        private int _actorId = SocialTuning.NoActor;
        private string _tab = "Talk";
        private string _lastChatLine;

        private GameObject _root;
        private GameObject _backdrop;
        private RectTransform _panel;
        private TMP_Text _headerName;
        private TMP_Text _headerBadge;
        private TMP_Text _trustBar;
        private TMP_Text _respectBar;
        private TMP_Text _bandChip;
        private RectTransform _tabStrip;
        private RectTransform _body;

        private PrisonerController _playerController;
        private PrisonerAI _talkingInmate;

        // ------------------------------------------------------------------ lifecycle

        public static SocialInteractionMenu EnsureInstance()
        {
            if (_instance != null && _instance._layoutVersion != LayoutVersion)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
            if (_instance != null) return _instance;
            var existing = FindAnyObjectByType<SocialInteractionMenu>();
            if (existing != null) { _instance = existing; return _instance; }
            var go = new GameObject("SocialInteractionMenu");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SocialInteractionMenu>();
            _instance.Build();
            return _instance;
        }

        public static void Open(int actorId)
        {
            var menu = EnsureInstance();
            if (menu._open && menu._actorId == actorId) { menu.Close(); return; }

            var world = SocialWorld.Instance;
            if (world != null)
            {
                var go = world.GetActorObject(actorId);
                if (go != null && SocialTalkGate.TryGetRefusal(go, out string refusal))
                {
                    SocialToastUI.Show(refusal);
                    return;
                }
            }

            menu.OpenInternal(actorId);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (!_open) return;
            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
            // The world keeps running — keep header bars live.
            RefreshHeader();
        }

        private void OpenInternal(int actorId)
        {
            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt) return;
            var identity = world.GetIdentity(actorId);
            if (identity == null) return;

            _actorId = actorId;
            _open = true;
            _lastChatLine = null;
            world.MarkMet(actorId);

            if (_root != null) _root.SetActive(true);
            UIMenuFocus.RegisterOpen();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            LockPlayerMovement(true);
            SetInmateTalkEngagement(actorId, true);

            CheckDeliveryCompletion(world, identity);

            _tab = "Talk";
            RebuildTabs();
            RefreshHeader();
            RebuildBody();
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            _actorId = SocialTuning.NoActor;
            if (_root != null) _root.SetActive(false);
            UIMenuFocus.RegisterClosed();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            LockPlayerMovement(false);
            SetInmateTalkEngagement(SocialTuning.NoActor, false);
        }

        private void SetInmateTalkEngagement(int actorId, bool engaged)
        {
            if (_talkingInmate != null)
            {
                _talkingInmate.SetTalkEngaged(false, null);
                _talkingInmate = null;
            }

            if (!engaged || actorId == SocialTuning.NoActor)
                return;

            var world = SocialWorld.Instance;
            if (world == null)
                return;

            var go = world.GetActorObject(actorId);
            if (go == null)
                return;

            var inmate = go.GetComponent<PrisonerAI>();
            if (inmate == null)
                return;

            if (_playerController == null)
                _playerController = FindAnyObjectByType<PrisonerController>();

            Transform face = _playerController != null ? _playerController.transform : null;
            inmate.SetTalkEngaged(true, face);
            _talkingInmate = inmate;
        }

        private void LockPlayerMovement(bool locked)
        {
            if (_playerController == null)
                _playerController = FindAnyObjectByType<PrisonerController>();
            if (_playerController != null)
                _playerController.SetMovementBlocked(locked);
        }

        /// <summary>Delivery favors complete the moment you talk to the recipient with the package.</summary>
        private void CheckDeliveryCompletion(SocialWorld world, NPCIdentity identity)
        {
            var inventory = FindInventory();
            if (inventory == null) return;
            foreach (var favor in new List<FavorInstance>(world.Favors.All))
            {
                if (favor.kind != FavorKind.Delivery || favor.state != FavorState.Active) continue;
                if (favor.targetActorId != identity.actorId) continue;
                if (favor.item == null || !inventory.HasItem(favor.item, 1)) continue;
                inventory.RemoveItem(favor.item, 1);
                world.Favors.Complete(favor);
                SocialToastUI.Show($"Package delivered. {world.GetIdentity(favor.npcActorId)?.ShortName ?? "They"} owes you one.");
            }
        }

        // ------------------------------------------------------------------ UI construction

        private void Build()
        {
            _layoutVersion = LayoutVersion;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortOrder;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();

            _root = new GameObject("MenuRoot", typeof(RectTransform));
            _root.transform.SetParent(transform, false);
            var rootRt = (RectTransform)_root.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropGo.transform.SetParent(_root.transform, false);
            var backdropRt = (RectTransform)backdropGo.transform;
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = Vector2.zero;
            backdropRt.offsetMax = Vector2.zero;
            var backdropImage = backdropGo.GetComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.42f);
            backdropGo.GetComponent<Button>().onClick.AddListener(Close);
            _backdrop = backdropGo;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(_root.transform, false);
            _panel = (RectTransform)panelGo.transform;
            _panel.anchorMin = new Vector2(0.5f, 0f);
            _panel.anchorMax = new Vector2(0.5f, 0f);
            _panel.pivot = new Vector2(0.5f, 0f);
            _panel.anchoredPosition = new Vector2(0f, 108f);
            _panel.sizeDelta = new Vector2(1500f, 760f);
            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = new Color(0.07f, 0.09f, 0.12f, 0.97f);

            var accentGo = new GameObject("TopAccent", typeof(RectTransform), typeof(Image));
            accentGo.transform.SetParent(_panel, false);
            var accentRt = (RectTransform)accentGo.transform;
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.pivot = new Vector2(0.5f, 1f);
            accentRt.anchoredPosition = Vector2.zero;
            accentRt.sizeDelta = new Vector2(0f, 4f);
            accentGo.GetComponent<Image>().color = Prison.PrisonUITheme.CautionYellow;

            var borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(_panel, false);
            var borderRt = (RectTransform)borderGo.transform;
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-2f, -2f);
            borderRt.offsetMax = new Vector2(2f, 2f);
            borderGo.GetComponent<Image>().color = new Color(0.22f, 0.26f, 0.30f, 0.85f);
            borderGo.transform.SetAsFirstSibling();

            _headerName = MakeText(_panel, "HeaderName", new Vector2(28f, -18f), new Vector2(820f, 40f), 32f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            _headerBadge = MakeText(_panel, "HeaderBadge", new Vector2(-64f, -18f), new Vector2(360f, 40f), 22f, FontStyles.Normal, TextAlignmentOptions.MidlineRight, anchorRight: true);
            _headerBadge.color = Prison.PrisonUITheme.ConcreteGrey;

            MakeCloseButton(_panel);

            _trustBar = MakeText(_panel, "TrustBar", new Vector2(28f, -64f), new Vector2(520f, 32f), 22f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            _respectBar = MakeText(_panel, "RespectBar", new Vector2(560f, -64f), new Vector2(420f, 32f), 22f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            _bandChip = MakeText(_panel, "BandChip", new Vector2(-28f, -64f), new Vector2(160f, 32f), 22f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, anchorRight: true);

            var stripGo = new GameObject("TabStrip", typeof(RectTransform));
            stripGo.transform.SetParent(_panel, false);
            _tabStrip = (RectTransform)stripGo.transform;
            _tabStrip.anchorMin = new Vector2(0f, 1f);
            _tabStrip.anchorMax = new Vector2(1f, 1f);
            _tabStrip.pivot = new Vector2(0.5f, 1f);
            _tabStrip.anchoredPosition = new Vector2(0f, -108f);
            _tabStrip.sizeDelta = new Vector2(-40f, 48f);
            var strip = stripGo.AddComponent<HorizontalLayoutGroup>();
            strip.spacing = 10f;
            strip.childForceExpandWidth = false;
            strip.childForceExpandHeight = true;
            strip.padding = new RectOffset(12, 12, 0, 0);

            var bodyGo = new GameObject("Body", typeof(RectTransform));
            bodyGo.transform.SetParent(_panel, false);
            _body = (RectTransform)bodyGo.transform;
            _body.anchorMin = new Vector2(0f, 0f);
            _body.anchorMax = new Vector2(1f, 1f);
            _body.offsetMin = new Vector2(28f, 24f);
            _body.offsetMax = new Vector2(-28f, -168f);
            var layout = bodyGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            _root.SetActive(false);
        }

        private void MakeCloseButton(RectTransform parent)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -16f);
            rt.sizeDelta = new Vector2(40f, 40f);
            go.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.28f, 0.95f);
            go.GetComponent<Button>().onClick.AddListener(Close);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "X";
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
        }

        private TMP_Text MakeText(RectTransform parent, string name, Vector2 pos, Vector2 size,
            float fontSize, FontStyles style, TextAlignmentOptions align, bool anchorRight = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchorRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rt.pivot = anchorRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = align;
            text.color = Color.white;
            return text;
        }

        // ------------------------------------------------------------------ header + tabs

        private NPCIdentity CurrentIdentity() => SocialWorld.Instance?.GetIdentity(_actorId);

        private void RefreshHeader()
        {
            var world = SocialWorld.Instance;
            var identity = CurrentIdentity();
            if (world == null || identity == null) return;

            _headerName.text = identity.DisplayName;

            if (identity.isGuard)
            {
                _headerBadge.text = $"Guard · {identity.guardArchetype}";
            }
            else
            {
                string gang = identity.gangId != SocialTuning.IndependentGangId
                    ? GangCatalog.Get(identity.gangId)?.displayName ?? "Gang"
                    : "Independent";
                _headerBadge.text = $"{gang} · {ArchetypeShortLabel(identity)}";
            }

            var record = world.Relationships.Get(_actorId, SocialTuning.PlayerActorId);
            _trustBar.text = $"Trust {Bar(record.trust)} {record.trust:+0;-0;0}";
            _respectBar.text = $"Respect {Bar(record.respect)} {record.respect:+0;-0;0}";

            var band = RelationshipMath.GetBand(RelationshipMath.Standing(record));
            _bandChip.text = StandingBandUI.Label(band);
            _bandChip.color = StandingBandUI.ColorOf(band);
        }

        private string ArchetypeShortLabel(NPCIdentity identity)
        {
            switch (identity.archetype)
            {
                case PrisonerArchetype.ShotCaller: return "Shot-Caller";
                case PrisonerArchetype.OldTimer: return "Old-Timer";
                default: return identity.archetype.ToString();
            }
        }

        private static string Bar(float value)
        {
            int filled = Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(-100f, 100f, value) * 10f), 0, 10);
            var sb = new System.Text.StringBuilder(10);
            for (int i = 0; i < 10; i++) sb.Append(i < filled ? "█" : "░");
            return sb.ToString();
        }

        private bool IsBusyPhase()
        {
            var tm = Prison.PrisonTimeManager.Instance;
            if (tm == null) return false;
            var e = tm.CurrentEvent;
            return e == Prison.PrisonEventType.MorningRollCall
                || e == Prison.PrisonEventType.RollCall
                || e == Prison.PrisonEventType.MiddayCount
                || e == Prison.PrisonEventType.EveningCount
                || e == Prison.PrisonEventType.NightRollCall;
        }

        private List<string> VisibleTabs()
        {
            var world = SocialWorld.Instance;
            var identity = CurrentIdentity();
            var tabs = new List<string>();
            if (identity == null) return tabs;
            if (IsBusyPhase()) return tabs; // busy NPCs: one-liner, no tabs

            tabs.Add("Profile");
            tabs.Add("Talk");
            tabs.Add("Gift");

            if (!identity.isGuard)
            {
                bool refuses = identity.gangId != SocialTuning.IndependentGangId
                    && !world.Gangs.IsMemberOf(identity.gangId)
                    && world.Gangs.RefusesTrade(identity.gangId);
                bool gangStore = identity.gangId != SocialTuning.IndependentGangId
                    && world.Gangs.IsMemberOf(identity.gangId)
                    && (GangCatalog.Get(identity.gangId)?.hasStore ?? false);
                if ((world.Trading.HasStockToday(_actorId) || gangStore) && !refuses)
                    tabs.Add("Trade");
                tabs.Add("Favors");
                tabs.Add("Threat");
            }
            else
            {
                if (identity.guardArchetype == GuardArchetype.Corrupt && world.IsKnownCorrupt(_actorId))
                    tabs.Add("Bribe");
                if (AnyMetInmate(world))
                    tabs.Add("Snitch");
            }
            return tabs;
        }

        private bool AnyMetInmate(SocialWorld world)
        {
            foreach (var inmate in world.Roster.Inmates())
                if (world.HasMet(inmate.actorId)) return true;
            return false;
        }

        private void RebuildTabs()
        {
            foreach (Transform child in _tabStrip)
                Destroy(child.gameObject);

            foreach (var tab in VisibleTabs())
            {
                string captured = tab;
                MakeButton(_tabStrip, tab, () => { _tab = captured; RebuildTabs(); RebuildBody(); },
                    width: 140f, highlighted: _tab == tab);
            }
        }

        private void RebuildBody()
        {
            foreach (Transform child in _body)
                Destroy(child.gameObject);

            var world = SocialWorld.Instance;
            var identity = CurrentIdentity();
            if (world == null || identity == null) return;

            if (IsBusyPhase())
            {
                AddLine("They're standing for count. Not the time.", Prison.PrisonUITheme.ConcreteGrey);
                return;
            }

            switch (_tab)
            {
                case "Profile": BuildProfileTab(world, identity); break;
                case "Talk": BuildTalkTab(world, identity); break;
                case "Gift": BuildGiftTab(world, identity); break;
                case "Trade": BuildTradeTab(world, identity); break;
                case "Favors": BuildFavorsTab(world, identity); break;
                case "Threat": BuildThreatTab(world, identity); break;
                case "Bribe": BuildBribeTab(world, identity); break;
                case "Snitch": BuildSnitchTab(world, identity); break;
            }
        }

        // ------------------------------------------------------------------ tabs

        private void BuildProfileTab(SocialWorld world, NPCIdentity identity)
        {
            AddLine(DialogueLibrary.ArchetypeBlurb(identity), Color.white);
            if (!identity.isGuard)
                AddLine($"Cell {identity.cellIndex + 1}", Prison.PrisonUITheme.ConcreteGrey);
            else if (!string.IsNullOrEmpty(identity.roleLabel))
                AddLine(identity.roleLabel, Prison.PrisonUITheme.ConcreteGrey);

            var known = world.KnownGiftPrefs(_actorId);
            string prefs = known.Count > 0 ? string.Join(", ", known) : "??? (gift to find out)";
            AddLine($"Likes: {prefs}", Prison.PrisonUITheme.ConcreteGrey);

            if (world.IsKnownSnitch(_actorId))
                AddLine("Word is: this one talks to the guards.", StandingBandUI.Hostile);

            var strongest = world.GetMemory(_actorId).StrongestAbout(SocialTuning.PlayerActorId, 4f);
            if (strongest != null)
            {
                string line = DialogueLibrary.MemoryReaction(strongest.Value, _actorId * 17 + strongest.Value.day);
                if (!string.IsNullOrEmpty(line))
                    AddLine($"They remember: \"{line}\"", Prison.PrisonUITheme.CautionYellow);
            }
        }

        private void BuildTalkTab(SocialWorld world, NPCIdentity identity)
        {
            if (!string.IsNullOrEmpty(_lastChatLine))
                AddLine($"\"{_lastChatLine}\"", Color.white);

            float trust = world.Relationships.GetTrust(_actorId, SocialTuning.PlayerActorId);
            string unlock;
            if (trust >= SocialTuning.IntelEscapeLoreTrust) unlock = "They'll share escape lore.";
            else if (trust >= SocialTuning.IntelLootRouteTrust) unlock = "They'll share loot & route hints.";
            else if (trust >= SocialTuning.IntelScheduleTrust) unlock = "They'll share schedule & guard hints.";
            else unlock = $"Build trust {SocialTuning.IntelScheduleTrust:0}+ for real intel.";
            AddLine(unlock, Prison.PrisonUITheme.ConcreteGrey);

            if (world.ChatUsedThisPhase(_actorId))
            {
                AddLine("You already talked this phase.", Prison.PrisonUITheme.ConcreteGrey);
            }
            else
            {
                AddButton($"Chat (+{SocialTuning.ChatTrust:0} trust)", () =>
                {
                    _lastChatLine = world.Chat(_actorId);
                    RebuildBody();
                });
            }
        }

        private void BuildGiftTab(SocialWorld world, NPCIdentity identity)
        {
            var inventory = FindInventory();
            if (inventory == null)
            {
                AddLine("No inventory found.", Prison.PrisonUITheme.ConcreteGrey);
                return;
            }

            AddLine("Give an item:", Prison.PrisonUITheme.ConcreteGrey);
            bool any = false;
            foreach (var slot in inventory.inventorySlots)
            {
                if (slot == null || slot.IsEmpty) continue;
                any = true;
                var item = slot.item;
                var known = world.KnownGiftPrefs(_actorId);
                string hint = known.Contains(item.category) ? " ♥" : "";
                AddButton($"{item.itemName}{hint}", () =>
                {
                    if (!inventory.RemoveItem(item, 1)) return;
                    float delta = world.Gift(_actorId, item);
                    SocialToastUI.Show(delta >= SocialTuning.GiftBaseTrust * SocialTuning.LikedGiftMultiplier
                        ? $"{identity.ShortName} really likes that."
                        : $"{identity.ShortName} takes the {item.itemName}.");
                    RebuildBody();
                });
            }
            if (!any)
                AddLine("You have nothing to give.", Prison.PrisonUITheme.ConcreteGrey);
        }

        private void BuildTradeTab(SocialWorld world, NPCIdentity identity)
        {
            var inventory = FindInventory();
            var wallet = Prison.PlayerWallet.Instance;
            float cash = wallet != null ? wallet.Balance : 0f;
            AddLine($"Your cash: ${cash:0}", Prison.PrisonUITheme.CautionYellow);

            float trust = world.Relationships.GetTrust(_actorId, SocialTuning.PlayerActorId);
            bool member = identity.gangId != SocialTuning.IndependentGangId && world.Gangs.IsMemberOf(identity.gangId);

            var stock = world.Trading.GetStock(_actorId);
            if (stock.Count > 0)
            {
                AddLine("— Their stock —", Prison.PrisonUITheme.ConcreteGrey);
                foreach (var entry in stock)
                {
                    var item = entry.item;
                    // Career ladder: harder facilities inflate what everything costs the player.
                    float price = TradeMath.ApplyFacilityPriceMult(
                        TradeMath.BuyPrice(TradeMath.EffectiveBaseValue(item), identity.traits.greed,
                            trust, member, item.category == ItemCategory.Contraband),
                        Prison.Career.CareerSession.TradePriceMult);
                    AddButton($"Buy {item.itemName} ×{entry.count} — ${price:0}", () =>
                    {
                        if (wallet == null || wallet.Balance < price) { SocialToastUI.Show("Not enough cash."); return; }
                        if (inventory == null || !inventory.AddItem(item, 1)) { SocialToastUI.Show("No room in your pockets."); return; }
                        wallet.Add(-price);
                        world.Trading.ConsumeStock(_actorId, item);
                        world.ApplyPlayerAct(_actorId, SocialEventType.Trade, 2f, 0f);
                        RebuildBody();
                    });
                }
            }

            if (identity.archetype == PrisonerArchetype.Hustler && inventory != null)
            {
                AddLine("— They'll buy —", Prison.PrisonUITheme.ConcreteGrey);
                foreach (var slot in inventory.inventorySlots)
                {
                    if (slot == null || slot.IsEmpty) continue;
                    var item = slot.item;
                    float pay = TradeMath.SellPrice(TradeMath.EffectiveBaseValue(item), identity.traits.greed,
                        item.category == ItemCategory.Contraband);
                    AddButton($"Sell {item.itemName} — ${pay:0}", () =>
                    {
                        if (!inventory.RemoveItem(item, 1)) return;
                        wallet?.Add(pay);
                        world.ApplyPlayerAct(_actorId, SocialEventType.Trade, 1f, 0f);
                        RebuildBody();
                    });
                }
            }

            if (member && (GangCatalog.Get(identity.gangId)?.hasStore ?? false))
            {
                var gang = GangCatalog.Get(identity.gangId);
                AddLine($"— {gang.displayName} store (under your bed after count) —", Prison.PrisonUITheme.ConcreteGrey);
                foreach (var entry in world.Trading.GetGangStoreStock(identity.gangId))
                {
                    var item = entry.item;
                    float price = TradeMath.ApplyFacilityPriceMult(
                        TradeMath.BuyPrice(TradeMath.EffectiveBaseValue(item), 50,
                            trust, true, item.category == ItemCategory.Contraband),
                        Prison.Career.CareerSession.TradePriceMult);
                    AddButton($"Order {item.itemName} — ${price:0}", () =>
                    {
                        if (wallet == null || wallet.Balance < price) { SocialToastUI.Show("Not enough cash."); return; }
                        wallet.Add(-price);
                        world.Trading.QueueBedDelivery(item, 1);
                        SocialToastUI.Show("It'll be under your bed after morning count.");
                        RebuildBody();
                    });
                }
            }
        }

        private void BuildFavorsTab(SocialWorld world, NPCIdentity identity)
        {
            var inventory = FindInventory();
            var wallet = Prison.PlayerWallet.Instance;

            // Initiation offer from the Shot-Caller (spec §5)
            if (identity.archetype == PrisonerArchetype.ShotCaller
                && world.Gangs.CanOfferInitiation(identity.gangId, world.CurrentDay))
            {
                var gang = GangCatalog.Get(identity.gangId);
                AddLine($"\"You've proven something. Run one job and you're {gang.displayName}.\"", Prison.PrisonUITheme.CautionYellow);
                AddButton("Accept initiation", () =>
                {
                    var favor = world.Favors.CreateInitiationFavor(identity, world.CurrentDay);
                    if (favor == null) { SocialToastUI.Show("They have nothing for you right now."); return; }
                    world.Gangs.OfferInitiation(identity.gangId, world.CurrentDay);
                    SocialToastUI.Show($"Initiation: {favor.Describe(world.Roster)} (by day {favor.deadlineDay})");
                    RebuildBody();
                });
                AddButton("Refuse (they won't ask again for a while)", () =>
                {
                    world.Gangs.OfferInitiation(identity.gangId, world.CurrentDay);
                    world.Gangs.RefuseOrFailInitiation(world.CurrentDay);
                    world.ApplyShotCallerRespectHit(identity.gangId);
                    RebuildBody();
                });
            }

            var offer = world.Favors.OpenOfferFor(_actorId);
            if (offer != null)
            {
                AddLine($"They ask: {offer.Describe(world.Roster)} (${offer.cashReward:0} in it for you)", Color.white);
                if (offer.state == FavorState.Offered)
                {
                    AddButton("Accept", () => { world.Favors.AcceptOffer(offer); RebuildBody(); });
                    AddButton("Decline", () => { world.Favors.Decline(offer); RebuildBody(); });
                }
                else if ((offer.kind == FavorKind.Fetch || offer.kind == FavorKind.Initiation)
                         && offer.item != null && inventory != null && inventory.HasItem(offer.item, 1))
                {
                    AddButton($"Hand over {offer.item.itemName}", () =>
                    {
                        inventory.RemoveItem(offer.item, 1);
                        world.Favors.Complete(offer);
                        RebuildBody();
                    });
                }
                else
                {
                    AddLine("(In progress — get it done before the deadline.)", Prison.PrisonUITheme.ConcreteGrey);
                }
            }

            AddLine("— Ask a favor —", Prison.PrisonUITheme.ConcreteGrey);
            BuildAskFavorButton(world, identity, FavorKind.Lookout,
                $"Lookout — ${SocialTuning.LookoutCost:0} (warns of guards, one phase)", SocialTuning.LookoutCost);
            BuildAskFavorButton(world, identity, FavorKind.Distraction,
                $"Distraction — ${SocialTuning.DistractionCost:0} (pulls the nearest guard ~30s)", SocialTuning.DistractionCost);

            if (world.Favors.CanAsk(FavorKind.SourceItem, identity, world.Gangs, out string srcBlocked))
            {
                AddButton("Source an item (1.5× price, 1–2 days)", () => BuildSourceSubmenu(world, identity));
            }
            else AddLine($"Source item: {srcBlocked}", Prison.PrisonUITheme.ConcreteGrey);

            if (world.Favors.CanAsk(FavorKind.HoldStash, identity, world.Gangs, out string stashBlocked))
            {
                AddButton("Hold my stash (2 items through shakedown)", () =>
                {
                    var handed = HandOverStash(world, identity);
                    if (handed == 0) SocialToastUI.Show("Nothing risky in your pockets to stash.");
                    RebuildBody();
                });
            }
            else AddLine($"Hold stash: {stashBlocked}", Prison.PrisonUITheme.ConcreteGrey);

            if (world.Favors.CanAsk(FavorKind.SilenceSnitch, identity, world.Gangs, out string silenceBlocked))
            {
                foreach (var inmate in world.Roster.Inmates())
                {
                    if (!world.IsKnownSnitch(inmate.actorId)) continue;
                    var snitch = inmate;
                    AddButton($"Silence {snitch.DisplayName} (gang standing −10)", () =>
                    {
                        world.Snitches.Mute(snitch.actorId, world.CurrentDay);
                        foreach (var mate in world.Roster.MembersOf(identity.gangId))
                            world.Relationships.ApplyDeltas(mate.actorId, SocialTuning.PlayerActorId,
                                -SocialTuning.SilenceSnitchGangStandingCost, 0f, mate.traits);
                        world.ApplyPlayerAct(snitch.actorId, SocialEventType.IntimidationSuccess);
                        SocialToastUI.Show($"{snitch.ShortName} will keep their mouth shut for {SocialTuning.SilenceSnitchMuteDays} days.");
                        RebuildBody();
                    });
                }
            }
            else AddLine($"Silence a snitch: {silenceBlocked}", Prison.PrisonUITheme.ConcreteGrey);
        }

        private void BuildAskFavorButton(SocialWorld world, NPCIdentity identity, FavorKind kind, string label, float cost)
        {
            if (world.Favors.CanAsk(kind, identity, world.Gangs, out string blocked))
            {
                AddButton(label, () =>
                {
                    var wallet = Prison.PlayerWallet.Instance;
                    if (wallet == null || wallet.Balance < cost) { SocialToastUI.Show("Not enough cash."); return; }
                    if (kind == FavorKind.Distraction && !SocialFavorRuntime.CanDistractAnyGuard())
                    {
                        SocialToastUI.Show("No guard close enough to pull.");
                        return;
                    }
                    wallet.Add(-cost);
                    world.Favors.StartAskFavor(kind, identity, world.CurrentDay);
                    SocialToastUI.Show(kind == FavorKind.Lookout
                        ? $"{identity.ShortName} is watching your back this phase."
                        : $"{identity.ShortName} starts something loud...");
                    RebuildBody();
                });
            }
            else
            {
                AddLine($"{kind}: {blocked}", Prison.PrisonUITheme.ConcreteGrey);
            }
        }

        private void BuildSourceSubmenu(SocialWorld world, NPCIdentity identity)
        {
            foreach (Transform child in _body)
                Destroy(child.gameObject);
            AddLine("What do you need sourced?", Color.white);
            foreach (var category in new[] { ItemCategory.CraftingPart, ItemCategory.Tool, ItemCategory.Contraband })
            {
                var cat = category;
                var sample = world.Favors.RandomItemOfCategory(cat);
                if (sample == null) continue;
                float price = TradeMath.ApplyFacilityPriceMult(
                    TradeMath.EffectiveBaseValue(sample) * SocialTuning.SourceItemPriceFactor,
                    Prison.Career.CareerSession.TradePriceMult);
                AddButton($"{cat} — ${price:0}", () =>
                {
                    var wallet = Prison.PlayerWallet.Instance;
                    if (wallet == null || wallet.Balance < price) { SocialToastUI.Show("Not enough cash."); return; }
                    wallet.Add(-price);
                    var favor = world.Favors.StartAskFavor(FavorKind.SourceItem, identity, world.CurrentDay, category: cat);
                    favor.item = sample;
                    SocialToastUI.Show($"{identity.ShortName} will have it in a day or two.");
                    RebuildBody();
                });
            }
            AddButton("Back", RebuildBody);
        }

        /// <summary>Hands your riskiest (confiscable) items to the holder — that's what stashing is for.</summary>
        private int HandOverStash(SocialWorld world, NPCIdentity identity)
        {
            var inventory = FindInventory();
            if (inventory == null) return 0;

            var toStash = new List<ItemData>();
            foreach (var slot in inventory.inventorySlots)
            {
                if (slot == null || slot.IsEmpty) continue;
                var item = slot.item;
                bool risky = item.category == ItemCategory.Tool
                    || item.category == ItemCategory.Weapon
                    || item.category == ItemCategory.Contraband;
                if (!risky) continue;
                toStash.Add(item);
                if (toStash.Count >= SocialTuning.HoldStashMaxItems) break;
            }
            if (toStash.Count == 0) return 0;

            var favor = world.Favors.StartAskFavor(FavorKind.HoldStash, identity, world.CurrentDay);
            favor.heldStash = new List<ItemData>();
            foreach (var item in toStash)
            {
                if (inventory.RemoveItem(item, 1))
                    favor.heldStash.Add(item);
            }
            SocialToastUI.Show($"{identity.ShortName} tucks away {favor.heldStash.Count} item(s) until after the shakedown.");
            return favor.heldStash.Count;
        }

        private void BuildThreatTab(SocialWorld world, NPCIdentity identity)
        {
            float respect = world.Relationships.GetRespect(_actorId, SocialTuning.PlayerActorId);
            float strength = Prison.PlayerStats.Instance != null ? Prison.PlayerStats.Instance.Strength : 0f;
            float chance = RelationshipMath.IntimidationChance(respect, strength, identity.traits.nerve);
            AddLine($"Success chance: ~{chance * 100f:0}% (your respect + strength vs their nerve)", Prison.PrisonUITheme.ConcreteGrey);
            AddLine("Fail: you lose respect — and they might report you.", Prison.PrisonUITheme.ConcreteGrey);
            if (world.IntimidateUsedThisPhase(_actorId))
            {
                AddLine("You've already leaned on them this phase. Push again later.", Prison.PrisonUITheme.ConcreteGrey);
                return;
            }
            AddButton("Intimidate", () =>
            {
                bool success = world.Intimidate(_actorId, out _);
                SocialToastUI.Show(success
                    ? $"{identity.ShortName} backs down."
                    : $"{identity.ShortName} laughs in your face.");
                RebuildBody();
            });
        }

        private void BuildBribeTab(SocialWorld world, NPCIdentity identity)
        {
            AddLine("\"Everything's negotiable. Quietly.\"", Color.white);
            // Career ladder: bribes are the steepest sink up the ladder (bribeCostMult).
            float bribeMult = Prison.Career.CareerSession.BribeCostMult;
            float clearTip = TradeMath.ApplyFacilityPriceMult(SocialTuning.BribeClearTip, bribeMult);
            float skipCell = TradeMath.ApplyFacilityPriceMult(SocialTuning.BribeSkipShakedown, bribeMult);
            float blindEye = TradeMath.ApplyFacilityPriceMult(SocialTuning.BribeBlindEye, bribeMult);
            AddButton($"Clear a tip against you — ${clearTip:0}", () =>
            {
                if (!world.BribeCorrupt(_actorId, clearTip, "cleartip"))
                    SocialToastUI.Show("Not enough cash.");
                RebuildBody();
            });
            AddButton($"Skip your cell next shakedown — ${skipCell:0}", () =>
            {
                if (!world.BribeCorrupt(_actorId, skipCell, "skipcell"))
                    SocialToastUI.Show("Not enough cash.");
                RebuildBody();
            });
            AddButton($"Blind eye this phase — ${blindEye:0}", () =>
            {
                if (!world.BribeCorrupt(_actorId, blindEye, "blindeye"))
                    SocialToastUI.Show("Not enough cash.");
                RebuildBody();
            });
        }

        private void BuildSnitchTab(SocialWorld world, NPCIdentity identity)
        {
            AddLine("Point a finger. Their cell gets tossed — and word gets around.", Prison.PrisonUITheme.ConcreteGrey);
            foreach (var inmate in world.Roster.Inmates())
            {
                if (!world.HasMet(inmate.actorId)) continue;
                var target = inmate;
                AddButton($"Tip about {target.DisplayName}", () =>
                {
                    world.PlayerSnitchOn(target.actorId, _actorId);
                    SocialToastUI.Show($"Ofc. {identity.lastName} nods. {target.ShortName}'s cell is getting searched.");
                    Close();
                });
            }
        }

        // ------------------------------------------------------------------ body helpers

        private void AddLine(string text, Color color)
        {
            var go = new GameObject("Line", typeof(RectTransform));
            go.transform.SetParent(_body, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 22f;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 32f;
        }

        private void AddButton(string text, System.Action onClick)
        {
            MakeButton(_body, text, onClick, width: -1f);
        }

        private void MakeButton(RectTransform parent, string text, System.Action onClick,
            float width = -1f, bool highlighted = false)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = highlighted
                ? new Color(0.85f, 0.75f, 0.3f, 0.85f)
                : new Color(0.16f, 0.2f, 0.24f, 0.9f);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 46f;
            if (width > 0f) layoutElement.preferredWidth = width;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)textGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 2f);
            rt.offsetMax = new Vector2(-8f, -2f);
            var label = textGo.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 20f;
            label.alignment = TextAlignmentOptions.Midline;
            label.color = highlighted ? Color.black : Color.white;

            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }

        private PlayerInventory FindInventory()
        {
            var world = SocialWorld.Instance;
            if (world != null && world.PlayerTransform != null)
            {
                var inv = world.PlayerTransform.GetComponentInChildren<PlayerInventory>();
                if (inv != null) return inv;
            }
            return FindAnyObjectByType<PlayerInventory>();
        }
    }
}
