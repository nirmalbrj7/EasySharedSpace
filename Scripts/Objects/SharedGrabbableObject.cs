using Unity.Netcode;
using UnityEngine;

namespace EasySharedSpace
{
    /// <summary>
    /// An object that can be grabbed and moved by players.
     /// The position and state are synchronized across all clients.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    public class SharedGrabbableObject : NetworkBehaviour
    {
        [Header("Grab Settings")]
        [Tooltip("Can this object be grabbed")]
        public bool isGrabbable = true;

        [Tooltip("Should the object use physics when not grabbed")]
        public bool usePhysics = true;

        [Tooltip("Smoothing factor for grabbed movement")]
        public float positionLerpSpeed = 15f;

        [Tooltip("Smoothing factor for grabbed rotation")]
        public float rotationLerpSpeed = 10f;

        [Tooltip("Distance threshold for ownership transfer")]
        public float ownershipTransferDistance = 0.1f;

        [Header("Visual Feedback")]
        [Tooltip("Material to apply when hovered")]
        public Material hoverMaterial;

        [Tooltip("Material to apply when grabbed")]
        public Material grabbedMaterial;

        [Header("Network Sync")]
        [Tooltip("How often to sync when not grabbed (per second)")]
        public float idleSyncRate = 5f;

        [Tooltip("Threshold for position sync when idle")]
        public float idlePositionThreshold = 0.01f;

        // Network state
        private NetworkVariable<bool> _isGrabbed = new NetworkVariable<bool>(false);
        private NetworkVariable<ulong> _grabbedByPlayer = new NetworkVariable<ulong>(ulong.MaxValue);
        private NetworkVariable<Vector3> _networkPosition = new NetworkVariable<Vector3>(Vector3.zero);
        private NetworkVariable<Quaternion> _networkRotation = new NetworkVariable<Quaternion>(Quaternion.identity);
        private NetworkVariable<Vector3> _networkVelocity = new NetworkVariable<Vector3>(Vector3.zero);

        // Local state
        private Rigidbody _rigidbody;
        private Renderer _renderer;
        private Material _originalMaterial;
        private Transform _grabTransform;
        private Vector3 _grabOffset;
        private Quaternion _grabRotationOffset;
        private float _idleSyncTimer;
        private Vector3 _lastSyncedPosition;

        public bool IsGrabbed => _isGrabbed.Value;
        public ulong GrabbedBy => _grabbedByPlayer.Value;
        public bool IsGrabbedByLocalPlayer => _isGrabbed.Value && _grabbedByPlayer.Value == NetworkManager.Singleton.LocalClientId;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _renderer = GetComponent<Renderer>();
            
            if (_renderer != null)
            {
                _originalMaterial = _renderer.material;
            }

            // Ensure rigidbody settings
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = usePhysics;
                _rigidbody.isKinematic = !usePhysics;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            _isGrabbed.OnValueChanged += OnGrabStateChanged;
            _networkPosition.OnValueChanged += OnNetworkPositionChanged;
            _networkRotation.OnValueChanged += OnNetworkRotationChanged;

            _lastSyncedPosition = transform.position;
        }

        public override void OnNetworkDespawn()
        {
            _isGrabbed.OnValueChanged -= OnGrabStateChanged;
            _networkPosition.OnValueChanged -= OnNetworkPositionChanged;
            _networkRotation.OnValueChanged -= OnNetworkRotationChanged;
            
            base.OnNetworkDespawn();
        }

        private void FixedUpdate()
        {
            if (IsGrabbedByLocalPlayer)
            {
                // Owner updates position based on grab point
                UpdateGrabbedPosition();
            }
            else if (IsServer && !_isGrabbed.Value && usePhysics)
            {
                // Server syncs physics objects periodically when idle
                SyncIdleObject();
            }
        }

        private void Update()
        {
            // Remote players interpolate to network position
            if (!IsOwner && !_isGrabbed.Value)
            {
                transform.position = Vector3.Lerp(transform.position, _networkPosition.Value, Time.deltaTime * positionLerpSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, _networkRotation.Value, Time.deltaTime * rotationLerpSpeed);
            }
        }

