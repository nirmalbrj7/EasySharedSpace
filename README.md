# EasySharedSpace - Unity Multiplayer made Simple

A simple multiplayer shared spatial space for Unity. Easily share positions, spawn objects, and create persistent anchors in a shared coordinate space. 

Suitable for prototyping, research, VR apps (Meta Quest), and fully-featured games without the need for static IP addresses.

---

## Features

- **Multiple Networking Solutions**: Connect via Direct IP (LAN), Unity Relay (Serverless Internet), Auto Discovery (Same WiFi), WebSocket (WebGL), Steam, or Epic Online Services.
- **Quest 3 Support built-in**: Auto-discovery means *two headsets find each other without typing IP addresses*.
- **Visual Tools for Researchers**: Track player latency, logging, trails, connections, and object movements.
- **Instant Testing**: Enter 127.0.0.1 directly in an exported build while running the Unity Editor.

---

## Installation & Quick Setup

Please refer to the [Publishing & Installation Guide](docs) to learn how to add this package to your Unity project via the Unity Package Manager (UPM).

### Minimum Dependencies
- `com.unity.netcode.gameobjects`
- `com.unity.transport`

*(For internet play via Unity Relay, you'll optionally need `com.unity.services.core`, `com.unity.services.relay`, and `com.unity.services.lobby`)*

---

## 2-Minute Quick Start (Simple IP)

### 1. The Easiest Scene Setup
In an empty scene, ensure you have:
1. A **NetworkManager** Game Object with:
   - `NetworkManager` (Unity Netcode)
   - `UnityTransport`
   - `SimpleIPNetworkManager` (Set Auto Start Host In Editor to *True*)
   - `SharedSpaceManager` (Assign a PlayerPrefab with `NetworkObject`, `NetworkTransform`, `SharedPlayer`, `DemoPlayerController`)
2. A **ConnectionCanvas** (UI) with `SimpleIPConnectionUI` component to host/join.
3. A Floor/Ground object (Plane).

### 2. Test It
- **Editor**: Press Play > Click **HOST** > Your local IP displays automatically!
- **Build**: Export the game, open the `.exe`, type `127.0.0.1` and click **JOIN**. You'll see two players synchronized.

*Keyboard Shortcuts:*
- **H**: Quick Host
- **J**: Quick Join (127.0.0.1)
- **Space**: Spawn object
- **T/L**: Toggle Player Trails / Name Labels

---

## Meta Quest 3 Guide (No IPs Required!)

For Quest users, typing IPs is a hassle. We use **Auto-Discovery** over local WiFi.

### Scene Setup
1. Add `QuestAutoDiscoveryManager` to your `NetworkManager`.
2. Use `QuestSimpleConnectionUI` on a World Space Canvas right in front of the user. Make your HOST and FIND ROOM buttons massive!
3. Add a simple Box Collider to the buttons for laser pointers.

### The Flow
- **Quest 1 (Host)**: Click `[HOST]` button.
- **Quest 2 (Client)**: Click `[FIND ROOM]`. It automatically detects the host! Click `[JOIN]`.

*Make sure your Android Manifest has `INTERNET` and `ACCESS_WIFI_STATE` permission enabled.*

---

## Research & UX Testing

If you are using this package for HCI or CSCW research, we have built-in data collection tools:

- **ResearchLogger**: Attach `ResearchTestSceneManager` to automatically write CSV files of player behavior, time-stamps, and latency. 
- **Latency Experiments**: Move alongside another player and read out movement ticks.
- **Spatial Object Synching**: Test user spatial memory using `SharedObjectSpawner`.

*To customize metrics output in `ResearchTestSceneManager`:*
```csharp
public void LogEvent(string eventName, string data) {
    string line = $"{Time.time},{eventName},{GetLocalPlayerName(data)}\n";
    File.AppendAllText("ResearchLog.csv", line);
}
```

---

## Moving beyond IP (Internet Play)
If you need connectivity over the open internet (without port forwarding):
Use the **Unity Relay** scripts in `NetworkingAlternatives`.

```csharp
// Host gets a short code (e.g. ABCD-1234)
string joinCode = await RelayNetworkManager.Instance.StartRelayHostAsync(4);

// Client joins instantly
await RelayNetworkManager.Instance.JoinRelayAsync("ABCD-1234");
```
*No more IP headaches, configuring routers, or port forwarding. Best part? The free tier is 50GB/month.*

---

## Contributing
Contributions, issues, and feature requests are welcome!
Please check our [Contributing Guide](CONTRIBUTING.md) for details on how to get started.

---

## License
This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for more information.

---

## ❤️ Support
If this project helped you in your research, prototyping, or game development, consider supporting the development!

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/nirmalbrj7)
