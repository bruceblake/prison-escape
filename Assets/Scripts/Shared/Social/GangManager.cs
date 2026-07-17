using System;
using System.Collections.Generic;

namespace Prison.Social
{
    /// <summary>
    /// Runtime gang membership state for the player: exclusive membership ladder
    /// (Outsider → Associate → Member → Trusted), initiation offers with cooldowns,
    /// and Traitor lockout (spec §5). Plain class owned by <see cref="SocialWorld"/>;
    /// gate math is pure and EditMode-testable.
    /// </summary>
    public class GangManager
    {
        private readonly SocialRoster _roster;
        private readonly RelationshipStore _relationships;

        private readonly bool[] _isMember = new bool[GangCatalog.GangCount];
        private readonly bool[] _traitorLocked = new bool[GangCatalog.GangCount];
        private readonly int[] _gangFavorsCompleted = new int[GangCatalog.GangCount];
        private readonly int[] _initiationCooldownUntilDay = new int[GangCatalog.GangCount];

        /// <summary>Gang the player joined as Member (exclusive), or Independent (-1).</summary>
        public int MemberGangId { get; private set; } = SocialTuning.IndependentGangId;

        /// <summary>Active initiation offer: gang id, or -1 when none. Deadline day is inclusive.</summary>
        public int PendingInitiationGangId { get; private set; } = SocialTuning.IndependentGangId;
        public int PendingInitiationDeadlineDay { get; private set; }

        public event Action<int, GangRank> OnRankChanged;
        public event Action<int> OnTraitorLocked;

        public GangManager(SocialRoster roster, RelationshipStore relationships)
        {
            _roster = roster;
            _relationships = relationships;
            for (int i = 0; i < GangCatalog.GangCount; i++)
                _initiationCooldownUntilDay[i] = -1;
        }

        /// <summary>Average Standing toward the player across living members of the gang.</summary>
        public float GangStanding(int gangId)
        {
            float sum = 0f;
            int count = 0;
            foreach (var member in _roster.MembersOf(gangId))
            {
                sum += _relationships.GetStanding(member.actorId, SocialTuning.PlayerActorId);
                count++;
            }
            return count > 0 ? sum / count : 0f;
        }

        public GangRank GetRank(int gangId)
        {
            if (gangId < 0 || gangId >= GangCatalog.GangCount) return GangRank.Outsider;
            if (_isMember[gangId])
            {
                return GangStanding(gangId) >= SocialTuning.TrustedMinStanding
                       && _gangFavorsCompleted[gangId] >= SocialTuning.TrustedMinGangFavors
                    ? GangRank.Trusted
                    : GangRank.Member;
            }
            return GangStanding(gangId) >= SocialTuning.AssociateMinStanding
                ? GangRank.Associate
                : GangRank.Outsider;
        }

        public GangRank PlayerBestRank() =>
            MemberGangId != SocialTuning.IndependentGangId ? GetRank(MemberGangId) : BestAssociateRank();

        private GangRank BestAssociateRank()
        {
            for (int g = 0; g < GangCatalog.GangCount; g++)
                if (GetRank(g) == GangRank.Associate) return GangRank.Associate;
            return GangRank.Outsider;
        }

        public bool IsTraitorLocked(int gangId) =>
            gangId >= 0 && gangId < GangCatalog.GangCount && _traitorLocked[gangId];

        public int GangFavorsCompleted(int gangId) =>
            gangId >= 0 && gangId < GangCatalog.GangCount ? _gangFavorsCompleted[gangId] : 0;

        /// <summary>Standing-propagation factor when the player helps/hurts a member of this gang (spec §5).</summary>
        public float PropagationFactor(int gangId)
        {
            var rank = GetRank(gangId);
            return rank == GangRank.Member || rank == GangRank.Trusted
                ? SocialTuning.PropagationMember
                : SocialTuning.PropagationOutsider;
        }

