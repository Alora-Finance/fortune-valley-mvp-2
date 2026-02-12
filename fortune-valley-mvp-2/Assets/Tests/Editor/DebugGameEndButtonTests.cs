using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for DebugGameEndButton to verify it fires the correct events
    /// without triggering the problematic OnGameEnd double-event chain.
    /// </summary>
    [TestFixture]
    public class DebugGameEndButtonTests
    {
        private DebugGameEndButton _button;
        private UnityEngine.GameObject _go;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _go = new UnityEngine.GameObject("TestDebugButton");
            _button = _go.AddComponent<DebugGameEndButton>();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAllSubscriptions();
            UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void ForceWin_RaisesGameEndWithSummary_PlayerWins()
        {
            bool receivedIsPlayerWin = false;
            GameSummary receivedSummary = null;
            GameEvents.OnGameEndWithSummary += (isWin, summary) =>
            {
                receivedIsPlayerWin = isWin;
                receivedSummary = summary;
            };

            _button.ForceWin();

            Assert.IsTrue(receivedIsPlayerWin);
            Assert.IsNotNull(receivedSummary);
            Assert.AreEqual(45, receivedSummary.DaysPlayed);
            Assert.AreEqual(3, receivedSummary.PlayerLots);
            Assert.AreEqual("Smart Investor!", receivedSummary.Headline);
        }

        [Test]
        public void ForceLose_RaisesGameEndWithSummary_PlayerLoses()
        {
            bool receivedIsPlayerWin = true;
            GameSummary receivedSummary = null;
            GameEvents.OnGameEndWithSummary += (isWin, summary) =>
            {
                receivedIsPlayerWin = isWin;
                receivedSummary = summary;
            };

            _button.ForceLose();

            Assert.IsFalse(receivedIsPlayerWin);
            Assert.IsNotNull(receivedSummary);
            Assert.AreEqual(60, receivedSummary.DaysPlayed);
            Assert.AreEqual(4, receivedSummary.RivalLots);
            Assert.AreEqual("The Rival Got Ahead", receivedSummary.Headline);
        }

        [Test]
        public void ForceWin_DoesNotRaiseOnGameEnd()
        {
            bool gameEndFired = false;
            GameEvents.OnGameEnd += (winner) => gameEndFired = true;

            _button.ForceWin();

            Assert.IsFalse(gameEndFired, "OnGameEnd should NOT fire — only OnGameEndWithSummary");
        }
    }
}
