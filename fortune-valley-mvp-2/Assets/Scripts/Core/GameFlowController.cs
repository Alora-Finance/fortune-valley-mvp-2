using UnityEngine;
using FortuneValley.UI.Panels;

namespace FortuneValley.Core
{
    /// <summary>
    /// Coordinates the full game flow: Title -> Rules -> Play -> Game Over -> Title.
    /// Thin orchestrator — each panel manages its own content.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private TitleScreenPanel _titleScreen;
        [SerializeField] private RulesCarouselPanel _rulesCarousel;
        [SerializeField] private GameEndPanel _gameEndPanel;

        [Header("Countdown")]
        [SerializeField] private CountdownOverlay _countdownOverlay;

        [Header("HUD")]
        [SerializeField] private GameObject _topFrame;
        [SerializeField] private GameObject _bottomFrame;

        [Header("Game")]
        [SerializeField] private GameManager _gameManager;

        private void OnEnable()
        {
            if (_titleScreen != null)
                _titleScreen.OnStartRequested += HandleStartRequested;
            if (_rulesCarousel != null)
                _rulesCarousel.OnCarouselComplete += HandleCarouselComplete;

            GameEvents.OnGameEndWithSummary += HandleGameEnd;
        }

        private void OnDisable()
        {
            if (_titleScreen != null)
                _titleScreen.OnStartRequested -= HandleStartRequested;
            if (_rulesCarousel != null)
                _rulesCarousel.OnCarouselComplete -= HandleCarouselComplete;

            GameEvents.OnGameEndWithSummary -= HandleGameEnd;
        }

        private void Start()
        {
            ShowTitleScreen();
        }

        /// <summary>
        /// Show the title screen and hide everything else.
        /// </summary>
        public void ShowTitleScreen()
        {
            // Hide HUD
            SetHUDVisible(false);

            // Hide gameplay panels
            if (_rulesCarousel != null)
                _rulesCarousel.Hide();
            if (_gameEndPanel != null)
                _gameEndPanel.gameObject.SetActive(false);

            // Return game state to NotStarted (no OnGameStart fired)
            if (_gameManager != null)
                _gameManager.ReturnToTitle();

            // Show title
            if (_titleScreen != null)
                _titleScreen.Show();
        }

        private void HandleStartRequested()
        {
            // Title -> Rules
            if (_titleScreen != null)
                _titleScreen.Hide();
            if (_rulesCarousel != null)
                _rulesCarousel.Show();
        }

        private void HandleCarouselComplete()
        {
            // Rules -> Gameplay (countdown plays first)
            if (_rulesCarousel != null)
                _rulesCarousel.Hide();

            SetHUDVisible(true);
            StartCountdownThen(() => _gameManager?.StartGame());
        }

        private void HandleGameEnd(bool isPlayerWin, GameSummary summary)
        {
            // HUD stays visible during game over so player can see final stats.
            // Activate and show the GameEndPanel directly — it starts inactive
            // (with a dark overlay Image on its root GO), so we only activate it
            // when there's actually a game-end event to display.
            if (_gameEndPanel != null)
            {
                _gameEndPanel.gameObject.SetActive(true);
                _gameEndPanel.ShowWithSummary(isPlayerWin, summary);
            }
        }

        /// <summary>
        /// Restart the game, skipping the title screen and rules carousel.
        /// A 3-2-1-GO! countdown plays before the game resumes.
        /// Called by the "Play Again" button on the game end screen.
        /// </summary>
        public void RestartGame()
        {
            // Deactivate the game end panel; HUD stays visible
            if (_gameEndPanel != null)
                _gameEndPanel.gameObject.SetActive(false);

            StartCountdownThen(() => _gameManager?.RestartGame());
        }

        /// <summary>
        /// Runs the countdown overlay then invokes onComplete.
        /// Falls back to immediate invocation if the overlay is not wired up.
        /// </summary>
        private void StartCountdownThen(System.Action onComplete)
        {
            if (_countdownOverlay != null)
                _countdownOverlay.StartCountdown(onComplete);
            else
                onComplete?.Invoke();
        }

        private void SetHUDVisible(bool visible)
        {
            if (_topFrame != null)
                _topFrame.SetActive(visible);
            if (_bottomFrame != null)
                _bottomFrame.SetActive(visible);
        }
    }
}
