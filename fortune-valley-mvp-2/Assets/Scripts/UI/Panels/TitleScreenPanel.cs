using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// Title screen shown when the game loads or when returning from game over.
    /// Displays the game name, a short story blurb, and buttons to start.
    /// </summary>
    public class TitleScreenPanel : UIPanel
    {
        [Header("Title Screen")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _storyText;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _howToPlayButton;

        /// <summary>
        /// Fired when the player wants to start (either button leads to rules carousel).
        /// </summary>
        public event Action OnStartRequested;

        private const string StoryBlurb =
            "You've just opened a small restaurant in a growing city. " +
            "But you're not the only one with big ambitions \u2014 a rival investor " +
            "is eyeing the same properties.\n\n" +
            "Can you outsmart them?";

        private void Awake()
        {
            if (_startButton != null)
                _startButton.onClick.AddListener(HandleStartClicked);
            if (_howToPlayButton != null)
                _howToPlayButton.onClick.AddListener(HandleStartClicked);
        }

        protected override void OnShow()
        {
            // Refresh text each time we show (in case references were set up late)
            if (_titleText != null)
                _titleText.text = "Fortune Valley";
            if (_storyText != null)
                _storyText.text = StoryBlurb;
        }

        private void HandleStartClicked()
        {
            OnStartRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (_startButton != null)
                _startButton.onClick.RemoveListener(HandleStartClicked);
            if (_howToPlayButton != null)
                _howToPlayButton.onClick.RemoveListener(HandleStartClicked);
        }
    }
}
