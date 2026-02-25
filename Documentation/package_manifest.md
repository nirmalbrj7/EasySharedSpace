# Easy Shared Space - Package Structure

## Folder Structure

```
EasySharedSpace/
├── package.json                    # Package manifest
├── README.md                       # Main documentation
├── SETUP_GUIDE.md                  # Step-by-step setup
├── package_manifest.md             # This file
│
├── Runtime/                        # Runtime scripts
│   ├── Core/
│   │   ├── SharedSpaceManager.cs
│   │   └── SharedPlayer.cs
│   ├── Anchors/
│   │   ├── SpatialAnchor.cs
│   │   └── SpatialAnchorManager.cs
│   ├── Spawning/
│   │   └── SharedObjectSpawner.cs
│   ├── Interaction/
│   │   ├── SharedGrabbableObject.cs
│   │   ├── SimpleRayGrabber.cs
│   │   ├── SharedSpaceTransform.cs
│   │   └── NetworkedTriggerZone.cs
│   ├── UI/
│   │   └── SharedSpaceUI.cs
│   └── EasySharedSpace.Runtime.asmdef
│
├── Editor/                         # Editor scripts (optional)
│   └── EasySharedSpace.Editor.asmdef
│
├── Samples~/                       # Sample scenes
│   └── BasicSharedSpace/
│       ├── Scenes/
│       │   └── DemoScene.unity
│       ├── Prefabs/
│       │   ├── Player.prefab
│       │   ├── Anchor.prefab
│       │   └── GrabbableCube.prefab
│       ├── Scripts/
│       │   ├── DemoPlayerController.cs
│       │   ├── DemoObjectSpawnerInput.cs
│       │   └── SharedSpaceDebugger.cs
│       └── README.md
│
└── Documentation~/                 # Additional docs
    └── API_REFERENCE.md
```

## Core Components

### Runtime/Core
- **SharedSpaceManager** - Main manager, handles connection and coordinate space
- **SharedPlayer** - Player representation with position sync

### Runtime/Anchors
- **SpatialAnchor** - Persistent reference points in shared space
- **SpatialAnchorManager** - Manages anchor lifecycle and persistence

### Runtime/Spawning
- **SharedObjectSpawner** - Runtime object spawning with network sync

### Runtime/Interaction
- **SharedGrabbableObject** - Grabbable objects with ownership transfer
- **SimpleRayGrabber** - Desktop ray-based grabbing
- **SharedSpaceTransform** - General networked transform sync
- **NetworkedTriggerZone** - Trigger zones that work across network

### Runtime/UI
- **SharedSpaceUI** - Connection UI and debug panel

## Assembly Definitions

- **EasySharedSpace.Runtime** - Main runtime assembly
  - References: Netcode for GameObjects

## Dependencies

```json
{
  "com.unity.netcode.gameobjects": "1.7.0",
  "com.unity.transport": "2.1.0"
}
```

## Quick Setup Checklist

1. [ ] Install Netcode for GameObjects
2. [ ] Install Easy Shared Space package
3. [ ] Create player prefab with SharedPlayer
4. [ ] Create anchor prefab with SpatialAnchor
5. [ ] Setup NetworkManager + SharedSpaceManager
6. [ ] Setup SpatialAnchorManager
7. [ ] Setup SharedObjectSpawner
8. [ ] Add UI for connection
9. [ ] Test with host + client
