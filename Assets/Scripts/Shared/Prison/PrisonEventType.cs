namespace Prison
{
    public enum PrisonEventType
    {
        /// <summary>Legacy: same rules as MorningRollCall (line-up at cell door stand point).</summary>
        RollCall,
        Breakfast,
        Lunch,
        Dinner,
        FreeTime,
        LightsOut,
        /// <summary>Morning line-up at assigned stand point outside cell; shakedown guard may sweep cells.</summary>
        MorningRollCall,
        /// <summary>Night bed check — player must be in cell (or dummy); night verifier guard walks cells.</summary>
        NightRollCall,
    }
}
