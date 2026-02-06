using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// Portfolio panel with scene-designed layout.
    /// Left side: investment buttons grouped by category (Stocks, Bonds+Bills, ETFs).
    /// Right side: selected asset details + buy/sell controls.
    /// All UI elements are found by path — no manual inspector wiring needed
    /// beyond the component being on the PortfolioPanel GameObject.
    /// </summary>
    public class PortfolioPanel : UIPanel
    {
        // ═══════════════════════════════════════════════════════════════
        // INSPECTOR DEPENDENCIES
        // ═══════════════════════════════════════════════════════════════

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME REFERENCES (found by path)
        // ═══════════════════════════════════════════════════════════════

        // Summary stats
        private TextMeshProUGUI _balanceText;
        private TextMeshProUGUI _totalGainText;
        private TextMeshProUGUI _portfolioLevelText;

        // Buy/Sell panel texts
        private TextMeshProUGUI _selectedAssetText;
        private TextMeshProUGUI _priceText;
        private TextMeshProUGUI _priceChangeText;
        private TextMeshProUGUI _riskLevelText;
        private TextMeshProUGUI _sharesOwnedText;

        // Buttons
        private Button _closeButton;
        private Button _buyButton;
        private Button _sellButton;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private InvestmentDefinition _selectedDef;
        private bool _isBuying = true; // true = buy mode, false = sell mode
        private Dictionary<Button, InvestmentDefinition> _investmentButtons
            = new Dictionary<Button, InvestmentDefinition>();
        private Color _normalButtonColor;
        private Color _selectedButtonColor = new Color(0.35f, 0.55f, 0.75f);
        private Button _highlightedButton;
        private Color _buyButtonNormalColor;
        private Color _sellButtonNormalColor;

        private int _ticksSinceRefresh;
        private const int REFRESH_INTERVAL = 5;
        private bool _initialized;

        // Track previous prices for daily change display
        private Dictionary<InvestmentDefinition, float> _previousPrices
            = new Dictionary<InvestmentDefinition, float>();

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;

            FindDependencies();
            FindUIElements();
            WireInvestmentButtons();
            WireActionButtons();
            ClearSelectedAssetDisplay();

            _initialized = true;
        }

        private void FindDependencies()
        {
            if (_investmentSystem == null)
                _investmentSystem = FindFirstObjectByType<InvestmentSystem>();
            if (_currencyManager == null)
                _currencyManager = FindFirstObjectByType<CurrencyManager>();
        }

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnCheckingBalanceChanged += HandleBalanceChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnCheckingBalanceChanged -= HandleBalanceChanged;
        }

        /// <summary>
        /// Update balance text immediately when currency changes,
        /// so the panel stays in sync with the HUD.
        /// </summary>
        private void HandleBalanceChanged(float newBalance, float delta)
        {
            if (!IsVisible || _balanceText == null) return;
            _balanceText.text = $"Balance: ${newBalance:N0}";
        }

        private void HandleTick(int tickNumber)
        {
            if (!IsVisible) return;

            // Update selected asset (price, daily change) every tick
            UpdateSelectedAssetDisplay();

            // Snapshot prices AFTER display so next tick shows true 1-day change
            SnapshotPrices();

            _ticksSinceRefresh++;
            if (_ticksSinceRefresh >= REFRESH_INTERVAL)
            {
                _ticksSinceRefresh = 0;
                RefreshLiveData();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PANEL OVERRIDES
        // ═══════════════════════════════════════════════════════════════

        protected override void OnShow()
        {
            Initialize();
            _ticksSinceRefresh = 0;

            // Snapshot current prices for change tracking
            SnapshotPrices();

            RefreshLiveData();
        }

        // ═══════════════════════════════════════════════════════════════
        // UI ELEMENT DISCOVERY (by path)
        // ═══════════════════════════════════════════════════════════════

        private void FindUIElements()
        {
            // Summary stats
            _balanceText = FindText("SummaryStatistics/BalanceText");
            _totalGainText = FindText("SummaryStatistics/TotalGainText");
            _portfolioLevelText = FindText("SummaryStatistics/PortfolioLevelText");

            // Close button — try multiple paths for scene naming flexibility
            _closeButton = FindButton("TopFrame/Button_Close")
                        ?? FindButton("TopFrame/CloseButton")
                        ?? FindButton("Header/CloseButton")
                        ?? FindButton("CloseButton");

            // Buy/Sell panel
            var buySellPath = "InvestmentInfoPanel/BuySellPanel";
            _selectedAssetText = FindText($"{buySellPath}/SelectedAssetText");
            _priceText = FindText($"{buySellPath}/SelectedAssetInfo/PriceText");
            _priceChangeText = FindText($"{buySellPath}/SelectedAssetInfo/PriceChangeText");
            _riskLevelText = FindText($"{buySellPath}/SelectedAssetInfo/RiskLevelText");
            _sharesOwnedText = FindText($"{buySellPath}/SelectedAssetInfo/SharesOwnedText")
                            ?? FindText($"{buySellPath}/SharesOwnedText");

            // Buy/Sell mode buttons
            _buyButton = FindButton($"{buySellPath}/Buy or Sell/ButtonGrid/BuyButton");
            _sellButton = FindButton($"{buySellPath}/Buy or Sell/ButtonGrid/SellButton");

            // Store normal colors for highlighting
            if (_buyButton != null)
                _buyButtonNormalColor = _buyButton.GetComponent<Image>()?.color ?? Color.white;
            if (_sellButton != null)
                _sellButtonNormalColor = _sellButton.GetComponent<Image>()?.color ?? Color.white;
        }

        /// <summary>
        /// Discovers all buttons in the 3 category grids and maps them
        /// to InvestmentDefinitions. Updates button labels to match definition names.
        /// Hides extra buttons if a grid has more buttons than definitions.
        /// </summary>
        private void WireInvestmentButtons()
        {
            if (_investmentSystem == null) return;

            var optionsRoot = transform.Find("InvestmentInfoPanel/InvestmentOptions");
            if (optionsRoot == null) return;

            // Map each grid path to the investment categories it shows
            var gridMappings = new (string path, InvestmentCategory[] categories)[]
            {
                ("Stocks/ButtonGrid", new[] { InvestmentCategory.Stock }),
                ("Bonds+Bills/ButtonGrid", new[] { InvestmentCategory.Bond, InvestmentCategory.TBill }),
                ("ETFs + Mutual Funds/ButtonGrid", new[] { InvestmentCategory.ETF }),
            };

            foreach (var (path, categories) in gridMappings)
            {
                Transform grid = optionsRoot.Find(path);
                if (grid == null) continue;

                // Get definitions for these categories, sorted by name
                var defs = _investmentSystem.AvailableInvestments
                    .Where(d => categories.Contains(d.Category))
                    .OrderBy(d => d.DisplayName)
                    .ToList();

                for (int i = 0; i < grid.childCount; i++)
                {
                    Transform child = grid.GetChild(i);
                    Button btn = child.GetComponent<Button>();
                    if (btn == null) continue;

                    if (i < defs.Count)
                    {
                        // Assign definition to this button
                        var def = defs[i];
                        _investmentButtons[btn] = def;

                        // Update button label to show the definition's name
                        var label = child.GetComponentInChildren<TextMeshProUGUI>();
                        if (label != null)
                            label.text = def.DisplayName;

                        // Store normal color from first button
                        if (i == 0 && _normalButtonColor == default)
                        {
                            var img = child.GetComponent<Image>();
                            if (img != null)
                                _normalButtonColor = img.color;
                        }

                        // Wire click handler
                        var capturedDef = def;
                        var capturedBtn = btn;
                        btn.onClick.AddListener(() => OnInvestmentSelected(capturedDef, capturedBtn));
                    }
                    else
                    {
                        // No definition for this button — hide it
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void WireActionButtons()
        {
            // Close
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);

                // Ensure the button can receive clicks
                if (_closeButton.targetGraphic == null)
                {
                    var img = _closeButton.GetComponent<Image>();
                    if (img != null)
                        _closeButton.targetGraphic = img;
                    else
                        UnityEngine.Debug.LogWarning("[PortfolioPanel] Close button has no Image — clicks may not register");
                }
            }
            else
            {
                UnityEngine.Debug.LogError("[PortfolioPanel] Close button not found — cannot wire close action");
            }

            // Buy/Sell mode toggle
            if (_buyButton != null)
                _buyButton.onClick.AddListener(() => SetTradeMode(true));
            if (_sellButton != null)
                _sellButton.onClick.AddListener(() => SetTradeMode(false));

            // Quantity buttons — found by path
            var qtyGridPath = "InvestmentInfoPanel/BuySellPanel/Quantity/ButtonGrid";
            Transform qtyGrid = transform.Find(qtyGridPath);
            if (qtyGrid == null) return;

            WireQuantityButton(qtyGrid, "x1Button", 1);
            WireQuantityButton(qtyGrid, "x5Button", 5);
            WireQuantityButton(qtyGrid, "x50Button", 50);

            // Max button uses special handler
            Transform maxGO = qtyGrid.Find("MaxButton");
            if (maxGO != null)
            {
                Button maxBtn = maxGO.GetComponent<Button>();
                if (maxBtn != null)
                    maxBtn.onClick.AddListener(ExecuteTradeMax);
            }
        }

        private void WireQuantityButton(Transform grid, string name, int quantity)
        {
            Transform go = grid.Find(name);
            if (go == null) return;

            Button btn = go.GetComponent<Button>();
            if (btn == null) return;

            int capturedQty = quantity;
            btn.onClick.AddListener(() => ExecuteTrade(capturedQty));
        }

        // ═══════════════════════════════════════════════════════════════
        // INVESTMENT SELECTION
        // ═══════════════════════════════════════════════════════════════

        private void OnInvestmentSelected(InvestmentDefinition def, Button btn)
        {
            // Unhighlight previous button
            if (_highlightedButton != null)
            {
                var prevImg = _highlightedButton.GetComponent<Image>();
                if (prevImg != null)
                    prevImg.color = _normalButtonColor;
            }

            _selectedDef = def;
            _highlightedButton = btn;
            _isBuying = true; // Reset to buy mode on new selection

            // Highlight selected button
            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = _selectedButtonColor;

            UpdateSelectedAssetDisplay();
            UpdateTradeModeHighlight();
        }

        // ═══════════════════════════════════════════════════════════════
        // TRADE MODE (Buy / Sell)
        // ═══════════════════════════════════════════════════════════════

        private void SetTradeMode(bool buying)
        {
            _isBuying = buying;
            UpdateTradeModeHighlight();
        }

        private void UpdateTradeModeHighlight()
        {
            // Highlight the active mode button
            if (_buyButton != null)
            {
                var buyImg = _buyButton.GetComponent<Image>();
                if (buyImg != null)
                    buyImg.color = _isBuying ? _selectedButtonColor : _buyButtonNormalColor;
            }
            if (_sellButton != null)
            {
                var sellImg = _sellButton.GetComponent<Image>();
                if (sellImg != null)
                    sellImg.color = !_isBuying ? _selectedButtonColor : _sellButtonNormalColor;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // TRADE EXECUTION
        // ═══════════════════════════════════════════════════════════════

        private void ExecuteTrade(int quantity)
        {
            if (_selectedDef == null || _investmentSystem == null) return;

            if (_isBuying)
            {
                _investmentSystem.BuyShares(_selectedDef, quantity);
            }
            else
            {
                var active = GetActiveInvestment(_selectedDef);
                if (active != null)
                    _investmentSystem.SellShares(active, quantity);
            }

            UpdateSelectedAssetDisplay();
            UpdateSummary();
        }

        private void ExecuteTradeMax()
        {
            if (_selectedDef == null || _investmentSystem == null) return;

            if (_isBuying)
            {
                if (_currencyManager == null) return;
                float price = _selectedDef.CurrentPrice;
                if (price <= 0) return;

                int maxShares = Mathf.FloorToInt(_currencyManager.Balance / price);
                if (maxShares > 0)
                    _investmentSystem.BuyShares(_selectedDef, maxShares);
            }
            else
            {
                var active = GetActiveInvestment(_selectedDef);
                if (active != null)
                    _investmentSystem.SellAllShares(active);
            }

            UpdateSelectedAssetDisplay();
            UpdateSummary();
        }

        // ═══════════════════════════════════════════════════════════════
        // DISPLAY UPDATES
        // ═══════════════════════════════════════════════════════════════

        private void RefreshLiveData()
        {
            UpdateSummary();
            UpdateSelectedAssetDisplay();
        }

        private void UpdateSummary()
        {
            if (_investmentSystem == null || _currencyManager == null) return;

            float balance = _currencyManager.Balance;
            float portfolioValue = _investmentSystem.TotalPortfolioValue;
            float totalGain = _investmentSystem.TotalGain;

            if (_balanceText != null)
                _balanceText.text = $"Balance: ${balance:N0}";

            if (_totalGainText != null)
            {
                string prefix = totalGain >= 0 ? "+" : "";
                _totalGainText.text = $"Total Earnings: {prefix}${totalGain:N0}";
                _totalGainText.color = totalGain >= 0 ? _gainColor : _lossColor;
            }

            // Portfolio risk level: weighted average of held investments
            if (_portfolioLevelText != null)
                _portfolioLevelText.text = $"Portfolio Risk Level:\n{GetPortfolioRiskLabel()}";
        }

        private void UpdateSelectedAssetDisplay()
        {
            if (_selectedDef == null)
            {
                ClearSelectedAssetDisplay();
                return;
            }

            // Asset name
            if (_selectedAssetText != null)
                _selectedAssetText.text = $"Selected Asset: {_selectedDef.DisplayName}";

            // Current price
            if (_priceText != null)
                _priceText.text = $"Price: ${_selectedDef.CurrentPrice:F2}";

            // Daily change (since last refresh)
            if (_priceChangeText != null)
            {
                float change = GetPriceChangePercent(_selectedDef);
                string prefix = change >= 0 ? "+" : "";
                _priceChangeText.text = $"Daily Change: {prefix}{change:F2}%";
                _priceChangeText.color = change >= 0 ? _gainColor : _lossColor;
            }

            // Risk level
            if (_riskLevelText != null)
            {
                string riskStr = _selectedDef.RiskLevel.ToString();
                _riskLevelText.text = $"Risk Level: {riskStr}";
                _riskLevelText.color = _selectedDef.RiskLevel switch
                {
                    RiskLevel.Low => _gainColor,
                    RiskLevel.Medium => new Color(1f, 0.8f, 0.2f),
                    RiskLevel.High => _lossColor,
                    _ => Color.white
                };
            }

            // Shares owned
            if (_sharesOwnedText != null)
            {
                var active = GetActiveInvestment(_selectedDef);
                int shares = active != null ? active.NumberOfShares : 0;
                _sharesOwnedText.text = $"Shares Owned: {shares}";
            }
        }

        private void ClearSelectedAssetDisplay()
        {
            if (_selectedAssetText != null)
                _selectedAssetText.text = "Selected Asset: ---";
            if (_priceText != null)
                _priceText.text = "Price: $---";
            if (_priceChangeText != null)
            {
                _priceChangeText.text = "Daily Change: ---%";
                _priceChangeText.color = Color.white;
            }
            if (_riskLevelText != null)
            {
                _riskLevelText.text = "Risk Level: ---";
                _riskLevelText.color = Color.white;
            }
            if (_sharesOwnedText != null)
                _sharesOwnedText.text = "Shares Owned: ---";
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private ActiveInvestment GetActiveInvestment(InvestmentDefinition def)
        {
            if (_investmentSystem == null) return null;
            return _investmentSystem.ActiveInvestments
                .FirstOrDefault(a => a.Definition == def);
        }

        /// <summary>
        /// Calculate a weighted-average risk label for the portfolio.
        /// </summary>
        private string GetPortfolioRiskLabel()
        {
            if (_investmentSystem == null) return "None";

            var holdings = _investmentSystem.ActiveInvestments;
            if (holdings.Count == 0) return "None";

            float totalValue = 0f;
            float weightedRisk = 0f;

            foreach (var inv in holdings)
            {
                float val = inv.CurrentValue;
                float risk = inv.Definition.RiskLevel switch
                {
                    RiskLevel.Low => 0f,
                    RiskLevel.Medium => 1f,
                    RiskLevel.High => 2f,
                    _ => 1f
                };
                totalValue += val;
                weightedRisk += val * risk;
            }

            if (totalValue <= 0) return "None";

            float avg = weightedRisk / totalValue;

            if (avg < 0.5f) return "Low";
            if (avg < 1.5f) return "Medium";
            return "High";
        }

        /// <summary>
        /// Store current prices so next tick can show 1-day change.
        /// </summary>
        private void SnapshotPrices()
        {
            if (_investmentSystem == null) return;

            foreach (var def in _investmentSystem.AvailableInvestments)
            {
                _previousPrices[def] = def.CurrentPrice;
            }
        }

        /// <summary>
        /// Get percentage price change since previous tick (1-day change).
        /// </summary>
        private float GetPriceChangePercent(InvestmentDefinition def)
        {
            if (_previousPrices.TryGetValue(def, out float prev) && prev > 0)
            {
                return (def.CurrentPrice - prev) / prev * 100f;
            }
            // Fallback: change from base price
            if (def.BasePricePerShare > 0)
                return (def.CurrentPrice - def.BasePricePerShare) / def.BasePricePerShare * 100f;
            return 0f;
        }

        // ═══════════════════════════════════════════════════════════════
        // PATH-BASED FINDERS
        // ═══════════════════════════════════════════════════════════════

        private TextMeshProUGUI FindText(string path)
        {
            Transform t = transform.Find(path);
            if (t == null)
            {
                UnityEngine.Debug.LogWarning($"[PortfolioPanel] Text not found at path: {path}");
                return null;
            }
            return t.GetComponent<TextMeshProUGUI>();
        }

        private Button FindButton(string path)
        {
            Transform t = transform.Find(path);
            if (t == null)
            {
                UnityEngine.Debug.LogWarning($"[PortfolioPanel] Button not found at path: {path}");
                return null;
            }
            var btn = t.GetComponent<Button>();
            if (btn == null)
                UnityEngine.Debug.LogWarning($"[PortfolioPanel] Found '{path}' but it has no Button component");
            return btn;
        }
    }
}
