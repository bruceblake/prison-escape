namespace Prison.Career
{
    /// <summary>
    /// Deterministic per-visit world seed: hash(worldId, facilityId, visitIndex).
    /// FNV-1a rather than string.GetHashCode so seeds are stable across runtimes and sessions
    /// (World Rules 17 within a run; fresh seed per revisit).
    /// </summary>
    public static class CareerSeed
    {
        public static int VisitSeed(string worldId, string facilityId, int visitIndex)
        {
            unchecked
            {
                const uint prime = 16777619;
                uint hash = 2166136261;

                void Mix(string s)
                {
                    if (s != null)
                        foreach (char c in s)
                        {
                            hash ^= c;
                            hash *= prime;
                        }
                    hash ^= '|';
                    hash *= prime;
                }

                Mix(worldId);
                Mix(facilityId);
                hash ^= (uint)visitIndex;
                hash *= prime;
                return (int)hash;
            }
        }
    }
}
