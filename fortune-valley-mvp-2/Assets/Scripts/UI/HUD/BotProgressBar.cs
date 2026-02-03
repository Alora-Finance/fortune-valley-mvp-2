using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays the rival bot's progress toward victory.
    /// Shows lots owned and warnings when targeting player lots.
    /// </summary>
    public class BotProgressBar : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // UI REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Progress Bar")]
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private Image _progressFillImage;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI _botLotsText;
        [SerializeField] private TextMeshProUGUI _warningText;

        [Header("Warning Indicator")]
        [SerializeField] private GameObject _warningIndicator;
        [SerializeField] private Image _warningIcon;

        [Header("Colors")]
        [SerializeField] private Color _lowThreatColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _mediumThreatColor = new Color(0.9f, 0.7f, 0.1f);
        [SerializeField] private Color _highThreatColor = new Color(0.9f, 0.2f, 0.2f);

        [Header("Animation")]
        [SerializeField] private float _warningFlashSpeed = 2f;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private int _totalLots;
        private int _botLots;
        private string _targetedLotName;
        private bool _isShowingWarning;
        private float _warningFlashTimer;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnRivalTargetingLot += HandleRivalTargeting;
        }

        private void OnDisable()
        {
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnRivalTargetingLot -= HandleRivalTargeting;
        }

        private void Update()
        {
            // Flash warning indicator
            if (_isShowingWarning && _warningIndicator != null)
            {
                _warningFlashTimer += Time.deltaTime * _warningFlashSpeed;
                float alpha = (Mathf.Sin(_warningFlashTimer * Mathf.PI) + 1f) / 2f;

                if (_warningIcon != null)
                {
                    var color = _warningIcon.color;
                    color.a = 0.5f + alpha * 0.5f;
                    _warningIcon.color = color;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (owner == Owner.Rival)
            {
                _botLots++;
                UpdateDisplay();

                // Clear warning if bot just bought the targeted lot
                if (_targetedLotName != null)
                {
                    HideWarning();
                }
            }
            else if (owner == Owner.Player)
            {
                // If player bought the lot bot was targeting, hide warning
                HideWarning();
            }
        }

        private void HandleRivalTargeting(string lotId)
        {
            ShowWarning(lotId);
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Initialize the progress bar with total lots count.
        /// </summary>
        public void Initialize(int totalLots, int botLotsOwned)
        {
            _totalLots = totalLots;
            _botLots = botLotsOwned;
            UpdateDisplay();
        }

        /// <summary>
        /// Update bot lots count directly.
        /// </summary>
        public void SetBotLots(int count)
        {
            _botLots = count;
            UpdateDisplay();
        }

        /// <summary>
        /// Show warning that bot is targeting a lot.
        /// </summary>
        public void ShowWarning(string lotName)
        {
            _targetedLotName = lotName;
            _isShowingWarning = true;
            _warningFlashTimer = 0f;

            if (_warningIndicator != null)
            {
                _warningIndicator.SetActive(true);
            }

            if (_warningText != null)
            {
                _warningText.gameObject.SetActive(true);
                _warningText.text = $"Bot eyeing: {lotName}";
            }
        }

        /// <summary>
        /// Hide the targeting warning.
        /// </summary>
        public void HideWarning()
        {
            _targetedLotName = null;
            _isShowingWarning = false;

            if (_warningIndicator != null)
            {
                _warningIndicator.SetActive(false);
            }

            if (_warningText != null)
            {
                _warningText.gameObject.SetActive(false);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateDisplay()
        {
            // Update progress slider
            if (_progressSlider != null && _totalLots > 0)
            {
                _progressSlider.value = (float)_botLots / _totalLots;
            }

            // Update text
            if (_botLotsText != null)
            {
                _botLotsText.text = $"Bot: {_botLots}/{_totalLots}";
            }

            // Update color based on threat level
            UpdateThreatColor();
        }

        private void UpdateThreatColor()
        {
            if (_progressFillImage == null || _totalLots == 0) return;

            float progress = (float)_botLots / _totalLots;

            if (progress < 0.33f)
            {
                _progressFillImage.color = _lowThreatColor;
            }
            else if (progress < 0.66f)
            {
                _progressFillImage.color = _mediumThreatColor;
            }
            else
            {
                _progressFillImage.color = _highThreatColor;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public int BotLots => _botLots;
        public int TotalLots => _totalLots;
        public float Progress => _totalLots > 0 ? (float)_botLots / _totalLots : 0f;
        public bool IsWarningActive => _isShowingWarning;
    }
}
