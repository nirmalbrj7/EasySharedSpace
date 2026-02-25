using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace EasySharedSpace
{
    /// <summary>
    /// Simple UI for managing shared space connection and status.
    /// </summary>
    public class SharedSpaceUI : MonoBehaviour
    {
        [Header("Connection UI")]
        public GameObject connectionPanel;
        public InputField ipInput;
        public Button hostButton;
        public Button joinButton;
        public Text statusText;

        [Header("In-Game UI")]
        public GameObject inGamePanel;
        public Text playerCountText;
        public Text anchorCountText;
        public Button disconnectButton;

        [Header("Settings")]
        public string defaultIp = "127.0.0.1";
        public bool showDebugInfo = true;

        private void Start()
        {
            // Set default IP
            if (ipInput != null)
            {
                ipInput.text = defaultIp;
            }

            // Setup buttons
            if (hostButton != null)
                hostButton.onClick.AddListener(OnHostClicked);

            if (joinButton != null)
                joinButton.onClick.AddListener(OnJoinClicked);

            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(OnDisconnectClicked);

            // Show connection panel initially
            ShowConnectionPanel();

            // Subscribe to events
            if (SharedSpaceManager.Instance != null)
            {
                SharedSpaceManager.Instance.OnSharedSpaceReady += OnSpaceReady;
                SharedSpaceManager.Instance.OnPlayerJoined += OnPlayerJoined;
                SharedSpaceManager.Instance.OnPlayerLeft += OnPlayerLeft;
            }
        }

        private void Update()
        {
            if (showDebugInfo && inGamePanel != null && inGamePanel.activeSelf)
            {
                UpdateDebugInfo();
            }
        }

        private void OnDestroy()
        {
            if (SharedSpaceManager.Instance != null)
            {
                SharedSpaceManager.Instance.OnSharedSpaceReady -= OnSpaceReady;
                SharedSpaceManager.Instance.OnPlayerJoined -= OnPlayerJoined;
                SharedSpaceManager.Instance.OnPlayerLeft -= OnPlayerLeft;
            }
        }

        private void OnHostClicked()
        {
            SharedSpaceManager.Instance?.StartHost();
            UpdateStatus("Starting host...");
        }

        private void OnJoinClicked()
        {
            string ip = ipInput != null ? ipInput.text : defaultIp;
            SharedSpaceManager.Instance?.JoinAsClient(ip);
            UpdateStatus($"Connecting to {ip}...");
        }

        private void OnDisconnectClicked()
        {
            SharedSpaceManager.Instance?.Disconnect();
            ShowConnectionPanel();
        }

        private void OnSpaceReady()
        {
            ShowInGamePanel();
        }

        private void OnPlayerJoined(ulong clientId)
        {
            UpdateStatus($"Player {clientId} joined!");
        }

        private void OnPlayerLeft(ulong clientId)
        {
            UpdateStatus($"Player {clientId} left!");
        }

        private void ShowConnectionPanel()
        {
            if (connectionPanel != null) connectionPanel.SetActive(true);
            if (inGamePanel != null) inGamePanel.SetActive(false);
        }

        private void ShowInGamePanel()
        {
            if (connectionPanel != null) connectionPanel.SetActive(false);
            if (inGamePanel != null) inGamePanel.SetActive(true);
            UpdateStatus("Connected!");
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[EasySharedSpace] {message}");
        }

        private void UpdateDebugInfo()
        {
            if (SharedSpaceManager.Instance != null)
            {
                int playerCount = SharedSpaceManager.Instance.ConnectedPlayers.Count + 1; // +1 for local
                if (playerCountText != null)
                    playerCountText.text = $"Players: {playerCount}";
            }

            if (SpatialAnchorManager.Instance != null)
            {
                int anchorCount = SpatialAnchorManager.Instance.AnchorCount;
                if (anchorCountText != null)
                    anchorCountText.text = $"Anchors: {anchorCount}";
            }
        }
    }
}
