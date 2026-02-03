using UnityEngine;
using FortuneValley.Core;
using FortuneValley.City;

namespace FortuneValley.UI
{
    /// <summary>
    /// Component attached to lot GameObjects in the 3D world.
    /// Links the visual representation to the lot definition data.
    /// Handles visual feedback for hover, selection, and ownership states.
    /// </summary>
    public class LotVisual : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Lot Reference")]
        [Tooltip("The lot definition this visual represents")]
        [SerializeField] private CityLotDefinition _lotDefinition;

        [Header("Visual Elements")]
        [Tooltip("Renderer to modify for visual feedback")]
        [SerializeField] private Renderer _mainRenderer;

        [Tooltip("Outline effect object (enable/disable for selection)")]
        [SerializeField] private GameObject _outlineEffect;

        [Tooltip("Ownership indicator (changes color based on owner)")]
        [SerializeField] private Renderer _ownerIndicator;

        [Header("Colors")]
        [SerializeField] private Color _availableColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        [SerializeField] private Color _playerOwnedColor = new Color(0.2f, 0.4f, 0.9f, 0.8f);
        [SerializeField] private Color _rivalOwnedColor = new Color(0.9f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color _hoverColor = new Color(1f, 0.9f, 0.3f, 0.6f);
        [SerializeField] private Color _selectedColor = new Color(1f, 1f, 0f, 0.8f);

        [Header("Materials")]
        [Tooltip("Material to use when hovered")]
        [SerializeField] private Material _hoverMaterial;

        [Header("Ownership Particles")]
        [Tooltip("Particle system for ownership visual feedback")]
        [SerializeField] private OwnershipParticles _ownershipParticles;

        [Tooltip("Original material (cached on Start)")]
        private Material _originalMaterial;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private bool _isHovered;
        private bool _isSelected;
        private Owner _currentOwner = Owner.None;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public CityLotDefinition LotDefinition => _lotDefinition;
        public bool IsHovered => _isHovered;
        public bool IsSelected => _isSelected;
        public Owner CurrentOwner => _currentOwner;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Cache original material
            if (_mainRenderer != null)
            {
                _originalMaterial = _mainRenderer.material;
            }

            // Hide outline initially
            if (_outlineEffect != null)
            {
                _outlineEffect.SetActive(false);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnLotPurchased += HandleLotPurchased;
        }

        private void OnDisable()
        {
            GameEvents.OnLotPurchased -= HandleLotPurchased;
        }

        private void Start()
        {
            UpdateVisuals();
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (_lotDefinition != null && _lotDefinition.LotId == lotId)
            {
                _currentOwner = owner;
                UpdateVisuals();

                // Update ownership particles
                _ownershipParticles?.SetOwner(owner);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Set the lot definition for this visual.
        /// </summary>
        public void SetLotDefinition(CityLotDefinition definition)
        {
            _lotDefinition = definition;
            UpdateVisuals();
        }

        /// <summary>
        /// Set hover state.
        /// </summary>
        public void SetHovered(bool hovered)
        {
            if (_isHovered == hovered) return;

            _isHovered = hovered;
            UpdateVisuals();
        }

        /// <summary>
        /// Set selected state.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;

            _isSelected = selected;
            UpdateVisuals();
        }

        /// <summary>
        /// Update ownership state from CityManager.
        /// </summary>
        public void RefreshOwnership(CityManager cityManager)
        {
            if (_lotDefinition == null || cityManager == null) return;

            _currentOwner = cityManager.GetOwner(_lotDefinition.LotId);
            UpdateVisuals();
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateVisuals()
        {
            UpdateOutline();
            UpdateOwnerIndicator();
            UpdateMaterial();
        }

        private void UpdateOutline()
        {
            if (_outlineEffect != null)
            {
                _outlineEffect.SetActive(_isSelected || _isHovered);
            }
        }

        private void UpdateOwnerIndicator()
        {
            if (_ownerIndicator == null) return;

            Color color;
            switch (_currentOwner)
            {
                case Owner.Player:
                    color = _playerOwnedColor;
                    break;
                case Owner.Rival:
                    color = _rivalOwnedColor;
                    break;
                default:
                    color = _availableColor;
                    break;
            }

            // Override with hover/selected colors
            if (_isSelected)
            {
                color = _selectedColor;
            }
            else if (_isHovered)
            {
                color = _hoverColor;
            }

            _ownerIndicator.material.color = color;
        }

        private void UpdateMaterial()
        {
            if (_mainRenderer == null) return;

            if (_isHovered && _hoverMaterial != null)
            {
                _mainRenderer.material = _hoverMaterial;
            }
            else if (_originalMaterial != null)
            {
                _mainRenderer.material = _originalMaterial;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EDITOR
        // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_lotDefinition == null) return;

            // Draw label with lot info
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"{_lotDefinition.DisplayName}\n${_lotDefinition.BaseCost:N0}"
            );
        }
#endif
    }
}
