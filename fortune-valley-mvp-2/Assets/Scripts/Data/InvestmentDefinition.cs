using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Defines an investment type (e.g., Savings Account, Stocks, Bonds).
    /// Create one ScriptableObject per investment type in the game.
    ///
    /// LEARNING DESIGN: Each investment type should clearly represent
    /// a different risk/reward profile to teach risk vs return.
    /// </summary>
    [CreateAssetMenu(fileName = "NewInvestment", menuName = "Fortune Valley/Investment Definition")]
    public class InvestmentDefinition : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // DISPLAY INFO
        // ═══════════════════════════════════════════════════════════════

        [Header("Display")]
        [Tooltip("Name shown to player (e.g., 'Savings Account')")]
        [SerializeField] private string _displayName;

        [Tooltip("Short description for students")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        // ═══════════════════════════════════════════════════════════════
        // FINANCIAL PARAMETERS
        // ═══════════════════════════════════════════════════════════════

        [Header("Financial Settings")]
        [Tooltip("Low = stable, Medium = some variance, High = volatile")]
        [SerializeField] private RiskLevel _riskLevel = RiskLevel.Low;

        [Tooltip("Annual return rate (0.05 = 5% per year)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _annualReturnRate = 0.05f;

        [Tooltip("For risky investments: multiplier range for returns. 1.0 = no change. (0.5, 1.5) means -50% to +50% of expected return.")]
        [SerializeField] private Vector2 _volatilityRange = new Vector2(1f, 1f);

        [Tooltip("How many game ticks between compound events")]
        [SerializeField] private int _compoundingFrequency = 30; // ~30 days = monthly

        [Tooltip("How many compound periods per 'year' (affects rate calculation). 12 = monthly, 4 = quarterly.")]
        [SerializeField] private int _compoundsPerYear = 12;

        [Tooltip("Minimum amount player can invest")]
        [SerializeField] private float _minimumDeposit = 100f;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public string DisplayName => _displayName;
        public string Description => _description;
        public RiskLevel RiskLevel => _riskLevel;
        public float AnnualReturnRate => _annualReturnRate;
        public Vector2 VolatilityRange => _volatilityRange;
        public int CompoundingFrequency => _compoundingFrequency;
        public int CompoundsPerYear => _compoundsPerYear;
        public float MinimumDeposit => _minimumDeposit;

        // ═══════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get a student-friendly explanation of this investment type.
        /// </summary>
        public string GetExplanation()
        {
            string riskDesc = _riskLevel switch
            {
                RiskLevel.Low => "very safe but grows slowly",
                RiskLevel.Medium => "moderately risky with better potential returns",
                RiskLevel.High => "risky - could gain a lot or lose money",
                _ => "unknown risk"
            };

            return $"{_displayName}: {_description}\n" +
                   $"This investment is {riskDesc}.\n" +
                   $"Expected return: ~{_annualReturnRate * 100:F1}% per year.";
        }

        /// <summary>
        /// Calculate expected value after N ticks (for UI projections).
        /// Note: This is theoretical; actual returns may vary due to volatility.
        /// </summary>
        public float ProjectValue(float principal, int ticks)
        {
            // How many compound events in this period?
            int compoundEvents = ticks / _compoundingFrequency;

            if (compoundEvents == 0)
                return principal;

            // Rate per compound period
            float ratePerPeriod = _annualReturnRate / _compoundsPerYear;

            // Compound interest formula: P * (1 + r)^n
            return principal * Mathf.Pow(1f + ratePerPeriod, compoundEvents);
        }
    }
}
