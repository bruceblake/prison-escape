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
        bool MovementBlocked { get; }
        int CellIndex { get; }
        void SetMovementBlocked(bool blocked);
        void SendToCell(int cellIndex = -1);
    }
}
