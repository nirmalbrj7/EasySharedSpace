using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasySharedSpace.Networking
{
    /// <summary>
    /// Cloud-based matchmaking service - NO IP NEEDED!
    /// Uses external services for matchmaking and connection
    /// </summary>
    public class MatchmakingService : MonoBehaviour
    {
        public static MatchmakingService Instance { get; private set; }

        [Header("Service Provider")]
        public MatchmakingProvider provider = MatchmakingProvider.UnityGamingServices;

        [Header("Room Settings")]
        public string roomName = "EasySharedSpace";
        public int maxPlayers = 4;
        public bool isPrivate = false;

        public enum MatchmakingProvider
        {
            UnityGamingServices,    // Unity Lobby + Relay
            PhotonPUN,              // Photon Unity Networking
            PhotonFusion,           // Photon Fusion
            MirrorListServer,       // Mirror's built-in list server
            Steamworks,             // Steam P2P
            EpicOnlineServices,     // EOS P2P
            PlayFab,                // PlayFab multiplayer
            Custom                  // Your own backend
        }

        // Events
        public Action<List<RoomInfo>> OnRoomListUpdated;
        public Action<string> OnJoinedRoom;
        public Action<string> OnLeftRoom;
        public Action<string> OnError;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        #region Unity Gaming Services (Lobby + Relay)

        #if ENABLE_UNITY_SERVICES
        
        public async void CreateRoomWithUGS(string roomName, bool isPrivate = false)
        {
            try
            {
                // Create lobby
                var lobbyOptions = new Unity.Services.Lobbies.CreateLobbyOptions
                {
                    IsPrivate = isPrivate,
                    Player = new Unity.Services.Lobbies.Player(id: Unity.Services.Authentication.AuthenticationService.Instance.PlayerId)
                };

                var lobby = await Unity.Services.Lobbies.LobbyService.Instance.CreateLobbyAsync(
                    roomName, maxPlayers, lobbyOptions);

                // Create relay allocation and store join code in lobby data
                var relayManager = RelayNetworkManager.Instance;
                string joinCode = await relayManager.StartRelayHostAsync(maxPlayers);

                // Update lobby with relay code
                await Unity.Services.Lobbies.LobbyService.Instance.UpdateLobbyAsync(
                    lobby.Id, new Unity.Services.Lobbies.UpdateLobbyOptions
                    {
                        Data = new Dictionary<string, Unity.Services.Lobbies.DataObject>
                        {
                            { "RelayCode", new Unity.Services.Lobbies.DataObject(
                                Unity.Services.Lobbies.DataObject.VisibilityOptions.Public, joinCode) }
                        }
                    });

                OnJoinedRoom?.Invoke(lobby.Id);
                Debug.Log($"[Matchmaking] Room created: {lobby.Id}, Relay: {joinCode}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Matchmaking] Create room failed: {e.Message}");
                OnError?.Invoke(e.Message);
            }
        }

        public async void JoinRoomWithUGS(string lobbyId)
        {
            try
            {
                // Join lobby
                var lobby = await Unity.Services.Lobbies.LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
                
                // Get relay code from lobby data
                if (lobby.Data != null && lobby.Data.ContainsKey("RelayCode"))
                {
                    string relayCode = lobby.Data["RelayCode"].Value;
                    
                    // Join via relay
                    var relayManager = RelayNetworkManager.Instance;
                    await relayManager.JoinRelayAsync(relayCode);
                    
                    OnJoinedRoom?.Invoke(lobbyId);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Matchmaking] Join room failed: {e.Message}");
                OnError?.Invoke(e.Message);
            }
        }

        public async void ListRoomsWithUGS()
        {
            try
            {
                var queryOptions = new Unity.Services.Lobbies.QueryLobbiesOptions();
                var response = await Unity.Services.Lobbies.LobbyService.Instance.QueryLobbiesAsync(queryOptions);

                List<RoomInfo> rooms = new List<RoomInfo>();
                foreach (var lobby in response.Results)
                {
                    rooms.Add(new RoomInfo
                    {
                        RoomId = lobby.Id,
                        RoomName = lobby.Name,
                        PlayerCount = lobby.Players.Count,
                        MaxPlayers = lobby.MaxPlayers,
                        IsPrivate = lobby.IsPrivate
                    });
                }

                OnRoomListUpdated?.Invoke(rooms);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Matchmaking] List rooms failed: {e.Message}");
                OnError?.Invoke(e.Message);
            }
        }

        #endif

        #endregion

        #region Photon PUN (Alternative)

        public void CreateRoomWithPhoton(string roomName)
        {
            #if PHOTON_UNITY_NETWORKING
            Photon.Pun.PhotonNetwork.CreateRoom(roomName, new Photon.Realtime.RoomOptions 
            { 
                MaxPlayers = (byte)maxPlayers 
            });
            #else
            Debug.LogWarning("[Matchmaking] Photon PUN not installed. Install via Package Manager.");
            #endif
        }

        public void JoinRoomWithPhoton(string roomName)
        {
            #if PHOTON_UNITY_NETWORKING
            Photon.Pun.PhotonNetwork.JoinRoom(roomName);
            #else
            Debug.LogWarning("[Matchmaking] Photon PUN not installed.");
            #endif
        }

        #endregion

        #region Steamworks (Steam P2P)

        public void CreateRoomWithSteam()
        {
            #if STEAMWORKS_NET
            // Steam P2P uses Steam's relay servers - no IP needed!
            Steamworks.SteamMatchmaking.CreateLobby(
                Steamworks.ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
            #else
            Debug.LogWarning("[Matchmaking] Steamworks.NET not installed.");
            #endif
        }

        public void JoinRoomWithSteam(ulong steamId)
        {
            #if STEAMWORKS_NET
            Steamworks.SteamMatchmaking.JoinLobby(new Steamworks.CSteamID(steamId));
            #else
            Debug.LogWarning("[Matchmaking] Steamworks.NET not installed.");
            #endif
        }

        #endregion

        #region Utility Methods

        public void QuickMatch()
        {
            switch (provider)
            {
                case MatchmakingProvider.UnityGamingServices:
                    // Try to join existing room or create new one
                    #if ENABLE_UNITY_SERVICES
                    ListRoomsWithUGS();
                    #endif
                    break;
                    
                case MatchmakingProvider.PhotonPUN:
                    #if PHOTON_UNITY_NETWORKING
                    Photon.Pun.PhotonNetwork.JoinRandomRoom();
                    #endif
                    break;
                    
                default:
                    Debug.Log($"[Matchmaking] Quick match not implemented for {provider}");
                    break;
            }
        }

        #endregion
    }

    [Serializable]
    public class RoomInfo
    {
        public string RoomId;
        public string RoomName;
        public int PlayerCount;
        public int MaxPlayers;
        public bool IsPrivate;
        public Dictionary<string, string> CustomData;
    }
}
