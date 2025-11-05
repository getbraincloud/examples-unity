# brainCloud + Photon Integration (BCKart Example)

> **Under active development. Features and behaviors may change.**

## Overview

**Target Audience:** Unity developers familiar with multiplayer networking  

This project demonstrates a complete multiplayer game flow using **Photon** for network object synchronization and **brainCloud** for user authentication, lobby management, and matchmaking.  

The core integration removes Photon’s default room-based logic in favor of brainCloud-managed lobbies. Photon is used locally for real-time object handling, while brainCloud manages authentication, lobby creation, and matchmaking.  

## Known Issues

- If the host leaves during gameplay, the match will be lost, all remaining users will return to the brainCloud Lobby.

---

## Getting Started

Mac Users, you may need to run ** xattr -r -d com.apple.quarantine ./BCKart-Photon.app ** and accept its permissions since the app is unsigned.

### Requirements

- Unity 2021 or later  
- Photon Fusion Unity package  
- Photon Fusion AppId configured in the PhotonEngine Dashboard https://dashboard.photonengine.com/ 
- brainCloud Unity SDK  
- Valid brainCloud App ID and Secret (configured in the Unity Inspector)  

### Setup Steps

1. Clone this repository and open the Unity project.  
2. Open the `Launch` scene and run it in the Unity Editor.  
3. Use the brainCloud Plugin to add a new app from Template choosing BCKart-Photon. This way all lobbies, and associated cloud scripts are created automatically for you brainCloud team's app.

---

## Features

- **brainCloud Lobby Management**  
  - Lobbies are managed through brainCloud, including creation, matchmaking, and host assignment.  
  - Photon's prior Room would have dictated this while in an open session.

- **Real-Time Updates**  
  - Player movement, kart state, and other game objects are synchronized using Photon Fusion.  

- **Player Metadata Sync**  
  - Player usernames and display names are managed through brainCloud.  
  - Metadata is serialized and synced to all clients during session startup.  

---

## Gameplay Flow

1. **Authentication**  
   - Players log in via brainCloud.  
   - Username is linked to brainCloud `UserName` endpoint, with display name editable in profile.  

2. **Lobby Interaction**  
   - Quick Find: find or create a lobby automatically.  
   - Create Game: manually create a new lobby.  
   - Join Lobby: find an existing lobby, if none are present will display an error

3. **Race Start & Relay Setup**  
   - `BCLobbyManager` coordinates the transition from lobby to the Photon Fusion session.  
   - Host signals when all players are ready, triggering the track load for all users.  

4. **End Race**  
   - Host signals all clients to leave the race scene and return to the lobby.  

---

## Developer Notes

- The **BCScripts/** folder contains the core game flow logic and UI scripts.    
- `BCLobbyManager` handles all lobby events, matchmaking, and connection to the Photon session.  

> Further Implementation Steps below noted how brainCloud was integrated into the pre-existing Photon Fusion Kart Demo

---

## Folder Structure & Analysis

### `Assets/Scripts/brainCloud/Core/BCLobbyManager`

- Contains integration for Photon Fusion with brainCloud-managed lobbies.  
- Handles real-time updates and network object synchronization.  

### `Assets/Scripts/brainCloud/Core/BCManager`

- The main Accesor to the brainCloud initialized client

---

## Implementation Steps

### brainCloud Plugin & Login Flow

- Added **brainCloud Plugin** to the project.  
- Created and connected the **Login Screen**:  
  - Modified the launch flow to integrate brainCloud authentication.  
  - Stripped out client-specific info and replaced with brainCloud user data.  
  - Usernames are sent to the brainCloud `UserName` endpoint.  
  - Profile screen shows a **non-editable username** and an **editable display name**.  

### PlayMode Screen

- **Quick Find:** Finds or creates a lobby.  
- Added **BCLobbyManager** to enable real-time transport (RTT) and create lobby listeners.  
- **Quick Join:** Supports find-or-create logic, updating host and join flows as needed.  

### EndRace UI

- Updated so the **host sends a signal** to all clients to leave the current session and return to the lobby screen.  

### Photon Removal / brainCloud Lobby Logic

- Photon rooms are no longer used as the authoritative session.  
- `LobbyUI` is now governed entirely by `BCLobbyManager` events.  
- The manager listens for lobby events and loads the game track for all users to connect.  
- Once appropriate events are triggered, connections to the local Photon session are initialized for real-time object handling.  

---

## Future Improvements

- Support for clients leaving to lobby, instead of launch on host loss / migration.
- Possible Support for persistent levels and state during host migration.  
- Improved real-time session analytics, e.g., time to join and start the race.  About to start race.

---

## References

- [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/)  
- [brainCloud API Reference](https://docs.braincloudservers.com/api/introduction/)  
- [Photon Fusion Kart Sample](https://doc.photonengine.com/fusion/current/game-samples/fusion-karts#overview)  
