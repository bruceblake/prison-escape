namespace Prison
{
    public interface IPrisoner
    {
        /// <summary>False triggers guard enforcement (grace period still counts as compliant here).</summary>
        bool IsCompliant { get; }
        /// <summary>True when at the stand point / zone required for the current schedule phase (for HUD).</summary>
        bool IsAtRequiredLocation { get; }
        /// <summary>During morning roll call: this inmate's cell has been shakedown by the sweeper guard.</summary>
        bool IsRollCallShakedownComplete { get; }
        /// <summary>True while this inmate's movement input is frozen (e.g. held at a stand point by guard/schedule logic).</summary>
        bool MovementBlocked { get; }
        /// <summary>Index of the cell this inmate is assigned to; used to key roll-call shakedown completion per cell.</summary>
        int CellIndex { get; }
        /// <summary>Freezes or releases this inmate's movement input.</summary>
        void SetMovementBlocked(bool blocked);
        /// <summary>Sends the inmate back to a cell. Pass -1 (default) to use the inmate's own assigned cell.</summary>
        void SendToCell(int cellIndex = -1);
    }
}
