using UnityEngine;
using EasySharedSpace;
using System.Collections.Generic;

namespace EasySharedSpace.Demo
{
    /// <summary>
    /// Simple visualizer for shared space research.
    /// Shows players and objects with labels and connections.
    /// </summary>
    public class SimpleVisualizer : MonoBehaviour
    {
        [Header("Player Visualization")]
        public bool showPlayerLabels = true;
        public bool showPlayerConnections = true;
        public bool showPlayerHistory = true;
        public int maxHistoryPoints = 50;
        public float historyInterval = 0.1f;

        [Header("Object Visualization")]
        public bool showObjectLabels = true;
        public bool showObjectVelocities = false;

        [Header("Colors")]
        public Color localPlayerColor = Color.green;
        public Color remotePlayerColor = Color.blue;
        public Color objectColor = Color.yellow;
        public Color connectionColor = Color.white;

        [Header("References")]
        public SharedSpaceManager spaceManager;

        private Dictionary<ulong, List<Vector3>> _playerHistories = new Dictionary<ulong, List<Vector3>>();
        private Dictionary<ulong, float> _lastHistoryTime = new Dictionary<ulong, float>();
        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;

        private void Start()
        {
            if (spaceManager == null)
                spaceManager = SharedSpaceManager.Instance;

            // Subscribe to player events
            if (spaceManager != null)
            {
                spaceManager.OnPlayerJoined += OnPlayerJoined;
                spaceManager.OnPlayerLeft += OnPlayerLeft;
            }

            InitializeStyles();
        }

        private void InitializeStyles()
        {
            _labelStyle = new GUIStyle();
            _labelStyle.normal.textColor = Color.white;
            _labelStyle.fontSize = 12;
            _labelStyle.alignment = TextAnchor.MiddleCenter;

            _titleStyle = new GUIStyle(_labelStyle);
            _titleStyle.fontSize = 14;
            _titleStyle.fontStyle = FontStyle.Bold;
        }

        private void Update()
        {
            if (spaceManager == null) return;

            // Record player positions for history
            foreach (var player in spaceManager.ConnectedPlayers)
            {
                ulong clientId = player.Key;
                SharedPlayer sharedPlayer = player.Value;

                if (!_playerHistories.ContainsKey(clientId))
                {
                    _playerHistories[clientId] = new List<Vector3>();
                    _lastHistoryTime[clientId] = 0;
                }

                // Add history point at interval
                if (Time.time - _lastHistoryTime[clientId] > historyInterval)
                {
                    _playerHistories[clientId].Add(sharedPlayer.transform.position);
                    _lastHistoryTime[clientId] = Time.time;

                    // Limit history size
                    if (_playerHistories[clientId].Count > maxHistoryPoints)
                    {
                        _playerHistories[clientId].RemoveAt(0);
                    }
                }
            }
        }

        private void OnRenderObject()
        {
            if (spaceManager == null) return;

            // Draw player connections
            if (showPlayerConnections)
            {
                DrawPlayerConnections();
            }

            // Draw player history trails
            if (showPlayerHistory)
            {
                DrawPlayerHistories();
            }

            // Draw object velocities
            if (showObjectVelocities)
            {
                DrawObjectVelocities();
            }
        }

        private void DrawPlayerConnections()
        {
            GL.Begin(GL.LINES);
            GL.Color(connectionColor);

            var players = new List<SharedPlayer>(spaceManager.ConnectedPlayers.Values);
            
            for (int i = 0; i < players.Count; i++)
            {
                for (int j = i + 1; j < players.Count; j++)
                {
                    Vector3 pos1 = players[i].transform.position + Vector3.up * 1.5f;
                    Vector3 pos2 = players[j].transform.position + Vector3.up * 1.5f;

                    GL.Vertex(pos1);
                    GL.Vertex(pos2);
                }
            }

            GL.End();
        }

        private void DrawPlayerHistories()
        {
            foreach (var history in _playerHistories)
            {
                if (history.Value.Count < 2) continue;

                GL.Begin(GL.LINE_STRIP);

                // Determine color based on local/remote
                bool isLocal = spaceManager.ConnectedPlayers.ContainsKey(history.Key) &&
                              spaceManager.ConnectedPlayers[history.Key].IsLocalPlayer;
                GL.Color(isLocal ? localPlayerColor : remotePlayerColor);

                foreach (Vector3 pos in history.Value)
                {
                    GL.Vertex(pos + Vector3.up * 0.1f);
                }

                GL.End();
            }
        }

