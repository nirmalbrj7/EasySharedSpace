using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace EasySharedSpace
{
    /// <summary>
    /// Main manager for the shared space experience.
    /// Handles network connection, player spawning, and coordinate space synchronization.
    /// </summary>
    public class SharedSpaceManager : NetworkBehaviour
    {
        public static SharedSpaceManager Instance { get; private set; }

        [Header("Player Settings")]
        [Tooltip("The player prefab to spawn for each connected client")]
        public GameObject playerPrefab;
        
        [Tooltip("Where to spawn players relative to the shared origin")]
        public Transform spawnOrigin;

        [Header("Network Settings")]
        [Tooltip("Auto-start as host when playing in editor")]
        public bool autoStartInEditor = true;

        [Header("Events")]
        public Action<ulong> OnPlayerJoined;
        public Action<ulong> OnPlayerLeft;
        public Action OnSharedSpaceReady;

        private Dictionary<ulong, SharedPlayer> _connectedPlayers = new Dictionary<ulong, SharedPlayer>();
        private bool _isSpaceReady = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (autoStartInEditor && !NetworkManager.Singleton.IsListening)
            {
                StartHost();
            }
#endif
        }

        /// <summary>
        /// Start as host (server + client)
        /// </summary>
        public void StartHost()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[EasySharedSpace] NetworkManager not found! Add a NetworkManager to your scene.");
                return;
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            NetworkManager.Singleton.StartHost();
            Debug.Log("[EasySharedSpace] Host started");
        }

        /// <summary>
        /// Join as client
        /// </summary>
        public void JoinAsClient(string ipAddress = "127.0.0.1", ushort port = 7777)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[EasySharedSpace] NetworkManager not found! Add a NetworkManager to your scene.");
                return;
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>()
                .SetConnectionData(ipAddress, port);
            
            NetworkManager.Singleton.StartClient();
            Debug.Log($"[EasySharedSpace] Connecting to {ipAddress}:{port}");
        }

        /// <summary>
        /// Disconnect from the network
        /// </summary>
        public void Disconnect()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[EasySharedSpace] Client connected: {clientId}");
            
            if (IsServer)
            {
                SpawnPlayer(clientId);
            }

            OnPlayerJoined?.Invoke(clientId);

            // Mark space as ready once local player connects
            if (clientId == NetworkManager.Singleton.LocalClientId && !_isSpaceReady)
            {
                _isSpaceReady = true;
                OnSharedSpaceReady?.Invoke();
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[EasySharedSpace] Client disconnected: {clientId}");
            
            if (_connectedPlayers.ContainsKey(clientId))
            {
                _connectedPlayers.Remove(clientId);
            }

            OnPlayerLeft?.Invoke(clientId);
        }

        private void SpawnPlayer(ulong clientId)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[EasySharedSpace] Player prefab not assigned!");
                return;
            }

            Vector3 spawnPos = spawnOrigin != null ? spawnOrigin.position : Vector3.zero;
            Quaternion spawnRot = spawnOrigin != null ? spawnOrigin.rotation : Quaternion.identity;

            // Add slight random offset for multiple players
            spawnPos += new Vector3(UnityEngine.Random.Range(-2f, 2f), 0, UnityEngine.Random.Range(-2f, 2f));

            GameObject playerObj = Instantiate(playerPrefab, spawnPos, spawnRot);
            playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

            SharedPlayer player = playerObj.GetComponent<SharedPlayer>();
            if (player != null)
            {
                _connectedPlayers[clientId] = player;
            }
        }

        /// <summary>
        /// Get the shared origin transform for coordinate synchronization
        /// </summary>
        public Transform GetSharedOrigin()
        {
            return spawnOrigin != null ? spawnOrigin : transform;
        }

        /// <summary>
        /// Transform a local position to shared space coordinates
        /// </summary>
        public Vector3 LocalToSharedSpace(Vector3 localPosition)
        {
            if (spawnOrigin != null)
            {
                return spawnOrigin.TransformPoint(localPosition);
            }
            return localPosition;
        }

        /// <summary>
        /// Transform a shared space position to local coordinates
        /// </summary>
        public Vector3 SharedToLocalSpace(Vector3 sharedPosition)
        {
            if (spawnOrigin != null)
            {
                return spawnOrigin.InverseTransformPoint(sharedPosition);
            }
            return sharedPosition;
        }

        public IReadOnlyDictionary<ulong, SharedPlayer> ConnectedPlayers => _connectedPlayers;
        public bool IsSpaceReady => _isSpaceReady;
    }
}
