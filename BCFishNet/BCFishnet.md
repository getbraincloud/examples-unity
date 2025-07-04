# brainCloud + FishNet Integration (CursorParty Demo)

> 🚧 **Under active development** – Features and behaviors may change.

## 🔍 Overview
Target Audience: Game developers familiar with Unity and multiplayer networking

This project demonstrates a complete multiplayer game flow using **FishNet** for network object synchronization and **brainCloud** for user authentication, matchmaking, lobby management, and real-time relay transport.

The core integration focuses on replacing FishNet's default transport with brainCloud's relay service, allowing you to write multiplayer logic with FishNet while offloading matchmaking and relay connectivity to brainCloud.

## ✅ Getting Started

### Requirements
- Unity 2021+
- FishNet Unity package
- brainCloud Unity SDK
- Valid brainCloud App ID and Secret (configured in Unity Inspector)

### Steps
1. Clone this repo and open the Unity project.
2. Go to the `Main` scene and run it in editor.

###As a Player

3. Authenticate and reach the Lobby.
4. Choose a lobby option and ready up.
5. Game scene will load when all players are ready.

---

## ✨ Features

- **Modular Transport Layer**
  - brainCloudTransport implements FishNet’s `Transport` class, allowing brainCloud to seamlessly act as FishNet's network backend.
  - Plug-and-play: no need to modify FishNet’s core. Attach this component to your NetworkManager and let brainCloud do the rest.

- **Lobby to Relay Flow**
  - Game starts with brainCloud lobby services.
  - Once the host is ready, the assigned relay server endpoint is used to initialize FishNet's connection with brainCloud's Relay Servers.

- **Real-Time Messaging**
  - Reliable and unreliable messages are routed through brainCloud's real-time relay.
  - Internal buffer queues manage data inflow for smooth handoff to FishNet.

- **Player Metadata Sync**
  - Players are assigned random colors and names.
  - This metadata is serialized and synced to all clients during session startup.
  - The lobby and game session can be joined in progress, syncing the client's assigned color and name. All colours placed allowed by game session user remain, even after host migration.

- **Scene Flow**
  - Login → Lobby Selection → Matchmaking → Relay Connect → Game Start.

---

## 🎮 Gameplay Flow

1. **Authentication**
   - Use one of the four player options to log in as the profile.
   - TODO: Player logs in anonymously using brainCloud.

2. **Lobby Interaction**
   - Choose from:
     - Create Game
     - Quick Find – Finds or creates a game. Preferred way to jump into the action.
     - Find Game (Search)

3. **Room Assignment**
   - Displays room code and participant list.
   - Waits for players to click "Ready Up".

4. **Relay Transport Setup**
   - Once everyone is ready, `ROOM_ASSIGNED` event is triggered.
   - brainCloudTransport takes over and connects via FishNet using relay parameters.

5. **Multiplayer Session Begins**
   - Players see each other in real-time.
   - Movement, actions (shockwave), paintsplat trail and player properties are synced via FishNet.

—
## 🧱 For Developers

If you're new to Unity networking:
- Focus first on the flow in `BCScripts/`
- The real-time networking is abstracted for you! You don’t need to modify `BrainCloudTransport.cs` unless you want to change how packets are handled.
- Use Unity Inspector to drag and assign references where needed (e.g., login UI, player prefab).

> You get matchmaking, relay connectivity, and real-time sync all handled for you using a plug-in approach. Focus your time on **game logic** and **player experience**.


## 📊 Useful Files for Reference
| Script | Purpose |
|--------|---------|
| `BrainCloudTransport.cs` | Custom transport layer for FishNet using brainCloud relay |

—

## 📁 Folder Structure & Analysis

### `Assets/Plugins/BCFishNet`
This folder contains the **FishNet transport plugin implementation** for brainCloud:

- `BrainCloudTransport.cs`
  - Implements FishNet's `Transport` abstract class.
  - Uses brainCloud's `RelayComms` system to send and receive network messages.
  - Buffers incoming data packets and dispatches them via FishNet's receive mechanism.
  - Provides connection lifecycle methods (`StartClient`, `StartServer`, `StopConnection`).
  - Handles Host Migration

### Purpose
This folder provides a **drop-in FishNet Transport backend** powered by brainCloud. By simply plugging in the `BrainCloudTransport` script in your `NetworkManager`, your multiplayer game will automatically use brainCloud's relay infrastructure for real-time communication. 

---

### `Assets/BCScripts`
This folder holds the **game logic and UI scripts** tied to user flow.



---

## 🚀 Future Improvements
    - Persistent Level for new people joining and migrating hosts to that solution
    - Time Counting how long it takes to get into a hosted and launched server

---

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).



