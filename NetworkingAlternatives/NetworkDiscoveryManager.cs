using UnityEngine;
using Unity.Netcode;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace EasySharedSpace.Networking
{
    /// <summary>
    /// Local network discovery - finds hosts on same WiFi/LAN without IP
    /// Uses UDP broadcast for discovery
    /// </summary>
    public class NetworkDiscoveryManager : MonoBehaviour
    {
        public static NetworkDiscoveryManager Instance { get; private set; }

        [Header("Discovery Settings")]
        public int discoveryPort = 47777;
        public float broadcastInterval = 2f;
        public float hostTimeout = 10f;

        [Header("Events")]
        public System.Action<DiscoveredHost> OnHostDiscovered;
        public System.Action<DiscoveredHost> OnHostLost;

        private UdpClient _udpClient;
        private Thread _receiveThread;
        private bool _isRunning = false;
        private bool _isBroadcasting = false;
        private float _lastBroadcastTime;
        private string _broadcastData = "";

        // Discovered hosts
        private Dictionary<string, DiscoveredHost> _discoveredHosts = new Dictionary<string, DiscoveredHost>();
        private Dictionary<string, float> _lastSeenTime = new Dictionary<string, float>();

        public class DiscoveredHost
        {
            public string HostName;
            public string IPAddress;
            public int Port;
            public string SessionName;
            public int PlayerCount;
            public int MaxPlayers;
            public float LastSeen;
        }

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

        private void Update()
        {
            if (_isBroadcasting && Time.time - _lastBroadcastTime > broadcastInterval)
            {
                BroadcastPresence();
                _lastBroadcastTime = Time.time;
            }

            // Check for timed out hosts
            List<string> timedOutHosts = new List<string>();
            foreach (var kvp in _lastSeenTime)
            {
                if (Time.time - kvp.Value > hostTimeout)
                {
                    timedOutHosts.Add(kvp.Key);
                }
            }

            foreach (var hostId in timedOutHosts)
            {
                if (_discoveredHosts.ContainsKey(hostId))
                {
                    OnHostLost?.Invoke(_discoveredHosts[hostId]);
                    _discoveredHosts.Remove(hostId);
                    _lastSeenTime.Remove(hostId);
                }
            }
        }

        #region Server (Host) Side

        /// <summary>
        /// Start broadcasting this host's presence on the network
        /// </summary>
        public void StartBroadcasting(string sessionName, int port)
        {
            _broadcastData = $"ESSHOST|{sessionName}|{port}|{SystemInfo.deviceName}";
            _isBroadcasting = true;
            
            StartListening();
            
            Debug.Log($"[NetworkDiscovery] Broadcasting: {_broadcastData}");
        }

        public void StopBroadcasting()
        {
            _isBroadcasting = false;
        }

        private void BroadcastPresence()
        {
            try
            {
                using (UdpClient client = new UdpClient())
                {
                    client.EnableBroadcast = true;
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, discoveryPort);
                    byte[] data = Encoding.UTF8.GetBytes(_broadcastData);
                    client.Send(data, data.Length, endPoint);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[NetworkDiscovery] Broadcast failed: {e.Message}");
            }
        }

        #endregion

        #region Client Side

        /// <summary>
        /// Start listening for host broadcasts
        /// </summary>
        public void StartDiscovery()
        {
            StartListening();
            Debug.Log("[NetworkDiscovery] Started listening for hosts...");
        }

        public void StopDiscovery()
        {
            _isRunning = false;
            _udpClient?.Close();
            _receiveThread?.Join(100);
        }

        private void StartListening()
        {
            if (_isRunning) return;

            _isRunning = true;
            _receiveThread = new Thread(new ThreadStart(ReceiveData));
            _receiveThread.IsBackground = true;
            _receiveThread.Start();
        }

        private void ReceiveData()
        {
            try
            {
                _udpClient = new UdpClient(discoveryPort);
                _udpClient.EnableBroadcast = true;

                while (_isRunning)
                {
                    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _udpClient.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(data);

                    ProcessDiscoveryMessage(message, remoteEndPoint.Address.ToString());
                }
            }
            catch (SocketException)
            {
                // Socket closed, expected when stopping
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NetworkDiscovery] Receive error: {e.Message}");
            }
        }

        private void ProcessDiscoveryMessage(string message, string ipAddress)
        {
            if (!message.StartsWith("ESSHOST|")) return;

            string[] parts = message.Split('|');
            if (parts.Length < 4) return;

            string sessionName = parts[1];
            int port = int.Parse(parts[2]);
            string hostName = parts[3];

            string hostId = $"{ipAddress}:{port}";

            DiscoveredHost host = new DiscoveredHost
            {
                HostName = hostName,
                IPAddress = ipAddress,
                Port = port,
                SessionName = sessionName,
                LastSeen = Time.time
            };

            bool isNewHost = !_discoveredHosts.ContainsKey(hostId);
            _discoveredHosts[hostId] = host;
            _lastSeenTime[hostId] = Time.time;

            if (isNewHost)
            {
                UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                {
                    OnHostDiscovered?.Invoke(host);
                });
            }
        }

        #endregion

        #region Public API

        public List<DiscoveredHost> GetDiscoveredHosts()
        {
            return new List<DiscoveredHost>(_discoveredHosts.Values);
        }

        public void ClearDiscoveredHosts()
        {
            _discoveredHosts.Clear();
            _lastSeenTime.Clear();
        }

        #endregion

        private void OnDestroy()
        {
            StopDiscovery();
        }
    }

    /// <summary>
    /// Helper to run actions on Unity main thread from background threads
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MainThreadDispatcher");
                    _instance = go.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private System.Collections.Generic.Queue<System.Action> _actions = new System.Collections.Generic.Queue<System.Action>();
        private readonly object _lock = new object();

        public void Enqueue(System.Action action)
        {
            lock (_lock)
            {
                _actions.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (_lock)
            {
                while (_actions.Count > 0)
                {
                    _actions.Dequeue()?.Invoke();
                }
            }
        }
    }
}
