# EasySharedSpace - Prefab Creation Guide

This guide explains how to create the necessary prefabs for the EasySharedSpace system.

---

## 1. Player Prefab

### Required Components:
```
PlayerPrefab
├── NetworkObject (component)
├── SharedPlayer (component)
├── CharacterController OR Rigidbody
├── Capsule Collider
├── DemoPlayerController (component) [for local control]
├── SimpleRayGrabber (component) [optional]
├── Visuals
│   ├── Body (Cube or Capsule Mesh)
│   ├── NameLabel (TextMesh - facing camera)
│   └── GroundCheck (Empty GameObject at feet)
└── Camera (if using first-person)
```

### Setup Steps:
1. Create empty GameObject named "PlayerPrefab"
2. Add **NetworkObject** component (from Netcode for GameObjects)
   - Check "Spawn with Observers" 
   - Uncheck "Destroy with Spawner"
3. Add **SharedPlayer** script
4. Add **CharacterController** or **Rigidbody** + Capsule Collider
5. Add **DemoPlayerController** script
   - Assign GroundCheck transform
   - Set GroundMask (e.g., "Ground" layer)
6. Add child object for visuals (capsule/body mesh)
7. Add child object with **TextMesh** for name label
8. Drag to Project window to create prefab

---

## 2. Grabbable Object Prefab

### Required Components:
```
GrabbableCube
├── NetworkObject
├── NetworkTransform
├── SharedGrabbableObject
├── BoxCollider (or mesh collider)
├── Rigidbody
└── MeshRenderer + MeshFilter (Cube)
```

### Setup Steps:
1. Create Cube (GameObject > 3D Object > Cube)
2. Add **NetworkObject** component
3. Add **NetworkTransform** component
   - Check "Sync Position" and "Sync Rotation"
4. Add **SharedGrabbableObject** script
5. Ensure Rigidbody is present
6. Add any collider (BoxCollider recommended)
7. Drag to Project window to create prefab

---

## 3. Shared Object Spawner Prefab

### Required Components:
```
ObjectSpawner
├── NetworkObject
├── SharedObjectSpawner
└── SpawnPoints (empty children marking spawn locations)
```

### Setup Steps:
1. Create empty GameObject
2. Add **NetworkObject**
3. Add **SharedObjectSpawner** script
4. Create empty children as spawn points
5. Assign grabbable prefabs to the spawner script
6. Drag to Project window to create prefab

---

## 4. Network Manager Prefab

### Required Components:
```
NetworkManager
├── NetworkManager (component - Unity Netcode)
├── UnityTransport (component)
├── NetworkObjectPool (optional but recommended)
├── SharedSpaceManager
└── (Optional) RelayNetworkManager
```

### Setup Steps:
1. Create empty GameObject named "NetworkManager"
2. Add **NetworkManager** component (Unity Netcode)
3. Add **UnityTransport** component (auto-added)
4. Add **SharedSpaceManager** script
   - Assign PlayerPrefab
   - Create and assign SpawnOrigin (empty GameObject)
5. (Optional) Add **RelayNetworkManager** for IP-less connections

---

## 5. Spatial Anchor Prefab

### Required Components:
```
SpatialAnchor
├── NetworkObject
├── SpatialAnchor (script)
└── Visuals
    └── AnchorVisual (simple mesh indicating anchor location)
```

---

## 6. UI Canvas Prefab

### Required Components:
```
UICanvas
├── Canvas (Screen Space - Overlay)
├── CanvasScaler
├── GraphicRaycaster
└── ConnectionPanel (UI Panel)
    ├── TitleText
    ├── ModeDropdown (Direct IP / Relay / Auto Discovery)
    ├── IPInputField
    ├── RelayCodeInputField
    ├── HostButton
    ├── JoinButton
    └── StatusText
```

---

## 7. Demo Scene Setup

### Scene Hierarchy:
```
DemoScene
├── Directional Light
├── Main Camera
├── Environment
│   ├── Floor (Plane with collider)
│   └── Walls/Boundaries
├── NetworkManager (prefab instance)
├── ObjectSpawner (prefab instance)
├── SpawnOrigin (empty GameObject at origin)
└── UICanvas (prefab instance)
```

### Scene Setup Steps:
1. Create new scene
2. Add NetworkManager prefab
3. Add floor plane (with collider, tag as "Ground")
4. Create SpawnOrigin empty GameObject at desired spawn location
5. Assign SpawnOrigin to SharedSpaceManager
6. Add ObjectSpawner
7. Add UICanvas
8. Wire up UI buttons to EnhancedDemoController methods

---

## 8. Quick Prefab Script (Editor)

Create this script in `Editor/QuickPrefabSetup.cs`:

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Netcode;

namespace EasySharedSpace.Editor
{
    public class QuickPrefabSetup : EditorWindow
    {
        [MenuItem("EasySharedSpace/Quick Setup/Create Player Prefab")]
        static void CreatePlayerPrefab()
        {
            GameObject player = new GameObject("PlayerPrefab");
            
            // Add required components
            player.AddComponent<NetworkObject>();
            player.AddComponent<SharedPlayer>();
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = new Vector3(0, 0.9f, 0);
            
            player.AddComponent<DemoPlayerController>();
            
            // Add visuals
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            DestroyImmediate(body.GetComponent<CapsuleCollider>());
            
            // Add name label
            GameObject nameLabel = new GameObject("NameLabel");
            nameLabel.transform.SetParent(player.transform);
            nameLabel.transform.localPosition = new Vector3(0, 2.2f, 0);
            var textMesh = nameLabel.AddComponent<TextMesh>();
            textMesh.characterSize = 0.1f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            
            // Add ground check
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform);
            groundCheck.transform.localPosition = new Vector3(0, 0.1f, 0);
            
            // Select the new object
            Selection.activeGameObject = player;
            
            Debug.Log("[QuickSetup] Player prefab created! Add it to your Project folder.");
        }
        
        [MenuItem("EasySharedSpace/Quick Setup/Create Grabbable Object")]
        static void CreateGrabbableObject()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "GrabbableObject";
            
            obj.AddComponent<NetworkObject>();
            obj.AddComponent<NetworkTransform>();
            obj.AddComponent<SharedGrabbableObject>();
            
            var rb = obj.GetComponent<Rigidbody>();
            if (rb == null) rb = obj.AddComponent<Rigidbody>();
            
            Selection.activeGameObject = obj;
            Debug.Log("[QuickSetup] Grabbable object created!");
        }
    }
}
#endif
```

---

## Next Steps

1. Create prefabs following the guide above
2. Assign prefabs to SharedSpaceManager (PlayerPrefab)
3. Assign prefabs to SharedObjectSpawner (ObjectPrefabs list)
4. Test in Editor with ParrelSync (for multi-instance testing)
5. Build and test with Relay for internet play (no IP needed!)
