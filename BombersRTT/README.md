# Bombers RTT

<p align="center">
    <img  src="../_screenshots/x_bombers.png?raw=true">
    Lobby, Chat, Presence, and RTT Multiplayer
</p>

---

_Places to get started with Lobbies_

[BombersNetworkManager.cs](https://github.com/getbraincloud/examples-unity/blob/master/BombersRTT/Assets/Scripts/Networking/BombersNetworkManager.cs#L65) | Create a lobby for others to join.

```csharp
GCore.Wrapper.LobbyService.CreateLobby(m_lastSelectedRegionType, 76, false, playerExtra, "", s_matchOptions, in_otherCxIds);
```

[BombersNetworkManager.cs](https://github.com/getbraincloud/examples-unity/blob/master/BombersRTT/Assets/Scripts/Networking/BombersNetworkManager.cs#L85) | Search for a lobby. A lobby will be made if none exist.

```csharp
GCore.Wrapper.LobbyService.FindOrCreateLobby(m_lastSelectedRegionType, 76, 2, algo,s_matchOptions, 1, false, playerExtra, "", s_matchOptions, in_otherCxIds);
```

[FriendCell.cs](https://github.com/getbraincloud/examples-unity/blob/master/BombersRTT/Assets/Scripts/UI/FriendCell.cs#L68) | Join a match that your friend is currently in.

```csharp
GCore.Wrapper.LobbyService.JoinLobby(m_data.Presence.LobbyId, true, playerExtra, "");
```

---

_Places to get started with chat_

[BombersNetworkManager.cs](https://github.com/getbraincloud/examples-unity/blob/master/BombersRTT/Assets/Scripts/Networking/BombersNetworkManager.cs#L1092) | Join the global chat.

```csharp
 GCore.Wrapper.ChatService.ChannelConnect(GCore.Wrapper.Client.AppId + ":gl:main", 25, onChannelConnected);
```

[MainMenuState.cs](https://github.com/getbraincloud/examples-unity/blob/d54976d03314243ffc9c3753e3bb7ad5d56531a9/BombersRTT/Assets/Scenes/States/MainMenuState/MainMenuState.cs#L271) | Post a chat message to the global chat.

```csharp
GCore.Wrapper.ChatService.PostChatMessage(GCore.Wrapper.Client.AppId + ":gl:main", in_field.text,
                    JsonWriter.Serialize(jsonData));
```

---

_Places to get started with presence_

[MainMenuState.cs](https://github.com/getbraincloud/examples-unity/blob/master/BombersRTT/Assets/Scenes/States/MainMenuState/MainMenuState.cs#L168) | Set your presence visibility to true, to allow others to get your presence updates.

```csharp
GCore.Wrapper.Client.PresenceService.SetVisibility(true);
```

[MainMenuState.cs](https://github.com/getbraincloud/examples-unity/blob/master/BombersRTT/Assets/Scenes/States/MainMenuState/MainMenuState.cs#L160-L161) | Start listening to the presence of friends.

```csharp
GCore.Wrapper.RTTService.RegisterRTTPresenceCallback(OnPresenceCallback);
GCore.Wrapper.Client.PresenceService.RegisterListenersForFriends(platform, true, presenceSuccess);
```

[GPlayerMgr.cs](https://github.com/getbraincloud/examples-unity/blob/master/BombersRTT/Assets/Framework/BaseManagers/GPlayerMgr.cs#L1089) | Update your current presence to let friends know what you are doing.

```csharp
GCore.Wrapper.Client.PresenceService.UpdateActivity(JsonWriter.Serialize(activity));
```

---

_Places to get started with multiplayer_

[BombersNetworkManager.cs](https://github.com/getbraincloud/examples-unity/blob/master/BombersRTT/Assets/Scripts/Networking/BombersNetworkManager.cs#L470) | After your lobby is ready, start the match by connecting to the relay server.

```csharp
GCore.Wrapper.Client.RelayService.Connect(connectionType, connectionOptions, null, onRSConnectError);
```

[BombersNetworkManager.cs](https://github.com/getbraincloud/examples-unity/blob/master/BombersRTT/Assets/Scripts/Networking/BombersNetworkManager.cs#L1062) | Send multiplayer command data from the current player to the server.

```csharp
 GCore.Wrapper.Client.RelayService.Send(Encoding.ASCII.GetBytes(JsonWriter.Serialize(json)), BrainCloud.Internal.RelayComms.TO_ALL_PLAYERS, true, true, 0);
```

[BombersNetworkManager.cs](https://github.com/getbraincloud/examples-unity/blob/master/BombersRTT/Assets/Scripts/Networking/BombersNetworkManager.cs#L577) | Handle the realtime data for your multiplayer game.

```csharp
GCore.Wrapper.Client.RelayService.RegisterDataCallback(onDataRecv);
...

private void onDataRecv(byte[] in_data)
        {
            Dictionary<string, object> jsonMessage = readRSData(in_data);
            ...
```

---

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
