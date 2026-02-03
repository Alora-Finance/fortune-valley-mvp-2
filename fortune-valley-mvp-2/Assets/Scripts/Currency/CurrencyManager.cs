using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Account types for player's money.
    /// Checking: Day-to-day spending (lot purchases, upgrades)
    /// Investing: Investment pool (buy/sell stocks and investments)
    /// </summary>
    public enum AccountType
    {
        Checking,
        Investing
    }

    /// <summary>
    /// Manages player's dual account system (Checking + Investing).
    /// All spending and earning flows through this manager.
    ///
    /// DESIGN NOTE: Dual accounts teach the concept of separating
    /// spending money from investment capital - a core financial skill.
    ///
    /// Rules:
    /// - Restaurant income → Checking
    /// - Lot purchases → from Checking
    /// - Investment buys → from Investing
    /// - Investment sells → to Investing
    /// - Player must Transfer between accounts
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Starting Balances")]
        [Tooltip("Money the player starts with in Checking account")]
        [SerializeField] private float _startingCheckingBalance = 500f;

        [Tooltip("Money the player starts with in Investing account")]
        [SerializeField] private float _startingInvestingBalance = 500f;

        [Header("Debug")]
        [SerializeField] private bool _logTransactions = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private float _checkingBalance;
        private float _investingBalance;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Current checking account balance.
        /// Used for: Lot purchases, restaurant upgrades, daily expenses.
        /// </summary>
        public float CheckingBalance => _checkingBalance;

        /// <summary>
        /// Current investing account balance.
        /// Used for: Buying and selling investments.
        /// </summary>
        public float InvestingBalance => _investingBalance;

        /// <summary>
        /// Total balance across both accounts.
        /// </summary>
        public float TotalBalance => _checkingBalance + _investingBalance;

        /// <summary>
        /// Legacy accessor for backwards compatibility.
        /// Returns total balance.
        /// </summary>
        public float Balance => TotalBalance;

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
        /// Reset both accounts to starting amounts.
        /// </summary>
        public void ResetBalance()
        {
            _checkingBalance = _startingCheckingBalance;
            _investingBalance = _startingInvestingBalance;

            GameEvents.RaiseCheckingBalanceChanged(_checkingBalance, 0f);
            GameEvents.RaiseInvestingBalanceChanged(_investingBalance, 0f);
            // Legacy event for backwards compatibility
            GameEvents.RaiseCurrencyChanged(TotalBalance, 0f);
        }

        /// <summary>
        /// Add money to a specific account.
        /// </summary>
        /// <param name="amount">Amount to add (must be positive)</param>
        /// <param name="account">Which account to add to</param>
        /// <param name="source">Description of where the money came from</param>
        public void Add(float amount, AccountType account, string source = "Unknown")
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Tried to add non-positive amount: {amount}");
                return;
            }

            if (account == AccountType.Checking)
            {
                _checkingBalance += amount;
                GameEvents.RaiseCheckingBalanceChanged(_checkingBalance, amount);
            }
            else
            {
                _investingBalance += amount;
                GameEvents.RaiseInvestingBalanceChanged(_investingBalance, amount);
            }

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] +${amount:F2} to {account} from {source}. " +
                          $"Checking: ${_checkingBalance:F2}, Investing: ${_investingBalance:F2}");
            }

            // Legacy events
            GameEvents.RaiseCurrencyChanged(TotalBalance, amount);
            GameEvents.RaiseIncomeGenerated(amount, source);
        }

        /// <summary>
        /// Add money to Checking account (convenience method).
        /// </summary>
        public void Add(float amount, string source = "Unknown")
        {
            Add(amount, AccountType.Checking, source);
        }

        /// <summary>
        /// Try to spend money from a specific account. Returns true if successful.
        /// </summary>
        /// <param name="amount">Amount to spend (must be positive)</param>
        /// <param name="account">Which account to spend from</param>
        /// <param name="reason">Description of what the money is for</param>
        /// <returns>True if had enough money and it was spent</returns>
        public bool TrySpend(float amount, AccountType account, string reason = "Unknown")
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Tried to spend non-positive amount: {amount}");
                return false;
            }

            float currentBalance = account == AccountType.Checking ? _checkingBalance : _investingBalance;

            if (currentBalance < amount)
            {
                if (_logTransactions)
                {
                    Debug.Log($"[CurrencyManager] Cannot spend ${amount:F2} from {account} for {reason}. " +
                              $"Balance: ${currentBalance:F2}");
                }
                return false;
            }

            if (account == AccountType.Checking)
            {
                _checkingBalance -= amount;
                GameEvents.RaiseCheckingBalanceChanged(_checkingBalance, -amount);
            }
            else
            {
                _investingBalance -= amount;
                GameEvents.RaiseInvestingBalanceChanged(_investingBalance, -amount);
            }

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] -${amount:F2} from {account} for {reason}. " +
                          $"Checking: ${_checkingBalance:F2}, Investing: ${_investingBalance:F2}");
            }

            // Legacy event
            GameEvents.RaiseCurrencyChanged(TotalBalance, -amount);
            return true;
        }

        /// <summary>
        /// Try to spend from Checking account (convenience method).
        /// </summary>
        public bool TrySpend(float amount, string reason = "Unknown")
        {
            return TrySpend(amount, AccountType.Checking, reason);
        }

        /// <summary>
        /// Transfer money between accounts.
        /// </summary>
        /// <param name="amount">Amount to transfer</param>
        /// <param name="from">Source account</param>
        /// <param name="to">Destination account</param>
        /// <returns>True if transfer succeeded</returns>
        public bool Transfer(float amount, AccountType from, AccountType to)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Tried to transfer non-positive amount: {amount}");
                return false;
            }

            if (from == to)
            {
                Debug.LogWarning("[CurrencyManager] Cannot transfer to the same account");
                return false;
            }

            float sourceBalance = from == AccountType.Checking ? _checkingBalance : _investingBalance;

            if (sourceBalance < amount)
            {
                if (_logTransactions)
                {
                    Debug.Log($"[CurrencyManager] Cannot transfer ${amount:F2} from {from}. " +
                              $"Balance: ${sourceBalance:F2}");
                }
                return false;
            }

            // Perform transfer
            if (from == AccountType.Checking)
            {
                _checkingBalance -= amount;
                _investingBalance += amount;
            }
            else
            {
                _investingBalance -= amount;
                _checkingBalance += amount;
            }

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] Transferred ${amount:F2} from {from} to {to}. " +
                          $"Checking: ${_checkingBalance:F2}, Investing: ${_investingBalance:F2}");
            }

            // Fire events
            GameEvents.RaiseCheckingBalanceChanged(_checkingBalance,
                from == AccountType.Checking ? -amount : amount);
            GameEvents.RaiseInvestingBalanceChanged(_investingBalance,
                from == AccountType.Investing ? -amount : amount);
            GameEvents.RaiseTransfer(amount, from, to);

            return true;
        }

        /// <summary>
        /// Check if player can afford an amount from a specific account.
        /// </summary>
        public bool CanAfford(float amount, AccountType account)
        {
            return account == AccountType.Checking
                ? _checkingBalance >= amount
                : _investingBalance >= amount;
        }

        /// <summary>
        /// Check if player can afford from Checking (convenience method).
        /// </summary>
        public bool CanAfford(float amount)
        {
            return CanAfford(amount, AccountType.Checking);
        }

        /// <summary>
        /// Get balance for a specific account.
        /// </summary>
        public float GetBalance(AccountType account)
        {
            return account == AccountType.Checking ? _checkingBalance : _investingBalance;
        }

        /// <summary>
        /// Set balance directly (use sparingly, mainly for testing).
        /// </summary>
        public void SetBalance(AccountType account, float amount)
        {
            if (account == AccountType.Checking)
            {
                float delta = amount - _checkingBalance;
                _checkingBalance = amount;
                GameEvents.RaiseCheckingBalanceChanged(_checkingBalance, delta);
            }
            else
            {
                float delta = amount - _investingBalance;
                _investingBalance = amount;
                GameEvents.RaiseInvestingBalanceChanged(_investingBalance, delta);
            }

            GameEvents.RaiseCurrencyChanged(TotalBalance, 0f);
        }

        /// <summary>
        /// Legacy SetBalance for backwards compatibility.
        /// Sets both accounts proportionally.
        /// </summary>
        public void SetBalance(float amount)
        {
            // Split evenly between accounts
            float half = amount / 2f;
            SetBalance(AccountType.Checking, half);
            SetBalance(AccountType.Investing, half);
        }
    }
}
