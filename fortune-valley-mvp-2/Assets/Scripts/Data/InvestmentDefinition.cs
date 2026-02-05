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

        [Tooltip("Minimum amount player can invest (legacy, kept for compatibility)")]
        [SerializeField] private float _minimumDeposit = 100f;

        // ═══════════════════════════════════════════════════════════════
        // SHARE PRICE SETTINGS
        // ═══════════════════════════════════════════════════════════════

        [Header("Share Price")]
        [Tooltip("Starting price per share")]
        [SerializeField] private float _basePricePerShare = 50f;

        // Runtime price state (not serialized)
        private float _currentPrice;
        private float _trendDirection;  // -1 to +1, persists between ticks for momentum
        private bool _priceInitialized = false;

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
        public float BasePricePerShare => _basePricePerShare;

        /// <summary>
        /// Current fluctuating price per share.
        /// </summary>
        public float CurrentPrice
        {
            get
            {
                if (!_priceInitialized)
                    InitializePrice();
                return _currentPrice;
            }
        }

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

        // ═══════════════════════════════════════════════════════════════
        // PRICE FLUCTUATION METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Initialize price to base value. Call at game start.
        /// Randomizes initial trend direction so each investment starts differently.
        /// </summary>
        public void InitializePrice()
        {
            _currentPrice = _basePricePerShare;
            // Start with a random trend direction (-1 to +1)
            _trendDirection = Random.Range(-1f, 1f);
            _priceInitialized = true;
        }

        /// <summary>
        /// Update price using momentum-based model. Call each tick.
        ///
        /// LEARNING DESIGN: Prices now trend up or down for periods, teaching students
        /// that markets have momentum. Higher risk = longer trends = bigger swings.
        /// This helps students see patterns like "holding through a downtrend" or
        /// "selling at the top of an uptrend."
        /// </summary>
        public void UpdatePrice()
        {
            if (!_priceInitialized)
                InitializePrice();

            // Step 1: Maybe reverse trend (lower chance = longer trends = more volatility)
            // Low risk: 20% chance to flip (trends are short, prices stable)
            // Medium risk: 10% chance (moderate trend length)
            // High risk: 5% chance (long trends = big swings up or down)
            float reversalChance = _riskLevel switch
            {
                RiskLevel.Low => 0.20f,
                RiskLevel.Medium => 0.10f,
                RiskLevel.High => 0.05f,
                _ => 0.10f
            };

            if (Random.value < reversalChance)
            {
                // Reverse the trend (flip sign and randomize magnitude a bit)
                _trendDirection = -Mathf.Sign(_trendDirection) * Random.Range(0.5f, 1f);
            }

            // Step 2: Calculate daily price change
            // Trend strength: how much the trend direction affects price
            // Higher risk = stronger trend impact
            float trendStrength = _riskLevel switch
            {
                RiskLevel.Low => 0.003f,     // 0.3% max trend impact
                RiskLevel.Medium => 0.01f,   // 1% max trend impact
                RiskLevel.High => 0.02f,     // 2% max trend impact
                _ => 0.01f
            };

            // Trend contribution: direction × strength
            float trendChange = _trendDirection * trendStrength;

            // Small random noise adds texture (±0.2%)
            float noise = Random.Range(-0.002f, 0.002f);

            // Upward drift based on expected annual return (long-term trend toward expected value)
            float dailyDrift = _annualReturnRate / 365f;

            // Total daily change
            float dailyChange = trendChange + noise + dailyDrift;

            // Step 3: Apply change to price
            _currentPrice *= (1f + dailyChange);

            // Clamp to floor (10% of base price) to prevent absurdly low values
            _currentPrice = Mathf.Max(_currentPrice, _basePricePerShare * 0.1f);
        }

        /// <summary>
        /// Reset price to base (for game restart).
        /// Also resets trend direction to a random starting value.
        /// </summary>
        public void ResetPrice()
        {
            _currentPrice = _basePricePerShare;
            _trendDirection = Random.Range(-1f, 1f);
            _priceInitialized = true;
        }
    }
}
