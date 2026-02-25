using UnityEngine;
using Unity.Netcode;
using EasySharedSpace;
using System.Collections.Generic;

/// <summary>
/// Enhanced demo controller with multiple networking options
/// Supports: Direct IP, Relay, Auto-discovery, and WebSocket connections
/// </summary>
public class EnhancedDemoController : MonoBehaviour
{
    [Header("Network Mode")]
    public NetworkConnectionMode connectionMode = NetworkConnectionMode.DirectIP;

    [Header("Direct IP Settings")]
    public string hostIP = "127.0.0.1";
    public ushort port = 7777;

    [Header("Relay Settings")]
    public string relayJoinCode = "";

    [Header("Auto Discovery")]
    public bool enableAutoDiscovery = true;
    public float discoveryInterval = 2f;

    [Header("UI References")]
    public GameObject connectionPanel;
    public GameObject inGamePanel;
    public UnityEngine.UI.Text statusText;
    public UnityEngine.UI.InputField ipInputField;
    public UnityEngine.UI.InputField relayCodeInput;
    public UnityEngine.UI.Dropdown modeDropdown;

    [Header("Demo Features")]
    public bool enablePlayerList = true;
    public bool enableChat = true;
    public bool enableObjectSpawner = true;

    private SharedSpaceManager _spaceManager;
    private Dictionary<ulong, string> _discoveredHosts = new Dictionary<ulong, string>();
    private float _lastDiscoveryTime;

    public enum NetworkConnectionMode
    {
        DirectIP,
        UnityRelay,
        AutoDiscovery,
        WebSocket,
        SteamNetworking,
        EpicOnlineServices
    }

    private void Start()
    {
        _spaceManager = SharedSpaceManager.Instance;
        SetupUI();
        ShowConnectionPanel();
    }

    private void Update()
    {
        // Auto-discovery logic
        if (connectionMode == NetworkConnectionMode.AutoDiscovery && enableAutoDiscovery)
        {
            if (Time.time - _lastDiscoveryTime > discoveryInterval)
            {
                SearchForHosts();
                _lastDiscoveryTime = Time.time;
            }
        }

        UpdateStatus();
    }

    #region UI Setup

    private void SetupUI()
    {
        if (modeDropdown != null)
        {
            modeDropdown.ClearOptions();
            modeDropdown.AddOptions(new List<string> {
                "Direct IP",
                "Unity Relay",
                "Auto Discovery",
                "WebSocket",
                "Steam Networking",
                "EOS"
            });
            modeDropdown.onValueChanged.AddListener(OnModeChanged);
        }
    }

    private void OnModeChanged(int index)
    {
        connectionMode = (NetworkConnectionMode)index;
        UpdateUIForMode();
    }

    private void UpdateUIForMode()
    {
        // Show/hide relevant input fields based on mode
        if (ipInputField != null)
            ipInputField.gameObject.SetActive(connectionMode == NetworkConnectionMode.DirectIP);
        
        if (relayCodeInput != null)
            relayCodeInput.gameObject.SetActive(connectionMode == NetworkConnectionMode.UnityRelay);
    }

    #endregion

    #region Connection Methods

    public void StartAsHost()
    {
        switch (connectionMode)
        {
            case NetworkConnectionMode.DirectIP:
                StartDirectHost();
                break;
            case NetworkConnectionMode.UnityRelay:
                StartRelayHost();
                break;
            case NetworkConnectionMode.AutoDiscovery:
                StartDiscoveryHost();
                break;
            case NetworkConnectionMode.WebSocket:
                StartWebSocketHost();
                break;
            default:
                Debug.LogWarning($"[EnhancedDemo] Mode {connectionMode} not fully implemented yet");
                StartDirectHost();
                break;
        }
    }

    public void JoinAsClient()
    {
        switch (connectionMode)
        {
            case NetworkConnectionMode.DirectIP:
                JoinDirectIP();
                break;
            case NetworkConnectionMode.UnityRelay:
                JoinRelay();
                break;
            case NetworkConnectionMode.AutoDiscovery:
                JoinDiscoveredHost();
                break;
            case NetworkConnectionMode.WebSocket:
                JoinWebSocket();
                break;
            default:
                Debug.LogWarning($"[EnhancedDemo] Mode {connectionMode} not fully implemented yet");
                JoinDirectIP();
                break;
        }
    }

    #endregion

    #region Direct IP (Original Method)

    private void StartDirectHost()
    {
        if (_spaceManager != null)
        {
            _spaceManager.StartHost();
            ShowInGamePanel();
            Debug.Log($"[EnhancedDemo] Direct IP Host started on port {port}");
        }
    }

