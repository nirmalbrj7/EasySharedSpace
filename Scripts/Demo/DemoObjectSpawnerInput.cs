using UnityEngine;
using EasySharedSpace;

/// <summary>
/// Demo script showing how to use the spawner with keyboard input.
/// </summary>
public class DemoObjectSpawnerInput : MonoBehaviour
{
    [Header("Spawn Settings")]
    public string objectToSpawn = "cube";
    public float spawnDistance = 2f;
    public Vector3 spawnOffset = Vector3.up;

    [Header("Input")]
    public KeyCode spawnKey = KeyCode.G;
    public KeyCode clearKey = KeyCode.C;

    [Header("Anchors")]
    public KeyCode createAnchorKey = KeyCode.H;
    public KeyCode saveAnchorsKey = KeyCode.J;
    public KeyCode loadAnchorsKey = KeyCode.K;

    void Update()
    {
        // Spawn object
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnObject();
        }

        // Clear all objects
        if (Input.GetKeyDown(clearKey))
        {
            ClearObjects();
        }

        // Create anchor
        if (Input.GetKeyDown(createAnchorKey))
        {
            CreateAnchor();
        }

        // Save anchors
        if (Input.GetKeyDown(saveAnchorsKey))
        {
            SaveAnchors();
        }

        // Load anchors
        if (Input.GetKeyDown(loadAnchorsKey))
        {
            LoadAnchors();
        }
    }

    void SpawnObject()
    {
        if (SharedSpaceManager.Instance?.IsSpaceReady != true)
        {
            Debug.Log("Not connected to shared space!");
            return;
        }

        Vector3 spawnPos = transform.position + transform.forward * spawnDistance + spawnOffset;
        
        var spawned = SharedObjectSpawner.Instance.SpawnObject(objectToSpawn, spawnPos);
        
        Debug.Log($"Spawned {objectToSpawn} at {spawnPos}");
    }

    void ClearObjects()
    {
        SharedObjectSpawner.Instance.DespawnAllObjects(objectToSpawn);
        Debug.Log($"Cleared all {objectToSpawn} objects");
    }

    void CreateAnchor()
    {
        if (SharedSpaceManager.Instance?.IsSpaceReady != true)
        {
            Debug.Log("Not connected to shared space!");
            return;
        }

        var anchor = SpatialAnchorManager.Instance.CreateAnchor(transform.position, transform.rotation);
        
        if (anchor != null)
        {
            Debug.Log($"Created anchor: {anchor.Id}");
        }
    }

    void SaveAnchors()
    {
        SpatialAnchorManager.Instance.SaveAnchors();
        Debug.Log("Anchors saved!");
    }

    void LoadAnchors()
    {
        SpatialAnchorManager.Instance.LoadAnchors();
        Debug.Log("Anchors loaded!");
    }
}
