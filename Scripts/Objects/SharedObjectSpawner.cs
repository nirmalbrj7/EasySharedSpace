using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace EasySharedSpace
{
    /// <summary>
    /// Spawns objects that are synchronized across all clients.
    /// Use this to create shared objects that all players can see and interact with.
    /// </summary>
    public class SharedObjectSpawner : NetworkBehaviour
    {
        public static SharedObjectSpawner Instance { get; private set; }

        [System.Serializable]
        public class SpawnableObject
        {
            [Tooltip("Unique identifier for this spawnable type")]
            public string objectId;
            
            [Tooltip("The prefab to spawn")]
            public GameObject prefab;
            
            [Tooltip("Maximum number of this type that can exist (0 = unlimited)")]
            public int maxCount = 0;
        }

        [Header("Spawnable Objects")]
        [Tooltip("List of objects that can be spawned at runtime")]
        public SpawnableObject[] spawnableObjects;

        [Header("Spawn Settings")]
        [Tooltip("Parent transform for spawned objects")]
        public Transform spawnContainer;

        [Tooltip("Default lifetime for spawned objects (0 = infinite)")]
        public float defaultLifetime = 0f;

        [Header("Debug")]
        public bool debugLogs = true;

        private Dictionary<string, SpawnableObject> _spawnableLookup = new Dictionary<string, SpawnableObject>();
        private Dictionary<string, List<NetworkObject>> _spawnedObjects = new Dictionary<string, List<NetworkObject>>();
        private ulong _nextSpawnId = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Build lookup
            foreach (var obj in spawnableObjects)
            {
                if (!string.IsNullOrEmpty(obj.objectId) && obj.prefab != null)
                {
                    _spawnableLookup[obj.objectId] = obj;
                    _spawnedObjects[obj.objectId] = new List<NetworkObject>();
                }
            }
        }

        /// <summary>
        /// Spawn an object by its ID at the specified position
        /// </summary>
        public NetworkObject SpawnObject(string objectId, Vector3 position, Quaternion? rotation = null)
        {
            if (!_spawnableLookup.TryGetValue(objectId, out SpawnableObject spawnable))
            {
                Debug.LogError($"[EasySharedSpace] Unknown spawnable ID: {objectId}");
                return null;
            }

            // Check max count
            if (spawnable.maxCount > 0 && _spawnedObjects[objectId].Count >= spawnable.maxCount)
            {
                Debug.LogWarning($"[EasySharedSpace] Max count reached for {objectId}");
                return null;
            }

            if (IsServer)
            {
                return SpawnObjectInternal(spawnable, position, rotation ?? Quaternion.identity);
            }
            else
            {
                // Client requests spawn
                SpawnObjectServerRpc(objectId, position, rotation ?? Quaternion.identity);
                return null;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnObjectServerRpc(string objectId, Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
        {
            if (!_spawnableLookup.TryGetValue(objectId, out SpawnableObject spawnable))
            {
                Debug.LogError($"[EasySharedSpace] Unknown spawnable ID: {objectId}");
                return;
            }

            SpawnObjectInternal(spawnable, position, rotation);
        }

        private NetworkObject SpawnObjectInternal(SpawnableObject spawnable, Vector3 position, Quaternion rotation)
        {
            GameObject instance = Instantiate(spawnable.prefab, position, rotation, spawnContainer);
            NetworkObject netObj = instance.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogError($"[EasySharedSpace] Spawned object {spawnable.objectId} is missing NetworkObject component!");
                Destroy(instance);
                return null;
            }

            netObj.Spawn();

            // Track spawned object
            _spawnedObjects[spawnable.objectId].Add(netObj);

            // Set up despawn callback
            netObj.OnDestroyCallback += (obj) => OnObjectDespawned(spawnable.objectId, obj);

            // Apply lifetime if set
            if (defaultLifetime > 0)
            {
                Invoke(nameof(DespawnObject), defaultLifetime);
            }

            if (debugLogs)
            {
                Debug.Log($"[EasySharedSpace] Spawned {spawnable.objectId} at {position}");
            }

            return netObj;
        }

        /// <summary>
        /// Spawn an object at a specific anchor location
        /// </summary>
        public NetworkObject SpawnAtAnchor(string objectId, string anchorId, Vector3 offset = default, Quaternion? rotation = null)
        {
            SpatialAnchor anchor = SpatialAnchorManager.Instance?.GetAnchor(anchorId);
            if (anchor == null)
            {
                Debug.LogError($"[EasySharedSpace] Anchor {anchorId} not found");
                return null;
            }

            Vector3 position = anchor.AnchorPosition + offset;
            Quaternion finalRotation = rotation ?? anchor.AnchorRotation;

            return SpawnObject(objectId, position, finalRotation);
        }

        /// <summary>
        /// Spawn an object relative to the shared origin
        /// </summary>
        public NetworkObject SpawnRelativeToOrigin(string objectId, Vector3 localPosition, Quaternion? localRotation = null)
        {
            if (SharedSpaceManager.Instance == null)
            {
                Debug.LogError("[EasySharedSpace] SharedSpaceManager not available");
                return null;
            }

            Vector3 worldPos = SharedSpaceManager.Instance.LocalToSharedSpace(localPosition);
            Quaternion worldRot = localRotation ?? Quaternion.identity;
            
            if (SharedSpaceManager.Instance.GetSharedOrigin() != null)
            {
                worldRot = SharedSpaceManager.Instance.GetSharedOrigin().rotation * worldRot;
            }

            return SpawnObject(objectId, worldPos, worldRot);
        }

        /// <summary>
        /// Despawn all objects of a specific type
        /// </summary>
        public void DespawnAllObjects(string objectId)
        {
            if (IsServer)
            {
                if (_spawnedObjects.TryGetValue(objectId, out List<NetworkObject> objects))
                {
                    foreach (var obj in new List<NetworkObject>(objects))
                    {
                        if (obj != null && obj.IsSpawned)
                        {
                            obj.Despawn(true);
                        }
                    }
                    objects.Clear();
                }
            }
            else
            {
                DespawnAllObjectsServerRpc(objectId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void DespawnAllObjectsServerRpc(string objectId)
        {
            DespawnAllObjects(objectId);
        }

        private void OnObjectDespawned(string objectId, NetworkObject obj)
        {
            if (_spawnedObjects.TryGetValue(objectId, out List<NetworkObject> objects))
            {
                objects.Remove(obj);
            }
        }

        private void DespawnObject(NetworkObject obj)
        {
            if (obj != null && obj.IsSpawned)
            {
                obj.Despawn(true);
            }
        }

        /// <summary>
        /// Get all spawned objects of a specific type
        /// </summary>
        public IReadOnlyList<NetworkObject> GetSpawnedObjects(string objectId)
        {
            if (_spawnedObjects.TryGetValue(objectId, out List<NetworkObject> objects))
            {
                return objects.AsReadOnly();
            }
            return new List<NetworkObject>().AsReadOnly();
        }

        /// <summary>
        /// Check if an object ID is valid
        /// </summary>
        public bool IsValidObjectId(string objectId)
        {
            return _spawnableLookup.ContainsKey(objectId);
        }

        /// <summary>
        /// Get count of spawned objects for a type
        /// </summary>
        public int GetSpawnedCount(string objectId)
        {
            if (_spawnedObjects.TryGetValue(objectId, out List<NetworkObject> objects))
            {
                return objects.Count;
            }
            return 0;
        }
    }
}