        private void DrawObjectVelocities()
        {
            var objects = FindObjectsOfType<SharedGrabbableObject>();
            
            GL.Begin(GL.LINES);
            GL.Color(objectColor);

            foreach (var obj in objects)
            {
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 start = obj.transform.position;
                    Vector3 end = start + rb.velocity * 0.5f;

                    GL.Vertex(start);
                    GL.Vertex(end);
                }
            }

            GL.End();
        }

        private void OnGUI()
        {
            if (spaceManager == null) return;

            // Draw player labels
            if (showPlayerLabels)
            {
                DrawPlayerLabels();
            }

            // Draw object labels
            if (showObjectLabels)
            {
                DrawObjectLabels();
            }

            // Draw info panel
            DrawInfoPanel();
        }

        private void DrawPlayerLabels()
        {
            foreach (var player in spaceManager.ConnectedPlayers)
            {
                SharedPlayer sharedPlayer = player.Value;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(
                    sharedPlayer.transform.position + Vector3.up * 2.2f);

                if (screenPos.z > 0) // In front of camera
                {
                    string label = sharedPlayer.PlayerName.Value;
                    if (sharedPlayer.IsLocalPlayer) label += " (You)";

                    Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
                    Vector2 size = new Vector2(100, 20);
                    Rect rect = new Rect(guiPos - size / 2, size);

                    GUI.Label(rect, label, _labelStyle);
                }
            }
        }

        private void DrawObjectLabels()
        {
            var objects = FindObjectsOfType<SharedGrabbableObject>();
            
            foreach (var obj in objects)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(
                    obj.transform.position + Vector3.up * 0.5f);

                if (screenPos.z > 0)
                {
                    string label = $"Object {obj.NetworkObjectId}";
                    
                    Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
                    Vector2 size = new Vector2(80, 20);
                    Rect rect = new Rect(guiPos - size / 2, size);

                    GUI.Label(rect, label, _labelStyle);
                }
            }
        }

        private void DrawInfoPanel()
        {
            float panelWidth = 250;
            float panelHeight = 120;
            Rect panelRect = new Rect(10, Screen.height - panelHeight - 10, panelWidth, panelHeight);

            GUI.Box(panelRect, "");
            
            float y = panelRect.y + 5;
            float lineHeight = 20;

            GUI.Label(new Rect(panelRect.x + 5, y, panelWidth - 10, lineHeight), 
                "=== SHARED SPACE ===", _titleStyle);
            y += lineHeight + 5;

            GUI.Label(new Rect(panelRect.x + 5, y, panelWidth - 10, lineHeight), 
                $"Players: {spaceManager.ConnectedPlayers.Count}");
            y += lineHeight;

            int objectCount = FindObjectsOfType<SharedGrabbableObject>().Length;
            GUI.Label(new Rect(panelRect.x + 5, y, panelWidth - 10, lineHeight), 
                $"Objects: {objectCount}");
            y += lineHeight;

            GUI.Label(new Rect(panelRect.x + 5, y, panelWidth - 10, lineHeight), 
                $"Your IP: {SimpleIPNetworkManager.Instance?.LocalIPAddress}");
            y += lineHeight;

            string status = SimpleIPNetworkManager.Instance?.ConnectionStatus ?? "Unknown";
            GUI.Label(new Rect(panelRect.x + 5, y, panelWidth - 10, lineHeight), 
                $"Status: {status}");
        }

        private void OnPlayerJoined(ulong clientId)
        {
            if (!_playerHistories.ContainsKey(clientId))
            {
                _playerHistories[clientId] = new List<Vector3>();
                _lastHistoryTime[clientId] = Time.time;
            }
        }

        private void OnPlayerLeft(ulong clientId)
        {
            if (_playerHistories.ContainsKey(clientId))
            {
                _playerHistories.Remove(clientId);
                _lastHistoryTime.Remove(clientId);
            }
        }

        private void OnDestroy()
        {
            if (spaceManager != null)
            {
                spaceManager.OnPlayerJoined -= OnPlayerJoined;
                spaceManager.OnPlayerLeft -= OnPlayerLeft;
            }
        }
    }
}
