namespace Prison.Social
{
    /// <summary>Base relationship deltas and memory weights per event type (spec §3/§4). Pure lookup.</summary>
    public static class SocialActs
    {
        /// <summary>Base (pre-modifier) deltas applied to the target's record of the actor.</summary>
        public static void GetBaseDeltas(SocialEventType type, out float trust, out float respect)
        {
            switch (type)
            {
                case SocialEventType.Chat:
                    trust = SocialTuning.ChatTrust; respect = 0f; break;
                case SocialEventType.Gift:
                    trust = SocialTuning.GiftBaseTrust; respect = 0f; break;
                case SocialEventType.FavorForNpc:
                    trust = SocialTuning.FavorTrustMin; respect = SocialTuning.FavorRespect; break;
                case SocialEventType.RiskyFavor:
                    trust = SocialTuning.RiskyFavorTrust; respect = SocialTuning.RiskyFavorRespect; break;
                case SocialEventType.Protection:
                    trust = SocialTuning.ProtectionTrust; respect = SocialTuning.ProtectionRespect; break;
                case SocialEventType.IntimidationSuccess:
                    trust = SocialTuning.IntimidateSuccessTrust; respect = SocialTuning.IntimidateSuccessRespect; break;
                case SocialEventType.IntimidationFail:
                    trust = SocialTuning.IntimidateFailTrust; respect = SocialTuning.IntimidateFailRespect; break;
                case SocialEventType.CaughtStealing:
                case SocialEventType.SnitchedOn:
                    trust = SocialTuning.CaughtStealingTrust; respect = SocialTuning.CaughtStealingRespect; break;
                case SocialEventType.GangBetrayal:
                    trust = SocialTuning.TraitorTrustPenalty; respect = SocialTuning.TraitorRespectPenalty; break;
                default:
                    trust = 0f; respect = 0f; break;
            }
        }

        /// <summary>Direct-memory weight for an event type (gift 2, favor 4, betrayal 10, crime witnessed 6).</summary>
        public static float MemoryWeight(SocialEventType type)
        {
            switch (type)
            {
                case SocialEventType.Chat: return 1f;
                case SocialEventType.Gift: return SocialTuning.GiftMemoryWeight;
                case SocialEventType.FavorForNpc:
                case SocialEventType.FavorForPlayer:
                case SocialEventType.RiskyFavor:
                case SocialEventType.Protection:
                case SocialEventType.Trade:
                    return SocialTuning.FavorMemoryWeight;
                case SocialEventType.IntimidationSuccess:
                case SocialEventType.IntimidationFail:
                case SocialEventType.Argument:
                    return 5f;
                case SocialEventType.CrimeWitnessed:
                case SocialEventType.BribeWitnessed:
                    return SocialTuning.CrimeWitnessedWeight;
                case SocialEventType.CaughtStealing:
                case SocialEventType.SnitchedOn:
                case SocialEventType.GangBetrayal:
                    return SocialTuning.BetrayalMemoryWeight;
                default: return 1f;
            }
        }

        /// <summary>Betrayal-class events get their negative deltas scaled by the observer's loyalty (×1–2).</summary>
        public static bool IsBetrayalClass(SocialEventType type) =>
            type == SocialEventType.CaughtStealing ||
            type == SocialEventType.SnitchedOn ||
            type == SocialEventType.GangBetrayal;
    }
}
