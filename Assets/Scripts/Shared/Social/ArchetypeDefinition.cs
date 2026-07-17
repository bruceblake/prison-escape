using System.Collections.Generic;
using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// Designer-facing archetype tuning: trait ranges, dialogue voice, behavior flags,
    /// favored gift categories, and trade stock size. Runtime falls back to
    /// <see cref="ArchetypeCatalog"/> code defaults when no asset exists, so the system
    /// works with zero authored assets (the v1 failure mode).
    /// Assets live in <c>Resources/Social/Archetypes</c> named after the enum value.
    /// </summary>
    [CreateAssetMenu(fileName = "Archetype", menuName = "Prison/Social/Archetype Definition")]
    public class ArchetypeDefinition : ScriptableObject
    {
        public PrisonerArchetype archetype;

        [Header("Trait ranges (rolled per NPC, 0–100)")]
        public Vector2Int aggressionRange = new Vector2Int(20, 60);
        public Vector2Int loyaltyRange = new Vector2Int(20, 60);
        public Vector2Int greedRange = new Vector2Int(20, 60);
        public Vector2Int sociabilityRange = new Vector2Int(20, 60);
        public Vector2Int nerveRange = new Vector2Int(20, 60);

        [Header("Voice & blurb")]
        [Tooltip("One-sentence profile blurb shown on the Talk Menu Profile tab.")]
        public string blurb = "";

        [Header("Behavior")]
        [Tooltip("Base chance (0–1) this NPC reports a remembered crime each phase; scaled by traits/standing.")]
        [Range(0f, 1f)] public float snitchBaseChance;
        [Tooltip("Items in daily trade stock: x = min, y = max. 0/0 = does not trade.")]
        public Vector2Int stockCountRange = Vector2Int.zero;

        [Header("Gifts")]
        public List<ItemCategory> favoredGiftCategories = new List<ItemCategory>();

        public ArchetypeProfile ToProfile()
        {
            return new ArchetypeProfile
            {
                archetype = archetype,
                aggressionMin = aggressionRange.x, aggressionMax = aggressionRange.y,
                loyaltyMin = loyaltyRange.x, loyaltyMax = loyaltyRange.y,
                greedMin = greedRange.x, greedMax = greedRange.y,
                sociabilityMin = sociabilityRange.x, sociabilityMax = sociabilityRange.y,
                nerveMin = nerveRange.x, nerveMax = nerveRange.y,
                blurb = blurb,
                snitchBaseChance = snitchBaseChance,
                stockMin = stockCountRange.x, stockMax = stockCountRange.y,
                favoredGiftCategories = favoredGiftCategories != null
                    ? favoredGiftCategories.ToArray()
                    : new ItemCategory[0],
            };
        }
    }

    /// <summary>Plain (asset-free, testable) archetype data used by the roster builder and services.</summary>
    public class ArchetypeProfile
    {
        public PrisonerArchetype archetype;
        public int aggressionMin, aggressionMax;
        public int loyaltyMin, loyaltyMax;
        public int greedMin, greedMax;
        public int sociabilityMin, sociabilityMax;
        public int nerveMin, nerveMax;
        public string blurb = "";
        public float snitchBaseChance;
        public int stockMin, stockMax;
        public ItemCategory[] favoredGiftCategories = new ItemCategory[0];

        public PersonalityTraits RollTraits(System.Random rng)
        {
            return new PersonalityTraits(
                rng.Next(aggressionMin, aggressionMax + 1),
                rng.Next(loyaltyMin, loyaltyMax + 1),
                rng.Next(greedMin, greedMax + 1),
                rng.Next(sociabilityMin, sociabilityMax + 1),
                rng.Next(nerveMin, nerveMax + 1));
        }
    }

    /// <summary>
    /// Code-default archetype profiles (spec §2). <see cref="Get"/> prefers a
    /// <c>Resources/Social/Archetypes/&lt;Name&gt;</c> asset when one exists.
    /// </summary>
    public static class ArchetypeCatalog
    {
        private static readonly Dictionary<PrisonerArchetype, ArchetypeProfile> _cache =
            new Dictionary<PrisonerArchetype, ArchetypeProfile>();

        public static ArchetypeProfile Get(PrisonerArchetype archetype)
        {
            if (_cache.TryGetValue(archetype, out var cached))
                return cached;

            ArchetypeProfile profile = null;
            try
            {
                var asset = Resources.Load<ArchetypeDefinition>($"Social/Archetypes/{archetype}");
                if (asset != null)
                    profile = asset.ToProfile();
            }
            catch (System.Exception)
            {
                // Resources.Load is a Unity icall — unavailable when the pure logic runs
                // outside the engine (out-of-band unit tests). Code defaults cover that.
            }
            if (profile == null)
                profile = CreateDefault(archetype);

            _cache[archetype] = profile;
            return profile;
        }

        /// <summary>Clears the Resources-override cache (tests / domain reload safety).</summary>
        public static void ResetCache() => _cache.Clear();

        public static ArchetypeProfile CreateDefault(PrisonerArchetype archetype)
        {
            switch (archetype)
            {
                case PrisonerArchetype.ShotCaller:
                    return new ArchetypeProfile
                    {
                        archetype = archetype,
                        aggressionMin = 55, aggressionMax = 85,
                        loyaltyMin = 70, loyaltyMax = 95,
                        greedMin = 30, greedMax = 60,
                        sociabilityMin = 40, sociabilityMax = 70,
                        nerveMin = 60, nerveMax = 90,
                        blurb = "Runs the crew. Nothing moves in their corner without a nod.",
                        snitchBaseChance = 0f,
                        stockMin = 0, stockMax = 1,
                        favoredGiftCategories = new[] { ItemCategory.Contraband },
                    };
                case PrisonerArchetype.Soldier:
                    return new ArchetypeProfile
                    {
                        archetype = archetype,
                        aggressionMin = 50, aggressionMax = 80,
                        loyaltyMin = 60, loyaltyMax = 90,
                        greedMin = 20, greedMax = 50,
                        sociabilityMin = 25, sociabilityMax = 55,
                        nerveMin = 50, nerveMax = 80,
                        blurb = "Gang muscle. Follows the shot-caller's lead on you.",
                        snitchBaseChance = 0f,
                        stockMin = 0, stockMax = 2,
                        favoredGiftCategories = new[] { ItemCategory.Consumable },
                    };
                case PrisonerArchetype.Hustler:
                    return new ArchetypeProfile
                    {
                        archetype = archetype,
                        aggressionMin = 10, aggressionMax = 40,
                        loyaltyMin = 25, loyaltyMax = 55,
                        greedMin = 65, greedMax = 95,
                        sociabilityMin = 65, sociabilityMax = 95,
                        nerveMin = 45, nerveMax = 75,
                        blurb = "Can get things. Hears everything, sells most of it.",
                        snitchBaseChance = 0.02f,
                        stockMin = 4, stockMax = 6,
                        favoredGiftCategories = new[] { ItemCategory.Tool, ItemCategory.Contraband },
                    };
                case PrisonerArchetype.OldTimer:
                    return new ArchetypeProfile
                    {
                        archetype = archetype,
                        aggressionMin = 5, aggressionMax = 25,
                        loyaltyMin = 45, loyaltyMax = 75,
                        greedMin = 10, greedMax = 40,
                        sociabilityMin = 35, sociabilityMax = 65,
                        nerveMin = 70, nerveMax = 95,
                        blurb = "Been here longer than the paint. Knows every route out that failed.",
                        snitchBaseChance = 0f,
                        stockMin = 0, stockMax = 1,
                        favoredGiftCategories = new[] { ItemCategory.Consumable },
                    };
                case PrisonerArchetype.Bruiser:
                    return new ArchetypeProfile
                    {
                        archetype = archetype,
                        aggressionMin = 65, aggressionMax = 95,
                        loyaltyMin = 30, loyaltyMax = 60,
                        greedMin = 25, greedMax = 55,
                        sociabilityMin = 10, sociabilityMax = 40,
                        nerveMin = 55, nerveMax = 85,
                        blurb = "Respect-first independent muscle. Speaks fluent intimidation.",
                        snitchBaseChance = 0f,
                        stockMin = 0, stockMax = 1,
                        favoredGiftCategories = new[] { ItemCategory.Weapon },
                    };
                case PrisonerArchetype.Snitch:
                    return new ArchetypeProfile
                    {
                        archetype = archetype,
                        aggressionMin = 5, aggressionMax = 30,
                        loyaltyMin = 5, loyaltyMax = 30,
                        greedMin = 40, greedMax = 70,
                        sociabilityMin = 55, sociabilityMax = 85,
                        nerveMin = 5, nerveMax = 30,
                        blurb = "Friendly face. A little too interested in what you're carrying.",
                        snitchBaseChance = 0.35f,
                        stockMin = 0, stockMax = 1,
                        favoredGiftCategories = new[] { ItemCategory.Consumable },
                    };
                case PrisonerArchetype.Loner:
                default:
                    return new ArchetypeProfile
                    {
                        archetype = PrisonerArchetype.Loner,
                        aggressionMin = 15, aggressionMax = 45,
                        loyaltyMin = 35, loyaltyMax = 65,
                        greedMin = 20, greedMax = 50,
                        sociabilityMin = 0, sociabilityMax = 20,
                        nerveMin = 40, nerveMax = 70,
                        blurb = "Keeps to themselves. Hard to reach — and immune to the rumor mill.",
                        snitchBaseChance = 0.01f,
                        stockMin = 0, stockMax = 0,
                        favoredGiftCategories = new[] { ItemCategory.CraftingPart },
                    };
            }
        }

        public static string GuardBlurb(GuardArchetype archetype)
        {
            switch (archetype)
            {
                case GuardArchetype.Corrupt: return "Looks the other way — for the right price.";
                case GuardArchetype.Rookie: return "New badge. Still learning where not to look.";
                case GuardArchetype.Veteran: return "Twenty years on the block. Smells trouble early.";
                default: return "Plays it straight, by the book, every shift.";
            }
        }

        /// <summary>Default guard traits per archetype (guards use the same five axes).</summary>
        public static PersonalityTraits RollGuardTraits(GuardArchetype archetype, System.Random rng)
        {
            switch (archetype)
            {
                case GuardArchetype.Corrupt:
                    return new PersonalityTraits(
                        rng.Next(30, 61), rng.Next(10, 41), rng.Next(65, 96), rng.Next(40, 71), rng.Next(50, 81));
                case GuardArchetype.Rookie:
                    return new PersonalityTraits(
                        rng.Next(20, 51), rng.Next(50, 81), rng.Next(20, 51), rng.Next(55, 86), rng.Next(15, 46));
                case GuardArchetype.Veteran:
                    return new PersonalityTraits(
                        rng.Next(40, 71), rng.Next(65, 96), rng.Next(15, 46), rng.Next(25, 56), rng.Next(60, 91));
                default:
                    return new PersonalityTraits(
                        rng.Next(30, 61), rng.Next(60, 91), rng.Next(10, 41), rng.Next(30, 61), rng.Next(40, 71));
            }
        }
    }
}
