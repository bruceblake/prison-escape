using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Day-counted suspicion window (pure logic, EditMode-testable).
    /// A day boundary is a Morning Roll Call; suspicion raised for N days expires after N boundaries.
    /// </summary>
    [System.Serializable]
    public struct SuspicionWindow
    {
        [SerializeField] private int remainingDays;

        public int RemainingDays => remainingDays;
        public bool IsActive => remainingDays > 0;

        /// <summary>Extends the window to at least <paramref name="days"/> (never shortens an active window).</summary>
        public void Raise(int days)
        {
            remainingDays = Mathf.Max(remainingDays, Mathf.Max(0, days));
        }

        /// <summary>Call once per Morning Roll Call.</summary>
        public void OnMorningRollCall()
        {
            if (remainingDays > 0)
                remainingDays--;
        }
    }
}
