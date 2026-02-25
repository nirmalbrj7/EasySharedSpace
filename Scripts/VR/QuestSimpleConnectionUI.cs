using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EasySharedSpace;

namespace EasySharedSpace.VR
{
    /// <summary>
    /// VR-Optimized connection UI for Quest 3.
    /// Big buttons, clear text, laser pointer support.
    /// NO KEYBOARD TYPING REQUIRED!
    /// </summary>
    public class QuestSimpleConnectionUI : MonoBehaviour
    {
        [Header("VR UI Panels")]
        public GameObject mainMenuPanel;
        public GameObject searchingPanel;
        public GameObject hostFoundPanel;
        public GameObject connectedPanel;

        [Header("Main Menu Buttons")]
        public Button hostButton;
        public Button findRoomButton;
        public Button directConnectButton;

        [Header("Searching UI")]
        public TextMeshProUGUI searchingText;
        public Button cancelSearchButton;
        public Transform hostListContainer;
        public GameObject hostButtonPrefab;

        [Header("Host Found UI")]
        public TextMeshProUGUI hostNameText;
        public Button connectToFoundHostButton;
        public Button searchAgainButton;

        [Header("Connected UI")]
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI playersText;
        public Button disconnectButton;
        public Button spawnObjectButton;

        [Header("Direct Connect (Backup)")]
        public GameObject directConnectPanel;
        public TMP_InputField ipInputField;
        public Button connectDirectButton;
        public Button backButton;

        [Header("Audio Feedback")]
        public AudioSource audioSource;
        public AudioClip buttonClickSound;
        public AudioClip successSound;
        public AudioClip errorSound;

        [Header("Laser Pointer")]
        public LineRenderer laserPointer;
        public LayerMask uiLayer;

        private QuestAutoDiscoveryManager _discoveryManager;
        private string _selectedHostId;

        private void Start()
        {
            _discoveryManager = QuestAutoDiscoveryManager.Instance;

            SetupButtons();
            ShowMainMenu();

            // Subscribe to discovery events
            if (_discoveryManager != null)
            {
                _discoveryManager.OnHostFound.AddListener(OnHostFound);
                _discoveryManager.OnConnected.AddListener(OnConnected);
            }
        }

        private void SetupButtons()
        {
            // Main menu
            if (hostButton != null)
                hostButton.onClick.AddListener(OnHostClicked);
            
            if (findRoomButton != null)
                findRoomButton.onClick.AddListener(OnFindRoomClicked);
            
            if (directConnectButton != null)
                directConnectButton.onClick.AddListener(OnDirectConnectClicked);

            // Searching
            if (cancelSearchButton != null)
                cancelSearchButton.onClick.AddListener(OnCancelSearchClicked);

            // Host found
            if (connectToFoundHostButton != null)
                connectToFoundHostButton.onClick.AddListener(OnConnectToFoundHostClicked);
            
            if (searchAgainButton != null)
                searchAgainButton.onClick.AddListener(OnSearchAgainClicked);

            // Connected
            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(OnDisconnectClicked);
            
            if (spawnObjectButton != null)
                spawnObjectButton.onClick.AddListener(OnSpawnObjectClicked);

            // Direct connect
            if (connectDirectButton != null)
                connectDirectButton.onClick.AddListener(OnConnectDirectClicked);
            
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
        }

        #region Button Handlers

        private void OnHostClicked()
        {
            PlaySound(buttonClickSound);
            
            // Start hosting
            _discoveryManager?.StartAsHost();
            
            ShowConnectedPanel();
            UpdateStatus("Hosting - Waiting for players...");
        }

        private void OnFindRoomClicked()
        {
            PlaySound(buttonClickSound);
            
            // Start searching
            _discoveryManager?.StartDiscovery();
            
            ShowSearchingPanel();
        }

        private void OnDirectConnectClicked()
        {
            PlaySound(buttonClickSound);
            ShowDirectConnectPanel();
        }

        private void OnCancelSearchClicked()
        {
            PlaySound(buttonClickSound);
            _discoveryManager?.StopDiscovery();
            ShowMainMenu();
        }

        private void OnConnectToFoundHostClicked()
        {
            PlaySound(buttonClickSound);
            
            if (!string.IsNullOrEmpty(_selectedHostId))
            {
                _discoveryManager?.ConnectToHost(_selectedHostId);
            }
        }

        private void OnSearchAgainClicked()
        {
            PlaySound(buttonClickSound);
            _discoveryManager?.StartDiscovery();
            ShowSearchingPanel();
        }

