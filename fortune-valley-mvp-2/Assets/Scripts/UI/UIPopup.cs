using UnityEngine;

namespace FortuneValley.UI
{
    /// <summary>
    /// Base class for all UI popups.
    /// Popups are modal dialogs that appear over other content.
    /// </summary>
    public abstract class UIPopup : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Popup Settings")]
        [Tooltip("The root GameObject to show/hide")]
        [SerializeField] protected GameObject _popupRoot;

        // ═══════════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════════

        public bool IsVisible { get; protected set; }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show this popup.
        /// </summary>
        public virtual void Show()
        {
            if (_popupRoot != null)
            {
                _popupRoot.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            IsVisible = true;
            OnShow();
        }

        /// <summary>
        /// Hide this popup.
        /// </summary>
        public virtual void Hide()
        {
            if (_popupRoot != null)
            {
                _popupRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            IsVisible = false;
            OnHide();
        }

        // ═══════════════════════════════════════════════════════════════
        // VIRTUAL METHODS (override in subclasses)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Called when the popup is shown. Override to setup data.
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// Called when the popup is hidden. Override to cleanup.
        /// </summary>
        protected virtual void OnHide() { }

        // ═══════════════════════════════════════════════════════════════
        // UI CALLBACKS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Called when cancel/close button is pressed.
        /// </summary>
        public virtual void OnCancelClicked()
        {
            UIManager.Instance.HidePopup(this);
        }

        /// <summary>
        /// Called when confirm/ok button is pressed.
        /// Override to handle confirmation logic.
        /// </summary>
        public virtual void OnConfirmClicked()
        {
            // Override in subclasses to handle specific confirm actions
            UIManager.Instance.HidePopup(this);
        }
    }
}
