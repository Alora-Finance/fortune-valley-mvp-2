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

        [Header("Enhanced Effects")]
        [Tooltip("Intensity multiplier for owned lots")]
        [SerializeField] private float _normalIntensity = 1f;
        [Tooltip("Intensity multiplier on purchase (brief burst)")]
        [SerializeField] private float _purchaseBurstIntensity = 3f;
        [Tooltip("Duration of the purchase burst effect")]
        [SerializeField] private float _burstDuration = 1f;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private Owner _currentOwner = Owner.None;
        private float _burstTimer;
        private bool _isBursting;

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
            bool isNewOwner = (owner != _currentOwner) && (owner != Owner.None);
            _currentOwner = owner;

            if (_particleSystem == null) return;

            var main = _particleSystem.main;
            var emission = _particleSystem.emission;

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

                // If this is a new purchase, trigger burst effect
                if (isNewOwner)
                {
                    TriggerPurchaseBurst();
                }
            }
        }

        /// <summary>
        /// Trigger an intensified burst effect (on purchase).
        /// </summary>
        public void TriggerPurchaseBurst()
        {
            _isBursting = true;
            _burstTimer = 0f;

            if (_particleSystem != null)
            {
                var emission = _particleSystem.emission;
                emission.rateOverTimeMultiplier = _purchaseBurstIntensity;

                // Emit a burst of particles
                _particleSystem.Emit(20);
            }
        }

        private void Update()
        {
            if (!_isBursting) return;

            _burstTimer += Time.deltaTime;
            float progress = _burstTimer / _burstDuration;

            if (_particleSystem != null)
            {
                var emission = _particleSystem.emission;

                if (progress >= 1f)
                {
                    // Burst complete, return to normal
                    emission.rateOverTimeMultiplier = _normalIntensity;
                    _isBursting = false;
                }
                else
                {
                    // Gradually reduce intensity
                    float intensity = Mathf.Lerp(_purchaseBurstIntensity, _normalIntensity, progress);
                    emission.rateOverTimeMultiplier = intensity;
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
