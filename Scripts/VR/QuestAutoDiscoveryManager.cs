using UnityEngine;
using Unity.Netcode;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace EasySharedSpace.VR
{
    /// <summary>
    /// Auto-discovery for Quest VR headsets on same WiFi network.
    /// NO TYPING REQUIRED! Headsets find each other automatically.
    /// </summary>
    public class QuestAutoDiscoveryManager : MonoBehaviour
    {
        [Header("Discovery Settings")]
        public int discoveryPort = 47777;
        public float broadcastInterval = 1f;
        public float hostTimeout = 5f;

        [Header("VR UI")]
        public Transform vrUIAnchor;
        public GameObject hostButtonPrefab;
        public Transform hostListContainer;
        public float buttonSpacing = 0.1f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnSearchingStarted;
        public UnityEngine.Events.UnityEvent OnHostFound;
        public UnityEngine.Events.UnityEvent OnConnected;

        private UdpClient _udpClient;
        private Thread _receiveThread;
        private bool _isRunning = false;
        private bool _isBroadcasting = false;
        private float _lastBroadcastTime;
        private string _sessionName = "QuestRoom";

        // Discovered hosts
        private Dictionary<string, DiscoveredHost> _discoveredHosts = new Dictionary<string, DiscoveredHost>();
        private Dictionary<string, float> _lastSeenTime = new Dictionary<string, float>();
        private List<GameObject> _hostButtons = new List<GameObject>();

        public class DiscoveredHost
        {
            public string HostId;
            public string HostName;
            public string IPAddress;
            public int Port;
            public string SessionName;
            public int PlayerCount;
            public float LastSeen;
        }

        public static QuestAutoDiscoveryManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _sessionName = "Quest-" + Random.Range(1000, 9999);
        }

        private void Start()
        {
            // Auto-start discovery when app launches
            StartDiscovery();
        }

        private void Update()
        {
            // Handle broadcasting for hosts
            if (_isBroadcasting && Time.time - _lastBroadcastTime > broadcastInterval)
            {
                BroadcastPresence();
                _lastBroadcastTime = Time.time;
            }

            // Check for timed out hosts
            CheckForTimeouts();

            // Update UI
            UpdateHostListUI();
        }

        #region Host Mode (Broadcasting)

        /// <summary>
        /// Start as host and broadcast presence to network
        /// </summary>
        public void StartAsHost()
        {
            // Start the network host
            SimpleIPNetworkManager.Instance?.StartHost();

            // Start broadcasting
            _sessionName = "Quest-" + Random.Range(1000, 9999);
            _isBroadcasting = true;
            _lastBroadcastTime = Time.time;

            StartListening();

            Debug.Log($"[QuestAutoDiscovery] Hosting as: {_sessionName}");
            Debug.Log($"[QuestAutoDiscovery] IP: {GetLocalIPAddress()}");
        }

        private void BroadcastPresence()
        {
            try
            {
                using (UdpClient client = new UdpClient())
                {
                    client.EnableBroadcast = true;
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, discoveryPort);
                    
                    // Broadcast format: QUESTHOST|SessionName|IP|Port|PlayerCount
                    string message = $"QUESTHOST|{_sessionName}|{GetLocalIPAddress()}|7777|{GetPlayerCount()}";
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    
                    client.Send(data, data.Length, endPoint);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[QuestAutoDiscovery] Broadcast failed: {e.Message}");
            }
        }

        #endregion

        #region Client Mode (Discovering)

        /// <summary>
        /// Start searching for hosts on the network
        /// </summary>
        public void StartDiscovery()
        {
            _isRunning = true;
            StartListening();
            OnSearchingStarted?.Invoke();
            Debug.Log("[QuestAutoDiscovery] Searching for hosts...");
        }

        public void StopDiscovery()
        {
            _isRunning = false;
            _udpClient?.Close();
            _receiveThread?.Join(100);
        }

        private void StartListening()
        {
            if (_receiveThread != null && _receiveThread.IsAlive) return;

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

                    ProcessDiscoveryMessage(message);
                }
            }
            catch (SocketException)
            {
                // Socket closed, expected
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuestAutoDiscovery] Receive error: {e.Message}");
            }
        }

        private void ProcessDiscoveryMessage(string message)
        {
            if (!message.StartsWith("QUESTHOST|")) return;

            string[] parts = message.Split('|');
            if (parts.Length < 5) return;

            string sessionName = parts[1];
            string ipAddress = parts[2];
            int port = int.Parse(parts[3]);
            int playerCount = int.Parse(parts[4]);

            string hostId = $"{ipAddress}:{port}";

            DiscoveredHost host = new DiscoveredHost
            {
                HostId = hostId,
                HostName = sessionName,
                IPAddress = ipAddress,
                Port = port,
                SessionName = sessionName,
                PlayerCount = playerCount,
                LastSeen = Time.time
            };

            bool isNew = !_discoveredHosts.ContainsKey(hostId);
            _discoveredHosts[hostId] = host;
            _lastSeenTime[hostId] = Time.time;

            if (isNew)
            {
                UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                {
                    OnHostFound?.Invoke();
                    Debug.Log($"[QuestAutoDiscovery] Found host: {sessionName} at {ipAddress}");
                });
            }
        }

        #endregion

        #region Connection

        /// <summary>
        /// Connect to a discovered host by ID
        /// </summary>
        public void ConnectToHost(string hostId)
        {
            if (_discoveredHosts.TryGetValue(hostId, out DiscoveredHost host))
            {
                Debug.Log($"[QuestAutoDiscovery] Connecting to {host.SessionName} at {host.IPAddress}");
                SimpleIPNetworkManager.Instance?.JoinAsClient(host.IPAddress);
                OnConnected?.Invoke();
            }
        }

        /// <summary>
        /// Connect to first available host (easiest for users)
        /// </summary>
        public void ConnectToFirstHost()
        {
            if (_discoveredHosts.Count > 0)
            {
                var firstHost = _discoveredHosts.Values.First();
                ConnectToHost(firstHost.HostId);
            }
        }

        #endregion

        #region UI Management

        private void UpdateHostListUI()
        {
            if (hostListContainer == null) return;

            // Clear old buttons if count changed
            if (_hostButtons.Count != _discoveredHosts.Count)
            {
                ClearHostButtons();
                CreateHostButtons();
            }
        }

        private void CreateHostButtons()
        {
            if (hostButtonPrefab == null) return;

            int index = 0;
            foreach (var host in _discoveredHosts.Values)
            {
                GameObject button = Instantiate(hostButtonPrefab, hostListContainer);
                button.transform.localPosition = new Vector3(0, index * -buttonSpacing, 0);
                
                // Set button text
                var textMesh = button.GetComponentInChildren<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = $"{host.SessionName}\n{host.PlayerCount} players";
                }

                // Make button clickable
                var hostId = host.HostId; // Capture for closure
                var clickable = button.GetComponent<QuestUIButton>();
                if (clickable != null)
                {
                    clickable.OnClick += () => ConnectToHost(hostId);
                }

                _hostButtons.Add(button);
                index++;
            }
        }

        private void ClearHostButtons()
        {
            foreach (var button in _hostButtons)
            {
                if (button != null) Destroy(button);
            }
            _hostButtons.Clear();
        }

        #endregion

        #region Utilities

        private void CheckForTimeouts()
        {
            List<string> timedOut = new List<string>();
            
            foreach (var kvp in _lastSeenTime)
            {
                if (Time.time - kvp.Value > hostTimeout)
                {
                    timedOut.Add(kvp.Key);
                }
            }

            foreach (var hostId in timedOut)
            {
                _discoveredHosts.Remove(hostId);
                _lastSeenTime.Remove(hostId);
            }
        }

        private int GetPlayerCount()
        {
            if (NetworkManager.Singleton != null)
            {
                return NetworkManager.Singleton.ConnectedClients.Count;
            }
            return 1; // Just the host
        }

        private string GetLocalIPAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        public List<DiscoveredHost> GetDiscoveredHosts()
        {
            return new List<DiscoveredHost>(_discoveredHosts.Values);
        }

        public bool HasDiscoveredHosts => _discoveredHosts.Count > 0;

        #endregion

        private void OnDestroy()
        {
            StopDiscovery();
        }
    }

    /// <summary>
    /// Helper component for VR UI buttons
    /// </summary>
    public class QuestUIButton : MonoBehaviour
    {
        public System.Action OnClick;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Hand") || other.CompareTag("FingerTip"))
            {
                OnClick?.Invoke();
            }
        }
    }
}
