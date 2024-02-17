using BrainCloud.JsonFx.Json;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public class ServerConnectionManager : NetworkBehaviour
{
    public bool IsDedicatedServer
    {
        get => BrainCloudManager.Singleton.IsDedicatedServer;
    }

    public BrainCloudS2S S2SWrapper
    {
        get => BrainCloudManager.Singleton.S2SWrapper;
    }

    private List<ulong> _connectedClients;
    private Lobby _currentLobby;

    private void Awake()
    {
        Debug.Log("[ServerConnectionManager - Awake()]");

        DontDestroyOnLoad(this);

        if (IsDedicatedServer)
        {
            Debug.Log("[ServerConnectionManager - Awake()] We are dedicated babayy");
            string lobbyId = Environment.GetEnvironmentVariable("LOBBY_ID");
            _connectedClients = new List<ulong>();
            Debug.Log("[ServerConnectionManager - Awake()] Got LobbyID? " + lobbyId );

            var requestJson = new Dictionary<string, object>();
            requestJson["service"] = "lobby";
            requestJson["operation"] = "GET_LOBBY_DATA";
            
            var requestDataJson = new Dictionary<string, object>();
            requestDataJson["lobbyId"] = lobbyId;

            requestJson["data"] = requestDataJson;

            string jsonString = JsonWriter.Serialize(requestJson);
            Debug.Log("[ServerConnectionManager - Awake()] Doin da S2S request");
            S2SWrapper.Request(jsonString, OnLobbyDataResponse);
        }
        else
        {

        }

        BrainCloudManager.Singleton.SceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Connecting);
    }

    private void OnLobbyDataResponse(string response)
    {
        Debug.Log("Lobby data response: " + response);

        Dictionary<string, object> responseJson = JsonReader.Deserialize<Dictionary<string, object>>(response);
        Dictionary<string, object> jsonData = responseJson["data"] as Dictionary<string, object>;

        if ((int)responseJson["status"] == 200)
        {
            _currentLobby = new Lobby(jsonData,
                jsonData["id"] as string);

            Debug.Log("Got lobby definition");
            foreach(UserInfo user in _currentLobby.Members)
            {
                Debug.Log("Member: " + user.Username);
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            BrainCloudManager.Singleton.SceneTransitionHandler.OnClientLoadedScene += ClientLoadedScene;

            //Tell brainCloud we are ready with S2S call
            var requestJson = new Dictionary<string, object>();
            requestJson["service"] = "lobby";
            requestJson["operation"] = "SYS_ROOM_READY";

            var requestDataJson = new Dictionary<string, object>();
            requestDataJson["lobbyId"] = _currentLobby.LobbyID;
            var requestConnectDataJson = new Dictionary<string, object>();
            requestConnectDataJson["address"] = BrainCloudManager.Singleton.UnityTransport.ConnectionData.Address;
            requestConnectDataJson["port"] = BrainCloudManager.Singleton.UnityTransport.ConnectionData.Port;
            requestDataJson["connectInfo"] = requestConnectDataJson;

            requestJson["data"] = requestDataJson;

            string jsonString = JsonWriter.Serialize(requestJson);
            S2SWrapper.Request(jsonString, OnLobbyReadyResponse);

        }
        else
        {
            Debug.Log("Couldn't find lobby data in response");
        }
    }

    private void OnLobbyReadyResponse(string response)
    {
        Debug.Log("Received lobby ready response: " + response);
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (IsDedicatedServer)
        {
            Debug.Log("Client connected: " + clientId);
            Debug.Log("Owner client ID: " + OwnerClientId);
        }
    }

    private void ClientLoadedScene(ulong clientId)
    {
        if (IsDedicatedServer)
        {
            Debug.Log("Client loaded scene: " + clientId);
            Debug.Log("Owner client ID: " + OwnerClientId);
        }
    }
}
