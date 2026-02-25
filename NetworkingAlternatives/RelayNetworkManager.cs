using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using System.Threading.Tasks;

namespace EasySharedSpace.Networking
{
    /// <summary>
    /// Relay-based network manager - NO STATIC IP NEEDED!
    /// Uses Unity Relay service for connection through firewall/NAT
    /// </summary>
    public class RelayNetworkManager : MonoBehaviour
    {
        public static RelayNetworkManager Instance { get; private set; }

        [Header("Unity Services")]
        public bool autoInitializeServices = true;
        public int maxConnections = 4;

        [Header("Events")]
        public Action<string> OnJoinCodeCreated;
        public Action OnRelayConnected;
        public Action<string> OnRelayError;

        private bool _isInitialized = false;
        private string _currentJoinCode = "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        #region Initialization

        public async void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                #if ENABLE_UNITY_SERVICES
                await Unity.Services.Core.UnityServices.InitializeAsync();
                
                if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
                {
                    await Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                
                _isInitialized = true;
                Debug.Log("[RelayNetworkManager] Unity Services initialized successfully");
                #else
                throw new Exception("Unity Services not enabled. Add via Package Manager.");
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayNetworkManager] Initialization failed: {e.Message}");
                OnRelayError?.Invoke(e.Message);
            }
        }

        #endregion

        #region Host with Relay

        /// <summary>
        /// Start a host using Unity Relay - creates a join code for others
        /// </summary>
        public async Task<string> StartRelayHostAsync(int maxPlayers = 4)
        {
            if (!_isInitialized) Initialize();

            try
            {
                #if ENABLE_UNITY_SERVICES
                // Create allocation for max players
                var allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(maxPlayers);
                
                // Get join code
                _currentJoinCode = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                
                // Setup transport with relay data
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetRelayServerData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );
                
                // Start host
                NetworkManager.Singleton.StartHost();
                
                OnJoinCodeCreated?.Invoke(_currentJoinCode);
                OnRelayConnected?.Invoke();
                
                Debug.Log($"[RelayNetworkManager] Host started. Join Code: {_currentJoinCode}");
                return _currentJoinCode;
                #else
                throw new Exception("Unity Services not enabled");
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayNetworkManager] Host failed: {e.Message}");
                OnRelayError?.Invoke(e.Message);
                return null;
            }
        }

        #endregion

        #region Client with Relay

        /// <summary>
        /// Join a host using relay join code - NO IP ADDRESS NEEDED!
        /// </summary>
        public async Task<bool> JoinRelayAsync(string joinCode)
        {
            if (!_isInitialized) Initialize();

            try
            {
                #if ENABLE_UNITY_SERVICES
                // Join allocation using code
                var joinAllocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(joinCode);
                
                // Setup transport
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetRelayServerData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData
                );
                
                // Start client
                NetworkManager.Singleton.StartClient();
                
                _currentJoinCode = joinCode;
                OnRelayConnected?.Invoke();
                
                Debug.Log($"[RelayNetworkManager] Joined via code: {joinCode}");
                return true;
                #else
                throw new Exception("Unity Services not enabled");
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayNetworkManager] Join failed: {e.Message}");
                OnRelayError?.Invoke(e.Message);
                return false;
            }
        }

        #endregion

        #region Utility

        public string GetCurrentJoinCode() => _currentJoinCode;

        public void Disconnect()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            _currentJoinCode = "";
        }

        #endregion
    }
}
