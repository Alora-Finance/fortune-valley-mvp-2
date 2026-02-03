using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Popup for purchasing a city lot.
    /// Shows lot details, cost, income bonus, and ROI information.
    /// </summary>
    public class LotPurchasePopup : UIPopup
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Lot Info")]
        [SerializeField] private TextMeshProUGUI _lotNameText;
        [SerializeField] private TextMeshProUGUI _lotDescriptionText;

        [Header("Economics")]
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _incomeBonusText;
        [SerializeField] private TextMeshProUGUI _roiText;
        [SerializeField] private TextMeshProUGUI _balanceText;
        [SerializeField] private TextMeshProUGUI _affordabilityText;

        [Header("Buttons")]
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _buyButtonText;

        [Header("Dependencies")]
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private CityManager _cityManager;

        [Header("Colors")]
        [SerializeField] private Color _canAffordColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _cannotAffordColor = new Color(0.8f, 0.2f, 0.2f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private CityLotDefinition _currentLot;
        private int _currentTick;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            // Find dependencies if not assigned
            if (_currencyManager == null)
            {
                _currencyManager = FindFirstObjectByType<CurrencyManager>();
            }

            if (_cityManager == null)
            {
                _cityManager = FindFirstObjectByType<CityManager>();
            }

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (_buyButton != null)
            {
                _buyButton.onClick.AddListener(OnBuyClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void OnEnable()
        {
            // Update when balance changes while popup is open
            GameEvents.OnCheckingBalanceChanged += OnBalanceChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCheckingBalanceChanged -= OnBalanceChanged;
        }

        private void OnBalanceChanged(float balance, float delta)
        {
            if (_currentLot != null)
            {
                UpdateAffordability();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Configure and show the popup for a specific lot.
        /// </summary>
        /// <param name="lot">The lot to display</param>
        /// <param name="currentTick">Current game tick (for purchase tracking)</param>
        public void ShowForLot(CityLotDefinition lot, int currentTick = 0)
        {
            _currentLot = lot;
            _currentTick = currentTick;

            UpdateDisplay();
            Show();
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateDisplay()
        {
            if (_currentLot == null) return;

            // Lot info
            if (_lotNameText != null)
            {
                _lotNameText.text = _currentLot.DisplayName;
            }

            if (_lotDescriptionText != null)
            {
                _lotDescriptionText.text = _currentLot.Description;
            }

            // Cost
            if (_costText != null)
            {
                _costText.text = $"Cost: ${_currentLot.BaseCost:N0}";
            }

            // Income bonus
            if (_incomeBonusText != null)
            {
                if (_currentLot.IncomeBonus > 0)
                {
                    _incomeBonusText.text = $"Income: +${_currentLot.IncomeBonus:N0}/day";
                }
                else
                {
                    _incomeBonusText.text = "No income bonus";
                }
            }

            // ROI calculation
            if (_roiText != null)
            {
                if (_currentLot.IncomeBonus > 0)
                {
                    int daysToPayback = Mathf.CeilToInt(_currentLot.BaseCost / _currentLot.IncomeBonus);
                    _roiText.text = $"Payback: ~{daysToPayback} days";
                }
                else
                {
                    _roiText.text = "";
                }
            }

            UpdateAffordability();
        }

        private void UpdateAffordability()
        {
            if (_currentLot == null || _currencyManager == null) return;

            float checkingBalance = _currencyManager.CheckingBalance;
            bool canAfford = checkingBalance >= _currentLot.BaseCost;

            // Balance display
            if (_balanceText != null)
            {
                _balanceText.text = $"Your Checking: ${checkingBalance:N0}";
            }

            // Affordability message
            if (_affordabilityText != null)
            {
                if (canAfford)
                {
                    _affordabilityText.text = "You can afford this!";
                    _affordabilityText.color = _canAffordColor;
                }
                else
                {
                    float needed = _currentLot.BaseCost - checkingBalance;
                    _affordabilityText.text = $"Need ${needed:N0} more";
                    _affordabilityText.color = _cannotAffordColor;
                }
            }

            // Buy button state
            if (_buyButton != null)
            {
                _buyButton.interactable = canAfford;
            }

            if (_buyButtonText != null)
            {
                _buyButtonText.text = canAfford ? "Buy" : "Can't Afford";
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // BUTTON CALLBACKS
        // ═══════════════════════════════════════════════════════════════

        private void OnBuyClicked()
        {
            if (_currentLot == null || _cityManager == null) return;

            // Attempt purchase
            if (_cityManager.TryPurchaseLot(_currentLot.LotId, _currentTick))
            {
                UnityEngine.Debug.Log($"[LotPurchasePopup] Successfully purchased {_currentLot.DisplayName}");

                // Close popup on success
                UIManager.Instance.HidePopup(this);
            }
            else
            {
                // Purchase failed - update display to show current state
                UnityEngine.Debug.Log($"[LotPurchasePopup] Failed to purchase {_currentLot.DisplayName}");
                UpdateAffordability();
            }
        }

        protected override void OnHide()
        {
            base.OnHide();
            _currentLot = null;
        }
    }
}
