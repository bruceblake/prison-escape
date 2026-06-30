using System;
using UnityEngine;

namespace Prison
{
    /// <summary>Player cash balance for HUD and contraband confiscation.</summary>
    public class PlayerWallet : MonoBehaviour
    {
        public static PlayerWallet Instance { get; private set; }

        [SerializeField] private float balance;

        public float Balance => balance;
        public bool HoldsContrabandCash { get; private set; }

        public event Action<float, float> OnBalanceChanged;

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

        public void SetBalance(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount))
            {
                Debug.LogWarning($"[PlayerWallet] Ignoring invalid balance value: {amount}");
                return;
            }

            float prev = balance;
            balance = Mathf.Max(0f, amount);
            OnBalanceChanged?.Invoke(prev, balance);
        }

        public void Add(float delta)
        {
            if (float.IsNaN(delta) || Mathf.Approximately(delta, 0f))
                return;
            SetBalance(balance + delta);
        }

        public void SetContrabandCashState(bool isDirty) => HoldsContrabandCash = isDirty;
    }
}
