namespace Prison
{
    /// <summary>
    /// Pure rules for restricted-zone activation (EditMode-testable).
    /// </summary>
    public static class RestrictedZoneRules
    {
        /// <summary>
        /// A zone is restricted either always, or only while the current schedule phase
        /// appears in its <paramref name="restrictedDuring"/> list.
        /// </summary>
        public static bool IsRestricted(bool alwaysRestricted, PrisonEventType[] restrictedDuring, PrisonEventType current)
        {
            if (alwaysRestricted) return true;
            if (restrictedDuring == null) return false;
            for (int i = 0; i < restrictedDuring.Length; i++)
            {
                if (restrictedDuring[i] == current)
                    return true;
            }
            return false;
        }
    }
}
