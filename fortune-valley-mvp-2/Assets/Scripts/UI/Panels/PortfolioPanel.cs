using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.UI.Components;
using FortuneValley.UI.Popups;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// Panel showing the player's investment portfolio.
    /// Displays total value, holdings, and gain/loss information.
    /// </summary>
    public class PortfolioPanel : UIPanel
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Summary")]
        [SerializeField] private TextMeshProUGUI _totalValueText;
        [SerializeField] private TextMeshProUGUI _totalGainText;
        [SerializeField] private TextMeshProUGUI _investingBalanceText;

        [Header("Holdings List")]
        [SerializeField] private Transform _holdingsContainer;
        [SerializeField] private InvestmentListItem _investmentItemPrefab;
        [SerializeField] private TextMeshProUGUI _emptyPortfolioText;

        [Header("Available Investments")]
        [SerializeField] private Transform _availableContainer;
        [SerializeField] private Button _buyNewInvestmentButton;

        [Header("Buttons")]
        [SerializeField] private Button _closeButton;

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Popups")]
        [SerializeField] private BuyInvestmentPopup _buyPopup;
        [SerializeField] private SellInvestmentPopup _sellPopup;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        [Header("Value Animation")]
        [SerializeField] private float _valueAnimationDuration = 0.5f;
        [SerializeField] private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private List<InvestmentListItem> _holdingItems = new List<InvestmentListItem>();
        private float _displayedTotalValue;
        private float _targetTotalValue;
        private float _valueAnimationTimer;
        private bool _isAnimatingValue;
        private float _previousTotalValue;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            FindDependencies();
            SetupButtons();
        }

        private void FindDependencies()
        {
            if (_investmentSystem == null)
            {
                _investmentSystem = FindFirstObjectByType<InvestmentSystem>();
            }

            if (_currencyManager == null)
            {
                _currencyManager = FindFirstObjectByType<CurrencyManager>();
            }

            if (_buyPopup == null)
            {
                _buyPopup = FindFirstObjectByType<BuyInvestmentPopup>();
            }

            if (_sellPopup == null)
            {
                _sellPopup = FindFirstObjectByType<SellInvestmentPopup>();
            }
        }

        private void SetupButtons()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (_buyNewInvestmentButton != null)
            {
                _buyNewInvestmentButton.onClick.AddListener(OnBuyNewClicked);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnInvestmentCreated += OnInvestmentChanged;
            GameEvents.OnInvestmentWithdrawn += OnInvestmentWithdrawn;
            GameEvents.OnInvestmentCompounded += OnInvestmentCompounded;
            GameEvents.OnInvestingBalanceChanged += OnBalanceChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnInvestmentCreated -= OnInvestmentChanged;
            GameEvents.OnInvestmentWithdrawn -= OnInvestmentWithdrawn;
            GameEvents.OnInvestmentCompounded -= OnInvestmentCompounded;
            GameEvents.OnInvestingBalanceChanged -= OnBalanceChanged;
        }

        private void OnInvestmentChanged(ActiveInvestment inv)
        {
            if (IsVisible) RefreshDisplay();
        }

        private void OnInvestmentWithdrawn(ActiveInvestment inv, float payout)
        {
            if (IsVisible) RefreshDisplay();
        }

        private void OnInvestmentCompounded(ActiveInvestment inv)
        {
            if (IsVisible)
            {
                RefreshDisplay();
                // Animate the value change to make compound interest visible
                AnimateValueChange();
            }
        }

        private void Update()
        {
            // Animate portfolio value counting up/down
            if (_isAnimatingValue)
            {
                _valueAnimationTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_valueAnimationTimer / _valueAnimationDuration);
                float curveProgress = _animationCurve.Evaluate(progress);

                _displayedTotalValue = Mathf.Lerp(_previousTotalValue, _targetTotalValue, curveProgress);
                UpdateTotalValueDisplay(_displayedTotalValue);

                if (progress >= 1f)
                {
                    _isAnimatingValue = false;
                }
            }
        }

        private void OnBalanceChanged(float balance, float delta)
        {
            if (IsVisible) UpdateBalanceDisplay();
        }

        // ═══════════════════════════════════════════════════════════════
        // PANEL OVERRIDES
        // ═══════════════════════════════════════════════════════════════

        protected override void OnShow()
        {
            RefreshDisplay();
        }

        // ═══════════════════════════════════════════════════════════════
        // DISPLAY METHODS
        // ═══════════════════════════════════════════════════════════════

        private void RefreshDisplay()
        {
            UpdateSummary();
            UpdateHoldingsList();
            UpdateBalanceDisplay();
        }

        private void UpdateSummary()
        {
            if (_investmentSystem == null) return;

            float totalValue = _investmentSystem.TotalPortfolioValue;
            float totalGain = _investmentSystem.TotalGain;
            float totalPrincipal = _investmentSystem.TotalPrincipal;
            float percentReturn = totalPrincipal > 0 ? (totalGain / totalPrincipal) * 100f : 0f;

            // Store target for animation
            _targetTotalValue = totalValue;

            // Total value (will be animated in Update)
            if (!_isAnimatingValue)
            {
                UpdateTotalValueDisplay(totalValue);
                _displayedTotalValue = totalValue;
            }

            // Total gain/loss
            if (_totalGainText != null)
            {
                string prefix = totalGain >= 0 ? "+" : "";
                _totalGainText.text = $"Total Return: {prefix}${totalGain:N2} ({prefix}{percentReturn:F1}%)";
                _totalGainText.color = totalGain >= 0 ? _gainColor : _lossColor;
            }
        }

        private void UpdateTotalValueDisplay(float value)
        {
            if (_totalValueText != null)
            {
                _totalValueText.text = $"Portfolio Value: ${value:N2}";

                // Color based on gain/loss from principal
                if (_investmentSystem != null)
                {
                    float totalPrincipal = _investmentSystem.TotalPrincipal;
                    if (value > totalPrincipal)
                    {
                        _totalValueText.color = _gainColor;
                    }
                    else if (value < totalPrincipal)
                    {
                        _totalValueText.color = _lossColor;
                    }
                    else
                    {
                        _totalValueText.color = Color.white;
                    }
                }
            }
        }

        private void AnimateValueChange()
        {
            // Start animating from current displayed value to new target
            _previousTotalValue = _displayedTotalValue;
            _valueAnimationTimer = 0f;
            _isAnimatingValue = true;
        }

        private void UpdateBalanceDisplay()
        {
            if (_investingBalanceText != null && _currencyManager != null)
            {
                _investingBalanceText.text = $"Investing Balance: ${_currencyManager.InvestingBalance:N2}";
            }
        }

        private void UpdateHoldingsList()
        {
            // Clear existing items
            ClearHoldingsList();

            if (_investmentSystem == null) return;

            var holdings = _investmentSystem.ActiveInvestments;

            // Show/hide empty message
            if (_emptyPortfolioText != null)
            {
                _emptyPortfolioText.gameObject.SetActive(holdings.Count == 0);
            }

            // Create item for each holding
            foreach (var investment in holdings)
            {
                CreateHoldingItem(investment);
            }
        }

        private void CreateHoldingItem(ActiveInvestment investment)
        {
            if (_investmentItemPrefab == null || _holdingsContainer == null) return;

            InvestmentListItem item = Instantiate(_investmentItemPrefab, _holdingsContainer);
            item.Setup(investment, OnSellClicked);
            _holdingItems.Add(item);
        }

        private void ClearHoldingsList()
        {
            foreach (var item in _holdingItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _holdingItems.Clear();
        }

        // ═══════════════════════════════════════════════════════════════
        // CALLBACKS
        // ═══════════════════════════════════════════════════════════════

        private void OnBuyNewClicked()
        {
            if (_buyPopup != null)
            {
                _buyPopup.Show();
            }
            else
            {
                UIManager.Instance.ShowPopup(PopupType.BuyInvestment);
            }
        }

        private void OnSellClicked(ActiveInvestment investment)
        {
            if (_sellPopup != null)
            {
                _sellPopup.ShowForInvestment(investment);
            }
            else
            {
                UIManager.Instance.ShowPopup(PopupType.SellInvestment);
            }
        }
    }
}
