using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Main game coordinator. Bootstraps the game and handles high-level state.
    ///
    /// DESIGN NOTE: This is intentionally thin. It coordinates, not controls.
    /// Each system manages itself; GameManager just starts/stops things.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // SYSTEM REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Core Systems")]
        [SerializeField] private TimeManager _timeManager;
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Gameplay Systems")]
        [SerializeField] private RestaurantSystem _restaurantSystem;
        [SerializeField] private InvestmentSystem _investmentSystem;
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private RivalAI _rivalAI;

        [Header("Auto Start")]
        [Tooltip("Automatically start the game on scene load")]
        [SerializeField] private bool _autoStart = true;

        [Header("Debug")]
        [SerializeField] private bool _logStateChanges = true;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private GameState _currentState = GameState.NotStarted;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public GameState CurrentState => _currentState;
        public bool IsPlaying => _currentState == GameState.Playing;

        // System accessors for UI
        public TimeManager TimeManager => _timeManager;
        public CurrencyManager CurrencyManager => _currencyManager;
        public RestaurantSystem RestaurantSystem => _restaurantSystem;
        public InvestmentSystem InvestmentSystem => _investmentSystem;
        public CityManager CityManager => _cityManager;
        public RivalAI RivalAI => _rivalAI;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            if (_autoStart)
            {
                StartGame();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameEnd += HandleGameEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnGameEnd -= HandleGameEnd;
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions to prevent memory leaks
            GameEvents.ClearAllSubscriptions();
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Start a new game.
        /// </summary>
        public void StartGame()
        {
            if (_currentState == GameState.Playing)
            {
                Debug.LogWarning("[GameManager] Game already in progress");
                return;
            }

            SetState(GameState.Playing);
            GameEvents.RaiseGameStart();

            if (_logStateChanges)
            {
                Debug.Log("[GameManager] Game started!");
            }
        }

        /// <summary>
        /// Restart the game (reset everything and start fresh).
        /// </summary>
        public void RestartGame()
        {
            if (_logStateChanges)
            {
                Debug.Log("[GameManager] Restarting game...");
            }

            // Systems will reset themselves when they receive OnGameStart
            SetState(GameState.Playing);
            GameEvents.RaiseGameStart();
        }

        /// <summary>
        /// Pause the game.
        /// </summary>
        public void PauseGame()
        {
            if (_currentState != GameState.Playing)
                return;

            SetState(GameState.Paused);
            _timeManager.StopTime();

            if (_logStateChanges)
            {
                Debug.Log("[GameManager] Game paused");
            }
        }

        /// <summary>
        /// Resume a paused game.
        /// </summary>
        public void ResumeGame()
        {
            if (_currentState != GameState.Paused)
                return;

            SetState(GameState.Playing);
            _timeManager.StartTime();

            if (_logStateChanges)
            {
                Debug.Log("[GameManager] Game resumed");
            }
        }

        /// <summary>
        /// Toggle pause state.
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
                PauseGame();
            else if (_currentState == GameState.Paused)
                ResumeGame();
        }

        /// <summary>
        /// Get a complete game summary for debugging or end-game display.
        /// </summary>
        public string GetGameSummary()
        {
            return $"=== Fortune Valley Summary ===\n\n" +
                   $"Day: {_timeManager.CurrentTick}\n\n" +
                   $"FINANCES:\n" +
                   $"Balance: ${_currencyManager.Balance:F0}\n" +
                   $"{_restaurantSystem.GetPerformanceSummary()}\n\n" +
                   $"INVESTMENTS:\n" +
                   $"{_investmentSystem.GetPortfolioSummary()}\n\n" +
                   $"CITY:\n" +
                   $"{_cityManager.GetCitySummary()}\n\n" +
                   $"RIVAL:\n" +
                   $"{_rivalAI.GetRivalStatus()}";
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void HandleGameEnd(Owner winner)
        {
            SetState(winner == Owner.Player ? GameState.Won : GameState.Lost);

            if (_logStateChanges)
            {
                string resultText = winner == Owner.Player
                    ? "Congratulations! You won!"
                    : "Game over. The rival won.";

                Debug.Log($"[GameManager] {resultText}");
                Debug.Log(GetGameSummary());
            }
        }

        private void SetState(GameState newState)
        {
            _currentState = newState;
        }

        private void ValidateReferences()
        {
            bool valid = true;

            if (_timeManager == null) { Debug.LogError("[GameManager] Missing TimeManager reference"); valid = false; }
            if (_currencyManager == null) { Debug.LogError("[GameManager] Missing CurrencyManager reference"); valid = false; }
            if (_restaurantSystem == null) { Debug.LogError("[GameManager] Missing RestaurantSystem reference"); valid = false; }
            if (_investmentSystem == null) { Debug.LogError("[GameManager] Missing InvestmentSystem reference"); valid = false; }
            if (_cityManager == null) { Debug.LogError("[GameManager] Missing CityManager reference"); valid = false; }
            if (_rivalAI == null) { Debug.LogError("[GameManager] Missing RivalAI reference"); valid = false; }

            if (!valid)
            {
                Debug.LogError("[GameManager] Missing references! Wire these in the Unity Editor.");
            }
        }
    }

    /// <summary>
    /// High-level game states.
    /// </summary>
    public enum GameState
    {
        NotStarted,
        Playing,
        Paused,
        Won,
        Lost
    }
}
