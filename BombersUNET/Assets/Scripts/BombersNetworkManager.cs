/*
 * UNET doesn't have a clean built-in way to detect that a host has left a match, so a general all-encompasing error and disconnection statement has been made
 * in this network manager to boot player back to the menu should the host leave the game.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using BrainCloudUNETExample.Game.PlayerInput;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using BrainCloud;
using BrainCloudUNETExample.Connection;

public class BombersNetworkManager : NetworkManager
{
    public GameObject m_gameManager;
    public GameObject m_gameInfo;

    public static BCLobbyInfo LobbyInfo;
    public static NetworkConnection LocalConnection;

    public static BombersPlayerController LocalPlayer { get { return m_localPlayer; } set { m_localPlayer = value; m_localPlayer.m_profileId = _BC.Client.ProfileId; } }
    private static BombersPlayerController m_localPlayer;

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        LocalConnection = conn;
        if (SceneManager.GetActiveScene().name == "Game" && m_matchOptions != null)
        {
            // only the owner will init the game info
            if (LobbyInfo != null && LobbyInfo.OwnerProfileId == _BC.Client.ProfileId)
            {
                StartCoroutine(InitializeGameInfo(m_matchOptions));
            }

            m_matchOptions = null;
        }
        base.OnClientSceneChanged(conn);
    }

    IEnumerator InitializeGameInfo(Dictionary<string, object> aMatchOptions)
    {
        Dictionary<string, object> matchOptions = aMatchOptions;

        while (GameObject.Find("GameInfo") == null)
        {
            yield return null;
        }

        GameObject.Find("GameInfo").GetComponent<GameInfo>().Initialize(matchOptions);
    }

    public override void OnClientError(NetworkConnection conn, int errorCode)
    {
        Debug.LogWarning("HitError");
        StopMatchMaker();
        StopClient();
        StartMatchMaker();
        if (GameObject.Find("GameManager") != null)
        {
            GameObject.Find("GameManager").GetComponent<BrainCloudUNETExample.Game.GameManager>().LeaveRoom();
            GameObject.Find("DialogDisplay").GetComponent<BrainCloudUNETExample.Connection.DialogDisplay>().HostLeft();
        }

        base.OnClientError(conn, errorCode);
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        Debug.LogWarning("HitDisconnect");
        if (LocalConnection != null && conn != null && conn == LocalConnection || conn == null || LocalConnection == null)
        {
            StopMatchMaker();
            StopClient();
            StartMatchMaker();
            if (GameObject.Find("GameManager") != null)
                GameObject.Find("GameManager").GetComponent<BrainCloudUNETExample.Game.GameManager>().LeaveRoom();
        }

        base.OnClientDisconnect(conn);
    }

    public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        if (success)
        {
            updateLobbyInfo((ulong)matchInfo.networkId);
            base.OnMatchCreate(success, extendedInfo, matchInfo);
            //LeaveLobby();
        }
        else
        {
            Debug.LogError("Create match failed");
        }
    }

    public override void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        if (success)
        {
            try
            {
                base.OnMatchJoined(success, extendedInfo, matchInfo);
            }
            catch (ArgumentException e)
            {
                Debug.Log("caught ArgumentException " + e);
            }
            catch (Exception e)
            {
                Debug.Log("caught Exception " + e);
            }
        }
        else
        {
            Debug.LogError("Join match failed");
        }

        LeaveLobby();
    }
    public static void RefreshBCVariable()
    {
        if (_BC == null) _BC = GameObject.Find("MainPlayer").GetComponent<BCConfig>().GetBrainCloud();
    }

    public void CreateLobby(Dictionary<string, object> in_matchOptions, string[] in_otherCxIds = null)
    {
        m_matchOptions = in_matchOptions;
        _BC.Client.RegisterRTTLobbyCallback(LobbyCallback);

        Dictionary<string, object> playerExtra = new Dictionary<string, object>();
        playerExtra.Add("nothing", "");

        _BC.LobbyService.CreateLobby("4v4", 76, false, playerExtra, "", m_matchOptions, in_otherCxIds);
    }

    public void FindLobby(Dictionary<string, object> in_matchOptions, string[] in_otherCxIds = null)
    {
        m_matchOptions = in_matchOptions;
        _BC.Client.RegisterRTTLobbyCallback(LobbyCallback);

        Dictionary<string, object> playerExtra = new Dictionary<string, object>();
        playerExtra.Add("nothing", "");
        int[] arry = { 10, 20, 80 };

        Dictionary<string, object> algo = new Dictionary<string, object>();
        algo[OperationParam.LobbyStrategy.Value] = "ranged-percent";
        algo[OperationParam.LobbyAlignment.Value] = "center";
        algo[OperationParam.LobbyRanges.Value] = arry;

        _BC.LobbyService.FindOrCreateLobby("4v4", 76, 2, algo, m_matchOptions, 1, false, playerExtra, "", m_matchOptions, in_otherCxIds);
    }

    private void updateLobbyInfo(ulong in_unetId)
    {
        m_matchOptions["unetId"] = in_unetId;
        _BC.LobbyService.UpdateLobbyConfig(LobbyInfo.LobbyId, m_matchOptions);
    }

    public void LeaveLobby()
    {
        if (LobbyInfo != null && LobbyInfo.LobbyId != "") _BC.LobbyService.LeaveLobby("1238812");// LobbyInfo.LobbyId);

        // cache the channel Id that you are connected to in order to disconnect, this is for demo purposes
        _BC.ChatService.ChannelDisconnect("22814:gl:main");
    }

    private void LobbyCallback(string in_response)
    {
        BrainCloudUNETExample.Matchmaking.Matchmaking matcher = FindObjectOfType<BrainCloudUNETExample.Matchmaking.Matchmaking>();
        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)BrainCloudUnity.BrainCloudPlugin.BCWrapped.JsonFx.Json.JsonReader.Deserialize(in_response);
        Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage["data"];
        Dictionary<string, object> lobbyData = jsonData;

        lobbyData = jsonData;
        if (lobbyData.ContainsKey("lobby"))
        {
            lobbyData = (Dictionary<string, object>)lobbyData["lobby"];
        }

        string operation = (string)jsonMessage["operation"];

        if (BombersNetworkManager.LobbyInfo == null)
        {
            BombersNetworkManager.LobbyInfo = new BCLobbyInfo();
        }
        switch (operation)
        {
            case "SIGNAL":
                {
                    if (matcher != null)
                    {
                        Dictionary<string, object> tempAddLobby = (Dictionary<string, object>)BrainCloudUnity.BrainCloudPlugin.BCWrapped.JsonFx.Json.JsonReader.Deserialize(in_response);
                        matcher.AddLobbyChatMessage(tempAddLobby);
                    }
                }
                break;

            case "STATUS_UPDATE":
            case "JOIN_SUCCESS":
            case "SETTINGS_UPDATE":
            case "MEMBER_LEFT":
            case "MEMBER_JOIN":
            case "MEMBER_UPDATE":
                {
                    bool bHasUNetIdSet = LobbyInfo.Settings != null ? LobbyInfo.Settings.ContainsKey("unetId") : false;
                    string preLobbyState = LobbyInfo.State;

                    LobbyInfo.LobbyJsonDataRaw = lobbyData;
                    LobbyInfo.LobbyId = (string)lobbyData["id"];
                    LobbyInfo.AppId = (string)lobbyData["appId"];
                    LobbyInfo.LobbyType = (string)lobbyData["lobbyType"];
                    LobbyInfo.State = (string)lobbyData["state"];
                    LobbyInfo.OwnerProfileId = (string)lobbyData["owner"];

                    LobbyInfo.Rating = (int)lobbyData["rating"];
                    LobbyInfo.TotalMembers = (int)lobbyData["numMembers"];
                    LobbyInfo.Version = (int)lobbyData["version"];

                    LobbyInfo.LobbyDefinition = (Dictionary<string, object>)lobbyData["lobbyTypeDef"];
                    LobbyInfo.Settings = (Dictionary<string, object>)lobbyData["settings"];

                    // parse array members into list
                    Array tempMembers = (Array)lobbyData["members"];

                    LobbyInfo.Members = new List<BCLobbyMemberInfo>();
                    foreach (Dictionary<string, object> item in tempMembers)
                    {
                        LobbyInfo.Members.Add(new BCLobbyMemberInfo(item));
                    }

                    if (operation == "MEMBER_JOIN" && jsonData.ContainsKey("member"))
                    {
                        if (matcher != null)
                        {
                            matcher.ShowLobby();
                        }
                    }

                    if (operation == "STATUS_UPDATE" && preLobbyState != "starting" &&
                        LobbyInfo.State == "starting" &&
                        LobbyInfo.OwnerProfileId == _BC.Client.ProfileId)
                    {
                        CreateUNETMatch();
                    }
                    else if (operation == "SETTINGS_UPDATE" &&
                        !bHasUNetIdSet && LobbyInfo.Settings.ContainsKey("unetId") &&
                        LobbyInfo.OwnerProfileId != _BC.Client.ProfileId)
                    {
                        JoinUNETMatch();
                    }
                }
                break;

            case "DISBANDED":
                {
                    if (LobbyInfo != null && LobbyInfo.LobbyId == (string)lobbyData["id"])
                    {
                        _BC.Client.DeregisterRTTLobbyCallback();
                        LobbyInfo = null;
                    }
                }
                break;
            case "JOIN_FAIL":
            case "ROOM_ASSIGNED":
            case "ROOM_CONNECT":
                {
                }
                break;

            default: { } break;
        }
    }

    public void CreateOrJoinUNETMatch(BCLobbyMemberInfo member)
    {
        // lets create the match
        if (!LobbyInfo.Settings.ContainsKey("unetId") && member.ProfileId == LobbyInfo.OwnerProfileId)
        {
            string gameName = LobbyInfo.Settings["gameName"] as string;
            uint maxPlayers = (uint)(int)LobbyInfo.Settings["maxPlayers"];

            matchMaker.CreateMatch(gameName, maxPlayers, true, "", "", "", 0, 0, this.OnMatchCreate);
        }
        // lets join the match 
        else if (member.ProfileId != LobbyInfo.OwnerProfileId && member.ProfileId == _BC.Client.ProfileId)
        {
            JoinUNETMatch();
        }
    }

    private void CreateUNETMatch()
    {
        BCLobbyMemberInfo member = LobbyInfo.GetMemberWithProfileId(_BC.Client.ProfileId);
        CreateOrJoinUNETMatch(member);
    }

    private void JoinUNETMatch()
    {
        long netId;
        try
        {
            netId = (long)LobbyInfo.Settings["unetId"];
        }
        catch (Exception)
        {

            long.TryParse(LobbyInfo.Settings["unetId"] as string, out netId);
        }

        m_matchOptions = null;
        _BC.Client.DeregisterRTTLobbyCallback();
        matchMaker.JoinMatch((NetworkID)netId, "", "", "", 0, 0, this.OnMatchJoined);
    }

    public void ConnectToGlobalChat()
    {
        _BC.Client.RegisterRTTChatCallback(chatCallback);
        _BC.Client.RegisterRTTEventCallback(eventCallback);

        // do a get channel call instead of manually appending these, this is for demo purposes
        _BC.ChatService.ChannelConnect(_BC.Client.AppId + ":gl:main", 25, onChannelConnected);
    }

    private void onChannelConnected(string in_json, object obj)
    {
        BrainCloudUNETExample.Matchmaking.Matchmaking matcher = FindObjectOfType<BrainCloudUNETExample.Matchmaking.Matchmaking>();
        if (matcher == null) return;

        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)BrainCloudUnity.BrainCloudPlugin.BCWrapped.JsonFx.Json.JsonReader.Deserialize(in_json);

        Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage["data"];
        Array firstChatMessagesData = (Array)jsonData["messages"];

        Dictionary<string, object> messageData = null;
        for (int i = 0; i < firstChatMessagesData.Length; ++i)
        {
            messageData = firstChatMessagesData.GetValue(i) as Dictionary<string, object>;
            matcher.AddGlobalChatMessage(messageData);
        }
    }

    public void DisconnectGlobalChat()
    {
        _BC.Client.DeregisterRTTChatCallback();
        _BC.Client.DeregisterRTTEventCallback();
    }

    private void chatCallback(string in_message)
    {
        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)BrainCloudUnity.BrainCloudPlugin.BCWrapped.JsonFx.Json.JsonReader.Deserialize(in_message);
        if (jsonMessage.ContainsKey("operation"))
        {
            BrainCloudUNETExample.Matchmaking.Matchmaking matcher = FindObjectOfType<BrainCloudUNETExample.Matchmaking.Matchmaking>();

            string operation = jsonMessage["operation"] as string;

            switch (operation)
            {
                case "INCOMING":
                    {
                        if (matcher != null)
                        {
                            matcher.AddGlobalChatMessage(jsonMessage);
                        }
                    }
                    break;

                case "DELETE":
                    {
                        if (matcher != null)
                        {
                            matcher.OnChatMessageDeleted(jsonMessage);
                        }
                    }
                    break;

                case "UPDATE":
                    {
                        if (matcher != null)
                        {
                            matcher.OnChatMessageUpdated(jsonMessage);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private void eventCallback(string in_message)
    {
        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)BrainCloudUnity.BrainCloudPlugin.BCWrapped.JsonFx.Json.JsonReader.Deserialize(in_message);
        switch (jsonMessage["operation"] as string)
        {
            case "GET_EVENTS":
                {
                    Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage["data"];
                    _BC.Client.EventService.DeleteIncomingEvent(jsonData["evId"] as string);

                    switch (jsonData["eventType"] as string)
                    {
                        case "OFFER_JOIN_LOBBY":
                            {
                                // bring up the offer to join display
                                // on the requested client
                                BrainCloudUNETExample.Matchmaking.Matchmaking matcher = FindObjectOfType<BrainCloudUNETExample.Matchmaking.Matchmaking>();
                                if (matcher)
                                {
                                    Dictionary<string, object> eventData = (Dictionary<string, object>)jsonData["eventData"];
                                    matcher.DisplayJoinLobbyOffer(eventData["profileId"] as string, eventData["userName"] as string);
                                }
                            }
                            break;

                        case "CONFIRM_JOIN_LOBBY":
                            {
                                // they confirmed to join the lobby!
                                Dictionary<string, object> eventData = (Dictionary<string, object>)jsonData["eventData"];
                                string[] cxIds = { eventData["lastConnectionId"] as string };

                                Dictionary<string, object> matchOptions = new Dictionary<string, object>();
                                matchOptions["gameTime"] = 300;
                                matchOptions["isPlaying"] = 0;
                                matchOptions["mapLayout"] = 0;
                                matchOptions["mapSize"] = 1;
                                matchOptions["gameName"] = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().PlayerName + "'s Room";
                                matchOptions["maxPlayers"] = 8;
                                matchOptions["lightPosition"] = 0;

                                WaitOnLobbyJoin();
                                // Find or create with someone else!
                                FindLobby(matchOptions, cxIds);
                            }
                            break;

                        default: break;
                    }
                }
                break;

            default: break;
        }
    }

    public static void WaitOnLobbyJoin()//Dictionary<string, object> in_matchOptions)
    {
        // if matchmaking, go to find lobby state
        BrainCloudUNETExample.Matchmaking.Matchmaking matcher = FindObjectOfType<BrainCloudUNETExample.Matchmaking.Matchmaking>();
        if (matcher)
        {
            matcher.HideControls();
            matcher.OnJoinRoomState();
            BombersNetworkManager thisInstance = BombersNetworkManager.singleton as BombersNetworkManager;
            _BC.Client.RegisterRTTLobbyCallback(thisInstance.LobbyCallback);

            // TODO delete this once join with others is working
            //thisInstance.FindLobby(in_matchOptions);
        }
    }

    public static BrainCloudWrapper _BC;
    public static Dictionary<string, object> m_matchOptions;

    void OnApplicationQuit()
    {
        LeaveLobby();
        DisconnectGlobalChat();

        // force whatever is aroudn to be sent out
        _BC.Update();
    }
}



/// <summary>
/// /////
/// </summary>
public class BCLobbyInfo
{
    public string LobbyId;
    public string AppId;
    public string LobbyType;
    public string State;
    public string OwnerProfileId;

    public int Rating;
    public int TotalMembers;
    public int Version;

    public Dictionary<string, object> LobbyJsonDataRaw;
    public Dictionary<string, object> LobbyDefinition;
    public Dictionary<string, object> Settings;
    public List<BCLobbyMemberInfo> Members;

    public bool IsOwner(string in_profile)
    {
        return OwnerProfileId == in_profile;
    }

    public BCLobbyMemberInfo GetMemberWithProfileId(string in_profileId)
    {
        BCLobbyMemberInfo toReturn = null;

        foreach (BCLobbyMemberInfo member in Members)
        {
            if (in_profileId == member.ProfileId)
            {
                toReturn = member;
                break;
            }
        }

        return toReturn;
    }

    public string GetTeamCodeWithProfileId(string in_profileId)
    {
        string toReturn = "green";

        foreach (BCLobbyMemberInfo member in Members)
        {
            if (in_profileId == member.ProfileId)
            {
                toReturn = member.Team;
                break;
            }
        }

        return toReturn;
    }

    public string GetOppositeTeamCodeWithProfileId(string in_profileId)
    {
        return GetTeamCodeWithProfileId(in_profileId) == "green" ? "red" : "green";
    }

    public bool IsMemberAlreadyInLobby(string in_profileId, out int in_index)
    {
        bool bToReturn = false;
        in_index = -1;
        BCLobbyMemberInfo member;
        for (int i = 0; i < Members.Count && !bToReturn; ++i)
        {
            member = Members[i];
            if (in_profileId == member.ProfileId)
            {
                bToReturn = true;
                in_index = i;
            }
        }

        return bToReturn;
    }
}

/// <summary>
/// //////
/// </summary>
public class BCLobbyMemberInfo
{
    public BCLobbyMemberInfo(Dictionary<string, object> in_dict)
    {
        Name = (string)in_dict["name"];
        PictureURL = (string)in_dict["pic"];
        Team = (string)in_dict["team"];

        if (in_dict.ContainsKey("cx")) CXId = (string)in_dict["cx"];

        Rating = (int)in_dict["rating"];

        IsReady = (bool)in_dict["isReady"];

        ExtraData = (Dictionary<string, object>)in_dict["extra"];

        if (in_dict.ContainsKey("profileId")) ProfileId = (string)in_dict["profileId"];
        else if (ExtraData.ContainsKey("profileId")) ProfileId = (string)ExtraData["profileId"];
    }

    public string ProfileId;
    public string Name;
    public string PictureURL;
    public string Team;

    public string CXId;

    public int Rating;
    public bool IsReady;

    public Dictionary<string, object> ExtraData;
}