    private void JoinDirectIP()
    {
        if (_spaceManager != null)
        {
            string ip = ipInputField != null ? ipInputField.text : hostIP;
            _spaceManager.JoinAsClient(ip, port);
            ShowInGamePanel();
            Debug.Log($"[EnhancedDemo] Connecting to {ip}:{port}");
        }
    }

    #endregion

    #region Unity Relay (No IP Needed!)

    private async void StartRelayHost()
    {
        #if ENABLE_UNITY_SERVICES
        try
        {
            // Initialize Unity Services
            await Unity.Services.Core.UnityServices.InitializeAsync();
            
            // Sign in anonymously
            await Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            // Create Relay allocation
            var allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(4);
            var joinCode = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            // Setup Relay transport
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
            
            // Start host
            _spaceManager.StartHost();
            
            // Display join code
            if (relayCodeInput != null)
                relayCodeInput.text = joinCode;
            
            ShowInGamePanel();
            Debug.Log($"[EnhancedDemo] Relay Host started. Join Code: {joinCode}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhancedDemo] Relay error: {e.Message}");
        }
        #else
        Debug.LogWarning("[EnhancedDemo] Unity Services not enabled. Enable in Package Manager.");
        #endif
    }

    private async void JoinRelay()
    {
        #if ENABLE_UNITY_SERVICES
        try
        {
            string code = relayCodeInput != null ? relayCodeInput.text : relayJoinCode;
            
            await Unity.Services.Core.UnityServices.InitializeAsync();
            await Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            // Join via code
            var joinAllocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(code);
            
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );
            
            _spaceManager.JoinAsClient();
            ShowInGamePanel();
            Debug.Log($"[EnhancedDemo] Joining via Relay code: {code}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhancedDemo] Relay join error: {e.Message}");
        }
        #else
        Debug.LogWarning("[EnhancedDemo] Unity Services not enabled.");
        #endif
    }

    #endregion

    #region Auto Discovery (LAN)

    private void StartDiscoveryHost()
    {
        // Start normal host + broadcast presence
        StartDirectHost();
        StartCoroutine(BroadcastPresence());
    }

    private System.Collections.IEnumerator BroadcastPresence()
    {
        // Simple UDP broadcast for LAN discovery
        // In production, use a proper discovery service
        while (NetworkManager.Singleton.IsHost)
        {
            // Broadcast host info to local network
            yield return new WaitForSeconds(discoveryInterval);
        }
    }

    private void SearchForHosts()
    {
        // Listen for broadcast messages
        // Update _discoveredHosts dictionary
    }

    private void JoinDiscoveredHost()
    {
        // Connect to selected discovered host
        // For now, fallback to direct IP
        JoinDirectIP();
    }

    #endregion

    #region WebSocket (WebGL Support)

    private void StartWebSocketHost()
    {
        // For WebGL builds, use WebSocket transport
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        
        // WebSocket host typically requires a server build
        // For browser-to-browser, use a relay or matchmaking service
        Debug.Log("[EnhancedDemo] For WebSocket/WebGL, consider using:"
            + "\n  - Unity Gaming Services Relay"
            + "\n  - Photon PUN/Fusion"
            + "\n  - Mirror with WebSocket transport + relay");
        
        // Fallback to direct
        StartDirectHost();
    }

    private void JoinWebSocket()
    {
        // WebSocket client connection
        JoinDirectIP();
    }

    #endregion

    #region UI Helpers

    private void ShowConnectionPanel()
    {
        if (connectionPanel != null) connectionPanel.SetActive(true);
        if (inGamePanel != null) inGamePanel.SetActive(false);
    }

    private void ShowInGamePanel()
    {
        if (connectionPanel != null) connectionPanel.SetActive(false);
        if (inGamePanel != null) inGamePanel.SetActive(true);
    }

    private void UpdateStatus()
    {
        if (statusText == null) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            statusText.text = $"Connected as {(NetworkManager.Singleton.IsHost ? "Host" : "Client")}";
            statusText.color = Color.green;
        }
        else
        {
            statusText.text = "Disconnected";
            statusText.color = Color.gray;
        }
    }

    public void Disconnect()
    {
        if (_spaceManager != null)
        {
            _spaceManager.Disconnect();
        }
        ShowConnectionPanel();
    }

    #endregion

    #region Demo Features

    public void SpawnRandomObject()
    {
        if (SharedObjectSpawner.Instance != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * 2f + Vector3.up * 2f;
            SharedObjectSpawner.Instance.SpawnObject(0, spawnPos, Quaternion.identity);
        }
    }

    public void ResetScene()
    {
        // Remove all spawned objects
        var spawnedObjects = FindObjectsOfType<SharedGrabbableObject>();
        foreach (var obj in spawnedObjects)
        {
            if (obj.NetworkObject != null && obj.NetworkObject.IsSpawned)
            {
                obj.NetworkObject.Despawn();
            }
        }
    }

    #endregion
}
