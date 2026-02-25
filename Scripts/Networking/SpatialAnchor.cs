using Unity.Netcode;
using UnityEngine;
using System;

namespace EasySharedSpace
{
    /// <summary>
    /// A persistent anchor point in shared space.
    /// All players see this anchor at the same world position.
    /// Use this to create shared reference points in your space.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class SpatialAnchor : NetworkBehaviour
    {
        [Header("Anchor Settings")]
        [Tooltip("Unique identifier for this anchor")]
        public string AnchorId;

        [Tooltip("Should this anchor persist across sessions")]
        public bool persistAcrossSessions = true;

        [Tooltip("Visual object to show anchor location")]
        public GameObject visualPrefab;

        [Tooltip("Show anchor gizmo in editor")]
        public bool showGizmo = true;

        [Tooltip("Color of the anchor gizmo")]
        public Color gizmoColor = Color.cyan;

        [Tooltip("Size of the anchor gizmo")]
        public float gizmoSize = 0.1f;

        [Header("Events")]
        public Action OnAnchorPlaced;
        public Action OnAnchorActivated;

        // Network synced state
        private NetworkVariable<bool> _isPlaced = new NetworkVariable<bool>(false);
        private NetworkVariable<Vector3> _anchorPosition = new NetworkVariable<Vector3>(Vector3.zero);
        private NetworkVariable<Quaternion> _anchorRotation = new NetworkVariable<Quaternion>(Quaternion.identity);

        private GameObject _visualInstance;
        private bool _initialized = false;

        public bool IsPlaced => _isPlaced.Value;
        public Vector3 AnchorPosition => _anchorPosition.Value;
        public Quaternion AnchorRotation => _anchorRotation.Value;
        public string Id => AnchorId;

        private void Awake()
        {
            // Generate unique ID if not set
            if (string.IsNullOrEmpty(AnchorId))
            {
                AnchorId = Guid.NewGuid().ToString();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isPlaced.OnValueChanged += OnPlacedStateChanged;

            if (_isPlaced.Value)
            {
                ApplyAnchorTransform();
                CreateVisual();
            }

            _initialized = true;

            // Register with manager
            if (SpatialAnchorManager.Instance != null)
            {
                SpatialAnchorManager.Instance.RegisterAnchor(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            _isPlaced.OnValueChanged -= OnPlacedStateChanged;
            
            if (SpatialAnchorManager.Instance != null)
            {
                SpatialAnchorManager.Instance.UnregisterAnchor(this);
            }

            base.OnNetworkDespawn();
        }

        /// <summary>
        /// Place this anchor at the current transform position (server only)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void PlaceAnchorServerRpc(Vector3 position, Quaternion rotation)
        {
            PlaceAnchor(position, rotation);
        }

        /// <summary>
        /// Place the anchor at a specific position
        /// </summary>
        public void PlaceAnchor(Vector3 position, Quaternion rotation)
        {
            if (!IsServer) return;

            _anchorPosition.Value = position;
            _anchorRotation.Value = rotation;
            _isPlaced.Value = true;

            Debug.Log($"[EasySharedSpace] Anchor '{AnchorId}' placed at {position}");
        }

        /// <summary>
        /// Place anchor at current transform position
        /// </summary>
        public void PlaceAtCurrentPosition()
        {
            if (IsServer)
            {
                PlaceAnchor(transform.position, transform.rotation);
            }
            else
            {
                PlaceAnchorServerRpc(transform.position, transform.rotation);
            }
        }

        private void OnPlacedStateChanged(bool oldValue, bool newValue)
        {
            if (newValue && !oldValue)
            {
                ApplyAnchorTransform();
                CreateVisual();
                OnAnchorPlaced?.Invoke();
            }
        }

        private void ApplyAnchorTransform()
        {
            transform.position = _anchorPosition.Value;
            transform.rotation = _anchorRotation.Value;
        }

        private void CreateVisual()
        {
            if (visualPrefab != null && _visualInstance == null)
            {
                _visualInstance = Instantiate(visualPrefab, transform);
                _visualInstance.transform.localPosition = Vector3.zero;
                _visualInstance.transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Get the position relative to the shared origin
        /// </summary>
        public Vector3 GetRelativePosition()
        {
            if (SharedSpaceManager.Instance != null)
            {
                return SharedSpaceManager.Instance.SharedToLocalSpace(_anchorPosition.Value);
            }
            return _anchorPosition.Value;
        }

        /// <summary>
        /// Set position relative to the shared origin
        /// </summary>
        public void SetRelativePosition(Vector3 relativePosition)
        {
            if (SharedSpaceManager.Instance != null)
            {
                Vector3 worldPos = SharedSpaceManager.Instance.LocalToSharedSpace(relativePosition);
                if (IsServer)
                {
                    PlaceAnchor(worldPos, transform.rotation);
                }
                else
                {
                    PlaceAnchorServerRpc(worldPos, transform.rotation);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            Gizmos.color = gizmoColor;
            Vector3 pos = Application.isPlaying ? _anchorPosition.Value : transform.position;
            
            // Draw cross
            Gizmos.DrawLine(pos + Vector3.up * gizmoSize, pos - Vector3.up * gizmoSize);
            Gizmos.DrawLine(pos + Vector3.right * gizmoSize, pos - Vector3.right * gizmoSize);
            Gizmos.DrawLine(pos + Vector3.forward * gizmoSize, pos - Vector3.forward * gizmoSize);
            
            // Draw sphere
            Gizmos.DrawWireSphere(pos, gizmoSize * 0.5f);

            // Draw label
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(AnchorId))
            {
                UnityEditor.Handles.Label(pos + Vector3.up * gizmoSize * 2, $"Anchor: {AnchorId}");
            }
            #endif
        }
    }
}
