using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison.Social
{
    /// <summary>
    /// Notebook social dossier (design of record: Social Dossier — Relationships &amp; Gangs).
    /// Two pages inside the notebook's social tab: Relationships (filterable list + detail
    /// with fog-of-war) and Gangs (rank, standing meters, roster fog, territory, store,
    /// active quest). Builds itself programmatically inside the host panel.
    /// </summary>
    public class SocialDossierUI : MonoBehaviour
    {
        private enum Page { Relationships, Gangs }
        private enum Filter { All, Inmates, Guards, Friends, Enemies, Gang }
        private enum Sort { Standing, Name, Trust, Respect }

        private Page _page = Page.Relationships;
        private Filter _filter = Filter.All;
        private Sort _sort = Sort.Standing;
        private int _selectedActor = SocialTuning.NoActor;

        private TMP_Text _headerTier;
        private RectTransform _pageStrip;
        private RectTransform _listRoot;
        private RectTransform _detailRoot;
        private RectTransform _gangsRoot;
        private bool _built;

        public static SocialDossierUI Attach(GameObject hostPanel)
        {
            var existing = hostPanel.GetComponent<SocialDossierUI>();
            if (existing != null) return existing;
            return hostPanel.AddComponent<SocialDossierUI>();
        }

        private float _nextAllowedRefresh;

        /// <summary>Host calls this every frame while the tab is open; rebuilds are throttled.</summary>
        public void Refresh() => Refresh(false);

        private void Refresh(bool force)
        {
            if (!_built) Build();
            if (!force && Time.unscaledTime < _nextAllowedRefresh) return;
            _nextAllowedRefresh = Time.unscaledTime + 0.5f;

            RefreshHeader();
            if (_page == Page.Relationships) { RefreshList(); RefreshDetail(); }
            else RefreshGangs();
        }

        // ------------------------------------------------------------------ build

        private void Build()
        {
            _built = true;

            // Clear any legacy v1 rows living under this panel.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Dossier")) continue;
                child.gameObject.SetActive(false);
            }

            _headerTier = MakeText(transform, "DossierHeader", new Vector2(8f, -4f), new Vector2(-16f, 26f), 18f,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, stretchX: true);

            var stripGo = new GameObject("DossierPageStrip", typeof(RectTransform));
            stripGo.transform.SetParent(transform, false);
            _pageStrip = (RectTransform)stripGo.transform;
            _pageStrip.anchorMin = new Vector2(0f, 1f);
            _pageStrip.anchorMax = new Vector2(1f, 1f);
            _pageStrip.pivot = new Vector2(0.5f, 1f);
            _pageStrip.anchoredPosition = new Vector2(0f, -32f);
            _pageStrip.sizeDelta = new Vector2(-16f, 30f);
            var strip = stripGo.AddComponent<HorizontalLayoutGroup>();
            strip.spacing = 6f;
            strip.childForceExpandWidth = false;
            strip.childForceExpandHeight = true;

            _listRoot = MakeScrollColumn("DossierList", new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                new Vector2(8f, 8f), new Vector2(-4f, -96f));
            _detailRoot = MakeScrollColumn("DossierDetail", new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                new Vector2(4f, 8f), new Vector2(-8f, -96f));
            _gangsRoot = MakeScrollColumn("DossierGangs", new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(8f, 8f), new Vector2(-8f, -96f));

            RebuildPageStrip();
        }

        private RectTransform MakeScrollColumn(string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(go.transform, false);
            var content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = go.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 18f;
            return content;
        }

        private void RebuildPageStrip()
        {
            foreach (Transform child in _pageStrip)
                Destroy(child.gameObject);

            MakeChip(_pageStrip, "Relationships", _page == Page.Relationships, () => { _page = Page.Relationships; RebuildPageStrip(); Refresh(true); });
            MakeChip(_pageStrip, "Gangs", _page == Page.Gangs, () => { _page = Page.Gangs; RebuildPageStrip(); Refresh(true); });

            if (_page == Page.Relationships)
            {
                foreach (Filter f in System.Enum.GetValues(typeof(Filter)))
                {
                    var captured = f;
                    MakeChip(_pageStrip, f.ToString(), _filter == f, () => { _filter = captured; RebuildPageStrip(); Refresh(true); }, small: true);
                }
                MakeChip(_pageStrip, $"Sort: {_sort}", false, () =>
                {
                    _sort = (Sort)(((int)_sort + 1) % System.Enum.GetValues(typeof(Sort)).Length);
                    RebuildPageStrip();
                    Refresh(true);
                }, small: true);
            }

            bool showRelationship = _page == Page.Relationships;
            _listRoot.parent.gameObject.SetActive(showRelationship);
            _detailRoot.parent.gameObject.SetActive(showRelationship);
            _gangsRoot.parent.gameObject.SetActive(!showRelationship);
        }

        // ------------------------------------------------------------------ header

        private void RefreshHeader()
        {
            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt) { if (_headerTier != null) _headerTier.text = "SOCIAL — no data"; return; }
            var tier = world.GetReputationTier();
            _headerTier.text = $"REPUTATION: {tier.ToString().ToUpperInvariant()}  ·  Day {world.CurrentDay}";
            _headerTier.color = Prison.PrisonUITheme.CautionYellow;
        }

        // ------------------------------------------------------------------ relationships page

        private List<NPCIdentity> FilteredActors(SocialWorld world)
        {
            var result = new List<NPCIdentity>();
            foreach (var identity in world.Roster.identities)
            {
                if (!world.HasHeardOf(identity.actorId) && !world.HasMet(identity.actorId)) continue;

                var band = world.Relationships.GetBand(identity.actorId, SocialTuning.PlayerActorId);
                switch (_filter)
                {
                    case Filter.Inmates: if (identity.isGuard) continue; break;
                    case Filter.Guards: if (!identity.isGuard) continue; break;
                    case Filter.Friends: if (!RelationshipMath.IsFriendBand(band)) continue; break;
                    case Filter.Enemies: if (!RelationshipMath.IsEnemyBand(band)) continue; break;
                    case Filter.Gang:
                        int myGang = world.Gangs.MemberGangId;
                        if (myGang == SocialTuning.IndependentGangId || identity.gangId != myGang) continue;
                        break;
                }
                result.Add(identity);
            }

            result.Sort((a, b) =>
            {
                var ra = world.Relationships.Get(a.actorId, SocialTuning.PlayerActorId);
                var rb = world.Relationships.Get(b.actorId, SocialTuning.PlayerActorId);
                switch (_sort)
                {
                    case Sort.Name: return string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.Ordinal);
                    case Sort.Trust: return rb.trust.CompareTo(ra.trust);
                    case Sort.Respect: return rb.respect.CompareTo(ra.respect);
                    default: return RelationshipMath.Standing(rb).CompareTo(RelationshipMath.Standing(ra));
                }
            });
            return result;
        }

        private void RefreshList()
        {
            foreach (Transform child in _listRoot)
                Destroy(child.gameObject);

            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt) return;

            var actors = FilteredActors(world);
            if (actors.Count == 0)
            {
                MakeText(_listRoot, "Empty", Vector2.zero, new Vector2(0f, 24f), 15f,
                    FontStyles.Italic, TextAlignmentOptions.MidlineLeft, layout: true)
                    .text = "Nobody yet. Get out there and talk.";
                return;
            }

            foreach (var identity in actors)
            {
                var record = world.Relationships.Get(identity.actorId, SocialTuning.PlayerActorId);
                var band = RelationshipMath.GetBand(RelationshipMath.Standing(record));
                bool met = world.HasMet(identity.actorId);

                string gangBadge = identity.isGuard ? "GUARD"
                    : identity.gangId != SocialTuning.IndependentGangId
                        ? (GangCatalog.Get(identity.gangId)?.displayName ?? "?").Substring(0, 3).ToUpperInvariant()
                        : "—";
                string icons = "";
                if (!identity.isGuard && world.Favors.OpenOfferFor(identity.actorId) != null) icons += " !";
                if (!identity.isGuard && world.Trading.HasStockToday(identity.actorId)) icons += " $";

                string name = met ? identity.DisplayName : $"{identity.DisplayName} (heard of)";
                string rowText = $"<color=#{ColorUtility.ToHtmlStringRGB(StandingBandUI.ColorOf(band))}>▐</color> {name}  <size=70%>[{gangBadge}]{icons}</size>\n" +
                                 $"<size=65%>T {MiniBar(record.trust)}  R {MiniBar(record.respect)}</size>";

                int captured = identity.actorId;
                MakeRowButton(_listRoot, rowText, met ? Color.white : new Color(0.65f, 0.65f, 0.65f),
                    _selectedActor == identity.actorId, () => { _selectedActor = captured; Refresh(true); });
            }
        }

        private void RefreshDetail()
        {
            foreach (Transform child in _detailRoot)
                Destroy(child.gameObject);

            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt) return;
            var identity = world.GetIdentity(_selectedActor);
            if (identity == null)
            {
                MakeText(_detailRoot, "Hint", Vector2.zero, new Vector2(0f, 24f), 15f,
                    FontStyles.Italic, TextAlignmentOptions.MidlineLeft, layout: true)
                    .text = "Select someone on the left.";
                return;
            }

            bool met = world.HasMet(identity.actorId);
            var record = world.Relationships.Get(identity.actorId, SocialTuning.PlayerActorId);
            float standing = RelationshipMath.Standing(record);
            var band = RelationshipMath.GetBand(standing);

            AddDetailLine(identity.DisplayName, 18f, FontStyles.Bold, Color.white);
            string sub = identity.isGuard
                ? $"Guard · {identity.guardArchetype}"
                : $"{(identity.gangId != SocialTuning.IndependentGangId ? GangCatalog.Get(identity.gangId)?.displayName : "Independent")} · {identity.archetype} · Cell {identity.cellIndex + 1}";
            AddDetailLine(sub, 14f, FontStyles.Normal, Prison.PrisonUITheme.ConcreteGrey);
            AddDetailLine($"Band: {StandingBandUI.Label(band)}", 15f, FontStyles.Bold, StandingBandUI.ColorOf(band));

            if (!met)
            {
                AddDetailLine("You've only heard the name. Talk to them in person.", 14f, FontStyles.Italic, Prison.PrisonUITheme.ConcreteGrey);
                return;
            }

            AddDetailLine($"Trust   {MiniBar(record.trust)} {record.trust:+0;-0;0}", 15f, FontStyles.Normal, Color.white);
            AddDetailLine($"Respect {MiniBar(record.respect)} {record.respect:+0;-0;0}", 15f, FontStyles.Normal, Color.white);
            AddDetailLine($"Standing {MiniBar(standing)} {standing:+0;-0;0}", 15f, FontStyles.Normal, Prison.PrisonUITheme.ConcreteGrey);
            AddDetailLine(DialogueLibrary.ArchetypeBlurb(identity), 14f, FontStyles.Italic, Prison.PrisonUITheme.ConcreteGrey);

            var known = world.KnownGiftPrefs(identity.actorId);
            AddDetailLine(known.Count > 0 ? $"Gifts: {string.Join(", ", known)}" : "Gifts: ???", 14f, FontStyles.Normal, Prison.PrisonUITheme.ConcreteGrey);

            if (world.IsKnownSnitch(identity.actorId))
                AddDetailLine("⚠ Known snitch", 14f, FontStyles.Bold, StandingBandUI.Hostile);
            if (identity.isGuard && world.IsKnownCorrupt(identity.actorId))
                AddDetailLine("$ Takes bribes", 14f, FontStyles.Bold, Prison.PrisonUITheme.CautionYellow);

            // Up to 3 memory snippets you know about (their strongest memories involving you).
            var memories = world.GetMemory(identity.actorId).AllAbout(SocialTuning.PlayerActorId);
            int shown = 0;
            foreach (var evt in memories)
            {
                if (shown >= 3) break;
                string line = DescribeMemory(evt);
                if (line == null) continue;
                AddDetailLine($"· {line} (day {evt.day})", 13f, FontStyles.Normal, Prison.PrisonUITheme.CautionYellow);
                shown++;
            }

            AddDetailLine("Talk to them in person to Chat / Gift / Trade.", 12f, FontStyles.Italic, Prison.PrisonUITheme.ConcreteGrey);
        }

        private string DescribeMemory(in SocialEvent evt)
        {
            switch (evt.type)
            {
                case SocialEventType.Chat: return "You talked";
                case SocialEventType.Gift: return "You gave them a gift";
                case SocialEventType.FavorForNpc: return "You did them a favor";
                case SocialEventType.RiskyFavor: return "You ran a risky job for them";
                case SocialEventType.Protection: return "You had their back";
                case SocialEventType.IntimidationSuccess: return "You leaned on them";
                case SocialEventType.IntimidationFail: return "You tried to lean on them — and failed";
                case SocialEventType.CrimeWitnessed: return evt.source == SocialEventSource.Heard ? "They heard about your business" : "They saw your business";
                case SocialEventType.BribeWitnessed: return "They saw you grease a guard";
                case SocialEventType.SnitchedOn: return "You ratted them out";
                case SocialEventType.CaughtStealing: return "They caught you stealing";
                case SocialEventType.GangBetrayal: return "You betrayed the gang";
                case SocialEventType.Trade: return "You did business";
                case SocialEventType.Argument: return "You got into it";
                default: return null;
            }
        }

        // ------------------------------------------------------------------ gangs page

        private void RefreshGangs()
        {
            foreach (Transform child in _gangsRoot)
                Destroy(child.gameObject);

            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt) return;

            // Your status
            int memberGang = world.Gangs.MemberGangId;
            string status = memberGang != SocialTuning.IndependentGangId
                ? $"You are {world.Gangs.GetRank(memberGang)} of the {GangCatalog.Get(memberGang)?.displayName}"
                : "Unaffiliated";
            AddGangLine(status, 17f, FontStyles.Bold, Color.white);

            foreach (var gang in GangCatalog.All())
            {
                if (world.Gangs.IsTraitorLocked(gang.gangId))
                    AddGangLine($"⚠ TRAITOR — {gang.displayName} won't take you back this run.", 14f, FontStyles.Bold, StandingBandUI.Enemy);
            }

            foreach (var gang in GangCatalog.All())
            {
                float standing = world.Gangs.GangStanding(gang.gangId);
                var rank = world.Gangs.GetRank(gang.gangId);
                string rankLabel = world.Gangs.IsTraitorLocked(gang.gangId) ? "LOCKED" : rank.ToString();

                AddGangLine($"— {gang.displayName} —", 17f, FontStyles.Bold, Prison.PrisonUITheme.CautionYellow);
                AddGangLine(gang.flavor, 13f, FontStyles.Italic, Prison.PrisonUITheme.ConcreteGrey);
                AddGangLine($"Standing {MiniBar(standing)} {standing:+0;-0;0}   ·   Your rank: {rankLabel}", 14f, FontStyles.Normal, Color.white);
                AddGangLine($"Territory: {gang.territoryLabel}", 13f, FontStyles.Normal, Prison.PrisonUITheme.ConcreteGrey);

                if (standing < 0f && !world.Gangs.IsMemberOf(gang.gangId))
                    AddGangLine($"✗ {gang.displayName} don't want you in their corner.", 13f, FontStyles.Bold, StandingBandUI.Hostile);

                // Roster with fog-of-war
                var rosterNames = new List<string>();
                foreach (var member in world.Roster.MembersOf(gang.gangId))
                {
                    bool known = world.HasMet(member.actorId) || world.HasHeardOf(member.actorId);
                    string star = member.archetype == PrisonerArchetype.ShotCaller ? "★ " : "";
                    rosterNames.Add(known ? $"{star}{member.DisplayName}" : $"{star}???");
                }
                AddGangLine($"Roster: {string.Join(" · ", rosterNames)}", 13f, FontStyles.Normal, Prison.PrisonUITheme.ConcreteGrey);

                if (gang.hasStore)
                {
                    string storeNote = world.Gangs.IsMemberOf(gang.gangId)
                        ? "Member shop open — order via Talk → Trade; delivers under your bed after count."
                        : "Members-only shop (join to unlock).";
                    AddGangLine($"Store: {storeNote}", 13f, FontStyles.Normal, Prison.PrisonUITheme.ConcreteGrey);
                }

                // Active quest: pending initiation or gang favor
                foreach (var favor in world.Favors.All)
                {
                    if (favor.gangId != gang.gangId || favor.state != FavorState.Active) continue;
                    if (favor.kind == FavorKind.Initiation)
                        AddGangLine($"► Initiation: {favor.Describe(world.Roster)} (by day {favor.deadlineDay})", 13f, FontStyles.Bold, Prison.PrisonUITheme.CautionYellow);
                    else if (favor.isGangFavor)
                        AddGangLine($"► Gang favor: {favor.Describe(world.Roster)}", 13f, FontStyles.Normal, Prison.PrisonUITheme.CautionYellow);
                }

                if (world.Gangs.IsMemberOf(gang.gangId))
                    AddGangLine($"Gang favors completed: {world.Gangs.GangFavorsCompleted(gang.gangId)} (Trusted at {SocialTuning.TrustedMinGangFavors}+ and standing {SocialTuning.TrustedMinStanding:0}+)", 12f, FontStyles.Normal, Prison.PrisonUITheme.ConcreteGrey);
            }

            int independents = 0;
            foreach (var inmate in world.Roster.Inmates())
                if (inmate.gangId == SocialTuning.IndependentGangId && (world.HasMet(inmate.actorId) || world.HasHeardOf(inmate.actorId)))
                    independents++;
            AddGangLine($"Independents known: {independents}", 13f, FontStyles.Normal, Prison.PrisonUITheme.ConcreteGrey);
        }

        // ------------------------------------------------------------------ helpers

        private static string MiniBar(float value)
        {
            int filled = Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(-100f, 100f, value) * 8f), 0, 8);
            var sb = new System.Text.StringBuilder(8);
            for (int i = 0; i < 8; i++) sb.Append(i < filled ? "█" : "░");
            return sb.ToString();
        }

        private void AddDetailLine(string text, float size, FontStyles style, Color color) =>
            AddLineTo(_detailRoot, text, size, style, color);

        private void AddGangLine(string text, float size, FontStyles style, Color color) =>
            AddLineTo(_gangsRoot, text, size, style, color);

        private void AddLineTo(RectTransform parent, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject("Line", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            var fitter = go.AddComponent<LayoutElement>();
            fitter.minHeight = size + 8f;
            fitter.flexibleWidth = 1f;
        }

        private void MakeRowButton(RectTransform parent, string text, Color textColor, bool selected, System.Action onClick)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = selected
                ? new Color(0.85f, 0.75f, 0.3f, 0.25f)
                : new Color(1f, 1f, 1f, 0.04f);
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 44f;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)textGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, 2f);
            rt.offsetMax = new Vector2(-6f, -2f);
            var label = textGo.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 14f;
            label.color = textColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }

        private void MakeChip(RectTransform parent, string text, bool active, System.Action onClick, bool small = false)
        {
            var go = new GameObject("Chip", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = active
                ? new Color(0.85f, 0.75f, 0.3f, 0.85f)
                : new Color(0.16f, 0.2f, 0.24f, 0.9f);
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 26f;
            layoutElement.preferredWidth = small ? 84f : 130f;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)textGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var label = textGo.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = small ? 12f : 14f;
            label.alignment = TextAlignmentOptions.Midline;
            label.color = active ? Color.black : Color.white;

            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }

        private TMP_Text MakeText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize,
            FontStyles style, TextAlignmentOptions align, bool stretchX = false, bool layout = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            if (layout)
            {
                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = size.y;
            }
            else if (stretchX)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(size.x, size.y);
            }
            else
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
            }
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = align;
            text.color = Color.white;
            return text;
        }
    }
}
