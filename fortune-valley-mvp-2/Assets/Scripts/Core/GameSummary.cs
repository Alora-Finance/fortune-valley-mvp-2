using System.Collections.Generic;

namespace FortuneValley.Core
{
    /// <summary>
    /// Data class containing summary statistics for the end game screen.
    /// Captures the player's financial journey for reflection and learning.
    /// </summary>
    [System.Serializable]
    public class GameSummary
    {
        // ═══════════════════════════════════════════════════════════════
        // TIME DATA
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Total game days (ticks) played.
        /// </summary>
        public int DaysPlayed;

        // ═══════════════════════════════════════════════════════════════
        // OWNERSHIP DATA
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Number of lots owned by the player at game end.
        /// </summary>
        public int PlayerLots;

        /// <summary>
        /// Number of lots owned by the rival at game end.
        /// </summary>
        public int RivalLots;

        /// <summary>
        /// Total number of lots in the city.
        /// </summary>
        public int TotalLots;

        // ═══════════════════════════════════════════════════════════════
        // FINANCIAL DATA
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Player's total net worth at game end (checking + investing + portfolio value).
        /// </summary>
        public float FinalNetWorth;

        /// <summary>
        /// Total gains from investments (compound interest earned).
        /// </summary>
        public float TotalInvestmentGains;

        /// <summary>
        /// Total income earned from the restaurant.
        /// </summary>
        public float TotalRestaurantIncome;

        /// <summary>
        /// Total money spent on lot purchases.
        /// </summary>
        public float TotalSpentOnLots;

        // ═══════════════════════════════════════════════════════════════
        // LEARNING DATA
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Key decisions the player made that affected the outcome.
        /// Used to highlight learning moments.
        /// </summary>
        public List<string> KeyDecisions = new List<string>();

        /// <summary>
        /// Number of times the player invested money.
        /// </summary>
        public int InvestmentCount;

        /// <summary>
        /// Highest portfolio value achieved during the game.
        /// </summary>
        public float PeakPortfolioValue;

        // ═══════════════════════════════════════════════════════════════
        // LEARNING REFLECTIONS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Short headline summarizing the outcome (e.g., "Smart Investor!" or "Keep Trying!").
        /// </summary>
        public string Headline;

        /// <summary>
        /// Reflection about the player's investment behavior and compound interest.
        /// </summary>
        public string InvestmentInsight;

        /// <summary>
        /// Reflection about opportunity cost decisions.
        /// </summary>
        public string OpportunityCostInsight;

        /// <summary>
        /// Counterfactual "what if" message to encourage deeper thinking.
        /// </summary>
        public string WhatIfMessage;

        /// <summary>
        /// Record of each lot purchased by the player with timing data.
        /// </summary>
        public List<LotPurchaseRecord> LotPurchases = new List<LotPurchaseRecord>();

        // ═══════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get a student-friendly summary of the game results.
        /// </summary>
        public string GetReadableSummary(bool isWin)
        {
            string outcome = isWin ? "Victory!" : "Defeat";
            string lotComparison = $"You owned {PlayerLots} lots, rival owned {RivalLots} lots.";
            string investmentSummary = TotalInvestmentGains > 0
                ? $"Your investments earned you ${TotalInvestmentGains:N0} through compound interest!"
                : "You didn't benefit from compound interest this game.";

            return $"{outcome}\n\n" +
                   $"Game lasted {DaysPlayed} days.\n" +
                   $"{lotComparison}\n\n" +
                   $"Final Net Worth: ${FinalNetWorth:N0}\n" +
                   $"Restaurant Income: ${TotalRestaurantIncome:N0}\n" +
                   $"{investmentSummary}";
        }

        /// <summary>
        /// Add a key decision to the summary.
        /// </summary>
        public void AddKeyDecision(string decision)
        {
            if (KeyDecisions.Count < 5) // Limit to 5 key decisions
            {
                KeyDecisions.Add(decision);
            }
        }
    }

    /// <summary>
    /// Records when and why a player purchased a specific lot.
    /// </summary>
    [System.Serializable]
    public struct LotPurchaseRecord
    {
        public string LotId;
        public string LotName;
        public float Cost;
        public float IncomeBonus;
        public int PurchasedOnDay;
    }
}
