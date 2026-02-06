using System;
using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Central event bus for loose coupling between systems.
    /// All game systems publish and subscribe through these static events.
    /// </summary>
    public static class GameEvents
    {
        // ═══════════════════════════════════════════════════════════════
        // TIME EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired every game tick. Core heartbeat of the simulation.
        /// </summary>
        public static event Action<int> OnTick;

        /// <summary>
        /// Fired when game speed changes (pause, 1x, 2x, etc.)
        /// </summary>
        public static event Action<float> OnGameSpeedChanged;

        // ═══════════════════════════════════════════════════════════════
        // CURRENCY EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired when player's total balance changes (legacy, for backwards compatibility).
        /// Parameters: new total balance, delta (positive = gained, negative = spent)
        /// </summary>
        public static event Action<float, float> OnCurrencyChanged;

        /// <summary>
        /// Fired when checking account balance changes.
        /// Parameters: new balance, delta
        /// </summary>
        public static event Action<float, float> OnCheckingBalanceChanged;

        /// <summary>
        /// Fired when investing account balance changes.
        /// Parameters: new balance, delta
        /// </summary>
        public static event Action<float, float> OnInvestingBalanceChanged;

        /// <summary>
        /// Fired when money is transferred between accounts.
        /// Parameters: amount, from account, to account
        /// </summary>
        public static event Action<float, AccountType, AccountType> OnTransfer;

        /// <summary>
        /// Fired when income is generated (for UI feedback).
        /// Parameters: amount, source description
        /// </summary>
        public static event Action<float, string> OnIncomeGenerated;

        /// <summary>
        /// Fired when income is generated with world position (for visual feedback).
        /// Parameters: amount, world position of income source
        /// </summary>
        public static event Action<float, Vector3> OnIncomeGeneratedWithPosition;

        // ═══════════════════════════════════════════════════════════════
        // INVESTMENT EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired when an investment compounds (grows).
        /// Parameter: the investment that just compounded
        /// </summary>
        public static event Action<ActiveInvestment> OnInvestmentCompounded;

        /// <summary>
        /// Fired when player creates a new investment.
        /// </summary>
        public static event Action<ActiveInvestment> OnInvestmentCreated;

        /// <summary>
        /// Fired when player withdraws an investment.
        /// Parameters: the investment, total payout received
        /// </summary>
        public static event Action<ActiveInvestment, float> OnInvestmentWithdrawn;

        // ═══════════════════════════════════════════════════════════════
        // CITY / LOT EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired when any lot is purchased.
        /// Parameters: lot ID, new owner
        /// </summary>
        public static event Action<string, Owner> OnLotPurchased;

        /// <summary>
        /// Fired when rival is about to buy a lot (warning for player).
        /// Parameter: lot ID the rival is targeting
        /// </summary>
        public static event Action<string> OnRivalTargetingLot;

        /// <summary>
        /// Fired when rival's target changes (with days until purchase).
        /// Parameters: lot ID, days until rival attempts purchase
        /// </summary>
        public static event Action<string, int> OnRivalTargetChanged;

        /// <summary>
        /// Fired when rival successfully purchases a lot.
        /// Parameter: lot ID that was purchased
        /// </summary>
        public static event Action<string> OnRivalPurchasedLot;

        // ═══════════════════════════════════════════════════════════════
        // GAME STATE EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired when the game ends.
        /// Parameter: the winner (Player or Rival)
        /// </summary>
        public static event Action<Owner> OnGameEnd;

        /// <summary>
        /// Fired when the game ends with full summary data.
        /// Parameters: isPlayerWin, summary data for end screen
        /// </summary>
        public static event Action<bool, GameSummary> OnGameEndWithSummary;

        /// <summary>
        /// Fired when a new game starts.
        /// </summary>
        public static event Action OnGameStart;

        // ═══════════════════════════════════════════════════════════════
        // RESTAURANT EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired when restaurant is upgraded.
        /// Parameter: new level
        /// </summary>
        public static event Action<int> OnRestaurantUpgraded;

        // ═══════════════════════════════════════════════════════════════
        // EVENT INVOKERS (called by systems to fire events)
        // ═══════════════════════════════════════════════════════════════

        public static void RaiseTick(int tickNumber) => OnTick?.Invoke(tickNumber);
        public static void RaiseGameSpeedChanged(float speed) => OnGameSpeedChanged?.Invoke(speed);
        public static void RaiseCurrencyChanged(float newBalance, float delta) => OnCurrencyChanged?.Invoke(newBalance, delta);
        public static void RaiseCheckingBalanceChanged(float balance, float delta) => OnCheckingBalanceChanged?.Invoke(balance, delta);
        public static void RaiseInvestingBalanceChanged(float balance, float delta) => OnInvestingBalanceChanged?.Invoke(balance, delta);
        public static void RaiseTransfer(float amount, AccountType from, AccountType to) => OnTransfer?.Invoke(amount, from, to);
        public static void RaiseIncomeGenerated(float amount, string source) => OnIncomeGenerated?.Invoke(amount, source);
        public static void RaiseIncomeGeneratedWithPosition(float amount, Vector3 position) => OnIncomeGeneratedWithPosition?.Invoke(amount, position);
        public static void RaiseInvestmentCompounded(ActiveInvestment inv) => OnInvestmentCompounded?.Invoke(inv);
        public static void RaiseInvestmentCreated(ActiveInvestment inv) => OnInvestmentCreated?.Invoke(inv);
        public static void RaiseInvestmentWithdrawn(ActiveInvestment inv, float payout) => OnInvestmentWithdrawn?.Invoke(inv, payout);
        public static void RaiseLotPurchased(string lotId, Owner owner) => OnLotPurchased?.Invoke(lotId, owner);
        public static void RaiseRivalTargetingLot(string lotId) => OnRivalTargetingLot?.Invoke(lotId);
        public static void RaiseRivalTargetChanged(string lotId, int daysUntil) => OnRivalTargetChanged?.Invoke(lotId, daysUntil);
        public static void RaiseRivalPurchasedLot(string lotId) => OnRivalPurchasedLot?.Invoke(lotId);
        public static void RaiseGameEnd(Owner winner) => OnGameEnd?.Invoke(winner);
        public static void RaiseGameEndWithSummary(bool isPlayerWin, GameSummary summary) => OnGameEndWithSummary?.Invoke(isPlayerWin, summary);
        public static void RaiseGameStart() => OnGameStart?.Invoke();
        public static void RaiseRestaurantUpgraded(int level) => OnRestaurantUpgraded?.Invoke(level);

        // ═══════════════════════════════════════════════════════════════
        // CLEANUP (call when exiting play mode or restarting)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Clears all event subscriptions. Call on game restart or cleanup.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnTick = null;
            OnGameSpeedChanged = null;
            OnCurrencyChanged = null;
            OnCheckingBalanceChanged = null;
            OnInvestingBalanceChanged = null;
            OnTransfer = null;
            OnIncomeGenerated = null;
            OnIncomeGeneratedWithPosition = null;
            OnInvestmentCompounded = null;
            OnInvestmentCreated = null;
            OnInvestmentWithdrawn = null;
            OnLotPurchased = null;
            OnRivalTargetingLot = null;
            OnRivalTargetChanged = null;
            OnRivalPurchasedLot = null;
            OnGameEnd = null;
            OnGameEndWithSummary = null;
            OnGameStart = null;
            OnRestaurantUpgraded = null;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SHARED ENUMS (used across multiple systems)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Who owns a lot or who won the game.
    /// </summary>
    public enum Owner
    {
        None,
        Player,
        Rival
    }

    /// <summary>
    /// Risk level for investments. Affects return rate and volatility.
    /// </summary>
    public enum RiskLevel
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Category of investment for grouping in the portfolio panel.
    /// </summary>
    public enum InvestmentCategory
    {
        Stock,
        ETF,
        Bond,
        TBill
    }
}
