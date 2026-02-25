using UnityEngine;
using Unity.Netcode;
using EasySharedSpace;
using System.Collections.Generic;

namespace EasySharedSpace.Demo
{
    /// <summary>
    /// Manager for research test scenes.
    /// Tracks and visualizes all shared objects and players.
    /// </summary>
    public class ResearchTestSceneManager : MonoBehaviour
    {
        [Header("Scene References")]
        public Transform visualizationRoot;
        public Camera sceneCamera;
        
        [Header("Visual Elements")]
        public bool showPlayerTrails = true;
        public bool showObjectLabels = true;
        public bool showSharedSpaceBounds = true;
        
        [Header("Materials")]
        public Material localPlayerMaterial;
        public Material remotePlayerMaterial;
        public Material sharedObjectMaterial;
        
        [Header("Debug Display")]
        public bool showDebugInfo = true;
        private string debugInfo = "";

        [Header("Test Objects")]
        public GameObject[] testPrefabs;
        public Transform[] spawnPoints;

        private SharedSpaceManager _spaceManager;
        private Dictionary<ulong, LineRenderer> _playerTrails = new Dictionary<ulong, LineRenderer>();
        private List<Vector3> _sharedObjectPositions = new List<Vector3>();

        private void Start()
        {
            _spaceManager = SharedSpaceManager.Instance;
            
            if (sceneCamera == null)
                sceneCamera = Camera.main;

            // Subscribe to player events
            if (_spaceManager != null)
            {
                _spaceManager.OnPlayerJoined += OnPlayerJoined;
                _spaceManager.OnPlayerLeft += OnPlayerLeft;
            }

            // Create visualization root if not set
            if (visualizationRoot == null)
            {
                GameObject root = new GameObject("VisualizationRoot");
                visualizationRoot = root.transform;
            }

            Debug.Log("[ResearchTestSceneManager] Initialized. Press keys for testing:");
            Debug.Log("  [H] - Start Host");
            Debug.Log("  [J] - Join (enter IP first)");
            Debug.Log("  [1-9] - Spawn test object");
            Debug.Log("  [R] - Reset all objects");
            Debug.Log("  [T] - Toggle trails");
            Debug.Log("  [L] - Toggle labels");
        }

        private void Update()
        {
            HandleInput();
            UpdateVisualizations();
            UpdateDebugInfo();
        }

        private void HandleInput()
        {
            // Host
            if (Input.GetKeyDown(KeyCode.H))
            {
                SimpleIPNetworkManager.Instance?.StartHost();
            }

            // Join (with default localhost for testing)
            if (Input.GetKeyDown(KeyCode.J))
            {
                SimpleIPNetworkManager.Instance?.JoinAsClient("127.0.0.1");
            }

            // Spawn objects with number keys
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SpawnTestObject(i);
                }
            }

