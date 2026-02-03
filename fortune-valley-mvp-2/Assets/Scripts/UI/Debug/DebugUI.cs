using UnityEngine;
using UnityEngine.InputSystem;
using FortuneValley.Core;

namespace FortuneValley.UI.Debug
{
    /// <summary>
    /// Simple debug UI for testing game systems before building the real UI.
    /// Uses OnGUI for quick iteration - replace with proper UI later.
    /// </summary>
    public class DebugUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("System References")]
        [SerializeField] private GameManager _gameManager;

        [Header("UI Settings")]
        [SerializeField] private bool _showDebugUI = false;  // Hidden by default, toggle with F1

        // Use const to avoid serialization issues with Key enum in New Input System
        private const Key ToggleKey = Key.F1;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private float _investAmount = 500f;
        private int _selectedInvestmentIndex = 0;
        private string _lastAction = "";
        private Vector2 _scrollPosition;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[ToggleKey].wasPressedThisFrame)
            {
                _showDebugUI = !_showDebugUI;
            }
        }

        private void OnGUI()
        {
            if (!_showDebugUI || _gameManager == null)
                return;

            // Main panel
            GUILayout.BeginArea(new Rect(10, 10, 350, Screen.height - 20));
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);

            DrawHeader();
            DrawGameState();
            DrawCurrencyPanel();
            DrawRestaurantPanel();
            DrawInvestmentPanel();
            DrawCityPanel();
            DrawRivalPanel();
            DrawActionLog();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ═══════════════════════════════════════════════════════════════
        // UI SECTIONS
        // ═══════════════════════════════════════════════════════════════

        private void DrawHeader()
        {
            GUILayout.Label("<size=18><b>Fortune Valley Debug</b></size>", CreateRichTextStyle());
            GUILayout.Label($"Press {ToggleKey} to toggle");
            GUILayout.Space(10);
        }

        private void DrawGameState()
        {
            GUILayout.Label("<b>═══ GAME STATE ═══</b>", CreateRichTextStyle());

            var time = _gameManager.TimeManager;
            GUILayout.Label($"Day: {time.CurrentTick}");
            GUILayout.Label($"Speed: {time.CurrentSpeed}x");
            GUILayout.Label($"State: {_gameManager.CurrentState}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause/Resume"))
            {
                _gameManager.TogglePause();
                LogAction("Toggled pause");
            }
            if (GUILayout.Button("Speed"))
            {
                time.CycleSpeed();
                LogAction($"Speed → {time.CurrentSpeed}x");
            }
            if (GUILayout.Button("Restart"))
            {
                _gameManager.RestartGame();
                LogAction("Game restarted");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        private void DrawCurrencyPanel()
        {
            GUILayout.Label("<b>═══ CURRENCY ═══</b>", CreateRichTextStyle());

            var currency = _gameManager.CurrencyManager;
            GUILayout.Label($"<size=16><b>${currency.Balance:F0}</b></size>", CreateRichTextStyle());

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+$100"))
            {
                currency.Add(100, "Debug");
                LogAction("+$100");
            }
            if (GUILayout.Button("+$1000"))
            {
                currency.Add(1000, "Debug");
                LogAction("+$1000");
            }
            if (GUILayout.Button("+$10000"))
            {
                currency.Add(10000, "Debug");
                LogAction("+$10000");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        private void DrawRestaurantPanel()
        {
            GUILayout.Label("<b>═══ RESTAURANT ═══</b>", CreateRichTextStyle());

            var restaurant = _gameManager.RestaurantSystem;
            GUILayout.Label($"Level: {restaurant.CurrentLevel}");
            GUILayout.Label($"Income: ${restaurant.IncomePerTick:F0}/day");
            GUILayout.Label($"Total earned: ${restaurant.TotalEarned:F0}");

            if (restaurant.CanUpgrade)
            {
                if (GUILayout.Button($"Upgrade (${restaurant.UpgradeCost:F0})"))
                {
                    if (restaurant.TryUpgrade())
                        LogAction($"Upgraded to level {restaurant.CurrentLevel}");
                    else
                        LogAction("Cannot afford upgrade");
                }
            }
            else
            {
                GUILayout.Label("Max level reached!");
            }

            GUILayout.Space(10);
        }

        private void DrawInvestmentPanel()
        {
            GUILayout.Label("<b>═══ INVESTMENTS ═══</b>", CreateRichTextStyle());

            var investments = _gameManager.InvestmentSystem;

            // Portfolio summary
            GUILayout.Label($"Portfolio: ${investments.TotalPortfolioValue:F0}");
            GUILayout.Label($"Total gain: ${investments.TotalGain:F0}");

            // Investment amount input
            GUILayout.BeginHorizontal();
            GUILayout.Label("Amount:", GUILayout.Width(60));
            string amountStr = GUILayout.TextField(_investAmount.ToString("F0"));
            if (float.TryParse(amountStr, out float newAmount))
                _investAmount = newAmount;
            GUILayout.EndHorizontal();

            // Investment type selection
            var available = investments.AvailableInvestments;
            if (available.Count > 0)
            {
                string[] names = new string[available.Count];
                for (int i = 0; i < available.Count; i++)
                    names[i] = available[i].DisplayName;

                _selectedInvestmentIndex = Mathf.Clamp(_selectedInvestmentIndex, 0, names.Length - 1);
                _selectedInvestmentIndex = GUILayout.SelectionGrid(_selectedInvestmentIndex, names, 2);

                var selected = available[_selectedInvestmentIndex];
                GUILayout.Label($"Risk: {selected.RiskLevel} | Return: {selected.AnnualReturnRate * 100:F0}%");

                if (GUILayout.Button($"Invest ${_investAmount:F0}"))
                {
                    var inv = investments.CreateInvestment(selected, _investAmount);
                    if (inv != null)
                        LogAction($"Invested ${_investAmount:F0} in {selected.DisplayName}");
                    else
                        LogAction("Investment failed");
                }
            }

            // Active investments
            if (investments.ActiveInvestments.Count > 0)
            {
                GUILayout.Label("Active investments:");
                foreach (var inv in investments.ActiveInvestments)
                {
                    GUILayout.BeginHorizontal();
                    string gainColor = inv.TotalGain >= 0 ? "green" : "red";
                    GUILayout.Label($"<color={gainColor}>{inv.Definition.DisplayName}: ${inv.CurrentValue:F0} ({inv.TotalGain:+0;-0})</color>",
                        CreateRichTextStyle(), GUILayout.Width(200));

                    if (GUILayout.Button("Withdraw"))
                    {
                        float payout = investments.WithdrawInvestment(inv);
                        LogAction($"Withdrew ${payout:F0}");
                        break; // List modified, exit loop
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10);
        }

        private void DrawCityPanel()
        {
            GUILayout.Label("<b>═══ CITY LOTS ═══</b>", CreateRichTextStyle());

            var city = _gameManager.CityManager;
            GUILayout.Label($"Player: {city.PlayerLotCount} | Rival: {city.RivalLotCount} | Available: {city.AvailableLotCount}");
            GUILayout.Label($"Lot income bonus: ${city.PlayerLotIncomeBonus:F0}/day");

            // List available lots
            var availableLots = city.GetAvailableLots();
            foreach (var lot in availableLots)
            {
                GUILayout.BeginHorizontal();

                bool canAfford = _gameManager.CurrencyManager.CanAfford(lot.BaseCost);
                string color = canAfford ? "white" : "gray";
                GUILayout.Label($"<color={color}>{lot.DisplayName} (${lot.BaseCost:F0})</color>",
                    CreateRichTextStyle(), GUILayout.Width(180));

                if (GUILayout.Button("Buy"))
                {
                    if (city.TryPurchaseLot(lot.LotId, _gameManager.TimeManager.CurrentTick))
                        LogAction($"Bought {lot.DisplayName}");
                    else
                        LogAction($"Cannot buy {lot.DisplayName}");
                }
                GUILayout.EndHorizontal();
            }

            // Show owned lots
            GUILayout.Label("Owned lots:");
            foreach (var lot in city.AllLots)
            {
                var owner = city.GetOwner(lot.LotId);
                if (owner == Owner.Player)
                    GUILayout.Label($"  <color=green>✓ {lot.DisplayName}</color>", CreateRichTextStyle());
                else if (owner == Owner.Rival)
                    GUILayout.Label($"  <color=red>✗ {lot.DisplayName} (Rival)</color>", CreateRichTextStyle());
            }

            GUILayout.Space(10);
        }

        private void DrawRivalPanel()
        {
            GUILayout.Label("<b>═══ RIVAL ═══</b>", CreateRichTextStyle());

            var rival = _gameManager.RivalAI;
            GUILayout.Label($"Money: ${rival.Money:F0}");
            GUILayout.Label($"Next purchase in: {rival.TicksUntilPurchase} days");

            if (!string.IsNullOrEmpty(rival.TargetedLotId))
            {
                var targetLot = _gameManager.CityManager.GetLot(rival.TargetedLotId);
                GUILayout.Label($"<color=yellow>⚠ Targeting: {targetLot?.DisplayName}</color>", CreateRichTextStyle());
            }

            GUILayout.Space(10);
        }

        private void DrawActionLog()
        {
            GUILayout.Label("<b>═══ LOG ═══</b>", CreateRichTextStyle());
            GUILayout.Label(_lastAction);
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private void LogAction(string action)
        {
            _lastAction = $"[Day {_gameManager.TimeManager.CurrentTick}] {action}";
        }

        private GUIStyle CreateRichTextStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            return style;
        }
    }
}
