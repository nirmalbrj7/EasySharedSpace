# Easy Shared Space

A simple Unity package for creating multiplayer shared spatial experiences. Perfect for collaborative VR/AR applications, virtual meetings, or any multiplayer experience where players need to share a common coordinate space.

## Features

- 🌐 **Easy Networking** - Simple host/client setup using Unity Netcode
- 📍 **Spatial Anchors** - Persistent reference points shared by all players
- 🎮 **Object Synchronization** - Spawn and share objects across the network
- 🤏 **Grab & Interact** - Built-in grab system for object interaction
- 🎯 **Simple API** - Easy-to-use component-based system
- 📦 **Ready to Use** - Includes demo scene and prefabs

## Requirements

- Unity 2022.3 LTS or newer
- Netcode for GameObjects 1.7.0+
- Unity Transport 2.1.0+

## Installation

### Via Unity Package Manager

1. Open Window → Package Manager
2. Click + → Add package from disk...
3. Select the `package.json` file from this package

### Manual Installation

1. Copy the `EasySharedSpace` folder into your project's `Packages` folder, or
2. Copy into `Assets` folder if you want to modify the source

## Quick Start

### 1. Setup Scene

1. Create an empty GameObject named `SharedSpaceManager`
2. Add `SharedSpaceManager` component
3. Assign a Player Prefab (create a simple capsule with `SharedPlayer` script)
4. Add a `NetworkManager` component to the same object or another

```csharp
// Your player controller - attach to player prefab
public class MyPlayerController : MonoBehaviour
{
    void Update()
    {
        // Simple movement
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        transform.Translate(new Vector3(h, 0, v) * Time.deltaTime * 5f);
    }
}
```

### 2. Start Networking

```csharp
// Start as host (server + client)
SharedSpaceManager.Instance.StartHost();

// Or join as client
SharedSpaceManager.Instance.JoinAsClient("192.168.1.100");
```

### 3. Create Spatial Anchors

```csharp
// Create a persistent anchor at current position
SpatialAnchorManager.Instance.CreateAnchor(transform.position);

// Get an existing anchor
SpatialAnchor anchor = SpatialAnchorManager.Instance.GetAnchor("my-anchor-id");
```

### 4. Spawn Shared Objects

```csharp
// Spawn an object that all players can see
SharedObjectSpawner.Instance.SpawnObject("cube", position);

// Spawn at an anchor
SharedObjectSpawner.Instance.SpawnAtAnchor("cube", "my-anchor-id");
```

## Core Components

### SharedSpaceManager
Main entry point for the shared space system. Handles connection, player spawning, and coordinate space.

**Key Methods:**
- `StartHost()` - Start as server + client
- `JoinAsClient(ip)` - Connect to a host
- `LocalToSharedSpace(localPos)` - Convert local to shared coordinates
- `SharedToLocalSpace(sharedPos)` - Convert shared to local coordinates

### SharedPlayer
Represents a player in the shared space. Automatically syncs position/rotation.

**Properties:**
- `IsLocalPlayer` - Is this the local player?
- `PlayerName` - Network synced player name
- `PlayerColor` - Network synced player color

### SpatialAnchor
Persistent reference point in shared space.

**Usage:**
```csharp
// Place anchor
anchor.PlaceAtCurrentPosition();

// Get position relative to shared origin
Vector3 relativePos = anchor.GetRelativePosition();
```

### SpatialAnchorManager
Manages all anchors. Handles persistence across sessions.

**Key Methods:**
- `CreateAnchor(position, rotation, id)` - Create new anchor
- `GetAnchor(id)` - Get anchor by ID
- `SaveAnchors()` - Persist to storage
- `LoadAnchors()` - Load from storage

### SharedObjectSpawner
Spawn synchronized objects.

**Setup:**
1. Add spawnable objects to the spawner
2. Assign unique IDs
3. Spawn at runtime

