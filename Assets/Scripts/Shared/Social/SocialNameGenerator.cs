using System;
using System.Collections.Generic;

namespace Prison.Social
{
    /// <summary>
    /// Deterministic name generation from a curated pool ("Eddie 'Wires' Malone").
    /// Same seed → same prison. Pure (System.Random), EditMode-testable.
    /// </summary>
    public static class SocialNameGenerator
    {
        private static readonly string[] FirstNames =
        {
            "Eddie", "Ray", "Marcus", "Sal", "Dmitri", "Lonnie", "Curtis", "Hank",
            "Tommy", "Vince", "Otis", "Reggie", "Floyd", "Ernie", "Waylon", "Cyrus",
            "Dez", "Milo", "Ivan", "Clay", "Rufus", "Ang", "Pete", "Silas",
            "Jerome", "Nico", "Walt", "Boone", "Ezra", "Gus", "Lamar", "Ossie",
        };

        private static readonly string[] Nicknames =
        {
            "Wires", "Two-Step", "Preacher", "Smokes", "The Ledger", "Half-Deck", "Knuckles", "Whisper",
            "Bricks", "Fingers", "The Saint", "Static", "Mumbles", "Ghost", "Pockets", "Rooster",
            "Tick", "Slim", "Iron", "Patch", "Echo", "Dice", "Chalk", "Vandal",
            "Moth", "Pliers", "Sarge", "Rats", "Cinder", "Lockjaw",
        };

        private static readonly string[] LastNames =
        {
            "Malone", "Ferro", "Okafor", "Reyes", "Volkov", "Briggs", "Calloway", "Duke",
            "Marsh", "Trask", "Sokol", "Whitaker", "Dominguez", "Crane", "Baxter", "Holt",
            "Ives", "Kowalski", "Navarro", "Ash", "Renner", "Doyle", "Fontaine", "Grimm",
            "Salazar", "Pope", "Lockhart", "Mercer", "Quill", "Stroud", "Vann", "Webb",
        };

        private static readonly string[] GuardLastNames =
        {
            "Hardin", "Pruitt", "Casey", "Voss", "McAllister", "Dunn", "Ferris", "Stone",
            "Radcliffe", "Boyd", "Ortega", "Klein", "Sharp", "Winters", "Groves", "Pike",
        };

        /// <summary>
        /// Draws unique names for a roster. Prisoners get first + nickname + last;
        /// guards get "Ofc. Last". Uniqueness holds per generator instance.
        /// </summary>
        public class Pool
        {
            private readonly System.Random _rng;
            private readonly List<int> _firstIdx;
            private readonly List<int> _nickIdx;
            private readonly List<int> _lastIdx;
            private readonly List<int> _guardLastIdx;

            public Pool(int seed)
            {
                _rng = new System.Random(seed);
                _firstIdx = ShuffledIndices(FirstNames.Length, _rng);
                _nickIdx = ShuffledIndices(Nicknames.Length, _rng);
                _lastIdx = ShuffledIndices(LastNames.Length, _rng);
                _guardLastIdx = ShuffledIndices(GuardLastNames.Length, _rng);
            }

            public void NextPrisonerName(out string first, out string nick, out string last)
            {
                first = FirstNames[TakeNext(_firstIdx, FirstNames.Length)];
                nick = Nicknames[TakeNext(_nickIdx, Nicknames.Length)];
                last = LastNames[TakeNext(_lastIdx, LastNames.Length)];
            }

            public void NextGuardName(out string first, out string last)
            {
                first = FirstNames[TakeNext(_firstIdx, FirstNames.Length)];
                last = GuardLastNames[TakeNext(_guardLastIdx, GuardLastNames.Length)];
            }

            private int TakeNext(List<int> indices, int poolSize)
            {
                if (indices.Count == 0)
                {
                    // Pool exhausted (population larger than pool) — reshuffle and reuse.
                    indices.AddRange(ShuffledIndices(poolSize, _rng));
                }
                int v = indices[indices.Count - 1];
                indices.RemoveAt(indices.Count - 1);
                return v;
            }

            private static List<int> ShuffledIndices(int count, System.Random rng)
            {
                var list = new List<int>(count);
                for (int i = 0; i < count; i++) list.Add(i);
                for (int i = count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
                return list;
            }
        }
    }
}
