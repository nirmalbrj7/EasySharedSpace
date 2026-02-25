using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;

namespace EasySharedSpace
{
    /// <summary>
    /// Simple IP-based network manager for local testing and research.
    /// No complicated setup - just enter IP and connect!
    /// </summary>
    public class SimpleIPNetworkManager : MonoBehaviour
    {
        [Header("Network Settings")]
        [Tooltip("Port to use for connection")]
        public ushort port = 7777;
        
        [Tooltip("Auto-start as host in Unity Editor")]
        public bool autoStartHostInEditor = true;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnHostStarted;
        public UnityEngine.Events.UnityEvent OnClientConnected;
        public UnityEngine.Events.UnityEvent OnDisconnected;

        [Header("Status")]
        [SerializeField] private string localIPAddress;
        [SerializeField] private string connectionStatus = "Disconnected";

        public static SimpleIPNetworkManager Instance { get; private set; }
        
        public string LocalIPAddress => localIPAddress;
        public string ConnectionStatus => connectionStatus;
        public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        public bool IsClient => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;
        public bool IsConnected => IsHost || IsClient;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Get local IP on startup
            localIPAddress = GetLocalIPAddress();
        }

        private void Start()
        {
            // Auto-start in editor for quick testing
            #if UNITY_EDITOR
            if (autoStartHostInEditor)
            {
                StartHost();
            }
            #endif
            
            // Subscribe to network events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            }
        }

        /// <summary>
        /// Start as Host (Server + Client)
        /// </summary>
        public void StartHost()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[SimpleIPNetworkManager] NetworkManager not found! Add NetworkManager to scene.");
                return;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[SimpleIPNetworkManager] Already running!");
                return;
            }

            // Setup transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData("0.0.0.0", port);
            }

            NetworkManager.Singleton.StartHost();
            connectionStatus = $"Host (Port: {port})";
            
            Debug.Log($"[SimpleIPNetworkManager] Host started on port {port}");
            Debug.Log($"[SimpleIPNetworkManager] Local IP: {localIPAddress}");
            Debug.Log($"[SimpleIPNetworkManager] Tell clients to connect to: {localIPAddress}:{port}");
            
            OnHostStarted?.Invoke();
        }

        /// <summary>
        /// Join as Client
        /// </summary>
        public void JoinAsClient(string ipAddress)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[SimpleIPNetworkManager] NetworkManager not found!");
                return;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[SimpleIPNetworkManager] Already connected!");
                return;
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                Debug.LogError("[SimpleIPNetworkManager] IP Address is empty!");
                return;
            }

            // Setup transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(ipAddress, port);
            }

            NetworkManager.Singleton.StartClient();
            connectionStatus = $"Connecting to {ipAddress}:{port}...";
            
            Debug.Log($"[SimpleIPNetworkManager] Connecting to {ipAddress}:{port}");
        }

        /// <summary>
        /// Disconnect from network
        /// </summary>
        public void Disconnect()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
                connectionStatus = "Disconnected";
                Debug.Log("[SimpleIPNetworkManager] Disconnected");
                OnDisconnected?.Invoke();
            }
        }

        private void OnClientConnectedCallback(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                connectionStatus = IsHost ? "Host (Running)" : "Connected";
                OnClientConnected?.Invoke();
            }
            Debug.Log($"[SimpleIPNetworkManager] Client connected: {clientId}");
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                connectionStatus = "Disconnected";
                OnDisconnected?.Invoke();
            }
            Debug.Log($"[SimpleIPNetworkManager] Client disconnected: {clientId}");
        }

        /// <summary>
        /// Get the local IP address of this machine
        /// </summary>
        public static string GetLocalIPAddress()
        {
            try
            {
                // Try to get the IP that's likely the WiFi/Ethernet IP
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                // Fallback to hostname method
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            return "127.0.0.1";
        }

        /// <summary>
        /// Copy local IP to clipboard (for easy sharing)
        /// </summary>
        public void CopyIPToClipboard()
        {
            GUIUtility.systemCopyBuffer = localIPAddress;
            Debug.Log($"[SimpleIPNetworkManager] IP copied to clipboard: {localIPAddress}");
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
        }
    }
}
