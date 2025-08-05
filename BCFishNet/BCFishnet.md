# brainCloud + FishNet Integration (CursorParty Demo)

> **Under active development. Features and behaviors may change.**

## Overview

**Target Audience:** Game developers familiar with Unity and multiplayer networking

This project demonstrates a complete multiplayer game flow using **FishNet** for network object synchronization and **brainCloud** for user authentication, matchmaking, lobby management, and real-time relay transport.

The core integration replaces FishNet's default transport with brainCloud's relay service, enabling you to write multiplayer logic with FishNet while delegating matchmaking and relay connectivity to brainCloud.

## Getting Started

### Requirements

- Unity 2021 or later
- FishNet Unity package
- brainCloud Unity SDK
- Valid brainCloud App ID and Secret (configured in Unity Inspector)

### Setup Steps

1. Clone this repository and open the Unity project.
2. Open the `Main` scene and run it in the Unity Editor.

### As a Player

3. Authenticate and enter the lobby.
4. Choose a lobby option and click "Ready Up".
5. The game scene will load when all players are ready.

---

## Features

- **Modular Transport Layer**
  - `brainCloudTransport` implements FishNet’s `Transport` class, enabling brainCloud to function as FishNet's network backend.
  - Plug-and-play: No need to modify FishNet’s core. Simply attach this component to your `NetworkManager`.

- **Lobby to Relay Flow**
  - Game starts with brainCloud lobby services.
  - Once the host is ready, the assigned relay server endpoint is used to initialize FishNet's connection through brainCloud's relay servers.

- **Real-Time Messaging**
  - Reliable and unreliable messages are routed through brainCloud's real-time relay.
  - Internal buffer queues manage data inflow to ensure a smooth handoff to FishNet.

- **Player Metadata Sync**
  - Players are assigned random colors and names.
  - This metadata is serialized and synced to all clients during session startup.
  - The lobby and game session support mid-session joins, and metadata (including color and name) is correctly synced, even after host migration.

- **Scene Flow**
  - Login → Lobby Selection → Matchmaking → Relay Connect → Game Start

---

## Gameplay Flow

1. **Authentication**
   - Use one of the four player options to log in.
   - Note: Currently logs in anonymously using brainCloud.

2. **Lobby Interaction**
   - Choose from:
     - Create Game
     - Quick Find (finds or creates a game)
     - Find Game (manual search)

3. **Room Assignment**
   - Displays room code and participant list.
   - Waits for players to click "Ready Up".

4. **Relay Transport Setup**
   - Once all players are ready, the `ROOM_ASSIGNED` event is triggered.
   - `brainCloudTransport` takes over and establishes a connection via FishNet using the provided relay parameters.

5. **Multiplayer Session Begins**
   - Players appear and interact in real time.
   - Movement, actions (shockwave), paint splat trails, and player properties are synchronized using FishNet.

---

## For Developers

If you're new to Unity networking:

- Focus first on the flow in the `BCScripts/` folder.
- Real-time networking is abstracted. You do not need to modify `BrainCloudTransport.cs` unless customizing packet handling.
- Use the Unity Inspector to assign references as needed (e.g., login UI, player prefab).

> Matchmaking, relay connectivity, and real-time sync are provided using a modular, pluggable approach. This lets you focus on **gameplay logic** and **player experience**.

---

## Useful Files for Reference

| Script | Purpose |
|--------|---------|
| `BrainCloudTransport.cs` | Custom transport layer for FishNet using brainCloud relay |

---

## Folder Structure & Analysis

### `Assets/Plugins/BCFishNet`

This folder contains the FishNet transport plugin implementation for brainCloud:

- `BrainCloudTransport.cs`
  - Implements FishNet's `Transport` abstract class.
  - Uses brainCloud's `RelayComms` system to send and receive messages.
  - Buffers incoming packets and dispatches them via FishNet.
  - Provides connection lifecycle methods (`StartClient`, `StartServer`, `StopConnection`).
  - Supports host migration.

**Purpose:**  
This is a drop-in FishNet transport backend powered by brainCloud. Plug `BrainCloudTransport` into your `NetworkManager` to enable real-time communication through brainCloud’s relay servers.

---

### `Assets/BCScripts`

This folder contains game logic and UI scripts that support user flow.

---

## Future Improvements

- Support for persistent levels when new players join or during host migration
- Measure and display time taken to enter a hosted and launched session

---

For more information on brainCloud and its services, see the [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction/) pages.
