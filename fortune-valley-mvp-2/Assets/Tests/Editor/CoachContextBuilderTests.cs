using System.Collections.Generic;
using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class CoachContextBuilderTests
    {
        private GameSummary CreateTestSummary(bool isWin)
        {
            var summary = new GameSummary
            {
                DaysPlayed = 45,
                PlayerLots = isWin ? 5 : 2,
                RivalLots = isWin ? 2 : 5,
                TotalLots = 7,
                FinalNetWorth = 12500f,
                TotalInvestmentGains = 3200f,
                TotalRestaurantIncome = 8400f,
                TotalSpentOnLots = 6000f,
                InvestmentCount = 4,
                PeakPortfolioValue = 5000f,
                Headline = isWin ? "Smart Investor!" : "The Rival Got Ahead",
                InvestmentInsight = "Compound interest helped.",
                OpportunityCostInsight = "Investing early paid off.",
                WhatIfMessage = "What if you had invested sooner?"
            };

            summary.KeyDecisions.Add("Invested in Tech Fund on Day 5");
            summary.KeyDecisions.Add("Bought Corner Lot on Day 10");

            summary.LotPurchases.Add(new LotPurchaseRecord
            {
                LotId = "lot_corner",
                LotName = "Corner Bistro",
                Cost = 2000f,
                IncomeBonus = 50f,
                PurchasedOnDay = 10
            });

            summary.LotPurchases.Add(new LotPurchaseRecord
            {
                LotId = "lot_cafe",
                LotName = "Downtown Cafe",
                Cost = 3000f,
                IncomeBonus = 75f,
                PurchasedOnDay = 25
            });

            return summary;
        }

        [Test]
        public void BuildContext_WithWin_ContainsVictory()
        {
            var summary = CreateTestSummary(true);
            string context = CoachContextBuilder.BuildContext(true, summary, null, null);

            // All 7 section headers should be present
            Assert.IsTrue(context.Contains("=== GAME OUTCOME ==="));
            Assert.IsTrue(context.Contains("=== FINANCIAL PERFORMANCE ==="));
            Assert.IsTrue(context.Contains("=== PORTFOLIO AT GAME END ==="));
            Assert.IsTrue(context.Contains("=== AVAILABLE INVESTMENTS ==="));
            Assert.IsTrue(context.Contains("=== LOT PURCHASE TIMELINE ==="));
            Assert.IsTrue(context.Contains("=== KEY DECISIONS ==="));
            Assert.IsTrue(context.Contains("=== LEARNING REFLECTIONS ==="));

            Assert.IsTrue(context.Contains("VICTORY"));
        }

        [Test]
        public void BuildContext_WithLoss_ContainsDefeat()
        {
            var summary = CreateTestSummary(false);
            string context = CoachContextBuilder.BuildContext(false, summary, null, null);

            Assert.IsTrue(context.Contains("DEFEAT"));
            Assert.IsTrue(context.Contains("Rival Lots: 5/7"));
        }

        [Test]
        public void BuildContext_IncludesSpecificValues()
        {
            var summary = CreateTestSummary(true);
            string context = CoachContextBuilder.BuildContext(true, summary, null, null);

            Assert.IsTrue(context.Contains("$12,500"), "Should contain FinalNetWorth");
            Assert.IsTrue(context.Contains("$3,200"), "Should contain TotalInvestmentGains");
            Assert.IsTrue(context.Contains("$8,400"), "Should contain TotalRestaurantIncome");
            Assert.IsTrue(context.Contains("45"), "Should contain DaysPlayed");
            Assert.IsTrue(context.Contains("Day 10"), "Should contain lot purchase day");
        }

        [Test]
        public void BuildContext_WithNoInvestments_SaysNone()
        {
            var summary = CreateTestSummary(true);
            string context = CoachContextBuilder.BuildContext(true, summary,
                new List<ActiveInvestment>(), null);

            Assert.IsTrue(context.Contains("no active investments"));
        }

        [Test]
        public void BuildContext_WithNullLists_DoesNotThrow()
        {
            // Null summary
            Assert.DoesNotThrow(() =>
            {
                CoachContextBuilder.BuildContext(true, null, null, null);
            });

            // Summary with null KeyDecisions and LotPurchases
            var summary = new GameSummary
            {
                DaysPlayed = 10,
                PlayerLots = 1,
                RivalLots = 0,
                TotalLots = 5
            };
            summary.KeyDecisions = null;
            summary.LotPurchases = null;

            Assert.DoesNotThrow(() =>
            {
                CoachContextBuilder.BuildContext(false, summary, null, null);
            });
        }

        [Test]
        public void BuildContext_WithLotPurchases_IncludesTimeline()
        {
            var summary = CreateTestSummary(true);
            string context = CoachContextBuilder.BuildContext(true, summary, null, null);

            Assert.IsTrue(context.Contains("Corner Bistro"), "Should include lot name");
            Assert.IsTrue(context.Contains("Downtown Cafe"), "Should include second lot name");
            Assert.IsTrue(context.Contains("Day 10"), "Should include purchase day");
            Assert.IsTrue(context.Contains("Day 25"), "Should include second purchase day");
        }
    }
}
