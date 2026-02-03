using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.City
{
    /// <summary>
    /// Controls particle effects that indicate lot ownership.
    /// Green particles for player-owned lots, red for rival-owned.
    /// Particles rise from the lot edges to provide visual feedback.
    /// </summary>
    public class OwnershipParticles : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Particle Colors")]
        [Tooltip("Color for player-owned lots")]
        [SerializeField] private Color _playerColor = new Color(0.2f, 1f, 0.3f, 1f);

        [Tooltip("Color for rival-owned lots")]
        [SerializeField] private Color _rivalColor = new Color(1f, 0.2f, 0.2f, 1f);

        [Header("References")]
        [Tooltip("The particle system to control")]
        [SerializeField] private ParticleSystem _particleSystem;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private Owner _currentOwner = Owner.None;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public Owner CurrentOwner => _currentOwner;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Set the owner to display particles for.
        /// Owner.None stops particles.
        /// Owner.Player shows green particles.
        /// Owner.Rival shows red particles.
        /// </summary>
        public void SetOwner(Owner owner)
        {
            if (owner == _currentOwner) return;
            _currentOwner = owner;

            if (_particleSystem == null) return;

            var main = _particleSystem.main;

            if (owner == Owner.None)
            {
                _particleSystem.Stop();
            }
            else
            {
                // Set color based on owner
                main.startColor = owner == Owner.Player ? _playerColor : _rivalColor;

                if (!_particleSystem.isPlaying)
                {
                    _particleSystem.Play();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Ensure particles are stopped initially
            if (_particleSystem != null)
            {
                _particleSystem.Stop();
            }
        }
    }
}
