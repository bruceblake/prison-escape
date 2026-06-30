using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Tracks per-cell morning shakedown during roll call. Schedule advances when every inmate's cell has been swept.
    /// </summary>
    public class MorningRollCallTracker : MonoBehaviour
    {
        public static MorningRollCallTracker Instance { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        private readonly HashSet<int> _cellsShakedownComplete = new();
        private bool _phaseActive;

        public event Action<int> OnCellShakedownComplete;
        public event Action OnAllInmatesShakedownComplete;

        public bool IsPhaseActive => _phaseActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged += OnScheduleEventChanged;
        }

        private void OnDisable()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged -= OnScheduleEventChanged;
        }

        private void OnScheduleEventChanged(PrisonEventType evt)
        {
            // BeginPhase is owned by PrisonTimeManager.SetCurrentEvent — only end here to avoid clearing progress twice.
            if (!PrisonEventExtensions.IsMorningLineUp(evt))
                EndPhase();
        }

        public void BeginPhase()
        {
            _cellsShakedownComplete.Clear();
            _phaseActive = true;
            if (debugLog)
                Debug.Log("[MorningRollCallTracker] Roll call / shakedown phase started.", this);
        }

        public void EndPhase()
        {
            _phaseActive = false;
            _cellsShakedownComplete.Clear();
        }

        /// <summary>Called by <see cref="MorningShakedownSweeper"/> after each cell is swept.</summary>
        public static void NotifyCellShakedownComplete(int cellIndex)
        {
            if (Instance == null)
                EnsureInstance();
            Instance?.MarkCellShakedownComplete(cellIndex);
        }

        public void MarkCellShakedownComplete(int cellIndex)
        {
            if (!_phaseActive)
                return;
            if (!_cellsShakedownComplete.Add(cellIndex))
                return;

            if (debugLog)
                Debug.Log($"[MorningRollCallTracker] Cell {cellIndex} shakedown complete ({_cellsShakedownComplete.Count} cells).", this);

            OnCellShakedownComplete?.Invoke(cellIndex);

            if (AreAllInmatesShakedownComplete())
            {
                if (debugLog)
                    Debug.Log("[MorningRollCallTracker] All inmates shakedown — advancing schedule.", this);
                OnAllInmatesShakedownComplete?.Invoke();
                PrisonTimeManager.Instance?.AdvanceMorningRollCallWhenComplete();
            }
        }

        public bool HasCellBeenShakedown(int cellIndex) =>
            _phaseActive && _cellsShakedownComplete.Contains(cellIndex);

        public bool IsInmateShakedownComplete(IPrisoner prisoner)
        {
            if (prisoner == null || !_phaseActive)
                return false;
            return _cellsShakedownComplete.Contains(prisoner.CellIndex);
        }

        /// <summary>Shakedown done — inmate may leave the roll-call stand for the next phase without guard arrest.</summary>
        public static bool IsInmateReleasedFromRollCallStand(IPrisoner prisoner)
        {
            var tm = PrisonTimeManager.Instance;
            if (tm == null || !tm.IsMorningRollCallShakedownGateActive || prisoner == null)
                return false;
            if (Instance == null || !Instance.IsInmateShakedownComplete(prisoner))
                return false;
            if (prisoner is PrisonerController pc && Time.unscaledTime < pc.RollCallReleaseAllowedAfter)
                return false;
            if (prisoner is PrisonerAI ai && Time.unscaledTime < ai.RollCallReleaseAllowedAfter)
                return false;
            return true;
        }

        public int CountInmatesShakedownComplete(out int totalInmates)
        {
            var list = new List<IPrisoner>(16);
            PrisonerPresence.GetAllPrisoners(list);
            totalInmates = list.Count;
            int done = 0;
            foreach (var p in list)
            {
                if (p != null && _cellsShakedownComplete.Contains(p.CellIndex))
                    done++;
            }
            return done;
        }

        public bool AreAllInmatesShakedownComplete()
        {
            int done = CountInmatesShakedownComplete(out int total);
            return total > 0 && done >= total;
        }

        public static void EnsureInstance()
        {
            if (Instance != null) return;
            if (PrisonTimeManager.Instance == null) return;
            var go = PrisonTimeManager.Instance.gameObject;
            if (go.GetComponent<MorningRollCallTracker>() == null)
                go.AddComponent<MorningRollCallTracker>();
        }
    }
}
