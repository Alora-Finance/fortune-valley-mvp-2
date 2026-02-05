using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages the player's restaurant income generation.
    ///
    /// LEARNING DESIGN: The restaurant is the "safe" baseline income.
    /// Students should understand: "I can always rely on my restaurant,
    /// but it won't make me rich fast. Is there a better use for my money?"
    ///
    /// This creates the foundation for understanding opportunity cost.
    /// </summary>
    public class RestaurantSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // DEPENDENCIES
        // ═══════════════════════════════════════════════════════════════

        [Header("Dependencies")]
        [Tooltip("Restaurant configuration (income rates, upgrade costs)")]
        [SerializeField] private RestaurantConfig _config;

        [Tooltip("Reference to currency manager for income deposits")]
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Debug")]
        [SerializeField] private bool _logIncome = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private int _currentLevel = 1;
        private float _totalEarned = 0f;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Current restaurant upgrade level.
        /// </summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>
        /// Income generated per tick at current level.
        /// </summary>
        public float IncomePerTick => _config.GetIncomeForLevel(_currentLevel);

        /// <summary>
        /// Total money earned from restaurant this game.
        /// </summary>
        public float TotalEarned => _totalEarned;

        /// <summary>
        /// Whether the restaurant can be upgraded.
        /// </summary>
        public bool CanUpgrade => _config.CanUpgrade(_currentLevel);

        /// <summary>
        /// Cost to upgrade to the next level, or -1 if max level.
        /// </summary>
        public float UpgradeCost => _config.GetUpgradeCost(_currentLevel);

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void HandleGameStart()
        {
            _currentLevel = 1;
            _totalEarned = 0f;
        }

        private void HandleTick(int tickNumber)
        {
            GenerateIncome();
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Attempt to upgrade the restaurant.
        /// Returns true if upgrade was successful.
        /// </summary>
        public bool TryUpgrade()
        {
            if (!CanUpgrade)
            {
                Debug.Log("[RestaurantSystem] Already at max level.");
                return false;
            }

            float cost = _config.GetUpgradeCost(_currentLevel);

            if (!_currencyManager.TrySpend(cost, $"Restaurant upgrade to level {_currentLevel + 1}"))
            {
                Debug.Log($"[RestaurantSystem] Cannot afford upgrade. Need ${cost:F0}");
                return false;
            }

            _currentLevel++;
            GameEvents.RaiseRestaurantUpgraded(_currentLevel);

            if (_logIncome)
            {
                Debug.Log($"[RestaurantSystem] Upgraded to level {_currentLevel}. " +
                         $"New income: ${IncomePerTick:F2}/tick");
            }

            return true;
        }

        /// <summary>
        /// Get student-friendly explanation of upgrade value.
        /// </summary>
        public string GetUpgradeExplanation()
        {
            return _config.GetUpgradeExplanation(_currentLevel);
        }

        /// <summary>
        /// Get summary of restaurant performance for UI.
        /// </summary>
        public string GetPerformanceSummary()
        {
            return $"Restaurant Level {_currentLevel}\n" +
                   $"Income: ${IncomePerTick:F0} per day\n" +
                   $"Total earned: ${_totalEarned:F0}";
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void GenerateIncome()
        {
            float income = IncomePerTick;
            _totalEarned += income;
            _currencyManager.Add(income, "Restaurant");

            // Raise event for visual feedback system (floating text, coin animation)
            GameEvents.RaiseIncomeGeneratedWithPosition(income, transform.position);

            if (_logIncome)
            {
                Debug.Log($"[RestaurantSystem] Generated ${income:F2}. Total: ${_totalEarned:F2}");
            }
        }
    }
}