            // Reset scene
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetScene();
            }

            // Toggle visualizations
            if (Input.GetKeyDown(KeyCode.T))
            {
                showPlayerTrails = !showPlayerTrails;
                Debug.Log($"[ResearchTestSceneManager] Player trails: {showPlayerTrails}");
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                showObjectLabels = !showObjectLabels;
                Debug.Log($"[ResearchTestSceneManager] Object labels: {showObjectLabels}");
            }

            // Manual spawn in front of camera
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SpawnObjectInFront();
            }
        }

        private void SpawnTestObject(int prefabIndex)
        {
            if (testPrefabs == null || testPrefabs.Length == 0) return;
            if (prefabIndex >= testPrefabs.Length) return;

            Vector3 spawnPos = GetSpawnPosition();
            
            if (SharedObjectSpawner.Instance != null)
            {
                SharedObjectSpawner.Instance.SpawnObject(prefabIndex, spawnPos, Quaternion.identity);
                Debug.Log($"[ResearchTestSceneManager] Spawned object {prefabIndex} at {spawnPos}");
            }
            else
            {
                // Fallback: spawn locally
                Instantiate(testPrefabs[prefabIndex], spawnPos, Quaternion.identity);
            }
        }

        private void SpawnObjectInFront()
        {
            if (sceneCamera == null) return;
            
            Vector3 spawnPos = sceneCamera.transform.position + sceneCamera.transform.forward * 2f;
            
            if (SharedObjectSpawner.Instance != null && testPrefabs.Length > 0)
            {
                SharedObjectSpawner.Instance.SpawnObject(0, spawnPos, Quaternion.identity);
            }
        }

        private Vector3 GetSpawnPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            }
            
            // Random position around origin
            return new Vector3(
                Random.Range(-3f, 3f),
                2f,
                Random.Range(-3f, 3f)
            );
        }

        private void ResetScene()
        {
            // Remove all spawned objects
            var spawnedObjects = FindObjectsOfType<SharedGrabbableObject>();
            int count = 0;
            foreach (var obj in spawnedObjects)
            {
                if (obj.NetworkObject != null && obj.NetworkObject.IsSpawned)
                {
                    if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                    {
                        obj.NetworkObject.Despawn();
                        count++;
                    }
                }
            }
            Debug.Log($"[ResearchTestSceneManager] Reset scene - removed {count} objects");
        }

        private void UpdateVisualizations()
        {
            if (!showPlayerTrails) return;
            if (_spaceManager == null) return;

            // Update player trails
            foreach (var player in _spaceManager.ConnectedPlayers)
            {
                ulong clientId = player.Key;
                SharedPlayer sharedPlayer = player.Value;

                if (!_playerTrails.ContainsKey(clientId))
                {
                    CreatePlayerTrail(clientId, sharedPlayer);
                }

                // Update trail position
                if (_playerTrails.ContainsKey(clientId))
                {
                    LineRenderer trail = _playerTrails[clientId];
                    // Add current position to trail (simplified)
                }
            }
        }

        private void CreatePlayerTrail(ulong clientId, SharedPlayer player)
        {
            GameObject trailObj = new GameObject($"PlayerTrail_{clientId}");
            trailObj.transform.SetParent(visualizationRoot);
            
            LineRenderer trail = trailObj.AddComponent<LineRenderer>();
            trail.startWidth = 0.05f;
            trail.endWidth = 0.05f;
            trail.material = player.IsLocalPlayer ? localPlayerMaterial : remotePlayerMaterial;
            trail.startColor = player.PlayerColor.Value;
            trail.endColor = player.PlayerColor.Value;
            
            _playerTrails[clientId] = trail;
        }

        private void UpdateDebugInfo()
        {
            if (!showDebugInfo) return;

            debugInfo = "=== SHARED SPACE DEBUG ===\n";
            debugInfo += $"Connected: {NetworkManager.Singleton?.IsConnectedClient}\n";
            debugInfo += $"Is Host: {NetworkManager.Singleton?.IsHost}\n";
            debugInfo += $"Players: {_spaceManager?.ConnectedPlayers.Count}\n";
            debugInfo += $"Local IP: {SimpleIPNetworkManager.Instance?.LocalIPAddress}\n";
            debugInfo += "========================\n";
        }

        private void OnPlayerJoined(ulong clientId)
        {
            Debug.Log($"[ResearchTestSceneManager] Player joined: {clientId}");
        }

        private void OnPlayerLeft(ulong clientId)
        {
            Debug.Log($"[ResearchTestSceneManager] Player left: {clientId}");
            
            if (_playerTrails.ContainsKey(clientId))
            {
                Destroy(_playerTrails[clientId].gameObject);
                _playerTrails.Remove(clientId);
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            // Draw debug info box
            GUI.Box(new Rect(10, 10, 300, 150), "");
            GUI.Label(new Rect(15, 15, 290, 140), debugInfo);

            // Draw controls help
            GUI.Box(new Rect(Screen.width - 310, 10, 300, 200), "Controls");
            string controls = 
                "[H] - Start Host\n" +
                "[J] - Join (localhost)\n" +
                "[1-9] - Spawn Objects\n" +
                "[Space] - Spawn in front\n" +
                "[R] - Reset Scene\n" +
                "[T] - Toggle Trails\n" +
                "[L] - Toggle Labels\n" +
                "\n" +
                "Click UI buttons for\n" +
                "full control";
            GUI.Label(new Rect(Screen.width - 305, 30, 290, 180), controls);
        }

        private void OnDestroy()
        {
            if (_spaceManager != null)
            {
                _spaceManager.OnPlayerJoined -= OnPlayerJoined;
                _spaceManager.OnPlayerLeft -= OnPlayerLeft;
            }
        }
    }
}
