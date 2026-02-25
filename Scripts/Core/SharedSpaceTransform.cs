using Unity.Netcode;
using UnityEngine;

namespace EasySharedSpace
{
    /// <summary>
    /// A transform that automatically synchronizes its position relative to the shared origin.
    /// Useful for objects that should maintain their position in shared space.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class SharedSpaceTransform : NetworkBehaviour
    {
        [Header("Synchronization")]
        [Tooltip("Sync position")]
        public bool syncPosition = true;

        [Tooltip("Sync rotation")]
        public bool syncRotation = true;

        [Tooltip("Sync scale")]
        public bool syncScale = false;

        [Tooltip("How often to sync (per second)")]
        public float syncRate = 10f;

        [Tooltip("Position threshold for sync")]
        public float positionThreshold = 0.01f;

        [Tooltip("Rotation threshold for sync (degrees)")]
        public float rotationThreshold = 1f;

        [Tooltip("Interpolate remote transforms")]
        public bool interpolate = true;

        [Tooltip("Interpolation speed")]
        public float lerpSpeed = 10f;

        // Network variables
        private NetworkVariable<Vector3> _netPosition = new NetworkVariable<Vector3>(Vector3.zero);
        private NetworkVariable<Quaternion> _netRotation = new NetworkVariable<Quaternion>(Quaternion.identity);
        private NetworkVariable<Vector3> _netScale = new NetworkVariable<Vector3>(Vector3.one);

        // Local state
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private Vector3 _lastScale;
        private float _syncTimer;
        private float _syncInterval;

        // Interpolation targets
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetScale;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            _syncInterval = 1f / syncRate;
            
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
            _targetScale = transform.localScale;
            
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
            _lastScale = transform.localScale;

            _netPosition.OnValueChanged += OnPositionChanged;
            _netRotation.OnValueChanged += OnRotationChanged;
            _netScale.OnValueChanged += OnScaleChanged;
        }

        public override void OnNetworkDespawn()
        {
            _netPosition.OnValueChanged -= OnPositionChanged;
            _netRotation.OnValueChanged -= OnRotationChanged;
            _netScale.OnValueChanged -= OnScaleChanged;
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (IsOwner)
            {
                UpdateOwner();
            }
            else
            {
                UpdateRemote();
            }
        }

        private void UpdateOwner()
        {
            _syncTimer += Time.deltaTime;

            bool shouldSync = _syncTimer >= _syncInterval;
            bool changed = false;

            if (syncPosition)
            {
                if (Vector3.Distance(transform.position, _lastPosition) > positionThreshold)
                {
                    _netPosition.Value = transform.position;
                    _lastPosition = transform.position;
                    changed = true;
                }
            }

            if (syncRotation)
            {
                if (Quaternion.Angle(transform.rotation, _lastRotation) > rotationThreshold)
                {
                    _netRotation.Value = transform.rotation;
                    _lastRotation = transform.rotation;
                    changed = true;
                }
            }

            if (syncScale)
            {
                if (transform.localScale != _lastScale)
                {
                    _netScale.Value = transform.localScale;
                    _lastScale = transform.localScale;
                    changed = true;
                }
            }

            if (shouldSync && changed)
            {
                _syncTimer = 0f;
            }
        }

        private void UpdateRemote()
        {
            if (interpolate)
            {
                if (syncPosition)
                    transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * lerpSpeed);
                if (syncRotation)
                    transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * lerpSpeed);
                if (syncScale)
                    transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * lerpSpeed);
            }
            else
            {
                if (syncPosition)
                    transform.position = _targetPosition;
                if (syncRotation)
                    transform.rotation = _targetRotation;
                if (syncScale)
                    transform.localScale = _targetScale;
            }
        }

        private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
        {
            if (!IsOwner)
            {
                _targetPosition = newValue;
            }
        }

        private void OnRotationChanged(Quaternion oldValue, Quaternion newValue)
        {
            if (!IsOwner)
            {
                _targetRotation = newValue;
            }
        }

        private void OnScaleChanged(Vector3 oldValue, Vector3 newValue)
        {
            if (!IsOwner)
            {
                _targetScale = newValue;
            }
        }

        /// <summary>
        /// Teleport to position (owner only)
        /// </summary>
        public void Teleport(Vector3 position, Quaternion? rotation = null, Vector3? scale = null)
        {
            if (!IsOwner) return;

            transform.position = position;
            _netPosition.Value = position;
            _targetPosition = position;
            _lastPosition = position;

            if (rotation.HasValue)
            {
                transform.rotation = rotation.Value;
                _netRotation.Value = rotation.Value;
                _targetRotation = rotation.Value;
                _lastRotation = rotation.Value;
            }

            if (scale.HasValue)
            {
                transform.localScale = scale.Value;
                _netScale.Value = scale.Value;
                _targetScale = scale.Value;
                _lastScale = scale.Value;
            }
        }
    }
}
