using UnityEngine;
using UnityEngine.UI;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Main game HUD controller.
    /// Manages the top bar (account displays, day counter, bot progress)
    /// and bottom bar (navigation buttons).
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES - TOP BAR
        // ═══════════════════════════════════════════════════════════════

        [Header("Account Displays")]
        [SerializeField] private AccountDisplay _checkingDisplay;
        [SerializeField] private AccountDisplay _investingDisplay;

        [Header("Day & Speed")]
        [SerializeField] private DaySpeedDisplay _daySpeedDisplay;

        [Header("Bot Progress")]
        [SerializeField] private BotProgressBar _botProgressBar;

        // ═══════════════════════════════════════════════════════════════
        // REFERENCES - BOTTOM BAR
        // ═══════════════════════════════════════════════════════════════

        [Header("Navigation Buttons")]
        [SerializeField] private Button _portfolioButton;
        [SerializeField] private Button _lotsButton;
        [SerializeField] private Button _transferButton;
        [SerializeField] private Button _restaurantButton;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            // Subscribe to balance events
            GameEvents.OnCheckingBalanceChanged += HandleCheckingChanged;
            GameEvents.OnInvestingBalanceChanged += HandleInvestingChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCheckingBalanceChanged -= HandleCheckingChanged;
            GameEvents.OnInvestingBalanceChanged -= HandleInvestingChanged;
        }

        private void Start()
        {
            SetupButtons();
        }

        private void SetupButtons()
        {
            if (_portfolioButton != null)
            {
                _portfolioButton.onClick.AddListener(OnPortfolioClicked);
            }

            if (_lotsButton != null)
            {
                _lotsButton.onClick.AddListener(OnLotsClicked);
            }

            if (_transferButton != null)
            {
                _transferButton.onClick.AddListener(OnTransferClicked);
            }

            if (_restaurantButton != null)
            {
                _restaurantButton.onClick.AddListener(OnRestaurantClicked);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleCheckingChanged(float balance, float delta)
        {
            if (_checkingDisplay != null)
            {
                _checkingDisplay.UpdateBalance(balance, delta);
            }
        }

        private void HandleInvestingChanged(float balance, float delta)
        {
            if (_investingDisplay != null)
            {
                _investingDisplay.UpdateBalance(balance, delta);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // BUTTON CALLBACKS
        // ═══════════════════════════════════════════════════════════════

        private void OnPortfolioClicked()
        {
            UIManager.Instance.TogglePanel(PanelType.Portfolio);
        }

        private void OnLotsClicked()
        {
            UIManager.Instance.TogglePanel(PanelType.Lots);
        }

        private void OnTransferClicked()
        {
            UIManager.Instance.ShowPopup(PopupType.Transfer);
        }

        private void OnRestaurantClicked()
        {
            UIManager.Instance.TogglePanel(PanelType.Restaurant);
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Initialize the HUD with current game state.
        /// Call after CurrencyManager is initialized.
        /// </summary>
        public void Initialize(float checkingBalance, float investingBalance, int currentDay)
        {
            if (_checkingDisplay != null)
            {
                _checkingDisplay.UpdateBalance(checkingBalance, 0);
            }

            if (_investingDisplay != null)
            {
                _investingDisplay.UpdateBalance(investingBalance, 0);
            }

            if (_daySpeedDisplay != null)
            {
                _daySpeedDisplay.UpdateDay(currentDay);
            }
        }
    }
}
