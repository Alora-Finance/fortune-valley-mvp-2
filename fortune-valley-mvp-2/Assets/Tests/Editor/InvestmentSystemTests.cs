using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for InvestmentSystem lifetime tracking.
    /// Uses reflection to wire private serialized fields since InvestmentSystem is a MonoBehaviour.
    /// Sets _balance directly to avoid event-ordering issues in Editor tests.
    /// </summary>
    [TestFixture]
    public class InvestmentSystemTests
    {
        private GameObject _rootGO;
        private InvestmentSystem _system;
        private CurrencyManager _currency;
        private TimeManager _time;
        private InvestmentDefinition _stockDef;

        [SetUp]
        public void SetUp()
        {
            _rootGO = new GameObject("TestRoot");

            _currency = _rootGO.AddComponent<CurrencyManager>();
            _time = _rootGO.AddComponent<TimeManager>();
            _system = _rootGO.AddComponent<InvestmentSystem>();

            // Create a test investment definition
            _stockDef = ScriptableObject.CreateInstance<InvestmentDefinition>();
            SetField(_stockDef, "_displayName", "TestStock");
            SetField(_stockDef, "_riskLevel", RiskLevel.Medium);
            SetField(_stockDef, "_annualReturnRate", 0.10f);
            SetField(_stockDef, "_basePricePerShare", 100f);
            SetField(_stockDef, "_category", InvestmentCategory.Stock);
            _stockDef.InitializePrice();

            // Wire dependencies via reflection
            SetField(_system, "_currencyManager", _currency);
            SetField(_system, "_timeManager", _time);
            var list = new System.Collections.Generic.List<InvestmentDefinition> { _stockDef };
            SetField(_system, "_availableInvestments", list);

            // Set balance directly (avoid relying on event chain)
            SetField(_currency, "_balance", 10000f);
            SetField(_currency, "_startingBalance", 10000f);
        }

        [TearDown]
        public void TearDown()
        {
            // Unsubscribe events before destroying to avoid stale callbacks
            Object.DestroyImmediate(_rootGO);
            Object.DestroyImmediate(_stockDef);
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            throw new System.Exception($"Field '{fieldName}' not found on {target.GetType().Name}");
        }

        // ═══════════════════════════════════════════════════════════════
        // TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void BuyShares_IncrementsLifetimeCountAndPrincipal()
        {
            var inv = _system.BuyShares(_stockDef, 5);

            Assert.IsNotNull(inv, "BuyShares returned null");
            Assert.AreEqual(1, _system.LifetimeTotalInvestmentsMade);
            Assert.AreEqual(5 * _stockDef.CurrentPrice, _system.LifetimeTotalPrincipalInvested, 1f);
        }

        [Test]
        public void BuyThenSellAll_LifetimeCountPreserved()
        {
            var inv = _system.BuyShares(_stockDef, 5);
            Assert.IsNotNull(inv, "BuyShares returned null");

            _system.SellAllShares(inv);

            // Lifetime count should still be 1 even after selling
            Assert.AreEqual(1, _system.LifetimeTotalInvestmentsMade);
            Assert.AreEqual(0, _system.ActiveInvestments.Count);
        }

        [Test]
        public void SellAllShares_RecordsRealizedGain()
        {
            var inv = _system.BuyShares(_stockDef, 10);
            Assert.IsNotNull(inv, "BuyShares returned null");
            float buyPrice = inv.AveragePurchasePrice;

            // Nudge the price up to simulate a gain
            SetField(_stockDef, "_currentPrice", buyPrice * 1.1f);

            _system.SellAllShares(inv);

            // Realized gain should be positive (all sold, no unrealized left)
            Assert.Greater(_system.LifetimeTotalGain, 0f,
                "LifetimeTotalGain should be positive after selling at profit");
        }

        [Test]
        public void PartialSell_RecordsProportionalGain()
        {
            var inv = _system.BuyShares(_stockDef, 10);
            Assert.IsNotNull(inv, "BuyShares returned null");
            float buyPrice = inv.AveragePurchasePrice;

            // Price goes up 10%
            float newPrice = buyPrice * 1.1f;
            SetField(_stockDef, "_currentPrice", newPrice);

            // Sell 5 of 10 shares
            _system.SellShares(inv, 5);

            // Expected realized gain: 5 shares * (newPrice - buyPrice)
            float expectedRealized = 5f * (newPrice - buyPrice);
            // Unrealized: 5 remaining shares * (newPrice - buyPrice) = same
            float expectedUnrealized = 5f * (newPrice - buyPrice);
            float expectedTotal = expectedRealized + expectedUnrealized;

            Assert.AreEqual(expectedTotal, _system.LifetimeTotalGain, 1f);
        }

        [Test]
        public void HandleGameStart_ResetsAllLifetimeFields()
        {
            // Buy and sell to accumulate lifetime data
            var inv = _system.BuyShares(_stockDef, 5);
            Assert.IsNotNull(inv, "BuyShares returned null");
            _system.SellAllShares(inv);

            Assert.Greater(_system.LifetimeTotalInvestmentsMade, 0,
                "Should have recorded a buy before testing reset");

            // Invoke HandleGameStart directly via reflection
            // (OnEnable event subscription may not fire in Editor tests)
            var method = typeof(InvestmentSystem).GetMethod("HandleGameStart",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_system, null);

            Assert.AreEqual(0, _system.LifetimeTotalInvestmentsMade);
            Assert.AreEqual(0f, _system.LifetimeTotalPrincipalInvested);
            Assert.AreEqual(0f, _system.LifetimeTotalGain);
            Assert.AreEqual(0f, _system.PeakPortfolioValue);
        }

        [Test]
        public void MultipleBuySellCycles_AccumulatesLifetimeData()
        {
            float price = _stockDef.CurrentPrice;

            // Cycle 1
            var inv1 = _system.BuyShares(_stockDef, 3);
            Assert.IsNotNull(inv1, "BuyShares (cycle 1) returned null");
            _system.SellAllShares(inv1);

            // Cycle 2
            var inv2 = _system.BuyShares(_stockDef, 7);
            Assert.IsNotNull(inv2, "BuyShares (cycle 2) returned null");
            _system.SellAllShares(inv2);

            Assert.AreEqual(2, _system.LifetimeTotalInvestmentsMade);
            // 3 shares + 7 shares at the same price
            float expectedPrincipal = (3 + 7) * price;
            Assert.AreEqual(expectedPrincipal, _system.LifetimeTotalPrincipalInvested, 1f);
        }

        [Test]
        public void LifetimeTotalGain_EqualsRealizedPlusUnrealized()
        {
            // Buy 10 shares, sell 5, keep 5
            var inv = _system.BuyShares(_stockDef, 10);
            Assert.IsNotNull(inv, "BuyShares returned null");
            float buyPrice = inv.AveragePurchasePrice;

            // Price up 20%
            float newPrice = buyPrice * 1.2f;
            SetField(_stockDef, "_currentPrice", newPrice);

            _system.SellShares(inv, 5);

            // Realized: 5 * (newPrice - buyPrice)
            float realized = 5f * (newPrice - buyPrice);
            // Unrealized: TotalGain on remaining 5 shares
            float unrealized = _system.TotalGain;
            float expected = realized + unrealized;

            Assert.AreEqual(expected, _system.LifetimeTotalGain, 0.1f);
        }

        [Test]
        public void SellAtLoss_RecordsNegativeRealizedGain()
        {
            var inv = _system.BuyShares(_stockDef, 10);
            Assert.IsNotNull(inv, "BuyShares returned null");
            float buyPrice = inv.AveragePurchasePrice;

            // Price drops 15%
            SetField(_stockDef, "_currentPrice", buyPrice * 0.85f);

            _system.SellAllShares(inv);

            Assert.Less(_system.LifetimeTotalGain, 0f,
                "LifetimeTotalGain should be negative after selling at loss");
        }
    }
}
