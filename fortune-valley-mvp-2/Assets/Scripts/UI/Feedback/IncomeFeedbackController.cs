using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.UI.HUD;

namespace FortuneValley.UI.Feedback
{
    /// <summary>
    /// Orchestrates the income visual feedback system.
    /// When income is generated: FloatingText -> CoinFly -> AccountPulse
    ///
    /// LEARNING DESIGN: This creates a satisfying feedback loop that makes
    /// earning money feel rewarding. Students should feel good about income,
    /// which sets up the contrast with the decision to invest (delayed gratification).
    /// </summary>
    public class IncomeFeedbackController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Pool Sizes")]
        [SerializeField] private int _floatingTextPoolSize = 10;
        [SerializeField] private int _coinPoolSize = 5;

        [Header("Prefabs")]
        [Tooltip("Prefab for floating text (must have FloatingText component)")]
        [SerializeField] private FloatingText _floatingTextPrefab;

        [Tooltip("Prefab for flying coin (must have CoinFlyAnimation component)")]
        [SerializeField] private CoinFlyAnimation _coinPrefab;

        [Header("References")]
        [Tooltip("The canvas to spawn feedback UI on")]
        [SerializeField] private Canvas _feedbackCanvas;

        [Tooltip("The world-space canvas for 3D floating text")]
        [SerializeField] private Canvas _worldCanvas;

        [Tooltip("The checking account display to pulse")]
        [SerializeField] private AccountDisplay _checkingDisplay;

        [Tooltip("Camera for world-to-screen conversion")]
        [SerializeField] private UnityEngine.Camera _mainCamera;

        [Header("Settings")]
        [Tooltip("Minimum income amount to show feedback for")]
        [SerializeField] private float _minimumAmountToShow = 1f;

        [Tooltip("Delay between coin launch and account pulse")]
        [SerializeField] private float _coinToPulseDelay = 0.6f;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private List<FloatingText> _floatingTextPool = new List<FloatingText>();
        private List<CoinFlyAnimation> _coinPool = new List<CoinFlyAnimation>();

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            FindReferences();
            InitializePools();
        }

        private void OnEnable()
        {
            GameEvents.OnIncomeGeneratedWithPosition += HandleIncomeWithPosition;
        }

        private void OnDisable()
        {
            GameEvents.OnIncomeGeneratedWithPosition -= HandleIncomeWithPosition;
        }

        // ═══════════════════════════════════════════════════════════════
        // INITIALIZATION
        // ═══════════════════════════════════════════════════════════════

        private void FindReferences()
        {
            if (_mainCamera == null)
            {
                _mainCamera = UnityEngine.Camera.main;
            }

            if (_checkingDisplay == null)
            {
                // Find checking account display in scene
                var displays = FindObjectsByType<AccountDisplay>(FindObjectsSortMode.None);
                foreach (var display in displays)
                {
                    if (display.AccountType == AccountType.Checking)
                    {
                        _checkingDisplay = display;
                        break;
                    }
                }
            }

            if (_feedbackCanvas == null)
            {
                // Try to find a suitable canvas
                var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (var canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        _feedbackCanvas = canvas;
                        break;
                    }
                }
            }
        }

        private void InitializePools()
        {
            // Create floating text pool
            if (_floatingTextPrefab != null && _worldCanvas != null)
            {
                for (int i = 0; i < _floatingTextPoolSize; i++)
                {
                    var text = Instantiate(_floatingTextPrefab, _worldCanvas.transform);
                    text.gameObject.SetActive(false);
                    _floatingTextPool.Add(text);
                }
            }

            // Create coin pool
            if (_coinPrefab != null && _feedbackCanvas != null)
            {
                for (int i = 0; i < _coinPoolSize; i++)
                {
                    var coin = Instantiate(_coinPrefab, _feedbackCanvas.transform);
                    coin.gameObject.SetActive(false);
                    _coinPool.Add(coin);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleIncomeWithPosition(float amount, Vector3 worldPosition)
        {
            if (amount < _minimumAmountToShow) return;

            // Step 1: Show floating text at world position
            ShowFloatingText(amount, worldPosition);

            // Step 2: Launch coin animation (with small delay)
            Invoke(nameof(LaunchCoinToAccount), 0.2f);
        }

        // ═══════════════════════════════════════════════════════════════
        // FEEDBACK METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show floating "+$X" text at world position.
        /// </summary>
        private void ShowFloatingText(float amount, Vector3 worldPosition)
        {
            var text = GetFloatingTextFromPool();
            if (text == null) return;

            string message = $"+${amount:N0}";
            text.Show(message, worldPosition, true);
        }

        /// <summary>
        /// Launch a coin to fly to the checking account display.
        /// </summary>
        private void LaunchCoinToAccount()
        {
            if (_mainCamera == null || _checkingDisplay == null) return;

            var coin = GetCoinFromPool();
            if (coin == null) return;

            // Get restaurant position (assuming it's the player's restaurant)
            Vector3 restaurantWorldPos = Vector3.zero;
            var restaurant = FindFirstObjectByType<RestaurantSystem>();
            if (restaurant != null)
            {
                restaurantWorldPos = restaurant.transform.position;
            }

            // Convert to screen position
            Vector3 startScreen = _mainCamera.WorldToScreenPoint(restaurantWorldPos);
            Vector2 startPos = new Vector2(startScreen.x, startScreen.y);

            // Get checking display screen position
            Vector2 endPos = GetAccountDisplayScreenPosition();

            // Launch coin with callback to pulse account
            coin.Fly(startPos, endPos, OnCoinArrived);
        }

        private void OnCoinArrived()
        {
            // Step 3: Pulse the checking account
            if (_checkingDisplay != null)
            {
                _checkingDisplay.PulseOnIncome();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // POOL METHODS
        // ═══════════════════════════════════════════════════════════════

        private FloatingText GetFloatingTextFromPool()
        {
            foreach (var text in _floatingTextPool)
            {
                if (!text.IsAnimating)
                {
                    return text;
                }
            }

            // Pool exhausted - expand if we have a prefab
            if (_floatingTextPrefab != null && _worldCanvas != null)
            {
                var text = Instantiate(_floatingTextPrefab, _worldCanvas.transform);
                _floatingTextPool.Add(text);
                return text;
            }

            return null;
        }

        private CoinFlyAnimation GetCoinFromPool()
        {
            foreach (var coin in _coinPool)
            {
                if (!coin.IsAnimating)
                {
                    return coin;
                }
            }

            // Pool exhausted - expand if we have a prefab
            if (_coinPrefab != null && _feedbackCanvas != null)
            {
                var coin = Instantiate(_coinPrefab, _feedbackCanvas.transform);
                _coinPool.Add(coin);
                return coin;
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════

        private Vector2 GetAccountDisplayScreenPosition()
        {
            if (_checkingDisplay == null) return new Vector2(Screen.width * 0.5f, Screen.height * 0.9f);

            var rectTransform = _checkingDisplay.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                // Return center of the rect
                Vector3 center = (corners[0] + corners[2]) / 2f;
                return new Vector2(center.x, center.y);
            }

            return new Vector2(Screen.width * 0.5f, Screen.height * 0.9f);
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS (for testing/manual triggering)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Manually trigger income feedback (for testing).
        /// </summary>
        public void TriggerIncomeFeedback(float amount, Vector3 worldPosition)
        {
            HandleIncomeWithPosition(amount, worldPosition);
        }
    }
}
