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
        /// Fired when player's balance changes.
        /// Parameters: new balance, delta (positive = gained, negative = spent)
        /// </summary>
        public static event Action<float, float> OnCurrencyChanged;

        /// <summary>
        /// Fired when income is generated (for UI feedback).
        /// Parameters: amount, source description
        /// </summary>
        public static event Action<float, string> OnIncomeGenerated;

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

        // ═══════════════════════════════════════════════════════════════
        // GAME STATE EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired when the game ends.
        /// Parameter: the winner (Player or Rival)
        /// </summary>
        public static event Action<Owner> OnGameEnd;

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
        public static void RaiseIncomeGenerated(float amount, string source) => OnIncomeGenerated?.Invoke(amount, source);
        public static void RaiseInvestmentCompounded(ActiveInvestment inv) => OnInvestmentCompounded?.Invoke(inv);
        public static void RaiseInvestmentCreated(ActiveInvestment inv) => OnInvestmentCreated?.Invoke(inv);
        public static void RaiseInvestmentWithdrawn(ActiveInvestment inv, float payout) => OnInvestmentWithdrawn?.Invoke(inv, payout);
        public static void RaiseLotPurchased(string lotId, Owner owner) => OnLotPurchased?.Invoke(lotId, owner);
        public static void RaiseRivalTargetingLot(string lotId) => OnRivalTargetingLot?.Invoke(lotId);
        public static void RaiseGameEnd(Owner winner) => OnGameEnd?.Invoke(winner);
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
            OnIncomeGenerated = null;
            OnInvestmentCompounded = null;
            OnInvestmentCreated = null;
            OnInvestmentWithdrawn = null;
            OnLotPurchased = null;
            OnRivalTargetingLot = null;
            OnGameEnd = null;
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
}
