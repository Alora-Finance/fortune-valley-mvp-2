using System.Collections.Generic;
using UnityEngine;

namespace FortuneValley.UI
{
    /// <summary>
    /// Manages all UI panels and popups in the game.
    /// Provides centralized control for showing/hiding UI elements.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════════════════════════

        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UIManager>();
                    if (_instance == null)
                    {
                        UnityEngine.Debug.LogError("[UIManager] No UIManager found in scene!");
                    }
                }
                return _instance;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Panel References")]
        [Tooltip("Portfolio panel showing investment holdings")]
        [SerializeField] private UIPanel _portfolioPanel;

        [Tooltip("Lots panel showing city lots")]
        [SerializeField] private UIPanel _lotsPanel;

        [Tooltip("Restaurant panel for upgrades")]
        [SerializeField] private UIPanel _restaurantPanel;

        [Header("Popup References")]
        [Tooltip("Lot purchase confirmation popup")]
        [SerializeField] private UIPopup _lotPurchasePopup;

        [Tooltip("Buy investment popup")]
        [SerializeField] private UIPopup _buyInvestmentPopup;

        [Tooltip("Sell investment popup")]
        [SerializeField] private UIPopup _sellInvestmentPopup;

        [Tooltip("Transfer between accounts popup")]
        [SerializeField] private UIPopup _transferPopup;

        [Header("Overlay")]
        [Tooltip("Dark overlay behind popups")]
        [SerializeField] private GameObject _popupOverlay;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private UIPanel _currentPanel;
        private Stack<UIPopup> _popupStack = new Stack<UIPopup>();

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Hide all UI at start
            HideAllPanels();
            HideAllPopups();
        }

        // ═══════════════════════════════════════════════════════════════
        // PANEL MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show a specific panel by type.
        /// </summary>
        public void ShowPanel(PanelType panelType)
        {
            // Hide current panel first
            if (_currentPanel != null)
            {
                _currentPanel.Hide();
            }

            UIPanel panel = GetPanel(panelType);
            if (panel != null)
            {
                panel.Show();
                _currentPanel = panel;
            }
        }

        /// <summary>
        /// Hide the currently open panel.
        /// </summary>
        public void HideCurrentPanel()
        {
            if (_currentPanel != null)
            {
                _currentPanel.Hide();
                _currentPanel = null;
            }
        }

        /// <summary>
        /// Toggle a panel (show if hidden, hide if shown).
        /// </summary>
        public void TogglePanel(PanelType panelType)
        {
            UIPanel panel = GetPanel(panelType);
            if (panel == null) return;

            if (_currentPanel == panel)
            {
                HideCurrentPanel();
            }
            else
            {
                ShowPanel(panelType);
            }
        }

        /// <summary>
        /// Hide all panels.
        /// </summary>
        public void HideAllPanels()
        {
            if (_portfolioPanel != null) _portfolioPanel.Hide();
            if (_lotsPanel != null) _lotsPanel.Hide();
            if (_restaurantPanel != null) _restaurantPanel.Hide();
            _currentPanel = null;
        }

        private UIPanel GetPanel(PanelType type)
        {
            return type switch
            {
                PanelType.Portfolio => _portfolioPanel,
                PanelType.Lots => _lotsPanel,
                PanelType.Restaurant => _restaurantPanel,
                _ => null
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // POPUP MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show a popup by type.
        /// </summary>
        public void ShowPopup(PopupType popupType)
        {
            UIPopup popup = GetPopup(popupType);
            if (popup != null)
            {
                ShowPopup(popup);
            }
        }

        /// <summary>
        /// Show a specific popup instance.
        /// </summary>
        public void ShowPopup(UIPopup popup)
        {
            if (popup == null) return;

            // Show overlay if this is the first popup
            if (_popupStack.Count == 0 && _popupOverlay != null)
            {
                _popupOverlay.SetActive(true);
            }

            _popupStack.Push(popup);
            popup.Show();
        }

        /// <summary>
        /// Hide the topmost popup.
        /// </summary>
        public void HideTopPopup()
        {
            if (_popupStack.Count > 0)
            {
                UIPopup popup = _popupStack.Pop();
                popup.Hide();

                // Hide overlay if no more popups
                if (_popupStack.Count == 0 && _popupOverlay != null)
                {
                    _popupOverlay.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Hide a specific popup.
        /// </summary>
        public void HidePopup(UIPopup popup)
        {
            if (popup == null) return;

            popup.Hide();

            // Rebuild stack without this popup
            var tempStack = new Stack<UIPopup>();
            while (_popupStack.Count > 0)
            {
                var p = _popupStack.Pop();
                if (p != popup)
                {
                    tempStack.Push(p);
                }
            }

            while (tempStack.Count > 0)
            {
                _popupStack.Push(tempStack.Pop());
            }

            // Hide overlay if no more popups
            if (_popupStack.Count == 0 && _popupOverlay != null)
            {
                _popupOverlay.SetActive(false);
            }
        }

        /// <summary>
        /// Hide all popups.
        /// </summary>
        public void HideAllPopups()
        {
            while (_popupStack.Count > 0)
            {
                _popupStack.Pop().Hide();
            }

            // Also hide any popups that might not be in stack
            if (_lotPurchasePopup != null) _lotPurchasePopup.Hide();
            if (_buyInvestmentPopup != null) _buyInvestmentPopup.Hide();
            if (_sellInvestmentPopup != null) _sellInvestmentPopup.Hide();
            if (_transferPopup != null) _transferPopup.Hide();

            if (_popupOverlay != null)
            {
                _popupOverlay.SetActive(false);
            }
        }

        private UIPopup GetPopup(PopupType type)
        {
            return type switch
            {
                PopupType.LotPurchase => _lotPurchasePopup,
                PopupType.BuyInvestment => _buyInvestmentPopup,
                PopupType.SellInvestment => _sellInvestmentPopup,
                PopupType.Transfer => _transferPopup,
                _ => null
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // CONVENIENCE ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Check if any popup is currently open.
        /// </summary>
        public bool IsPopupOpen => _popupStack.Count > 0;

        /// <summary>
        /// Check if any panel is currently open.
        /// </summary>
        public bool IsPanelOpen => _currentPanel != null;

        /// <summary>
        /// Get the lot purchase popup for configuration.
        /// </summary>
        public UIPopup LotPurchasePopup => _lotPurchasePopup;

        /// <summary>
        /// Get the buy investment popup for configuration.
        /// </summary>
        public UIPopup BuyInvestmentPopup => _buyInvestmentPopup;

        /// <summary>
        /// Get the sell investment popup for configuration.
        /// </summary>
        public UIPopup SellInvestmentPopup => _sellInvestmentPopup;

        /// <summary>
        /// Get the transfer popup for configuration.
        /// </summary>
        public UIPopup TransferPopup => _transferPopup;
    }

    /// <summary>
    /// Types of panels in the game.
    /// </summary>
    public enum PanelType
    {
        Portfolio,
        Lots,
        Restaurant
    }

    /// <summary>
    /// Types of popups in the game.
    /// </summary>
    public enum PopupType
    {
        LotPurchase,
        BuyInvestment,
        SellInvestment,
        Transfer
    }
}
