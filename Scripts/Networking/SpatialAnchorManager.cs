using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace EasySharedSpace
{
    /// <summary>
    /// Manages all spatial anchors in the shared space.
    /// Handles anchor discovery, persistence, and coordinate synchronization.
    /// </summary>
    public class SpatialAnchorManager : NetworkBehaviour
    {
        public static SpatialAnchorManager Instance { get; private set; }

        [Header("Anchor Management")]
        [Tooltip("Prefab for creating new anchors at runtime")]
        public SpatialAnchor anchorPrefab;

        [Tooltip("Parent transform for spawned anchors")]
        public Transform anchorContainer;

        [Header("Persistence")]
        [Tooltip("Save anchors to PlayerPrefs")]
        public bool useLocalPersistence = true;

        [Tooltip("Key prefix for saved anchors")]
        public string saveKeyPrefix = "ESS_Anchor_";

        [Header("Debug")]
        [Tooltip("Show debug logs")]
        public bool debugLogs = true;

        [Header("Events")]
        public Action<SpatialAnchor> OnAnchorAdded;
        public Action<SpatialAnchor> OnAnchorRemoved;
        public Action OnAnchorsLoaded;

        private Dictionary<string, SpatialAnchor> _anchors = new Dictionary<string, SpatialAnchor>();
        private bool _isInitialized = false;

        public IReadOnlyDictionary<string, SpatialAnchor> Anchors => _anchors;
        public int AnchorCount => _anchors.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer && useLocalPersistence)
            {
                LoadAnchors();
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Register an anchor with the manager
        /// </summary>
        public void RegisterAnchor(SpatialAnchor anchor)
        {
            if (anchor == null || string.IsNullOrEmpty(anchor.Id)) return;

            if (!_anchors.ContainsKey(anchor.Id))
            {
                _anchors[anchor.Id] = anchor;
                OnAnchorAdded?.Invoke(anchor);
                
                if (debugLogs)
                {
                    Debug.Log($"[EasySharedSpace] Registered anchor: {anchor.Id}");
                }
            }
        }

        /// <summary>
        /// Unregister an anchor
        /// </summary>
        public void UnregisterAnchor(SpatialAnchor anchor)
        {
            if (anchor == null || string.IsNullOrEmpty(anchor.Id)) return;

            if (_anchors.ContainsKey(anchor.Id))
            {
                _anchors.Remove(anchor.Id);
                OnAnchorRemoved?.Invoke(anchor);
            }
        }

        /// <summary>
        /// Create a new anchor at the specified position
        /// </summary>
        public SpatialAnchor CreateAnchor(Vector3 position, Quaternion? rotation = null, string customId = null)
        {
            if (anchorPrefab == null)
            {
                Debug.LogError("[EasySharedSpace] Anchor prefab not assigned!");
                return null;
            }

            SpatialAnchor anchor;

            if (IsServer)
            {
                anchor = Instantiate(anchorPrefab, position, rotation ?? Quaternion.identity, anchorContainer);
                
                if (!string.IsNullOrEmpty(customId))
                {
                    anchor.AnchorId = customId;
                }

                NetworkObject netObj = anchor.GetComponent<NetworkObject>();
                netObj.Spawn();

                anchor.PlaceAnchor(position, rotation ?? Quaternion.identity);
            }
            else
            {
                // Client requests anchor creation
                CreateAnchorServerRpc(position, rotation ?? Quaternion.identity, customId ?? "");
                return null;
            }

            return anchor;
        }

        [ServerRpc(RequireOwnership = false)]
        private void CreateAnchorServerRpc(Vector3 position, Quaternion rotation, string customId)
        {
            CreateAnchor(position, rotation, string.IsNullOrEmpty(customId) ? null : customId);
        }

        /// <summary>
        /// Get an anchor by its ID
        /// </summary>
        public SpatialAnchor GetAnchor(string anchorId)
        {
            _anchors.TryGetValue(anchorId, out SpatialAnchor anchor);
            return anchor;
        }

        /// <summary>
        /// Remove an anchor
        /// </summary>
        public void RemoveAnchor(string anchorId)
        {
            if (IsServer)
            {
                if (_anchors.TryGetValue(anchorId, out SpatialAnchor anchor))
                {
                    anchor.GetComponent<NetworkObject>().Despawn(true);
                }
            }
            else
            {
                RemoveAnchorServerRpc(anchorId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RemoveAnchorServerRpc(string anchorId)
        {
            RemoveAnchor(anchorId);
        }

        /// <summary>
        /// Clear all anchors
        /// </summary>
        public void ClearAllAnchors()
        {
            if (IsServer)
            {
                List<string> ids = new List<string>(_anchors.Keys);
                foreach (var id in ids)
                {
                    RemoveAnchor(id);
                }
            }
            else
            {
                ClearAllAnchorsServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ClearAllAnchorsServerRpc()
        {
            ClearAllAnchors();
        }

        /// <summary>
        /// Save all anchors to local storage
        /// </summary>
        public void SaveAnchors()
        {
            if (!useLocalPersistence) return;

            // Clear old saves
            string existingKeys = PlayerPrefs.GetString($"{saveKeyPrefix}Keys", "");
            foreach (var key in existingKeys.Split(','))
            {
                if (!string.IsNullOrEmpty(key))
                {
                    PlayerPrefs.DeleteKey($"{saveKeyPrefix}{key}_pos");
                    PlayerPrefs.DeleteKey($"{saveKeyPrefix}{key}_rot");
                }
            }

            // Save current anchors
            List<string> anchorIds = new List<string>();
            foreach (var kvp in _anchors)
            {
                string id = kvp.Key;
                SpatialAnchor anchor = kvp.Value;

                if (anchor.persistAcrossSessions)
                {
                    anchorIds.Add(id);
                    
                    Vector3 pos = anchor.AnchorPosition;
                    Quaternion rot = anchor.AnchorRotation;
                    
                    PlayerPrefs.SetString($"{saveKeyPrefix}{id}_pos", $"{pos.x},{pos.y},{pos.z}");
                    PlayerPrefs.SetString($"{saveKeyPrefix}{id}_rot", $"{rot.x},{rot.y},{rot.z},{rot.w}");
                }
            }

            PlayerPrefs.SetString($"{saveKeyPrefix}Keys", string.Join(",", anchorIds));
            PlayerPrefs.Save();

            if (debugLogs)
            {
                Debug.Log($"[EasySharedSpace] Saved {anchorIds.Count} anchors");
            }
        }

        /// <summary>
        /// Load anchors from local storage
        /// </summary>
        public void LoadAnchors()
        {
            if (!useLocalPersistence || !IsServer) return;

            string keysStr = PlayerPrefs.GetString($"{saveKeyPrefix}Keys", "");
            if (string.IsNullOrEmpty(keysStr)) return;

            string[] keys = keysStr.Split(',');
            int loadedCount = 0;

            foreach (var id in keys)
            {
                if (string.IsNullOrEmpty(id)) continue;

                string posStr = PlayerPrefs.GetString($"{saveKeyPrefix}{id}_pos", "");
                string rotStr = PlayerPrefs.GetString($"{saveKeyPrefix}{id}_rot", "");

                if (string.IsNullOrEmpty(posStr) || string.IsNullOrEmpty(rotStr)) continue;

                Vector3 pos = ParseVector3(posStr);
                Quaternion rot = ParseQuaternion(rotStr);

                CreateAnchor(pos, rot, id);
                loadedCount++;
            }

            if (debugLogs)
            {
                Debug.Log($"[EasySharedSpace] Loaded {loadedCount} anchors");
            }

            OnAnchorsLoaded?.Invoke();
        }

        private Vector3 ParseVector3(string str)
        {
            string[] parts = str.Split(',');
            if (parts.Length == 3)
            {
                return new Vector3(
                    float.Parse(parts[0]),
                    float.Parse(parts[1]),
                    float.Parse(parts[2])
                );
            }
            return Vector3.zero;
        }

        private Quaternion ParseQuaternion(string str)
        {
            string[] parts = str.Split(',');
            if (parts.Length == 4)
            {
                return new Quaternion(
                    float.Parse(parts[0]),
                    float.Parse(parts[1]),
                    float.Parse(parts[2]),
                    float.Parse(parts[3])
                );
            }
            return Quaternion.identity;
        }
    }
}