        /// <summary>Rivals refuse to trade below gang standing −25.</summary>
        public bool RefusesTrade(int npcGangId)
        {
            if (npcGangId == SocialTuning.IndependentGangId) return false;
            return GangStanding(npcGangId) < SocialTuning.RivalTradeRefusalStanding;
        }

        /// <summary>
        /// Whether this gang's Shot-Caller can offer initiation right now:
        /// Associate standing, not already a member of any gang, not traitor-locked,
        /// no pending offer, and past any refusal cooldown.
        /// </summary>
        public bool CanOfferInitiation(int gangId, int currentDay)
        {
            if (gangId < 0 || gangId >= GangCatalog.GangCount) return false;
            if (MemberGangId != SocialTuning.IndependentGangId) return false; // exclusive join
            if (_traitorLocked[gangId]) return false;
            if (PendingInitiationGangId != SocialTuning.IndependentGangId) return false;
            if (_initiationCooldownUntilDay[gangId] >= 0 && currentDay < _initiationCooldownUntilDay[gangId]) return false;
            return GetRank(gangId) == GangRank.Associate;
        }

        public void OfferInitiation(int gangId, int currentDay, int deadlineDays = 2)
        {
            PendingInitiationGangId = gangId;
            PendingInitiationDeadlineDay = currentDay + deadlineDays;
        }

        /// <summary>Refusing (or failing) initiation: 2-day cooldown, −5 respect with the Shot-Caller (applied by caller).</summary>
        public void RefuseOrFailInitiation(int currentDay)
        {
            int gangId = PendingInitiationGangId;
            if (gangId == SocialTuning.IndependentGangId) return;
            _initiationCooldownUntilDay[gangId] = currentDay + SocialTuning.InitiationCooldownDays;
            PendingInitiationGangId = SocialTuning.IndependentGangId;
        }

        /// <summary>Initiation favor completed → Member. Locks membership in the rival gang for this facility run.</summary>
        public void CompleteInitiation()
        {
            int gangId = PendingInitiationGangId;
            if (gangId == SocialTuning.IndependentGangId) return;
            PendingInitiationGangId = SocialTuning.IndependentGangId;
            _isMember[gangId] = true;
            MemberGangId = gangId;
            OnRankChanged?.Invoke(gangId, GetRank(gangId));
        }

        public void CompleteGangFavor(int gangId)
        {
            if (gangId < 0 || gangId >= GangCatalog.GangCount) return;
            var before = GetRank(gangId);
            _gangFavorsCompleted[gangId]++;
            var after = GetRank(gangId);
            if (after != before)
                OnRankChanged?.Invoke(gangId, after);
        }

        /// <summary>Expires a pending initiation whose deadline passed. Returns true if it expired.</summary>
        public bool TickInitiationDeadline(int currentDay)
        {
            if (PendingInitiationGangId == SocialTuning.IndependentGangId) return false;
            if (currentDay <= PendingInitiationDeadlineDay) return false;
            RefuseOrFailInitiation(currentDay);
            return true;
        }

        /// <summary>
        /// Betraying your gang (snitching on a member, stealing the stash, leaving after Member):
        /// Traitor lockout — no rejoin this facility run. Relationship penalties and rival bonus
        /// are applied by <see cref="SocialWorld"/>, which has the full roster context.
        /// </summary>
        public void MarkTraitor(int gangId)
        {
            if (gangId < 0 || gangId >= GangCatalog.GangCount) return;
            _traitorLocked[gangId] = true;
            if (MemberGangId == gangId)
                MemberGangId = SocialTuning.IndependentGangId;
            _isMember[gangId] = false;
            OnTraitorLocked?.Invoke(gangId);
            OnRankChanged?.Invoke(gangId, GangRank.Outsider);
        }

        /// <summary>True when the player is Member/Trusted of this specific gang.</summary>
        public bool IsMemberOf(int gangId) =>
            gangId >= 0 && gangId < GangCatalog.GangCount && _isMember[gangId];
    }
}
