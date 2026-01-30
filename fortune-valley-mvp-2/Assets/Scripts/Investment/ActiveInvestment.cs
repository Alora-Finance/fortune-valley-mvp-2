using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Runtime data for an active investment held by the player.
    /// This is NOT a MonoBehaviour - it's pure data managed by InvestmentSystem.
    ///
    /// LEARNING NOTE: All values are exposed explicitly so students can see
    /// exactly how their investment is performing and WHY.
    /// </summary>
    [System.Serializable]
    public class ActiveInvestment
    {
        // ═══════════════════════════════════════════════════════════════
        // INVESTMENT IDENTITY
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Unique ID for this investment instance.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Reference to the investment type definition (ScriptableObject).
        /// </summary>
        public InvestmentDefinition Definition { get; private set; }

        // ═══════════════════════════════════════════════════════════════
        // CORE VALUES (what students need to see)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// The original amount invested. Never changes.
        /// </summary>
        public float Principal { get; private set; }

        /// <summary>
        /// Current value including all gains/losses.
        /// </summary>
        public float CurrentValue { get; private set; }

        /// <summary>
        /// Total gain or loss (CurrentValue - Principal).
        /// Positive = profit, negative = loss.
        /// </summary>
        public float TotalGain => CurrentValue - Principal;

        /// <summary>
        /// Percentage return: (CurrentValue / Principal - 1) * 100
        /// </summary>
        public float PercentageReturn => Principal > 0 ? (CurrentValue / Principal - 1f) * 100f : 0f;

        // ═══════════════════════════════════════════════════════════════
        // TIME TRACKING
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Tick when investment was created.
        /// </summary>
        public int CreatedAtTick { get; private set; }

        /// <summary>
        /// Number of ticks this investment has been held.
        /// </summary>
        public int TicksHeld { get; private set; }

        /// <summary>
        /// Tick when the last compound event occurred.
        /// </summary>
        public int LastCompoundTick { get; private set; }

        /// <summary>
        /// How many times this investment has compounded.
        /// LEARNING NOTE: Showing this helps students understand compound frequency.
        /// </summary>
        public int CompoundCount { get; private set; }

        // ═══════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════

        public ActiveInvestment(InvestmentDefinition definition, float principal, int currentTick)
        {
            Id = System.Guid.NewGuid().ToString();
            Definition = definition;
            Principal = principal;
            CurrentValue = principal;
            CreatedAtTick = currentTick;
            LastCompoundTick = currentTick;
            TicksHeld = 0;
            CompoundCount = 0;
        }

        // ═══════════════════════════════════════════════════════════════
        // METHODS (called by InvestmentSystem)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Called each tick to update time held.
        /// </summary>
        public void IncrementTicksHeld()
        {
            TicksHeld++;
        }

        /// <summary>
        /// Apply compound interest. Returns true if compounding occurred.
        /// </summary>
        public bool TryCompound(int currentTick)
        {
            // Check if enough ticks have passed since last compound
            int ticksSinceLastCompound = currentTick - LastCompoundTick;

            if (ticksSinceLastCompound >= Definition.CompoundingFrequency)
            {
                // Calculate compound interest
                // Using the formula: newValue = oldValue * (1 + rate)
                // Rate is per compound period, derived from annual rate
                float ratePerPeriod = Definition.AnnualReturnRate / Definition.CompoundsPerYear;

                // Apply volatility for risky investments
                float actualRate = ApplyVolatility(ratePerPeriod);

                // Compound!
                float previousValue = CurrentValue;
                CurrentValue = CurrentValue * (1f + actualRate);

                // Update tracking
                LastCompoundTick = currentTick;
                CompoundCount++;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Apply random volatility based on risk level.
        /// Low risk = no volatility, High risk = significant swings.
        /// </summary>
        private float ApplyVolatility(float baseRate)
        {
            if (Definition.RiskLevel == RiskLevel.Low)
            {
                // No volatility - steady returns
                return baseRate;
            }

            // Apply volatility: rate can swing within the defined range
            float volatilityMultiplier = Random.Range(
                Definition.VolatilityRange.x,
                Definition.VolatilityRange.y
            );

            return baseRate * volatilityMultiplier;
        }

        /// <summary>
        /// Get a plain-language explanation of this investment's performance.
        /// LEARNING NOTE: This helps students articulate what happened.
        /// </summary>
        public string GetPerformanceExplanation()
        {
            string gainOrLoss = TotalGain >= 0 ? "gained" : "lost";
            string absGain = Mathf.Abs(TotalGain).ToString("F2");

            if (CompoundCount == 0)
            {
                return $"Your ${Principal:F2} hasn't compounded yet. It will grow after {Definition.CompoundingFrequency} days.";
            }

            return $"Your ${Principal:F2} has {gainOrLoss} ${absGain} ({PercentageReturn:F1}%) " +
                   $"after {TicksHeld} days and {CompoundCount} compound events.";
        }
    }
}
