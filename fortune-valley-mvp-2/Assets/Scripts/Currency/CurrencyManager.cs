using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Single source of truth for player's money.
    /// All spending and earning flows through this manager.
    ///
    /// DESIGN NOTE: This is intentionally simple. A single currency
    /// reduces cognitive load and keeps focus on core learning concepts.
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Starting Balance")]
        [Tooltip("Money the player starts with")]
        [SerializeField] private float _startingBalance = 1000f;

        [Header("Debug")]
        [SerializeField] private bool _logTransactions = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private float _balance;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Current player balance.
        /// </summary>
        public float Balance => _balance;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void HandleGameStart()
        {
            ResetBalance();
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Reset balance to starting amount.
        /// </summary>
        public void ResetBalance()
        {
            _balance = _startingBalance;
            GameEvents.RaiseCurrencyChanged(_balance, 0f);
        }

        /// <summary>
        /// Add money to the player's balance.
        /// </summary>
        /// <param name="amount">Amount to add (must be positive)</param>
        /// <param name="source">Description of where the money came from</param>
        public void Add(float amount, string source = "Unknown")
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Tried to add non-positive amount: {amount}");
                return;
            }

            _balance += amount;

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] +${amount:F2} from {source}. New balance: ${_balance:F2}");
            }

            GameEvents.RaiseCurrencyChanged(_balance, amount);
            GameEvents.RaiseIncomeGenerated(amount, source);
        }

        /// <summary>
        /// Try to spend money. Returns true if successful.
        /// </summary>
        /// <param name="amount">Amount to spend (must be positive)</param>
        /// <param name="reason">Description of what the money is for</param>
        /// <returns>True if had enough money and it was spent</returns>
        public bool TrySpend(float amount, string reason = "Unknown")
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Tried to spend non-positive amount: {amount}");
                return false;
            }

            if (_balance < amount)
            {
                if (_logTransactions)
                {
                    Debug.Log($"[CurrencyManager] Cannot spend ${amount:F2} for {reason}. Balance: ${_balance:F2}");
                }
                return false;
            }

            _balance -= amount;

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] -${amount:F2} for {reason}. New balance: ${_balance:F2}");
            }

            GameEvents.RaiseCurrencyChanged(_balance, -amount);
            return true;
        }

        /// <summary>
        /// Check if player can afford an amount.
        /// </summary>
        public bool CanAfford(float amount)
        {
            return _balance >= amount;
        }

        /// <summary>
        /// Set balance directly (use sparingly, mainly for testing).
        /// </summary>
        public void SetBalance(float amount)
        {
            float delta = amount - _balance;
            _balance = amount;
            GameEvents.RaiseCurrencyChanged(_balance, delta);
        }
    }
}