        private void OnDisconnectClicked()
        {
            PlaySound(buttonClickSound);
            SimpleIPNetworkManager.Instance?.Disconnect();
            ShowMainMenu();
        }

        private void OnSpawnObjectClicked()
        {
            PlaySound(buttonClickSound);
            
            if (SharedObjectSpawner.Instance != null)
            {
                // Spawn in front of player
                Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
                SharedObjectSpawner.Instance.SpawnObject(0, spawnPos, Quaternion.identity);
            }
        }

        private void OnConnectDirectClicked()
        {
            PlaySound(buttonClickSound);
            
            string ip = ipInputField?.text;
            if (!string.IsNullOrEmpty(ip))
            {
                SimpleIPNetworkManager.Instance?.JoinAsClient(ip);
                ShowConnectedPanel();
            }
        }

        private void OnBackClicked()
        {
            PlaySound(buttonClickSound);
            ShowMainMenu();
        }

        #endregion

        #region Event Handlers

        private void OnHostFound()
        {
            // Auto-connect if only one host found (super simple!)
            if (_discoveryManager.GetDiscoveredHosts().Count == 1)
            {
                _discoveryManager.ConnectToFirstHost();
            }
            else
            {
                // Show host selection
                ShowHostFoundPanel();
                UpdateHostList();
            }
        }

        private void OnConnected()
        {
            PlaySound(successSound);
            ShowConnectedPanel();
        }

        #endregion

        #region UI Management

        private void ShowMainMenu()
        {
            HideAllPanels();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        private void ShowSearchingPanel()
        {
            HideAllPanels();
            if (searchingPanel != null) searchingPanel.SetActive(true);
            
            if (searchingText != null)
                searchingText.text = "Looking for rooms on this WiFi...";
        }

        private void ShowHostFoundPanel()
        {
            HideAllPanels();
            if (hostFoundPanel != null) hostFoundPanel.SetActive(true);
        }

        private void ShowConnectedPanel()
        {
            HideAllPanels();
            if (connectedPanel != null) connectedPanel.SetActive(true);
        }

        private void ShowDirectConnectPanel()
        {
            HideAllPanels();
            if (directConnectPanel != null) directConnectPanel.SetActive(true);
        }

        private void HideAllPanels()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (searchingPanel != null) searchingPanel.SetActive(false);
            if (hostFoundPanel != null) hostFoundPanel.SetActive(false);
            if (connectedPanel != null) connectedPanel.SetActive(false);
            if (directConnectPanel != null) directConnectPanel.SetActive(false);
        }

        private void UpdateHostList()
        {
            // Clear existing
            foreach (Transform child in hostListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create buttons for each host
            var hosts = _discoveryManager.GetDiscoveredHosts();
            float yOffset = 0;
            
            foreach (var host in hosts)
            {
                GameObject buttonObj = Instantiate(hostButtonPrefab, hostListContainer);
                buttonObj.transform.localPosition = new Vector3(0, yOffset, 0);
                
                var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"{host.SessionName}\n{host.PlayerCount} players";
                }

                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    string hostId = host.HostId; // Capture
                    button.onClick.AddListener(() => {
                        _selectedHostId = hostId;
                        hostNameText.text = $"Join {host.SessionName}?";
                        ShowHostFoundPanel();
                    });
                }

                yOffset -= 0.15f;
            }
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void Update()
        {
            // Update player count
            if (connectedPanel != null && connectedPanel.activeSelf)
            {
                var spaceManager = SharedSpaceManager.Instance;
                if (spaceManager != null && playersText != null)
                {
                    playersText.text = $"Players: {spaceManager.ConnectedPlayers.Count}";
                }
            }

            // Update laser pointer
            UpdateLaserPointer();
        }

        #endregion

        #region Laser Pointer

        private void UpdateLaserPointer()
        {
            if (laserPointer == null) return;

            // Ray from controller (assuming right hand)
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 10f, uiLayer))
            {
                laserPointer.SetPosition(0, transform.position);
                laserPointer.SetPosition(1, hit.point);
                laserPointer.startColor = Color.green;
                laserPointer.endColor = Color.green;
            }
            else
            {
                laserPointer.SetPosition(0, transform.position);
                laserPointer.SetPosition(1, transform.position + transform.forward * 10f);
                laserPointer.startColor = Color.red;
                laserPointer.endColor = Color.red;
            }
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion
    }
}
