# brainCloud + Photon Integration (BCKart Example)

## Overview

**Target Audience:** Unity developers familiar with multiplayer networking  

This project demonstrates a complete multiplayer game flow using **Photon** for network object synchronization and **brainCloud** for user authentication, lobby management, and matchmaking.  

The core integration removes Photon’s default room-based logic in favor of brainCloud-managed lobbies. Photon is used locally for real-time object handling, while brainCloud manages authentication, lobby creation, matchmaking, and leaderboards.  If the host leaves during gameplay, the match will be lost, all remaining users will return to the brainCloud Lobby. This way the lobby remains, and while not active in the gameplay session, others will be able to join the Lobby.

---

## Getting Started

Mac Users, you may need to run ** xattr -r -d com.apple.quarantine ./BCKart-Photon.app ** and accept its permissions since the app is unsigned.

Please ensure that you have setup your Fusion AppId, and configure it in Unity via Tools -> Fusion -> Realtime Settings. The app will not work until you do so.

It is highly recommended to manage your Fusion App Servers, with a Custom Authentication configured as the AuthenticatePhoton webhook, associated with the AuthenticatePhoton.ccjs Cloud Script File of your app.

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
4. Review Setting up Custom Authentication section below.


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


## Setting Up Custom Authentication (brainCloud ↔ Photon)

To complete the authentication flow between brainCloud and Photon Fusion, you must configure a brainCloud Webhook, link it in the Photon Dashboard, and ensure your Unity client sends the correct AuthenticationValues through Photon when connecting.

The following assumes:

  - You've already cloned this repository

  - You created a new brainCloud app using the BCKart-Photon Template from the Unity Plugin

  - You have working Fusion AppId settings in the Unity project

### 1. Configure the AuthenticatePhoton Webhook in brainCloud

1. Log into the brainCloud portal: https://portalx.braincloudservers.com

2. Navigate to:
  Design → Cloud Code → WebHooks
  (direct link: https://portalx.braincloudservers.com/#/app/design/cloud-code/web-hooks)

3. Look for an existing webhook named AuthenticatePhoton.
  - If it already exists (the template app usually creates it automatically), you may skip to Section 2.

4. If not present, create one:

  - Click + New WebHook

  - Name it: AuthenticatePhoton

  - Point it to the Cloud Script → AuthenticatePhoton.ccjs

  - Set the webhook to enforce the secret using URL parameters, not HTTP headers

  - Save the webhook and copy the final generated webhook URL

  - It will look something like:
  ```https://api.braincloudservers.com/webhook/<appId>/AuthenticatePhoton/<webhookSecret>```


This URL is what Photon will use to validate and authenticate players.

### 2. Configure Photon Custom Authentication

1. Log into the Photon Dashboard: https://dashboard.photonengine.com

2. Select your Fusion App → Manage

3. Under Authentication / Custom Authentication, set:

  - Type: Custom

  - URL: Paste the brainCloud webhook URL from Section 1
  Example:
  ```https://api.braincloudservers.com/webhook/<appId>/AuthenticatePhoton/<webhookSecret> ```


Photon will now call your brainCloud webhook each time a client attempts to join a Fusion session.

### 3. Required Unity Client Code (AuthenticationValues)

The client must pass Custom Authentication to Photon using the player's active brainCloud ProfileId and SessionId.
These values allow the Cloud Script to validate the user and attach them to the session.

Below is the final code snapshot used in GameLauncher.cs:

```// Create a new AuthenticationValues
AuthenticationValues authentication = new AuthenticationValues();

// Setup
authentication.AuthType = CustomAuthenticationType.Custom;
authentication.AddAuthParameter("user", BCManager.Wrapper.Client.ProfileId);
authentication.AddAuthParameter("sessionId", BCManager.Wrapper.Client.SessionID);

_runner.StartGame(new StartGameArgs
{
    GameMode = _gameMode,
    SessionName = BCManager.LobbyManager.LobbyId,
    ObjectProvider = _pool,
    SceneManager = _levelManager,
    PlayerCount = ServerInfo.MaxUsers,
    EnableClientSessionCreation = false,
    AuthValues = authentication // pass the AuthenticationValues
}); 
```

### What This Does

  - ProfileId identifies the brainCloud user

  - SessionId validates their active authenticated session

  - Photon forwards these to the AuthenticatePhoton Webhook

  - The webhook runs AuthenticatePhoton.ccjs, which confirms the session and constructs the return payload Photon expects

This is the required bridge between your brainCloud login and Photon Fusion server-side authentication.

### 4. Summary of Flow

1. Client logs into brainCloud → receives profile + session info

2. Client joins Photon Fusion → passes ProfileId & SessionId via AuthenticationValues

3. Photon calls brainCloud Webhook → Cloud Script validates user session

4. Authentication succeeds → Photon admits user into the session

5. Player joins the Fusion server and gameplay proceeds

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
- **Leaderboards:** Support dynamic leaderboards by use of CloudScript for viewing and posting for each track and game mode type

### EndRace UI

- Updated so the **host sends a signal** to all clients to leave the current session and return to the lobby screen.  

### Photon Removal / brainCloud Lobby Logic

- Photon rooms are no longer used as the authoritative session.  
- `LobbyUI` is now governed entirely by `BCLobbyManager` events.  
- The manager listens for lobby events and loads the game track for all users to connect.  
- Once appropriate events are triggered, connections to the local Photon session are initialized for real-time object handling.  

---

## Future Improvements

- Possible Support for persistent levels and state during host migration.  
- Better Photon Integration for custom server authentication

---

## References

- [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/)  
- [brainCloud API Reference](https://docs.braincloudservers.com/api/introduction/)  
- [Photon Fusion Kart Sample](https://doc.photonengine.com/fusion/current/game-samples/fusion-karts#overview)  
