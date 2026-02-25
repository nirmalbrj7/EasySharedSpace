#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Netcode;

namespace EasySharedSpace.Editor
{
    /// <summary>
    /// Quick prefab setup tools for EasySharedSpace
    /// Access via: Menu -> EasySharedSpace -> Quick Setup
    /// </summary>
    public class QuickPrefabSetup : EditorWindow
    {
        [MenuItem("EasySharedSpace/Quick Setup/Create Player Prefab")]
        static void CreatePlayerPrefab()
        {
            GameObject player = new GameObject("PlayerPrefab");
            
            // Add required components
            player.AddComponent<NetworkObject>();
            player.AddComponent<SharedPlayer>();
            
            // Add CharacterController or Rigidbody
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = new Vector3(0, 0.9f, 0);
            controller.radius = 0.3f;
            
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
            textMesh.text = "Player";
            
            // Add ground check
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform);
            groundCheck.transform.localPosition = new Vector3(0, 0.1f, 0);
            
            // Create prefab from the GameObject
            string path = "Assets/EasySharedSpace/Prefabs/Player/PlayerPrefab.prefab";
            EnsureDirectoryExists("Assets/EasySharedSpace/Prefabs/Player");
            PrefabUtility.SaveAsPrefabAsset(player, path);
            
            // Select the new object
            Selection.activeGameObject = player;
            
            Debug.Log("[QuickSetup] Player prefab created! Saved to: " + path);
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
            
            // Create prefab
            string path = "Assets/EasySharedSpace/Prefabs/Objects/GrabbableCube.prefab";
            EnsureDirectoryExists("Assets/EasySharedSpace/Prefabs/Objects");
            PrefabUtility.SaveAsPrefabAsset(obj, path);
            
            Selection.activeGameObject = obj;
            Debug.Log("[QuickSetup] Grabbable object created! Saved to: " + path);
        }
        
        [MenuItem("EasySharedSpace/Quick Setup/Create Network Manager")]
        static void CreateNetworkManager()
        {
            GameObject manager = new GameObject("NetworkManager");
            
            // Add NetworkManager (Netcode for GameObjects)
            manager.AddComponent<NetworkManager>();
            
            // Add Unity Transport
            manager.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            
            // Add SharedSpaceManager
            var sharedManager = manager.AddComponent<SharedSpaceManager>();
            sharedManager.autoStartInEditor = true;
            
            // Add optional Relay support
            manager.AddComponent<EasySharedSpace.Networking.RelayNetworkManager>();
            
            // Create prefab
            string path = "Assets/EasySharedSpace/Prefabs/Network/NetworkManager.prefab";
            EnsureDirectoryExists("Assets/EasySharedSpace/Prefabs/Network");
            PrefabUtility.SaveAsPrefabAsset(manager, path);
            
            Selection.activeGameObject = manager;
            Debug.Log("[QuickSetup] NetworkManager created! Remember to assign PlayerPrefab in inspector.");
        }
        
        [MenuItem("EasySharedSpace/Quick Setup/Create Object Spawner")]
        static void CreateObjectSpawner()
        {
            GameObject spawner = new GameObject("SharedObjectSpawner");
            
            spawner.AddComponent<NetworkObject>();
            var sharedSpawner = spawner.AddComponent<SharedObjectSpawner>();
            
            // Create spawn point
            GameObject spawnPoint = new GameObject("SpawnPoint_01");
            spawnPoint.transform.SetParent(spawner.transform);
            spawnPoint.transform.position = new Vector3(0, 2, 0);
            
            // Create prefab
            string path = "Assets/EasySharedSpace/Prefabs/Objects/SharedObjectSpawner.prefab";
            EnsureDirectoryExists("Assets/EasySharedSpace/Prefabs/Objects");
            PrefabUtility.SaveAsPrefabAsset(spawner, path);
            
            Selection.activeGameObject = spawner;
            Debug.Log("[QuickSetup] Object Spawner created! Assign grabbable prefabs to the list.");
        }
        
        [MenuItem("EasySharedSpace/Quick Setup/Create Spatial Anchor")]
        static void CreateSpatialAnchor()
        {
            GameObject anchor = new GameObject("SpatialAnchor");
            
            anchor.AddComponent<NetworkObject>();
            anchor.AddComponent<SpatialAnchor>();
            
            // Visual indicator
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "AnchorVisual";
            visual.transform.SetParent(anchor.transform);
            visual.transform.localScale = Vector3.one * 0.1f;
            DestroyImmediate(visual.GetComponent<SphereCollider>());
            
            // Create prefab
            string path = "Assets/EasySharedSpace/Prefabs/Objects/SpatialAnchor.prefab";
            EnsureDirectoryExists("Assets/EasySharedSpace/Prefabs/Objects");
            PrefabUtility.SaveAsPrefabAsset(anchor, path);
            
            Selection.activeGameObject = anchor;
            Debug.Log("[QuickSetup] Spatial Anchor prefab created!");
        }
        
        [MenuItem("EasySharedSpace/Quick Setup/Setup Demo Scene")]
        static void SetupDemoScene()
        {
            // Create floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(10, 1, 10);
            floor.tag = "Ground";
            
            // Create spawn origin
            GameObject spawnOrigin = new GameObject("SpawnOrigin");
            spawnOrigin.transform.position = new Vector3(0, 0, 0);
            
            // Create lighting if needed
            if (GameObject.Find("Directional Light") == null)
            {
                GameObject light = new GameObject("Directional Light");
                Light lightComp = light.AddComponent<Light>();
                lightComp.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
            
            // Create camera if needed
            if (Camera.main == null)
            {
                GameObject cam = new GameObject("Main Camera");
                cam.tag = "MainCamera";
                cam.AddComponent<Camera>();
                cam.transform.position = new Vector3(0, 5, -10);
                cam.transform.LookAt(Vector3.zero);
            }
            
            Debug.Log("[QuickSetup] Demo scene setup complete!");
            Debug.Log("Next steps:");
            Debug.Log("1. Create/assign NetworkManager prefab");
            Debug.Log("2. Create/assign PlayerPrefab to SharedSpaceManager");
            Debug.Log("3. Assign SpawnOrigin to SharedSpaceManager");
        }
        
        private static void EnsureDirectoryExists(string path)
        {
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
