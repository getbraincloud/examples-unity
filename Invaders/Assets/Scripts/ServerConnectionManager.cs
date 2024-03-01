using BrainCloud.JsonFx.Json;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public class ServerConnectionManager : NetworkBehaviour
{
    public bool IsDedicatedServer;

    public BrainCloudS2S S2SWrapper
    {
        get => BrainCloudManager.Singleton.S2SWrapper;
    }

    private Dictionary<ulong, UserInfo> _connectedClients;
    private Lobby _currentLobby;

    private Coroutine _serverShutdownCR;


    private void Awake()
    {
        Debug.Log("[ServerConnectionManager - Awake()]");

        IsDedicatedServer = Application.isBatchMode && !Application.isEditor;

        DontDestroyOnLoad(this);

        if (IsDedicatedServer)
        {
            Debug.Log("[ServerConnectionManager - Awake()] We are dedicated");
            string lobbyId = Environment.GetEnvironmentVariable("LOBBY_ID");
            _connectedClients = new Dictionary<ulong, UserInfo>();
            Debug.Log("[ServerConnectionManager - Awake()] Got LobbyID? " + lobbyId );

            var requestJson = new Dictionary<string, object>();
            requestJson["service"] = "lobby";
            requestJson["operation"] = "GET_LOBBY_DATA";
            
            var requestDataJson = new Dictionary<string, object>();
            requestDataJson["lobbyId"] = lobbyId;

            requestJson["data"] = requestDataJson;

            string jsonString = JsonWriter.Serialize(requestJson);
            S2SWrapper.Request(jsonString, OnLobbyDataResponse);
        }
    }

    private void Start()
    {
        //connect to server
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
        BrainCloudManager.Singleton.SceneTransitionHandler.OnClientLoadedScene += ClientLoadedScene;

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
            Debug.Log("Invalid lobby, shutting down server");
            Application.Quit();
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

            if(_serverShutdownCR != null)
            {
                Debug.Log("Player connected during shutdown timer, canceling shutdown");
                StopCoroutine(_serverShutdownCR);
                _serverShutdownCR = null;
            }
        }
        else
        {
            //Send server validation request
            UserInfo localUserInfo = BrainCloudManager.Singleton.LocalUserInfo;
            Debug.Log($"Sending request for cId:{clientId} pId:{localUserInfo.ProfileID} passCode:{localUserInfo.PassCode}");
            ValidateConnectedClientServerRpc(clientId, localUserInfo.ProfileID, localUserInfo.PassCode);
        }
    }

    private void OnClientDisconnectedCallback(ulong clientId)
    {
        if (IsDedicatedServer)
        {
            Debug.Log("Client disconnected: " + clientId);
            Debug.Log("Owner client ID: " + OwnerClientId);

            if (_connectedClients.ContainsKey(clientId))
            {
                _connectedClients.Remove(clientId);
            }

            CheckServerEmpty();
        }
    }

    public void CheckServerEmpty()
    {
        Debug.Log($"Checking if servers empty: Players left: ${_connectedClients.Count}");
        if (IsDedicatedServer)
        {
            if (_connectedClients.Count == 0)
            {
                if (_serverShutdownCR != null)
                {
                    StopCoroutine(_serverShutdownCR);
                }

                Debug.Log("All players left the server, starting shutdown timer");

                _serverShutdownCR = StartCoroutine(InitServerShutdownTimer());
            }
        } 
    }

    private IEnumerator InitServerShutdownTimer()
    {
        //shut down after 15 seconds, unless a player reconnects in that time, then stop and clear this coroutine
        yield return new WaitForSeconds(15f);

        Debug.Log("Server been empty for 15 seconds. Shutting down.");
        Application.Quit();
    }

    private void ClientLoadedScene(ulong clientId)
    {
        if (IsDedicatedServer)
        {
            Debug.Log("Client loaded scene: " + clientId);
            Debug.Log("Owner client ID: " + OwnerClientId);
        }
    }

    private void OnAllLoaded()
    {
        //everyone's here, allegedly.
        //start the game
        Debug.Log("Everyone is here let's load up the game");
        BrainCloudManager.Singleton.SceneTransitionHandler.SwitchScene("InGame");
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckAllPlayerConnectedServerRpc()
    {
        if(_connectedClients.Count == _currentLobby.Members.Count)
        {
            OnAllLoaded();
        }
    }

    [ClientRpc]
    public void ValidateConnectedClientClientRpc(bool isValid)
    {
        Debug.Log($"Received server validation. Am I valid? ${isValid}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void ValidateConnectedClientServerRpc(ulong clientId, string playerId, string passCode)
    {
        Debug.Log($"Received validation request for cId:{clientId} pId:{playerId} passCode:{passCode}");
        //match with playerId and passcode found in lobby data
        if(_currentLobby != null)
        {
            UserInfo foundMember = _currentLobby.FindMemberByPlayerId(playerId);
            if(foundMember != null)
            {
                //check pass code
                if (foundMember.PassCode.Equals(passCode))
                {
                    //player connection is valid
                    Debug.Log($"Player {playerId} has valid passcode");
                    _connectedClients.Add(clientId, foundMember);
                    CheckAllPlayerConnectedServerRpc();
                }
                else
                {
                    //player connection invalid - kick them out
                    Debug.LogWarning($"Player {playerId} has invalid passcode");
                }
            }
            else
            {
                //member not found in lobby data
            }
        }
        else
        {
            //current lobby data is invalid
        }
    }
}
