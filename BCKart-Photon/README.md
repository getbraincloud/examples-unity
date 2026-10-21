# brainCloud + Photon Integration (BCKart Example)

## Overview

**Target Audience:** Unity developers familiar with multiplayer networking  

This project demonstrates a complete multiplayer game flow using **Photon** for network object synchronization and **brainCloud** for user authentication, lobby management, and matchmaking.  

The core integration removes Photon’s default room-based logic in favor of brainCloud-managed lobbies. Photon is used locally for real-time object handling, while brainCloud manages authentication, lobby creation, matchmaking, and leaderboards.  If the host leaves during gameplay, the match will be lost, all remaining users will return to the brainCloud Lobby. This way the lobby remains, and while not active in the gameplay session, others will be able to join the Lobby.

---

## Getting Started

Mac Users, you may need to run ** xattr -r -d com.apple.quarantine ./BCKart-Photon.app ** and accept its permissions since the app is unsigned.

Please ensure that you have setup your Fusion AppId, and configure it in Unity via Tools -> Fusion -> Realtime Settings. The app will not work until you do so.

It is highly recommended to secure your Fusion App with Custom Authentication using brainCloud's built-in Photon integration (Design → Integrations → Multiplayer in the brainCloud portal). See Setting Up Custom Authentication below.

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

brainCloud has a built-in Photon integration that validates a player's brainCloud session when they connect to Photon Fusion. No webhook or cloud script is needed. You turn on the integration in the brainCloud portal, register its URL and secret in the Photon Dashboard, and the Unity client sends its brainCloud ProfileId and SessionId through Photon's AuthenticationValues.

The following assumes:

  - You've already cloned this repository

  - You created a new brainCloud app using the BCKart-Photon Template from the Unity Plugin

  - You have working Fusion AppId settings in the Unity project

### 1. Enable the Photon Integration in brainCloud

1. Log into the brainCloud portal: https://portalx.braincloudservers.com

2. Select your app and navigate to:
  Design → Integrations → Multiplayer

3. On the **Photon** card, tick **Enable**, then click **Save Changes**.

4. Copy the two values the card shows:

  - **Authentication URL**. It looks like:
  ```https://api.braincloudservers.com/photonauth/<appId>```

  - **Secret**. It is generated on first save. You can also enter your own, or click **Regenerate** to rotate it.

> Regenerating the secret takes effect when you save. Every Photon connection fails until the new secret is entered in the Photon Dashboard.

### 2. Configure Photon Custom Authentication

1. Log into the Photon Dashboard: https://dashboard.photonengine.com

2. Select your Fusion App → Manage

3. Under Authentication / Custom Authentication, add a provider:

  - Type: Custom

  - URL: the **Authentication URL** from Section 1

  - Key/value pair: key `secret`, value = the **Secret** from Section 1

Photon now calls brainCloud each time a client connects to a Fusion session.

### 3. Required Unity Client Code (AuthenticationValues)

The client must pass Custom Authentication to Photon using the player's active brainCloud ProfileId and SessionId.
brainCloud uses these values to confirm the player has a valid session for your app.

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

The parameter names `user` and `sessionId` are required by the brainCloud endpoint. Don't rename them.

### What This Does

  - ProfileId identifies the brainCloud user

  - SessionId validates their active authenticated session

  - Photon forwards these, along with the secret, to the brainCloud Authentication URL

  - brainCloud confirms the session belongs to that profile and to your app, then returns the ResultCode Photon expects

This is the required bridge between your brainCloud login and Photon Fusion server-side authentication.

### 4. Summary of Flow

1. Client logs into brainCloud → receives profile + session info

2. Client joins Photon Fusion → passes ProfileId & SessionId via AuthenticationValues

3. Photon calls the brainCloud Authentication URL → brainCloud validates the session

4. Authentication succeeds → Photon admits user into the session

5. Player joins the Fusion server and gameplay proceeds

> Each validation is metered as an API call against your app and is rate limited per app.

> If your app was set up with the older **AuthenticatePhoton** webhook and `AuthenticatePhoton.ccjs` cloud script, switch the Photon Dashboard URL to the Authentication URL above. You can then delete the webhook and the script.

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

---

## References

- [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/)  
- [brainCloud API Reference](https://docs.braincloudservers.com/api/introduction/)  
- [Photon Fusion Kart Sample](https://doc.photonengine.com/fusion/current/game-samples/fusion-karts#overview)  
