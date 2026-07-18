using System.Collections.Generic;

namespace Prison.Social
{
    /// <summary>
    /// Template dialogue lines with token substitution, keyed by archetype voice ×
    /// relationship band × memory type (spec: no LLM/generative dialogue).
    /// Deterministic pick via caller-supplied hash so lines feel stable per NPC/day.
    /// </summary>
    public static class DialogueLibrary
    {
        // ------------------------------------------------------------- greetings by band

        private static readonly string[] EnemyLines =
        {
            "You got a lot of nerve showing your face here.",
            "Walk away before this gets worse for you.",
            "We're done talking. Forever.",
        };

        private static readonly string[] HostileLines =
        {
            "What do YOU want?",
            "Make it quick.",
            "I don't talk to people like you for free.",
        };

        private static readonly string[] NeutralLines =
        {
            "Yeah? What's up.",
            "Another day in paradise, huh.",
            "You need something?",
        };

        private static readonly string[] FriendlyLines =
        {
            "Hey, good to see you walking around.",
            "You're alright, you know that?",
            "What can I do for you, friend?",
        };

        private static readonly string[] AllyLines =
        {
            "My people saw you coming — you're always welcome.",
            "Anything you need, you ask me first.",
            "You and me, we look out for each other.",
        };

        private static readonly string[] ConfidantLines =
        {
            "Whatever you're planning... count me in quiet.",
            "You know I'd hold anything for you. Anything.",
            "Talk low. Walls got ears, but not for us.",
        };

        // ------------------------------------------------------------- archetype flavor

        private static readonly Dictionary<PrisonerArchetype, string[]> ArchetypeFlavor =
            new Dictionary<PrisonerArchetype, string[]>
            {
                { PrisonerArchetype.ShotCaller, new[]
                    { "Everything in this block goes through me.", "My crew keeps this place civilized. Remember that." } },
                { PrisonerArchetype.Soldier, new[]
                    { "Boss says jump, I ask which wall.", "I watch the corner. That's the job." } },
                { PrisonerArchetype.Hustler, new[]
                    { "I can get things. For a price. Always for a price.", "Commissary's a joke — my stock is better." } },
                { PrisonerArchetype.OldTimer, new[]
                    { "Seen a hundred of you come and go.", "This place was worse in the old days. Or better. Hard to say." } },
                { PrisonerArchetype.Bruiser, new[]
                    { "You lift? You should lift.", "Respect is the only currency that doesn't get confiscated." } },
                { PrisonerArchetype.Snitch, new[]
                    { "I hear things, you know. All kinds of things.", "Just being friendly! No harm in friendly." } },
                { PrisonerArchetype.Loner, new[]
                    { "...", "I didn't ask for company." } },
            };

        // ------------------------------------------------------------- memory reactions

        private static readonly Dictionary<SocialEventType, string[]> MemoryLines =
            new Dictionary<SocialEventType, string[]>
            {
                { SocialEventType.Gift, new[] { "Still got that thing you gave me.", "You come bearing gifts again?" } },
                { SocialEventType.FavorForNpc, new[] { "I don't forget who came through for me.", "You did right by me. That counts." } },
                { SocialEventType.RiskyFavor, new[] { "That run you did? Nobody else would've.", "You've got steel, doing that for me." } },
                { SocialEventType.Protection, new[] { "You stood there when it mattered.", "I remember who had my back." } },
                { SocialEventType.IntimidationSuccess, new[] { "Easy. No trouble here. We're good.", "I don't want problems with you." } },
                { SocialEventType.IntimidationFail, new[] { "You tried to lean on me. Cute.", "Push me again. See what happens." } },
                { SocialEventType.CrimeWitnessed, new[] { "Saw you crawl out of that vent, man.", "I know what you've been carrying. Relax — for now." } },
                { SocialEventType.BribeWitnessed, new[] { "Saw you slip that guard something.", "Money changing hands with a badge. Interesting." } },
                { SocialEventType.SnitchedOn, new[] { "You rat. You RAT.", "Everyone knows what you did. Sleep light." } },
                { SocialEventType.CaughtStealing, new[] { "Touch my stuff again and we're done.", "Thief. I keep count." } },
                { SocialEventType.GangBetrayal, new[] { "Traitors don't get second chances here.", "You turned on the family. That never washes off." } },
                { SocialEventType.Argument, new[] { "We got loud, whatever. It's done.", "Still sore about our little chat." } },
            };

        // ------------------------------------------------------------- intel by trust band

