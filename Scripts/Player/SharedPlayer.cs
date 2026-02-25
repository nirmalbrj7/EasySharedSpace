using Unity.Netcode;
using UnityEngine;

namespace EasySharedSpace
{
    /// <summary>
    /// Represents a player in the shared space.
    /// Automatically synchronizes player position and rotation across the network.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class SharedPlayer : NetworkBehaviour
    {
        [Header("Player Info")]
        [Tooltip("Display name for this player")]
        public NetworkVariable<string> PlayerName = new NetworkVariable<string>("Player");

        [Tooltip("Color to identify this player")]
        public NetworkVariable<Color> PlayerColor = new NetworkVariable<Color>(Color.white);

        [Header("Synchronization")]
        [Tooltip("How often to sync position (per second)")]
        public float syncRate = 30f;

        [Tooltip("Threshold for position change before syncing")]
        public float positionThreshold = 0.01f;

        [Tooltip("Threshold for rotation change before syncing")]
        public float rotationThreshold = 1f;

        [Tooltip("Smooth movement for remote players")]
        public bool interpolatePosition = true;

        [Tooltip("How fast to interpolate to target position")]
        public float lerpSpeed = 15f;

        // Network synced transforms
        private NetworkVariable<Vector3> _networkPosition = new NetworkVariable<Vector3>(
            Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        private NetworkVariable<Quaternion> _networkRotation = new NetworkVariable<Quaternion>(
            Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Local state
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private float _syncInterval;
        private float _syncTimer;

        // Interpolation targets
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;

        public bool IsLocalPlayer => IsOwner;
        public ulong PlayerId => OwnerClientId;

        private void Start()
        {
            _syncInterval = 1f / syncRate;
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;

            // Subscribe to network variable changes
            _networkPosition.OnValueChanged += OnPositionChanged;
            _networkRotation.OnValueChanged += OnRotationChanged;

            // Set random color for this player
            if (IsOwner)
            {
                PlayerColor.Value = Random.ColorHSV();
                PlayerName.Value = $"Player {NetworkObjectId}";
            }
        }

        public override void OnDestroy()
        {
            _networkPosition.OnValueChanged -= OnPositionChanged;
            _networkRotation.OnValueChanged -= OnRotationChanged;
            base.OnDestroy();
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

        /// <summary>
        /// Owner updates network variables when position changes
        /// </summary>
        private void UpdateOwner()
        {
            _syncTimer += Time.deltaTime;

            bool positionChanged = Vector3.Distance(transform.position, _lastPosition) > positionThreshold;
            bool rotationChanged = Quaternion.Angle(transform.rotation, _lastRotation) > rotationThreshold;

            if (_syncTimer >= _syncInterval && (positionChanged || rotationChanged))
            {
                _networkPosition.Value = transform.position;
                _networkRotation.Value = transform.rotation;
                
                _lastPosition = transform.position;
                _lastRotation = transform.rotation;
                _syncTimer = 0f;
            }
        }

        /// <summary>
        /// Remote players interpolate to network position
        /// </summary>
        private void UpdateRemote()
        {
            if (interpolatePosition)
            {
                transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * lerpSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * lerpSpeed);
            }
            else
            {
                transform.position = _targetPosition;
                transform.rotation = _targetRotation;
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

        /// <summary>
        /// Teleport player to a specific position (owner only)
        /// </summary>
        public void Teleport(Vector3 position, Quaternion? rotation = null)
        {
            if (!IsOwner) return;

            transform.position = position;
            if (rotation.HasValue)
            {
                transform.rotation = rotation.Value;
            }

            _networkPosition.Value = position;
            _networkRotation.Value = rotation ?? transform.rotation;
            
            _targetPosition = position;
            _targetRotation = rotation ?? transform.rotation;
            _lastPosition = position;
            _lastRotation = rotation ?? transform.rotation;
        }

        /// <summary>
        /// Set player name
        /// </summary>
        public void SetName(string name)
        {
            if (IsOwner)
            {
                PlayerName.Value = name;
            }
        }

        /// <summary>
        /// Set player color
        /// </summary>
        public void SetColor(Color color)
        {
            if (IsOwner)
            {
                PlayerColor.Value = color;
            }
        }
    }
}