        /// <summary>
        /// Try to grab this object
        /// </summary>
        public bool TryGrab(Transform grabPoint, Vector3? hitPoint = null)
        {
            if (!isGrabbable || _isGrabbed.Value) return false;

            // Request ownership and grab
            RequestGrabServerRpc(NetworkManager.Singleton.LocalClientId, grabPoint.position, grabPoint.rotation, hitPoint ?? transform.position);
            
            _grabTransform = grabPoint;
            Vector3 grabPos = hitPoint ?? transform.position;
            _grabOffset = grabPoint.InverseTransformPoint(grabPos);
            _grabRotationOffset = Quaternion.Inverse(grabPoint.rotation) * transform.rotation;

            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestGrabServerRpc(ulong playerId, Vector3 grabPos, Quaternion grabRot, Vector3 hitPoint)
        {
            if (_isGrabbed.Value) return;

            // Transfer ownership to grabbing player
            NetworkObject.ChangeOwnership(playerId);
            
            _grabbedByPlayer.Value = playerId;
            _isGrabbed.Value = true;
            
            // Store relative transform
            _networkPosition.Value = transform.position;
            _networkRotation.Value = transform.rotation;

            // Disable physics while grabbed
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }

            GrabbedClientRpc(playerId);
        }

        [ClientRpc]
        private void GrabbedClientRpc(ulong playerId)
        {
            if (playerId != NetworkManager.Singleton.LocalClientId)
            {
                // Other players see grab feedback
                UpdateVisualFeedback(true);
            }
        }

        /// <summary>
        /// Release this object
        /// </summary>
        public void Release(Vector3? throwVelocity = null)
        {
            if (!IsGrabbedByLocalPlayer) return;

            Vector3 velocity = throwVelocity ?? Vector3.zero;
            ReleaseServerRpc(velocity);
            
            _grabTransform = null;
        }

        [ServerRpc]
        private void ReleaseServerRpc(Vector3 throwVelocity)
        {
            _isGrabbed.Value = false;
            _grabbedByPlayer.Value = ulong.MaxValue;

            // Return ownership to server
            NetworkObject.RemoveOwnership();

            // Re-enable physics
            if (_rigidbody != null && usePhysics)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.velocity = throwVelocity;
            }

            ReleasedClientRpc(throwVelocity);
        }

        [ClientRpc]
        private void ReleasedClientRpc(Vector3 throwVelocity)
        {
            UpdateVisualFeedback(false);
            
            if (!IsServer && _rigidbody != null && usePhysics)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.velocity = throwVelocity;
            }
        }

        private void UpdateGrabbedPosition()
        {
            if (_grabTransform == null) return;

            // Calculate target position based on grab point and offset
            Vector3 targetPos = _grabTransform.TransformPoint(_grabOffset);
            Quaternion targetRot = _grabTransform.rotation * _grabRotationOffset;

            // Smooth movement
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.fixedDeltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * rotationLerpSpeed);

            // Sync to network
            _networkPosition.Value = transform.position;
            _networkRotation.Value = transform.rotation;
        }

        private void SyncIdleObject()
        {
            _idleSyncTimer += Time.fixedDeltaTime;
            
            if (_idleSyncTimer >= 1f / idleSyncRate)
            {
                if (Vector3.Distance(transform.position, _lastSyncedPosition) > idlePositionThreshold)
                {
                    _networkPosition.Value = transform.position;
                    _networkRotation.Value = transform.rotation;
                    _lastSyncedPosition = transform.position;
                }
                _idleSyncTimer = 0f;
            }
        }

        private void OnGrabStateChanged(bool oldValue, bool newValue)
        {
            UpdateVisualFeedback(newValue);
        }

        private void OnNetworkPositionChanged(Vector3 oldValue, Vector3 newValue)
        {
            if (!IsOwner && !_isGrabbed.Value)
            {
                // Target position updated, will interpolate in Update
            }
        }

        private void OnNetworkRotationChanged(Quaternion oldValue, Quaternion newValue)
        {
            if (!IsOwner && !_isGrabbed.Value)
            {
                // Target rotation updated, will interpolate in Update
            }
        }

        private void UpdateVisualFeedback(bool grabbed)
        {
            if (_renderer == null) return;

            if (grabbed && grabbedMaterial != null)
            {
                _renderer.material = grabbedMaterial;
            }
            else if (!grabbed && _originalMaterial != null)
            {
                _renderer.material = _originalMaterial;
            }
        }

        /// <summary>
        /// Set hover state for visual feedback
        /// </summary>
        public void SetHovered(bool hovered)
        {
            if (_renderer == null || _isGrabbed.Value) return;

            if (hovered && hoverMaterial != null)
            {
                _renderer.material = hoverMaterial;
            }
            else if (_originalMaterial != null)
            {
                _renderer.material = _originalMaterial;
            }
        }
    }
}