```csharp
// Configure in inspector or code
spawner.spawnableObjects = new[] {
    new SharedObjectSpawner.SpawnableObject {
        objectId = "cube",
        prefab = cubePrefab,
        maxCount = 10
    }
};
```

### SharedGrabbableObject
Object that can be grabbed and moved by players.

**Features:**
- Automatic ownership transfer on grab
- Physics support
- Smooth network interpolation
- Visual feedback

## Example: Complete Setup

```csharp
using UnityEngine;
using EasySharedSpace;

public class MySharedExperience : MonoBehaviour
{
    public GameObject playerPrefab;
    public SpatialAnchor anchorPrefab;
    public GameObject[] spawnablePrefabs;

    void Start()
    {
        // Setup shared space
        SharedSpaceManager.Instance.playerPrefab = playerPrefab;
        
        // Setup anchor manager
        SpatialAnchorManager.Instance.anchorPrefab = anchorPrefab;
        
        // Setup spawner
        var spawner = SharedObjectSpawner.Instance;
        spawner.spawnableObjects = new[]
        {
            new SharedObjectSpawner.SpawnableObject {
                objectId = "box",
                prefab = spawnablePrefabs[0],
                maxCount = 20
            }
        };

        // Auto-start in editor
        #if UNITY_EDITOR
        SharedSpaceManager.Instance.StartHost();
        #endif
    }

    void Update()
    {
        // Press A to spawn object
        if (Input.GetKeyDown(KeyCode.A))
        {
            Vector3 spawnPos = transform.position + transform.forward * 2f;
            SharedObjectSpawner.Instance.SpawnObject("box", spawnPos);
        }

        // Press S to create anchor
        if (Input.GetKeyDown(KeyCode.S))
        {
            SpatialAnchorManager.Instance.CreateAnchor(transform.position);
        }
    }
}
```

## XR/VR Setup

For XR applications:

1. Add XR Origin to your scene
2. Attach `SharedPlayer` to the camera rig
3. Use hand/controller position for grabbing

```csharp
public class XRGrabber : MonoBehaviour
{
    public Transform handTransform;
    public InputAction grabAction;
    
    private SharedGrabbableObject _grabbedObject;

    void Update()
    {
        if (grabAction.WasPressedThisFrame())
        {
            TryGrab();
        }
        else if (grabAction.WasReleasedThisFrame())
        {
            Release();
        }
    }

    void TryGrab()
    {
        Collider[] hits = Physics.OverlapSphere(handTransform.position, 0.1f);
        foreach (var hit in hits)
        {
            var grabbable = hit.GetComponent<SharedGrabbableObject>();
            if (grabbable != null && grabbable.TryGrab(handTransform))
            {
                _grabbedObject = grabbable;
                break;
            }
        }
    }

    void Release()
    {
        if (_grabbedObject != null)
        {
            _grabbedObject.Release();
            _grabbedObject = null;
        }
    }
}
```

## Tips

1. **Anchor Persistence** - Anchors are saved to PlayerPrefs. For production, implement your own save system.

2. **Network Performance** - Adjust sync rates on `SharedPlayer` for your needs:
   - Higher sync rate = smoother but more bandwidth
   - Use thresholds to reduce unnecessary updates

3. **Ownership** - Only the owner can modify a `NetworkVariable`. Use ServerRPCs for client requests.

4. **Physics** - For physics objects, ensure they're not kinematic when not grabbed for proper simulation.

## Troubleshooting

**Can't connect to host:**
- Check firewall settings
- Verify IP address is correct
- Ensure both devices are on same network

**Objects not syncing:**
- Verify NetworkObject component is present
- Check that object is spawned via SharedObjectSpawner
- Ensure client is connected before spawning

**Anchors not loading:**
- Check `useLocalPersistence` is enabled
- Anchors are only loaded on host/server

## License

MIT License - Free to use in personal and commercial projects.

## Support

For issues and feature requests, please use the GitHub issue tracker.
