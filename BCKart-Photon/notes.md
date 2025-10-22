- Followed the kart settings from https://doc.photonengine.com/fusion/current/game-samples/fusion-karts#overview 
 - Added BC Plugin To Project
 - Create & hooked up Login Screen
    - Modified Launch Flow 
    - Stripped out Client Info - for brainCloud Info
    - Update User name goes to brainCloud UserName endpoint
    - Added Username non editable to profile screen, and the editable display name
- Update PlayMode screen
   - Quick Find, will find or create a lobby 
   - Added BCLobbyManager to enable RTT, and create the lobby listener
   - Added Quick Join, which is find or create, updated host and join flows

**** Additional Steps to Remove Photon Server acting as a "Room" in order to use brainCloud's Lobby
     Logic. LobbyUI will be governed by the BCLobbyManager events. It will listen and when appropriate
     load the game track for all users to connect. I think we can start making the connection to the photon
     local session whenever after the events