        private static readonly string[] ScheduleIntel =
        {
            "Guards rotate lazy after the evening count. Nobody watches the corridor for a few minutes.",
            "Morning sweep always starts at the low cells. High numbers get a head start.",
            "Rookie on shift always chats through half his patrol. Slow feet, that one.",
        };

        private static readonly string[] LootRouteIntel =
        {
            "Workshop bins get restocked with parts. Nobody counts the screws.",
            "Check under the yard bleachers — stuff gets stashed and forgotten there.",
            "Some cells hide loose vent covers. Handy, if you had a screwdriver.",
        };

        private static readonly string[] EscapeLoreIntel =
        {
            "Vent behind cell 12 ain't welded. You didn't hear that from me.",
            "Old maintenance shaft past the showers — they never fixed the grate right.",
            "Fence by the far yard corner sags. Cameras hate the morning fog too.",
        };

        private static readonly string[] InmateMandatoryTravelRefusal =
        {
            "Not right now — we gotta go.",
            "Can't stop. Schedule's on our ass.",
            "Talk later. I'm late already.",
            "Move — count don't wait.",
        };

        private static readonly string[] GuardNonComplianceRefusal =
        {
            "Not a good time. Get where you're supposed to be.",
            "You're out of line. Move along.",
            "I'm not chatting. You ain't compliant.",
            "Handle your business first, then we'll talk.",
        };

        // ------------------------------------------------------------- API

        public static string Greeting(NPCIdentity identity, StandingBand band, int variantHash)
        {
            string[] pool;
            switch (band)
            {
                case StandingBand.Enemy: pool = EnemyLines; break;
                case StandingBand.Hostile: pool = HostileLines; break;
                case StandingBand.Friendly: pool = FriendlyLines; break;
                case StandingBand.Ally: pool = AllyLines; break;
                case StandingBand.Confidant: pool = ConfidantLines; break;
                default: pool = NeutralLines; break;
            }
            // Sprinkle archetype flavor on neutral+ moods roughly a third of the time.
            if (band != StandingBand.Enemy && band != StandingBand.Hostile
                && identity != null && (variantHash & 3) == 0
                && ArchetypeFlavor.TryGetValue(identity.archetype, out var flavor))
                pool = flavor;
            return Pick(pool, variantHash);
        }

        public static string MemoryReaction(in SocialEvent evt, int variantHash) =>
            MemoryLines.TryGetValue(evt.type, out var pool) ? Pick(pool, variantHash) : null;

        /// <summary>Chat intel line for the trust band (spec §6): &lt;25 flavor only · ≥25 schedule · ≥50 loot/routes · ≥75 escape lore.</summary>
        public static string Intel(float trust, int variantHash)
        {
            if (trust >= SocialTuning.IntelEscapeLoreTrust) return Pick(EscapeLoreIntel, variantHash);
            if (trust >= SocialTuning.IntelLootRouteTrust) return Pick(LootRouteIntel, variantHash);
            if (trust >= SocialTuning.IntelScheduleTrust) return Pick(ScheduleIntel, variantHash);
            return null;
        }

        public static string ArchetypeBlurb(NPCIdentity identity)
        {
            if (identity == null) return "";
            return identity.isGuard
                ? ArchetypeCatalog.GuardBlurb(identity.guardArchetype)
                : ArchetypeCatalog.Get(identity.archetype).blurb;
        }

        public static string TerritoryWarnOff(string gangName, int variantHash)
        {
            var pool = new[]
            {
                $"This corner belongs to the {gangName}. Keep walking.",
                $"{gangName} turf. You lost?",
                $"Wrong side of the yard, friend. {gangName} only.",
            };
            return Pick(pool, variantHash);
        }

        public static string InmateRefuseMandatoryTravel(NPCIdentity identity, int variantHash)
        {
            string line = Pick(InmateMandatoryTravelRefusal, variantHash);
            return FormatSpeakerLine(identity, line);
        }

        public static string GuardRefusePlayerNonCompliance(NPCIdentity identity, int variantHash)
        {
            string line = Pick(GuardNonComplianceRefusal, variantHash);
            return FormatSpeakerLine(identity, line);
        }

        private static string FormatSpeakerLine(NPCIdentity identity, string line)
        {
            if (identity == null || string.IsNullOrEmpty(line))
                return line ?? "";
            if (identity.isGuard)
                return $"Ofc. {identity.lastName}: {line}";
            return $"{identity.ShortName}: {line}";
        }

        private static string Pick(string[] pool, int hash)
        {
            if (pool == null || pool.Length == 0) return "";
            int idx = hash % pool.Length;
            if (idx < 0) idx += pool.Length;
            return pool[idx];
        }
    }
}
