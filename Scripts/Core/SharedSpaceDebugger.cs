using UnityEngine;
using Unity.Netcode;
using EasySharedSpace;

/// <summary>
/// Debug helper to visualize shared space state.
/// </summary>
public class SharedSpaceDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showGUI = true;
    public bool showAnchors = true;
    public bool showPlayers = true;
    public bool showSpawnedObjects = true;

    [Header("Visual Settings")]
    public float anchorLabelOffset = 0.5f;
    public float playerLabelOffset = 2f;
    public GUIStyle labelStyle;

    private void OnGUI()
    {
        if (!showGUI) return;

        // Draw main panel
        GUILayout.BeginArea(new Rect(10, 10, 300, 400), "Shared Space Debug", "box");
        
        // Connection status
        bool isConnected = NetworkManager.Singleton?.IsConnectedClient ?? false;
        bool isHost = NetworkManager.Singleton?.IsHost ?? false;
        bool isServer = NetworkManager.Singleton?.IsServer ?? false;
        
        GUILayout.Label($"Status: {(isHost ? "Host" : isServer ? "Server" : isConnected ? "Client" : "Disconnected")}");
        
        if (SharedSpaceManager.Instance != null)
        {
            GUILayout.Label($"Space Ready: {SharedSpaceManager.Instance.IsSpaceReady}");
            GUILayout.Label($"Players: {SharedSpaceManager.Instance.ConnectedPlayers.Count + 1}");
        }

        if (SpatialAnchorManager.Instance != null)
        {
            GUILayout.Label($"Anchors: {SpatialAnchorManager.Instance.AnchorCount}");
        }

        GUILayout.Space(10);

        // Player list
        if (showPlayers && SharedSpaceManager.Instance != null)
        {
            GUILayout.Label("--- Players ---");
            foreach (var kvp in SharedSpaceManager.Instance.ConnectedPlayers)
            {
                var player = kvp.Value;
                GUILayout.Label($"  {player.PlayerName.Value} (ID: {kvp.Key})");
            }
        }

        GUILayout.Space(10);

        // Anchor list
        if (showAnchors && SpatialAnchorManager.Instance != null)
        {
            GUILayout.Label("--- Anchors ---");
            foreach (var kvp in SpatialAnchorManager.Instance.Anchors)
            {
                var anchor = kvp.Value;
                GUILayout.Label($"  {anchor.Id}: {anchor.AnchorPosition}");
            }
        }

        GUILayout.EndArea();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw anchor positions
        if (showAnchors && SpatialAnchorManager.Instance != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var kvp in SpatialAnchorManager.Instance.Anchors)
            {
                var anchor = kvp.Value;
                Vector3 pos = anchor.AnchorPosition;
                
                Gizmos.DrawWireSphere(pos, 0.1f);
                Gizmos.DrawLine(pos, pos + Vector3.up * 0.5f);
            }
        }

        // Draw player positions
        if (showPlayers && SharedSpaceManager.Instance != null)
        {
            Gizmos.color = Color.green;
            foreach (var kvp in SharedSpaceManager.Instance.ConnectedPlayers)
            {
                var player = kvp.Value;
                Vector3 pos = player.transform.position;
                
                Gizmos.DrawWireSphere(pos, 0.3f);
                Gizmos.DrawLine(pos, pos + Vector3.up * 1.8f);
            }
        }

        // Draw shared origin
        if (SharedSpaceManager.Instance != null)
        {
            Transform origin = SharedSpaceManager.Instance.GetSharedOrigin();
            if (origin != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(origin.position, 0.5f);
                Gizmos.DrawLine(origin.position, origin.position + origin.forward * 1f);
            }
        }
    }
}
