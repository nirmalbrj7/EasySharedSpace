using Unity.Netcode;
using UnityEngine;
using System;

namespace EasySharedSpace
{
    /// <summary>
    /// A trigger zone that works across the network.
    /// Detects when players enter/exit and syncs events.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NetworkedTriggerZone : NetworkBehaviour
    {
        [Header("Zone Settings")]
        [Tooltip("Layer mask for triggering objects")]
        public LayerMask triggerLayers;

        [Tooltip("Only trigger for players")]
        public bool playersOnly = true;

        [Tooltip("Require ownership to trigger")]
        public bool requireOwnership = false;

        [Header("Events")]
        public Action<ulong> OnPlayerEnter;
        public Action<ulong> OnPlayerExit;
        public Action<GameObject> OnObjectEnter;
        public Action<GameObject> OnObjectExit;

        [Header("Visuals")]
        [Tooltip("Show zone in editor")]
        public bool showGizmo = true;
        public Color gizmoColor = new Color(0, 1, 0, 0.3f);

        [Header("Debug")]
        public bool logEvents = true;

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            if (!IsValidTarget(other)) return;

            var netObj = other.GetComponent<NetworkObject>();
            if (netObj == null) return;

            ulong clientId = netObj.OwnerClientId;

            OnZoneEnteredClientRpc(clientId, other.transform.position);
            
            if (logEvents)
            {
                Debug.Log($"[NetworkedTriggerZone] Player {clientId} entered zone");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer) return;

            if (!IsValidTarget(other)) return;

            var netObj = other.GetComponent<NetworkObject>();
            if (netObj == null) return;

            ulong clientId = netObj.OwnerClientId;

            OnZoneExitedClientRpc(clientId, other.transform.position);

            if (logEvents)
            {
                Debug.Log($"[NetworkedTriggerZone] Player {clientId} exited zone");
            }
        }

        private bool IsValidTarget(Collider other)
        {
            // Check layer
            if ((triggerLayers.value & (1 << other.gameObject.layer)) == 0)
                return false;

            // Check if player
            if (playersOnly && other.GetComponent<SharedPlayer>() == null)
                return false;

            // Check ownership
            if (requireOwnership)
            {
                var netObj = other.GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsOwner)
                    return false;
            }

            return true;
        }

        [ClientRpc]
        private void OnZoneEnteredClientRpc(ulong clientId, Vector3 position)
        {
            OnPlayerEnter?.Invoke(clientId);
            
            // Check if local player
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                OnLocalPlayerEnter();
            }
        }

        [ClientRpc]
        private void OnZoneExitedClientRpc(ulong clientId, Vector3 position)
        {
            OnPlayerExit?.Invoke(clientId);

            // Check if local player
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                OnLocalPlayerExit();
            }
        }

        /// <summary>
        /// Override this for local player enter behavior
        /// </summary>
        protected virtual void OnLocalPlayerEnter()
        {
        }

        /// <summary>
        /// Override this for local player exit behavior
        /// </summary>
        protected virtual void OnLocalPlayerExit()
        {
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            Gizmos.color = gizmoColor;
            
            if (_collider is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.DrawCube(box.center, box.size * 0.95f);
            }
            else if (_collider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), sphere.radius);
            }
            else if (_collider is CapsuleCollider capsule)
            {
                // Simplified capsule gizmo
                Vector3 center = transform.TransformPoint(capsule.center);
                Gizmos.DrawWireSphere(center, capsule.radius);
            }
        }
    }
}
