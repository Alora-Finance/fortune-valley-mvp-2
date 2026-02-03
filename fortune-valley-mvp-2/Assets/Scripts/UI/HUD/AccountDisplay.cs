using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays a single account balance (Checking or Investing).
    /// Shows the balance and visual feedback when it changes.
    /// </summary>
    public class AccountDisplay : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Account Type")]
        [SerializeField] private AccountType _accountType;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private TextMeshProUGUI _balanceText;
        [SerializeField] private TextMeshProUGUI _deltaText;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color _normalColor = Color.white;

        [Header("Animation")]
        [SerializeField] private float _deltaDisplayDuration = 1.5f;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private float _currentBalance;
        private float _deltaTimer;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            // Set label based on account type
            if (_labelText != null)
            {
                _labelText.text = _accountType == AccountType.Checking ? "Checking" : "Investing";
            }

            // Hide delta initially
            if (_deltaText != null)
            {
                _deltaText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Handle delta text fading
            if (_deltaTimer > 0)
            {
                _deltaTimer -= Time.deltaTime;
                if (_deltaTimer <= 0 && _deltaText != null)
                {
                    _deltaText.gameObject.SetActive(false);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Update the displayed balance.
        /// </summary>
        /// <param name="newBalance">The new balance to display</param>
        /// <param name="delta">The change amount (for visual feedback)</param>
        public void UpdateBalance(float newBalance, float delta)
        {
            _currentBalance = newBalance;

            // Update balance text
            if (_balanceText != null)
            {
                _balanceText.text = FormatCurrency(newBalance);
            }

            // Show delta feedback
            if (delta != 0 && _deltaText != null)
            {
                ShowDelta(delta);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void ShowDelta(float delta)
        {
            _deltaText.gameObject.SetActive(true);

            if (delta > 0)
            {
                _deltaText.text = $"+{FormatCurrency(delta)}";
                _deltaText.color = _gainColor;
            }
            else
            {
                _deltaText.text = FormatCurrency(delta);
                _deltaText.color = _lossColor;
            }

            _deltaTimer = _deltaDisplayDuration;
        }

        private string FormatCurrency(float amount)
        {
            if (Mathf.Abs(amount) >= 1000)
            {
                return $"${amount:N0}";
            }
            return $"${amount:F2}";
        }

        // ═══════════════════════════════════════════════════════════════
        // ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public AccountType AccountType => _accountType;
        public float CurrentBalance => _currentBalance;
    }
}
