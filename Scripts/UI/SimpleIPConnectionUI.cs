using UnityEngine;
using UnityEngine.UI;
using EasySharedSpace;

namespace EasySharedSpace.UI
{
    /// <summary>
    /// Simple UI for IP-based connection.
    /// Attach to a Canvas in your scene.
    /// </summary>
    public class SimpleIPConnectionUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject connectionPanel;
        public GameObject inGamePanel;
        
        [Header("Connection Panel")]
        public Text ipDisplayText;
        public InputField ipInputField;
        public Button hostButton;
        public Button joinButton;
        public Button copyIPButton;
        public Text statusText;
        
        [Header("In-Game Panel")]
        public Text connectedPlayersText;
        public Button disconnectButton;
        public Button spawnObjectButton;
        
        [Header("Visual Feedback")]
        public Color normalColor = Color.white;
        public Color connectedColor = Color.green;
        public Color errorColor = Color.red;

        private SimpleIPNetworkManager _networkManager;
        private SharedSpaceManager _spaceManager;

        private void Start()
        {
            _networkManager = SimpleIPNetworkManager.Instance;
            _spaceManager = SharedSpaceManager.Instance;

            SetupUI();
            ShowConnectionPanel();
        }

        private void SetupUI()
        {
            // Display local IP
            if (ipDisplayText != null && _networkManager != null)
            {
                ipDisplayText.text = $"Your IP: {_networkManager.LocalIPAddress}";
            }

            // Setup buttons
            if (hostButton != null)
                hostButton.onClick.AddListener(OnHostClicked);
            
            if (joinButton != null)
                joinButton.onClick.AddListener(OnJoinClicked);
            
            if (copyIPButton != null)
                copyIPButton.onClick.AddListener(OnCopyIPClicked);
            
            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(OnDisconnectClicked);
            
            if (spawnObjectButton != null)
                spawnObjectButton.onClick.AddListener(OnSpawnObjectClicked);

            // Set default IP for testing
            if (ipInputField != null)
            {
                ipInputField.text = "127.0.0.1";
            }
        }

        private void Update()
        {
            UpdateStatus();
            UpdatePlayerList();
        }

        #region Button Handlers

        private void OnHostClicked()
        {
            if (_networkManager != null)
            {
                _networkManager.StartHost();
                ShowInGamePanel();
            }
        }

        private void OnJoinClicked()
        {
            if (_networkManager != null && ipInputField != null)
            {
                string ip = ipInputField.text.Trim();
                if (string.IsNullOrEmpty(ip))
                {
                    SetStatus("Please enter an IP address!", errorColor);
                    return;
                }
                
                _networkManager.JoinAsClient(ip);
                ShowInGamePanel();
            }
        }

        private void OnCopyIPClicked()
        {
            if (_networkManager != null)
            {
                _networkManager.CopyIPToClipboard();
                SetStatus("IP copied to clipboard!", normalColor);
            }
        }

        private void OnDisconnectClicked()
        {
            if (_networkManager != null)
            {
                _networkManager.Disconnect();
                ShowConnectionPanel();
            }
        }

        private void OnSpawnObjectClicked()
        {
            // Spawn a test object
            if (SharedObjectSpawner.Instance != null)
            {
                Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 2f;
                SharedObjectSpawner.Instance.SpawnObject(0, spawnPos, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("[SimpleIPConnectionUI] No SharedObjectSpawner found in scene!");
            }
        }

        #endregion

        #region UI Updates

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
            if (statusText == null || _networkManager == null) return;

            statusText.text = $"Status: {_networkManager.ConnectionStatus}";
            
            if (_networkManager.IsConnected)
            {
                statusText.color = connectedColor;
            }
            else
            {
                statusText.color = normalColor;
            }
        }

        private void SetStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }

        private void UpdatePlayerList()
        {
            if (connectedPlayersText == null || _spaceManager == null) return;

            int playerCount = _spaceManager.ConnectedPlayers.Count;
            string text = $"Connected Players: {playerCount}\n";
            
            foreach (var player in _spaceManager.ConnectedPlayers)
            {
                string playerName = player.Value.PlayerName.Value;
                bool isLocal = player.Value.IsLocalPlayer;
                text += $"- {playerName} {(isLocal ? "(You)" : "")}\n";
            }
            
            connectedPlayersText.text = text;
        }

        #endregion
    }
}
