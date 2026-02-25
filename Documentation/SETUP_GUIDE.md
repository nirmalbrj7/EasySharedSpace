# Easy Shared Space - Setup Guide

## Step-by-Step Setup

### Step 1: Install Dependencies

1. Open Unity Package Manager (Window → Package Manager)
2. Install Netcode for GameObjects:
   - Click + → Add package by name
   - Enter: `com.unity.netcode.gameobjects`
   - Click Add

### Step 2: Install Easy Shared Space

1. In Package Manager, click + → Add package from disk...
2. Navigate to this package folder
3. Select `package.json`

### Step 3: Create Player Prefab

1. Create a new GameObject in scene
2. Add a Capsule or Cube for visual
3. Add Components:
   - `NetworkObject`
   - `SharedPlayer` (from EasySharedSpace)
   - Your movement script
4. Drag to Project window to create prefab
5. Delete from scene

### Step 4: Create Anchor Prefab

1. Create empty GameObject
2. Add Components:
   - `NetworkObject`
   - `SpatialAnchor` (from EasySharedSpace)
3. Add a visual (small sphere or cube)
4. Create prefab
5. Delete from scene

### Step 5: Setup Scene

1. Create empty GameObject, name it `NetworkSetup`
2. Add `NetworkManager` component
3. Add `UnityTransport` component (if not auto-added)
4. Add `SharedSpaceManager` component
   - Assign Player Prefab
5. Create another empty GameObject, name it `AnchorManager`
6. Add `SpatialAnchorManager` component
   - Assign Anchor Prefab
7. Create another empty GameObject, name it `ObjectSpawner`
8. Add `SharedObjectSpawner` component
   - Add your spawnable objects

### Step 6: Create UI

1. Create Canvas (GameObject → UI → Canvas)
2. Create UI Buttons for Host/Join
3. Create `SharedSpaceUI` script on Canvas
4. Assign UI references
5. Add Input Field for IP address

### Step 7: Create Grabbable Objects

1. Create a Cube
2. Add `NetworkObject`
3. Add `Rigidbody`
4. Add `SharedGrabbableObject`
5. Create prefab
6. Add to spawner list

### Step 8: Add Player Ray Grabber

1. On your player camera, add `SimpleRayGrabber`
2. Configure layer mask for grabbable objects

### Step 9: Test

1. Build and run as host
2. Run in editor, join as client (use 127.0.0.1)
3. Test movement and grabbing

## Demo Scene

A complete demo scene is included in `Samples~/BasicSharedSpace/`.

To import:
1. In Package Manager, select Easy Shared Space
2. Under Samples, click Import

## Network Architecture

```
Host (Server + Client)
├── Spawns anchors
├── Syncs all objects
└── Manages state

Clients
├── Send inputs
├── Receive updates
└── Interpolate positions
```

## Key Concepts

### Shared Origin
The `spawnOrigin` transform in SharedSpaceManager defines the shared coordinate space. All players see positions relative to this origin.

### Ownership
- Server owns anchors
- Player owns their grabbed object
- Ownership transfers on grab

### Spawning Flow
1. Client calls ServerRpc
2. Server instantiates and spawns
3. NetworkObject replicates to all clients

## Common Patterns

### Respawn at Anchor
```csharp
public void RespawnAtAnchor(string anchorId)
{
    var anchor = SpatialAnchorManager.Instance.GetAnchor(anchorId);
    if (anchor != null)
    {
        transform.position = anchor.AnchorPosition + Vector3.up;
    }
}
```

### Count Objects Near Anchor
```csharp
public int CountObjectsNearAnchor(string anchorId, float radius)
{
    var anchor = SpatialAnchorManager.Instance.GetAnchor(anchorId);
    if (anchor == null) return 0;
    
    Collider[] hits = Physics.OverlapSphere(anchor.AnchorPosition, radius);
    return hits.Length;
}
```

### Sync Custom Data
```csharp
public class MySyncedObject : NetworkBehaviour
{
    private NetworkVariable<float> health = new NetworkVariable<float>(100f);
    
    public void TakeDamage(float damage)
    {
        if (IsServer)
        {
            health.Value -= damage;
        }
    }
}
```

## Performance Tips

1. **Limit Network Variables** - Don't sync everything
2. **Use Thresholds** - Only sync when values change significantly
3. **Pool Objects** - Reuse spawned objects instead of destroying
4. **Reduce Sync Rate** - Lower sync rate for distant objects

## Next Steps

- Add voice chat
- Implement room codes
- Add authentication
- Create persistent rooms
- Add spatial audio
