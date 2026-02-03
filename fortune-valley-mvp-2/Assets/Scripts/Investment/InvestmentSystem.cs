using System.Collections.Generic;
using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages all player investments.
    ///
    /// LEARNING DESIGN: This is the core teaching system. Students must:
    /// 1. See their money grow over time (compound interest)
    /// 2. Understand risk/reward tradeoffs (different investment types)
    /// 3. Experience the time value of money (earlier investments grow more)
    ///
    /// All values are explicit and trackable to support learning reflection.
    /// </summary>
    public class InvestmentSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // DEPENDENCIES
        // ═══════════════════════════════════════════════════════════════

        [Header("Dependencies")]
        [Tooltip("Reference to currency manager")]
        [SerializeField] private CurrencyManager _currencyManager;

        [Tooltip("Reference to time manager (for current tick)")]
        [SerializeField] private TimeManager _timeManager;

        [Header("Available Investments")]
        [Tooltip("Investment types players can choose from")]
        [SerializeField] private List<InvestmentDefinition> _availableInvestments;

        [Header("Debug")]
        [SerializeField] private bool _logCompounding = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private List<ActiveInvestment> _activeInvestments = new List<ActiveInvestment>();

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// All currently active investments.
        /// </summary>
        public IReadOnlyList<ActiveInvestment> ActiveInvestments => _activeInvestments;

        /// <summary>
        /// All investment types available to the player.
        /// </summary>
        public IReadOnlyList<InvestmentDefinition> AvailableInvestments => _availableInvestments;

        /// <summary>
        /// Total value of all active investments.
        /// </summary>
        public float TotalPortfolioValue
        {
            get
            {
                float total = 0f;
                foreach (var inv in _activeInvestments)
                {
                    total += inv.CurrentValue;
                }
                return total;
            }
        }

        /// <summary>
        /// Total amount originally invested (sum of all principals).
        /// </summary>
        public float TotalPrincipal
        {
            get
            {
                float total = 0f;
                foreach (var inv in _activeInvestments)
                {
                    total += inv.Principal;
                }
                return total;
            }
        }

        /// <summary>
        /// Total gain/loss across all investments.
        /// </summary>
        public float TotalGain => TotalPortfolioValue - TotalPrincipal;

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
            _activeInvestments.Clear();
        }

        private void HandleTick(int tickNumber)
        {
            UpdateAllInvestments(tickNumber);
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Create a new investment.
        /// </summary>
        /// <param name="definition">Type of investment</param>
        /// <param name="amount">Amount to invest</param>
        /// <returns>The created investment, or null if failed</returns>
        public ActiveInvestment CreateInvestment(InvestmentDefinition definition, float amount)
        {
            // Validate minimum deposit
            if (amount < definition.MinimumDeposit)
            {
                Debug.Log($"[InvestmentSystem] Amount ${amount:F0} is below minimum ${definition.MinimumDeposit:F0}");
                return null;
            }

            // Try to spend the money from Investing account
            if (!_currencyManager.TrySpend(amount, AccountType.Investing, $"Investment in {definition.DisplayName}"))
            {
                Debug.Log($"[InvestmentSystem] Cannot afford ${amount:F0} investment from Investing account");
                return null;
            }

            // Create the investment
            var investment = new ActiveInvestment(definition, amount, _timeManager.CurrentTick);
            _activeInvestments.Add(investment);

            GameEvents.RaiseInvestmentCreated(investment);

            Debug.Log($"[InvestmentSystem] Created ${amount:F0} investment in {definition.DisplayName}");

            return investment;
        }

        /// <summary>
        /// Withdraw an investment (cash out).
        /// </summary>
        /// <param name="investment">The investment to withdraw</param>
        /// <returns>Amount received (current value), or 0 if failed</returns>
        public float WithdrawInvestment(ActiveInvestment investment)
        {
            if (!_activeInvestments.Contains(investment))
            {
                Debug.LogWarning("[InvestmentSystem] Investment not found");
                return 0f;
            }

            float payout = investment.CurrentValue;
            _activeInvestments.Remove(investment);

            // Add the money back to Investing account
            _currencyManager.Add(payout, AccountType.Investing, $"Withdrew {investment.Definition.DisplayName}");

            GameEvents.RaiseInvestmentWithdrawn(investment, payout);

            Debug.Log($"[InvestmentSystem] Withdrew ${payout:F2} from {investment.Definition.DisplayName}. " +
                     $"Gain: ${investment.TotalGain:F2} ({investment.PercentageReturn:F1}%)");

            return payout;
        }

        /// <summary>
        /// Get investment by ID.
        /// </summary>
        public ActiveInvestment GetInvestment(string id)
        {
            return _activeInvestments.Find(inv => inv.Id == id);
        }

        /// <summary>
        /// Get a portfolio summary for students.
        /// </summary>
        public string GetPortfolioSummary()
        {
            if (_activeInvestments.Count == 0)
            {
                return "You have no active investments.\n" +
                       "Investing allows your money to grow over time through compound interest!";
            }

            string summary = $"Portfolio: {_activeInvestments.Count} investment(s)\n" +
                            $"Total invested: ${TotalPrincipal:F0}\n" +
                            $"Current value: ${TotalPortfolioValue:F0}\n" +
                            $"Total gain/loss: ${TotalGain:F0} ({GetTotalPercentageReturn():F1}%)\n\n";

            foreach (var inv in _activeInvestments)
            {
                summary += $"• {inv.Definition.DisplayName}: ${inv.CurrentValue:F0} " +
                          $"({(inv.TotalGain >= 0 ? "+" : "")}{inv.TotalGain:F0})\n";
            }

            return summary;
        }

        /// <summary>
        /// Get comparison text to help students understand investment vs saving.
        /// </summary>
        public string GetInvestmentVsSavingComparison(float amount, InvestmentDefinition definition, int ticks)
        {
            float projectedValue = definition.ProjectValue(amount, ticks);
            float projectedGain = projectedValue - amount;

            return $"If you invest ${amount:F0} in {definition.DisplayName}:\n" +
                   $"• After {ticks} days: ~${projectedValue:F0}\n" +
                   $"• Potential gain: ~${projectedGain:F0}\n\n" +
                   $"If you keep ${amount:F0} in your wallet:\n" +
                   $"• After {ticks} days: ${amount:F0}\n" +
                   $"• Gain: $0\n\n" +
                   $"The trade-off: Invested money is locked up and can't buy lots immediately.";
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateAllInvestments(int currentTick)
        {
            foreach (var investment in _activeInvestments)
            {
                // Update time held
                investment.IncrementTicksHeld();

                // Try to compound
                if (investment.TryCompound(currentTick))
                {
                    if (_logCompounding)
                    {
                        Debug.Log($"[InvestmentSystem] {investment.Definition.DisplayName} compounded! " +
                                 $"Value: ${investment.CurrentValue:F2}, Gain: ${investment.TotalGain:F2}");
                    }

                    GameEvents.RaiseInvestmentCompounded(investment);
                }
            }
        }

        private float GetTotalPercentageReturn()
        {
            if (TotalPrincipal <= 0)
                return 0f;

            return (TotalPortfolioValue / TotalPrincipal - 1f) * 100f;
        }
    }
}
