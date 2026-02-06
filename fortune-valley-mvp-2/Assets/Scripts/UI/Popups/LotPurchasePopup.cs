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
        private Vector3 _targetWorldPosition;
        private bool _initialized = false;

        [Header("Positioning")]
        [Tooltip("Vertical offset above the lot in world units")]
        [SerializeField] private float _worldYOffset = 2f;

        [Tooltip("Screen edge padding in pixels")]
        [SerializeField] private float _screenPadding = 20f;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Lazy init: finds dependencies and wires buttons once.
        /// Safe to call before Start() (e.g. from ShowForLot on an inactive GameObject).
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

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
        /// <param name="worldPosition">World position of the lot to position popup near</param>
        public void ShowForLot(CityLotDefinition lot, int currentTick = 0, Vector3? worldPosition = null)
        {
            EnsureInitialized();

            _currentLot = lot;
            _currentTick = currentTick;

            if (worldPosition.HasValue)
            {
                _targetWorldPosition = worldPosition.Value;
            }

            UpdateDisplay();
            Show();

            // Position after Show() so the RectTransform is active and sized
            if (worldPosition.HasValue)
            {
                PositionNearLot(_targetWorldPosition);
            }
        }

        /// <summary>
        /// Position the popup in screen space near the lot's world position.
        /// Converts world → screen → canvas local coords, then clamps to screen bounds.
        /// </summary>
        private void PositionNearLot(Vector3 worldPos)
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) return;

            // Get the RectTransform of the popup panel
            RectTransform popupRect = (_popupRoot != null)
                ? _popupRoot.GetComponent<RectTransform>()
                : GetComponent<RectTransform>();
            if (popupRect == null) return;

            // Find parent canvas for coordinate conversion
            Canvas canvas = popupRect.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            // Convert lot world position (offset upward) to screen point
            Vector3 screenPoint = cam.WorldToScreenPoint(worldPos + Vector3.up * _worldYOffset);

            // If lot is behind the camera, don't reposition
            if (screenPoint.z < 0) return;

            // Convert screen point to canvas local position
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                new Vector2(screenPoint.x, screenPoint.y),
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
                out localPoint
            );

            // Clamp so the popup stays on screen
            Vector2 popupSize = popupRect.rect.size;
            Vector2 canvasSize = canvasRect.rect.size;
            float halfW = popupSize.x * 0.5f;
            float halfH = popupSize.y * 0.5f;
            float padded = _screenPadding;

            float minX = -canvasSize.x * 0.5f + halfW + padded;
            float maxX = canvasSize.x * 0.5f - halfW - padded;
            float minY = -canvasSize.y * 0.5f + halfH + padded;
            float maxY = canvasSize.y * 0.5f - halfH - padded;

            localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
            localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

            popupRect.anchoredPosition = localPoint;
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
