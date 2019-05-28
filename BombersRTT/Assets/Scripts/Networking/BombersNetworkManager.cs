

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using BrainCloud;
using Gameframework;
using BrainCloudUNETExample.Game;
using BrainCloud.JsonFx.Json;
using System.Text;

namespace BrainCloudUNETExample
{
    public class BombersNetworkManager : SingletonBehaviour<BombersNetworkManager> //NetworkManager
    {
        public static BCLobbyInfo LobbyInfo;
        public static BCRoomServerInfo RoomServerInfo;

        public static BombersPlayerController LocalPlayer { get { return m_localPlayer; } set { m_localPlayer = value; if (value != null) { m_localPlayer.ProfileId = GCore.Wrapper.Client.ProfileId; } } }
        private static BombersPlayerController m_localPlayer;

        public IEnumerator InitializeGameInfo()
        {
            GPlayerMgr.Instance.UpdateActivity(GPlayerMgr.LOCATION_GAME, GPlayerMgr.STATUS_PLAYING, "");

            while (GameObject.Find("GameInfo") == null)
            {
                yield return YieldFactory.GetWaitForEndOfFrame();
            }

            GameObject.Find("GameInfo").GetComponent<GameInfo>().Initialize();
            GameObject.Find("GameManager").GetComponent<GameManager>().ForcedStart();
        }

        IEnumerator InitializeGameInfo(Dictionary<string, object> aMatchOptions)
        {
            GPlayerMgr.Instance.UpdateActivity(GPlayerMgr.LOCATION_GAME, GPlayerMgr.STATUS_PLAYING, "");

            Dictionary<string, object> matchOptions = aMatchOptions;
            GameObject gameInfo = GameObject.Find("GameInfo");
            while (gameInfo == null)
            {
                yield return YieldFactory.GetWaitForEndOfFrame();
                gameInfo = GameObject.Find("GameInfo");
            }

            gameInfo.GetComponent<GameInfo>().Initialize(matchOptions);
            if (LobbyInfo.OwnerProfileId == GCore.Wrapper.Client.ProfileId)
            {
                GameObject.Find("GameManager").GetComponent<GameManager>().ForcedStart();
            }
        }

        public void CreateLobby(Dictionary<string, object> in_matchOptions, string[] in_otherCxIds = null)
        {
            s_matchOptions = in_matchOptions;
            GCore.Wrapper.RTTService.RegisterRTTLobbyCallback(LobbyCallback);

            CancelFindRequest();

            Dictionary<string, object> playerExtra = new Dictionary<string, object>();
            playerExtra.Add("cxId", GCore.Wrapper.Client.RTTConnectionID);
            playerExtra.Add(GBomberRTTConfigManager.JSON_GOLD_WINGS, GPlayerMgr.Instance.GetCurrencyBalance(GBomberRTTConfigManager.CURRENCY_GOLD_WINGS) > 0 ? true : false);
            GCore.Wrapper.LobbyService.CreateLobby(m_lastSelectedRegionType, 76, false, playerExtra, "", s_matchOptions, in_otherCxIds);
        }

        public void FindLobby(Dictionary<string, object> in_matchOptions, string[] in_otherCxIds = null)
        {
            s_matchOptions = in_matchOptions;
            GCore.Wrapper.RTTService.RegisterRTTLobbyCallback(LobbyCallback);

            CancelFindRequest();

            Dictionary<string, object> playerExtra = new Dictionary<string, object>();
            playerExtra.Add("cxId", GCore.Wrapper.Client.RTTConnectionID);
            playerExtra.Add(GBomberRTTConfigManager.JSON_GOLD_WINGS, GPlayerMgr.Instance.GetCurrencyBalance(GBomberRTTConfigManager.CURRENCY_GOLD_WINGS) > 0 ? true : false);
            int[] arry = { 10, 20, 80 };

            Dictionary<string, object> algo = new Dictionary<string, object>();
            algo[OperationParam.LobbyStrategy.Value] = "ranged-percent";
            algo[OperationParam.LobbyAlignment.Value] = "center";
            algo[OperationParam.LobbyRanges.Value] = arry;

            GCore.Wrapper.LobbyService.FindOrCreateLobby(m_lastSelectedRegionType, 76, 2, algo, s_matchOptions, 1, false, playerExtra, "", s_matchOptions, in_otherCxIds);
        }

        private string m_lastSelectedRegionType = "4v4_can";
        public void SetSelectedRegion( string in_region)
        {
            m_lastSelectedRegionType = in_region;
        }

        public void CancelFindRequest()
        {
            GCore.Wrapper.LobbyService.CancelFindRequest(m_lastSelectedRegionType);
        }

        private void updateLobbyInfo(ulong in_unetId)
        {
            s_matchOptions["unetId"] = in_unetId;
            GCore.Wrapper.LobbyService.UpdateSettings(LobbyInfo.LobbyId, s_matchOptions);
        }

        private string m_continueJoinOwnerId = "";
        public void ContinueJoinRoom(string in_newOwner)
        {
            m_continueJoinOwnerId = in_newOwner;
            GCore.Wrapper.Client.RelayService.DeregisterDataCallback();
            GCore.Wrapper.Client.RelayService.Disconnect();
            StopCoroutine("startingInCountDown");
            GStateManager.Instance.ChangeState(MainMenuState.STATE_NAME);
            GStateManager.Instance.OnInitializeDelegate += onMainMenuLoadedFromContinueGame;
        }

        private void onMainMenuLoadedFromContinueGame(BaseState in_state)
        {
            if (in_state as MainMenuState != null)
            {
                GStateManager.Instance.OnInitializeDelegate -= onMainMenuLoadedFromContinueGame;

                bool bWaitForMatch = false;
                List<string> cxIds = new List<string>();
                foreach (LobbyMemberInfo member in LobbyInfo.Members)
                {
                    if (member.LobbyReadyUp && member.ProfileId == GCore.Wrapper.Client.ProfileId)
                    {
                        bWaitForMatch = true;
                    }
                    // if they are readied up 
                    if (member.LobbyReadyUp) cxIds.Add(member.CXId);
                }

                if (bWaitForMatch)
                {
                    GCore.Wrapper.RTTService.RegisterRTTLobbyCallback(LobbyCallback);
                    GStateManager.Instance.PushSubState(JoiningGameSubState.STATE_NAME);
                }

                // the new owner to create the match
                if (m_continueJoinOwnerId == GCore.Wrapper.Client.ProfileId)
                {
                    // they confirmed to join the lobby!
                    Dictionary<string, object> matchOptions = new Dictionary<string, object>();
                    matchOptions["gameTime"] = (int)LobbyInfo.Settings["gameTime"];
                    matchOptions["gameTimeSel"] = (int)LobbyInfo.Settings["gameTimeSel"];
                    matchOptions["isPlaying"] = 0;
                    matchOptions["mapLayout"] = (int)LobbyInfo.Settings["mapLayout"];
                    matchOptions["mapSize"] = (int)LobbyInfo.Settings["mapSize"];
                    matchOptions["gameName"] = GPlayerMgr.Instance.PlayerData.PlayerName + "'s Room";
                    matchOptions["maxPlayers"] = (int)LobbyInfo.Settings["maxPlayers"];
                    matchOptions["lightPosition"] = (int)LobbyInfo.Settings["lightPosition"];

                    // Find or create with someone else!
                    CreateLobby(matchOptions, cxIds.ToArray());
                }

                m_continueJoinOwnerId = "";
            }
        }

        public void DestroyMatch()
        {
            StopCoroutine("startingInCountDown");
            GCore.Wrapper.Client.RelayService.DeregisterDataCallback();
            GCore.Wrapper.Client.RelayService.Disconnect();
            GStateManager stateMgr = GStateManager.Instance;
            if (stateMgr.CurrentStateId == MainMenuState.STATE_NAME)
            {
                stateMgr.ClearAllSubStates();
            }
            else
            {
                stateMgr.ChangeState(MainMenuState.STATE_NAME);
            }
        }

        public void LeaveLobby()
        {
            if (LobbyInfo != null && LobbyInfo.LobbyId != "")
            {
                GCore.Wrapper.RelayService.Disconnect();
                GCore.Wrapper.LobbyService.LeaveLobby(LobbyInfo.LobbyId);
                LobbyInfo.LobbyId = "";
                GCore.Wrapper.RTTService.DeregisterRTTLobbyCallback();
            };
        }

        public void LobbyCallback(string in_response)
        {
            MainMenuState mainMenu = GStateManager.Instance.CurrentState as MainMenuState;
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_response);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            Dictionary<string, object> lobbyData = jsonData;

            lobbyData = jsonData;
            if (lobbyData.ContainsKey("lobby"))
            {
                lobbyData = (Dictionary<string, object>)lobbyData["lobby"];
            }

            string operation = (string)jsonMessage["operation"];

            if (LobbyInfo == null)
            {
                LobbyInfo = new BCLobbyInfo();
            }

            string lobbyId = jsonData.ContainsKey("lobbyId") ? (string)jsonData["lobbyId"] : "";
            if (lobbyId != "") LobbyInfo.LobbyId = lobbyId;
            switch (operation)
            {
                case "SIGNAL":
                    {
                        if (mainMenu != null)
                        {
                            Dictionary<string, object> tempAddLobby = (Dictionary<string, object>)JsonReader.Deserialize(in_response);
                            Dictionary<string, object> jsonLobbyData = tempAddLobby.ContainsKey("data") ?
                                                                    (Dictionary<string, object>)tempAddLobby["data"] : tempAddLobby;

                            Dictionary<string, object> signalData = jsonLobbyData.ContainsKey("signalData") ?
                                                                    (Dictionary<string, object>)jsonLobbyData["signalData"] : jsonLobbyData;
                            if (signalData.ContainsKey("compression") ||
                                signalData.ContainsKey("maplayout") ||
                                signalData.ContainsKey("mapsize") ||
                                signalData.ContainsKey("gametime") ||
                                signalData.ContainsKey("gameName"))
                            {
                                LobbySubState baseState = (LobbySubState)GStateManager.Instance.FindSubState(LobbySubState.STATE_NAME);
                                if (signalData.ContainsKey("compression"))
                                {
                                    BaseNetworkBehavior.MSG_ENCODED = (int)signalData["compression"];
                                    baseState.UpdateCompressionDropdown((int)BaseNetworkBehavior.MSG_ENCODED);
                                }
                                if (signalData.ContainsKey("maplayout"))
                                {
                                    baseState.UpdateMapLayoutDropdown((int)signalData["maplayout"]);
                                }
                                if (signalData.ContainsKey("mapsize"))
                                {
                                    baseState.UpdateMapSizeDropdown((int)signalData["mapsize"]);
                                }
                                if (signalData.ContainsKey("gametime"))
                                {
                                    baseState.UpdateGameTimeDropdown((int)signalData["gametime"]);
                                }
                                if (signalData.ContainsKey("gameName"))
                                {
                                    baseState.UpdateGameName((string)signalData["gameName"]);
                                }
                            }
                            else
                                mainMenu.AddLobbyChatMessage(tempAddLobby);
                        }
                    }
                    break;

                case "MEMBER_JOIN":
                    ReadLobbyInfo(lobbyData);

                    if (jsonData.ContainsKey("member"))
                    {
                        LobbyMemberInfo newMember = new LobbyMemberInfo(jsonData["member"] as Dictionary<string, object>);
                        if (newMember.ProfileId == GCore.Wrapper.Client.ProfileId)
                        {
                            BaseState joinGameState = GStateManager.Instance.FindSubState(JoiningGameSubState.STATE_NAME);
                            bool canJoin = joinGameState != null;
                            if (canJoin)
                            {
                                GStateManager.Instance.PopSubState(joinGameState.StateInfo);
                            }

                            if (canJoin && mainMenu != null)
                            {
                                mainMenu.ShowLobby();
                            }
                            else
                            {
                                LeaveLobby();
                            }
                        }
                    }
                    break;
                case "STATUS_UPDATE":
                case "JOIN_SUCCESS":
                case "SETTINGS_UPDATE":
                case "MEMBER_LEFT":
                case "MEMBER_UPDATE":
                case "STARTING":
                    {
                        ReadLobbyInfo(lobbyData);
                    }
                    break;

                case "DISBANDED":
                    {
                        if (LobbyInfo != null && LobbyInfo.LobbyId == lobbyId)
                        {
                            Dictionary<string, object> reasonDict = (Dictionary<string, object>)jsonData["reason"];
                            int codeInt = (int)reasonDict["code"];
                            if (codeInt != ReasonCodes.RTT_ROOM_READY)
                            {
                                BaseState baseState = GStateManager.Instance.FindSubState(LobbySubState.STATE_NAME);
                                GStateManager.Instance.PopSubState(baseState.StateInfo);
                                LeaveLobby();
                                HudHelper.DisplayMessageDialog("ERROR", "THERE WAS A CONNECTION ERROR.  PLEASE TRY AGAIN SOON.", "OK");
                            }
                        }
                    }
                    break;
                case "ROOM_ASSIGNED":
                    {
                        ReadRoomAssignedInfo(jsonData);
                    }
                    break;
                case "ROOM_READY":
                    {
                        // deregister them all! we're going in game
                        CreateRoomServerMatch();
                    }
                    break;
                case "JOIN_FAIL":
                    {
                        Dictionary<string, object> reasonDict = (Dictionary<string, object>)jsonData["reason"];
                        string desc = (string)reasonDict["desc"];
                        int codeInt = (int)reasonDict["code"];
                        if (codeInt != ReasonCodes.RTT_FIND_REQUEST_CANCELLED)
                        {
                            HudHelper.DisplayMessageDialog("ERROR", desc.ToUpper(), "OK");
                        }

                        // ensure to remove the joining game sub state
                        BaseState joinGameState = GStateManager.Instance.FindSubState(JoiningGameSubState.STATE_NAME);
                        bool canJoin = joinGameState != null;
                        if (canJoin)
                        {
                            GStateManager.Instance.PopSubState(joinGameState.StateInfo);
                        }
                    }
                    break;
                case "ROOM_CONNECT":
                    {
                    }
                    break;

                default: { } break;
            }
        }

        public void ReadRoomAssignedInfo(Dictionary<string, object> jsonData)
        {
            if (RoomServerInfo == null)
            {
                RoomServerInfo = new BCRoomServerInfo();
            }
            Dictionary<string, object> connectData = (Dictionary<string, object>)jsonData["connectData"];
            RoomServerInfo.UserPassCode = (string)jsonData["passcode"];
            RoomServerInfo.LobbyId = (string)jsonData["lobbyId"];

            if (connectData.ContainsKey("url")) RoomServerInfo.Url = (string)connectData["url"];
            else if (connectData.ContainsKey("address")) RoomServerInfo.Url = (string)connectData["address"];

            if (connectData.ContainsKey("wsPort")) RoomServerInfo.Port = (int)connectData["wsPort"];
            else if (connectData.ContainsKey("ports"))
            {
                Dictionary<string, object> ports = (Dictionary<string, object>)connectData["ports"];

#if !SMRJ_HACK2
                int rsConnectionType = GetTesterProtocol();
                SetTesterCompression();
#else
                int rsConnectionType = RS_CONNECTION_OVERRIDE;
#endif

#if UNITY_WEBGL
                rsConnectionType = 0;
#endif
                switch (rsConnectionType)
                {
                    //web socket 
                    case 0:
                        {
                            RoomServerInfo.Port = (int)ports["ws"];
                        }
                        break;

                    // TCP 
                    case 1:
                        {
                            RoomServerInfo.Port = (int)ports["tcp"];
                        }
                        break;
                    // UDP
                    case 2:
                    default:
                        {
                            RoomServerInfo.Port = (int)ports["udp"];
                        }
                        break;
                }
            }
        }

        public void ReadLobbyInfo(Dictionary<string, object> lobbyData)
        {
            LobbyInfo.LobbyJsonDataRaw = lobbyData;
            if (lobbyData.ContainsKey("lobbyType")) LobbyInfo.LobbyType = (string)lobbyData["lobbyType"];
            if (lobbyData.ContainsKey("state")) LobbyInfo.State = (string)lobbyData["state"];
            if (lobbyData.ContainsKey("owner")) LobbyInfo.OwnerProfileId = (string)lobbyData["owner"];

            if (lobbyData.ContainsKey("rating")) LobbyInfo.Rating = (int)lobbyData["rating"];
            if (lobbyData.ContainsKey("version")) LobbyInfo.Version = (int)lobbyData["version"];

            if (lobbyData.ContainsKey("lobbyTypeDef")) LobbyInfo.LobbyDefinition = (Dictionary<string, object>)lobbyData["lobbyTypeDef"];
            if (lobbyData.ContainsKey("settings")) LobbyInfo.Settings = (Dictionary<string, object>)lobbyData["settings"];

            // parse array members into list
            if (lobbyData.ContainsKey("members"))
            {
                Array tempMembers = (Array)lobbyData["members"];

                if (LobbyInfo.Members == null)
                {
                    LobbyInfo.Members = new List<LobbyMemberInfo>();
                }
                else
                {
                    LobbyInfo.Members.Clear();
                }

                short count = 0;
                LobbyMemberInfo tempItem = null;
                foreach (Dictionary<string, object> item in tempMembers)
                {
                    tempItem = new LobbyMemberInfo(item);
                    tempItem.NetId = ++count;
                    LobbyInfo.Members.Add(tempItem);
                }
            }
        }

#if SMRJ_HACK2
        public static int RS_CONNECTION_OVERRIDE = 2;
#endif
        public void ConnectToRoomServerService()
        {
            GCore.Wrapper.RTTService.DeregisterAllRTTCallbacks();
            GCore.Wrapper.RTTService.RegisterRTTLobbyCallback(Instance.LobbyCallback);
            GCore.Wrapper.Client.RelayService.RegisterDataCallback(onDataRecv);
            Dictionary<string, object> connectionOptions = new Dictionary<string, object>();
#if !SMRJ_HACK2
            int rsConnectionType = GetTesterProtocol();
            SetTesterCompression();
#else
            int rsConnectionType = RS_CONNECTION_OVERRIDE;
#endif
            connectionOptions["ssl"] = false;
            connectionOptions["host"] = RoomServerInfo.Url;
            connectionOptions["port"] = RoomServerInfo.Port;
            connectionOptions["passcode"] = RoomServerInfo.UserPassCode;
            connectionOptions["lobbyId"] = RoomServerInfo.LobbyId;

            RelayConnectionType connectionType = rsConnectionType == 0 ? RelayConnectionType.WEBSOCKET :
                                               rsConnectionType == 1 ? RelayConnectionType.TCP : RelayConnectionType.UDP;

#if UNITY_WEBGL
            connectionType = RelayConnectionType.WEBSOCKET;
#endif

            GCore.Wrapper.Client.RelayService.Connect(connectionType, connectionOptions, null, onRSConnectError);
        }

        private void onRSConnectError(int status, int reasonCode, string message, object cbObject)
        {
            if (!GStateManager.Instance.IsLoadingState && !GStateManager.Instance.IsLoadingSubState &&
                GStateManager.Instance.FindSubState(LobbySubState.STATE_NAME) != null &&
                GStateManager.Instance.FindSubState(GenericMessageSubState.STATE_NAME) == null)
            {
                try
                {
                    Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(message);
                    string messageDisplay = (string)jsonMessage["status_message"];
                    HudHelper.DisplayMessageDialog("ERROR", messageDisplay.ToUpper(), "OK", onRSConnectErrorHandle);
                }
                catch (Exception)
                {
                    HudHelper.DisplayMessageDialog("ERROR", "THERE WAS A NETWORKING ERROR", "OK", onRSConnectErrorHandle);
                }
            }
        }

        public void ConnectRTT()
        {
            if (GCore.Wrapper.RTTService.IsRTTEnabled())
            {
                OnEnableRTTSuccess("", null);
            }
            else
            {
                GCore.Wrapper.RTTService.EnableRTT(RTTConnectionType.WEBSOCKET, OnEnableRTTSuccess, OnEnableRTTFailed);
            }
        }
        private void onRSConnectErrorHandle()
        {
            ConnectRTT();
            DestroyMatch();
        }

        private int GetTesterProtocol()
        {
            if (GPlayerMgr.Instance.PlayerData.IsTester)
            {
                LobbySubState baseState = (LobbySubState)GStateManager.Instance.FindSubState(LobbySubState.STATE_NAME);
                if (baseState)
                {
                    return baseState.GetProtocol();
                }
            }
            return GConfigManager.GetIntValue("RSConnectionType");
        }

        private void SetTesterCompression()
        {
            if (GPlayerMgr.Instance.PlayerData.IsTester)
            {
                LobbySubState baseState = (LobbySubState)GStateManager.Instance.FindSubState(LobbySubState.STATE_NAME);
                if (baseState)
                {
                    BaseNetworkBehavior.MSG_ENCODED = baseState.GetCompression();
                    return;
                }
            }
            BaseNetworkBehavior.MSG_ENCODED = 2;
        }

        private Dictionary<string, object> readRSData(byte[] in_data)
        {
            Dictionary<string, object> jsonMessage = null;
            string in_json = Encoding.ASCII.GetString(in_data);
            if (in_json.Length > 0)
            {
                try
                {
                    if (in_json[0] == '{')
                    {
                        jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_json);
                    }
                    else
                    {
                        jsonMessage = deserializeData(in_data, in_json);
                    }
                }
                catch (Exception)
                {
                    jsonMessage = deserializeData(in_data, in_json);
                }
            }

            return jsonMessage;
        }

        private Dictionary<string, object> deserializeData(byte[] in_data, string in_json)
        {
            Dictionary<string, object> jsonMessage = null;
            if (BaseNetworkBehavior.MSG_ENCODED == 2)
            {
                jsonMessage = BaseNetworkBehavior.DeserializeData(in_data);
            }
            else
            {
                jsonMessage = BaseNetworkBehavior.DeserializeString(in_json);
            }
            return jsonMessage;
        }

        private void onDataRecv(byte[] in_data)
        {
            Dictionary<string, object> jsonMessage = readRSData(in_data);

            if (jsonMessage != null && jsonMessage.ContainsKey(BaseNetworkBehavior.OPERATION))
            {
                string operation = (string)jsonMessage[BaseNetworkBehavior.OPERATION];
                switch (operation)
                {
                    // most often!
                    case BaseNetworkBehavior.TRANSFORM_UPDATE:
                        {
                            readTransformUpdate(jsonMessage);
                        }
                        break;

                    // JSON
                    case "CONNECT":
                        {
                            // someone connected!
                            if (jsonMessage.ContainsKey(BrainCloudConsts.JSON_PROFILE_ID))
                            {
                                string profileId = jsonMessage[BrainCloudConsts.JSON_PROFILE_ID] as string;
                                string ownerId = jsonMessage["ownerId"] as string;
                                LobbyInfo.OwnerProfileId = ownerId;

                                LobbyMemberInfo memberInfo = LobbyInfo.GetMemberWithProfileId(profileId);

                                if (memberInfo != null)
                                {
                                    short netId = Convert.ToInt16(jsonMessage["netId"]);
                                    memberInfo.NetId = netId;
                                    if (memberInfo.PlayerController != null) memberInfo.PlayerController.NetId = netId;
                                }

                                if (profileId == GCore.Wrapper.Client.ProfileId)
                                {
                                    initGame();
                                }
                            }
                        }
                        break;

                    // JSON
                    case "NET_ID":
                        {
                            string profileId = jsonMessage[BrainCloudConsts.JSON_PROFILE_ID] as string;
                            LobbyMemberInfo memberInfo = LobbyInfo.GetMemberWithProfileId(profileId);

                            if (memberInfo != null)
                            {
                                short netId = Convert.ToInt16(jsonMessage["netId"]);
                                memberInfo.NetId = netId;
                                if (memberInfo.PlayerController != null) memberInfo.PlayerController.NetId = netId;
                            }
                        }
                        break;

                    // JSON
                    case "DISCONNECT":
                        {
                            GameObject gManObj = GameObject.Find("GameManager");
                            GameManager gMan = gManObj != null ? gManObj.GetComponent<GameManager>() : null;
                            if (gMan != null && gMan.m_gameState != GameManager.eGameState.GAME_STATE_GAME_OVER)
                            {
                                // someone left
                                if (jsonMessage.ContainsKey(BrainCloudConsts.JSON_PROFILE_ID))
                                {
                                    string profileId = jsonMessage[BrainCloudConsts.JSON_PROFILE_ID] as string;
                                    LobbyMemberInfo memberInfo = LobbyInfo.RemoveMemberWithProfileId(profileId);

                                    // someone was removed, add a new server bot
                                    if (memberInfo != null)
                                    {
                                        gMan.DisplayDialogMessage(memberInfo.Name + " left the game");
                                        gMan.RpcDestroyPlayerPlane(memberInfo.NetId, -1);
                                        Dictionary<string, object> botDict = new Dictionary<string, object>();

                                        botDict["name"] = "serverBot - " + memberInfo.Name;
                                        botDict["pic"] = "";
                                        botDict["cxId"] = "";
                                        botDict["rating"] = 0;
                                        botDict["isReady"] = true;

                                        LobbyMemberInfo newMember = null;
                                        botDict["team"] = memberInfo.Team;
                                        botDict[BrainCloudConsts.JSON_PROFILE_ID] = "serverBot " + memberInfo.Name;
                                        newMember = new LobbyMemberInfo(botDict);
                                        newMember.NetId = memberInfo.NetId; // reuse old net id
                                        LobbyInfo.Members.Add(newMember);
                                        gMan.RpcSpawnPlayer(newMember.ProfileId);
                                    }

                                    bool bLastPlayer = true;
                                    // check if last player or not
                                    foreach (LobbyMemberInfo member in LobbyInfo.Members)
                                    {
                                        if (member.Name.Contains(GameManager.SERVER_BOT)) continue;
                                        if (member.ProfileId != GCore.Wrapper.Client.ProfileId)
                                        {
                                            bLastPlayer = false;
                                            break;
                                        }
                                    }
                                    if (bLastPlayer)
                                    {
                                        gMan.DisplayDialogMessage("You are the last player in the game.");
                                    }
                                }
                            }
                        }
                        break;

                    // JSON
                    case "joined":
                        {
                            // User Joined
                            if (jsonMessage.ContainsKey(BrainCloudConsts.JSON_PROFILE_ID))
                            {
                                string profileId = jsonMessage[BrainCloudConsts.JSON_PROFILE_ID] as string;
                                LobbyMemberInfo memberInfo = LobbyInfo.GetMemberWithProfileId(profileId);

                                if (memberInfo != null)
                                {
                                    memberInfo.IsReady = true;

                                    GameObject obj = GameObject.Find("GameManager");
                                    if (obj != null && profileId != GCore.Wrapper.Client.ProfileId)
                                        obj.GetComponent<GameManager>().DisplayDialogMessage(memberInfo.Name + " joined the game");
                                }
                            }
                        }
                        break;

                    // JSON
                    case "MIGRATE_OWNER":
                        {
                            if (GameObject.Find("GameManager") == null)
                            {
                                GCore.Wrapper.Client.RelayService.DeregisterDataCallback();
                                return;
                            }
                            // new owner
                            if (jsonMessage.ContainsKey(BrainCloudConsts.JSON_PROFILE_ID))
                            {
                                string profileId = jsonMessage[BrainCloudConsts.JSON_PROFILE_ID] as string;
                                LobbyInfo.OwnerProfileId = profileId;

                                LobbyMemberInfo memberInfo = LobbyInfo.GetMemberWithProfileId(profileId);
                                if (memberInfo != null)
                                    GameObject.Find("GameManager").GetComponent<GameManager>().DisplayDialogMessage(memberInfo.Name + " is the new host");

                                // forces a refresh of variables for server bots!
                                // and show should control them afterwards
                                foreach (LobbyMemberInfo member in LobbyInfo.Members)
                                {
                                    if (member.PlayerController != null)
                                    {
                                        member.PlayerController.ProfileId = member.ProfileId;
                                        member.PlayerController.NetId = member.NetId;
                                    }
                                }
                            }
                        }
                        break;

                    // serialize properly
                    case BaseNetworkBehavior.ENTITY_START:
                        {
                            GameObject gManObj = GameObject.Find("GameManager");
                            GameManager gMan = gManObj != null ? gManObj.GetComponent<GameManager>() : null;
                            if (gManObj == null)
                            {
                                GCore.Wrapper.Client.RelayService.DeregisterDataCallback();
                                return;
                            }

                            if (jsonMessage.ContainsKey(BaseNetworkBehavior.NET_ID))
                            {
                                if (jsonMessage.ContainsKey(BaseNetworkBehavior.TYPE))
                                {
                                    string classType = jsonMessage[BaseNetworkBehavior.TYPE] as string;

                                    if (classType == PLANE_CONTROLLER)
                                    {
                                        string profileId = jsonMessage[BaseNetworkBehavior.NET_ID] as string;
                                        Vector3 spawnPoint = new Vector3(BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_X),
                                                                     BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_Y),
                                                                     BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_Z));
                                        gMan.RpcSpawnPlayer(profileId, spawnPoint);
                                    }
                                    else if (classType == MEMBER_ADD)
                                    {
                                        string data = jsonMessage[BaseNetworkBehavior.DATA] as string;
                                        Dictionary<string, object> jsonData = BaseNetworkBehavior.DeserializeString(data, SPECIAL_INNER_JOIN, SPECIAL_INNER_SPLIT);
                                        LobbyMemberInfo newMember = new LobbyMemberInfo(jsonData);
                                        LobbyInfo.Members.Add(newMember);
                                    }
                                    else if (classType == ROCK)
                                    {
                                        string data = jsonMessage[BaseNetworkBehavior.DATA] as string;
                                        Vector3 spawnPoint = new Vector3(BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_X),
                                                                      BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_Y),
                                                                      BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_Z));
                                        Quaternion rotation = Quaternion.Euler(new Vector3(BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.ROTATION_X),
                                                                                           BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.ROTATION_Y),
                                                                                           BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.ROTATION_Z)));
                                        Instantiate((GameObject)Resources.Load("Prefabs/Game/" + data), spawnPoint, rotation);
                                    }
                                    else if (classType == SHIP)
                                    {
                                        Vector3 spawnPoint = new Vector3(BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_X),
                                                                     BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_Y),
                                                                     BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_Z));
                                        Quaternion rotation = Quaternion.Euler(new Vector3(BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.ROTATION_X),
                                                                                           BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.ROTATION_Y),
                                                                                           BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.ROTATION_Z)));

                                        GameObject ship = Instantiate((GameObject)Resources.Load("Prefabs/Game/Ship"), spawnPoint, rotation);

                                        ShipController controller = ship.GetComponent<ShipController>();
                                        string netId = jsonMessage[BaseNetworkBehavior.NET_ID] as string;
                                        int indexOfSpecialVals = netId.IndexOf("***");
                                        string carrierType = netId.Substring(0, indexOfSpecialVals);
                                        int shipId = int.Parse(netId.Substring(indexOfSpecialVals + 3));
                                        controller.SetShipType((ShipController.eShipType)Enum.Parse(typeof(ShipController.eShipType), carrierType, true), (shipId % 2) + 1, shipId);
                                    }
                                    else if (classType == BULLET)
                                    {
                                        readCreateBullet(jsonMessage);
                                    }
                                    else if (classType == BULLET_HIT)
                                    {
                                        readBulletHit(jsonMessage);
                                    }
                                    else if (classType == BOMB)
                                    {
                                        readCreateBomb(jsonMessage);
                                    }
                                    else if (classType == PICKUP)
                                    {
                                        int netId = GConfigManager.ReadIntSafely(jsonMessage, BaseNetworkBehavior.NET_ID);
                                        Vector3 spawnPoint = new Vector3(BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_X),
                                                                     BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_Y),
                                                                     BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_Z));

                                        gMan.RpcSpawnBombPickup(spawnPoint, netId);
                                    }
                                    else if (classType == HIT_TARGET)
                                    {
                                        string netId = jsonMessage[BaseNetworkBehavior.NET_ID] as string;
                                        string data = jsonMessage[BaseNetworkBehavior.DATA] as string;
                                        int indexOfSpecialVals = netId.IndexOf("***");
                                        int shipId = int.Parse(netId.Substring(0, indexOfSpecialVals));
                                        int targetId = int.Parse(netId.Substring(indexOfSpecialVals + 3));

                                        gMan.RpcHitShipTargetPoint(shipId, targetId, data);
                                    }
                                    else if (classType == LOBBY_UP)
                                    {
                                        string profileId = jsonMessage[BaseNetworkBehavior.NET_ID] as string;
                                        string data = jsonMessage[BaseNetworkBehavior.DATA] as string;
                                        LobbyMemberInfo member = LobbyInfo.GetMemberWithProfileId(profileId);
                                        if (member != null)
                                        {
                                            member.LobbyReadyUp = data == "true";
                                            gMan.RefreshQuitGameDisplay();
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    // serialize properly
                    case BaseNetworkBehavior.ENTITY_DESTROY:
                        {
                            GameObject gManObj = GameObject.Find("GameManager");
                            GameManager gMan = gManObj != null ? gManObj.GetComponent<GameManager>() : null;

                            if (gManObj == null)
                            {
                                GCore.Wrapper.Client.RelayService.DeregisterDataCallback();
                                return;
                            }

                            if (jsonMessage.ContainsKey(BaseNetworkBehavior.NET_ID))
                            {
                                if (jsonMessage.ContainsKey(BaseNetworkBehavior.TYPE))
                                {
                                    string classType = jsonMessage[BaseNetworkBehavior.TYPE] as string;
                                    if (classType == PLANE_CONTROLLER)
                                    {
                                        short netId = Convert.ToInt16(jsonMessage[BaseNetworkBehavior.NET_ID]);
                                        short data = Convert.ToInt16(jsonMessage[BaseNetworkBehavior.DATA]);
                                        gMan.RpcDestroyPlayerPlane(netId, data);
                                    }
                                    else if (classType == SHIP)
                                    {
                                        string netId = jsonMessage[BaseNetworkBehavior.NET_ID] as string;
                                        string data = jsonMessage[BaseNetworkBehavior.DATA] as string;
                                        gMan.RpcDestroyedShip(int.Parse(netId), data);
                                    }
                                    else if (classType == BULLET)
                                    {
                                        readDeleteBullet(jsonMessage);
                                    }
                                    else if (classType == BOMB)
                                    {
                                        readDeleteBomb(jsonMessage);
                                    }
                                    else if (classType == PICKUP)
                                    {
                                        short netId = Convert.ToInt16(jsonMessage[BaseNetworkBehavior.NET_ID]);
                                        int data = GConfigManager.ReadIntSafely(jsonMessage, BaseNetworkBehavior.DATA);
                                        gMan.RpcBombPickedUp(netId, data);
                                    }
                                    else if (classType == GAME)
                                    {
                                        gMan.RpcEndGame();
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        private void readBulletHit(Dictionary<string, object> jsonMessage)
        {
            BulletInfo bulletInfo = null;
            if (jsonMessage.ContainsKey(BaseNetworkBehavior.DATA))
            {
                string data = jsonMessage[BaseNetworkBehavior.DATA] as string;
                bulletInfo = BulletInfo.GetBulletInfo(data);
            }
            else
            {
                bulletInfo = new BulletInfo(jsonMessage);
            }

            GameObject.Find("GameManager").GetComponent<GameManager>().RpcBulletHitPlayer(bulletInfo);
        }

        private void readCreateBullet(Dictionary<string, object> jsonMessage)
        {
            BulletInfo bulletInfo = null;
            if (jsonMessage.ContainsKey(BaseNetworkBehavior.DATA))
            {
                string data = jsonMessage[BaseNetworkBehavior.DATA] as string;
                bulletInfo = BulletInfo.GetBulletInfo(data);
            }
            else
            {
                bulletInfo = new BulletInfo(jsonMessage);
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().CreateRemoteBullet(bulletInfo);
        }

        private void readDeleteBullet(Dictionary<string, object> jsonMessage)
        {
            BulletInfo bulletInfo = null;
            if (jsonMessage.ContainsKey(BaseNetworkBehavior.DATA))
            {
                string data = jsonMessage[BaseNetworkBehavior.DATA] as string;
                bulletInfo = BulletInfo.GetBulletInfo(data);
            }
            else
            {
                bulletInfo = new BulletInfo(jsonMessage);
            }

            GameObject.Find("GameManager").GetComponent<GameManager>().DeleteBullet(bulletInfo);
        }

        private void readCreateBomb(Dictionary<string, object> jsonMessage)
        {
            BombInfo bombInfo = null;
            if (jsonMessage.ContainsKey(BaseNetworkBehavior.DATA))
            {
                string data = jsonMessage[BaseNetworkBehavior.DATA] as string;
                bombInfo = BombInfo.GetBombInfo(data);
            }
            else
            {
                bombInfo = new BombInfo(jsonMessage);
            }

            GameObject.Find("GameManager").GetComponent<GameManager>().RpcSpawnBomb(bombInfo);
        }

        private void readDeleteBomb(Dictionary<string, object> jsonMessage)
        {
            BombInfo bombInfo = null;
            int hitSurface = 0;    // TODO
            if (jsonMessage.ContainsKey(BaseNetworkBehavior.DATA))
            {
                string data = jsonMessage[BaseNetworkBehavior.DATA] as string;
                bombInfo = BombInfo.GetBombInfo(data);
            }
            else
            {
                bombInfo = new BombInfo(jsonMessage);
            }

            GameObject.Find("GameManager").GetComponent<GameManager>().RpcDeleteBomb(bombInfo, hitSurface);//, int.Parse(data));
        }

        private void readTransformUpdate(Dictionary<string, object> jsonMessage)
        {
            if (jsonMessage.ContainsKey(BaseNetworkBehavior.NET_ID))
            {
                short netId = Convert.ToInt16(jsonMessage[BaseNetworkBehavior.NET_ID]);
                float lastPing = BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.LAST_PING);

                float posX = BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_X);
                float posY = BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_Y);
                float posZ = BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.POSITION_Z);

                float rotX = BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.ROTATION_X);
                float rotY = BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.ROTATION_Y);
                float rotZ = BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.ROTATION_Z);

                float velX = BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.VELOCITY_X);
                float velY = BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.VELOCITY_Y);
                float velZ = BaseNetworkBehavior.ConvertToFloat(jsonMessage, BaseNetworkBehavior.VELOCITY_Z);

                BaseNetworkBehavior.UpdateTransform(netId, lastPing,
                    new Vector3(posX, posY, posZ),
                    new Vector3(rotX, rotY, rotZ),
                    new Vector3(velX, velY, velZ));
            }
        }

        public void OnEnableRTTSuccess(string in_stringData, object in_obj)
        {
            GDebug.Log(string.Format("Success | {0}", in_stringData));
            ConnectToGlobalChat();
            GEventManager.TriggerEvent(GEventManager.ON_RTT_ENABLED);
        }

        public void OnEnableRTTFailed(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            HudHelper.DisplayMessageDialog("ERROR", "ARE YOU STILL THERE?", "YES!", onReconnectRTT);
        }

        private void onReconnectRTT()
        {
            if (GStateManager.Instance.CurrentStateId != MainMenuState.STATE_NAME)
                GStateManager.Instance.ChangeState(MainMenuState.STATE_NAME);

            GCore.Wrapper.RTTService.DisableRTT();
            GCore.Wrapper.RTTService.EnableRTT(RTTConnectionType.WEBSOCKET, OnEnableRTTSuccess, OnEnableRTTFailed);
        }

        public bool AllMembersJoined()
        {
            bool toReturn = LobbyInfo.Members.Count == 1;

            // iterate over lobby members seeing if everyone is here
            if (!toReturn)
            {
                toReturn = true;
                foreach (LobbyMemberInfo member in LobbyInfo.Members)
                {
                    if (!member.Name.Contains(GameManager.SERVER_BOT) && !member.IsReady)
                    {
                        toReturn = false;
                        break;
                    }
                }
            }
            return toReturn;
        }

        private void sendJoinedRoomServerMessage()
        {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json[BaseNetworkBehavior.OPERATION] = "joined";
            json[BrainCloudConsts.JSON_PROFILE_ID] = GCore.Wrapper.Client.ProfileId;

            GCore.Wrapper.Client.RelayService.Send(Encoding.ASCII.GetBytes(JsonWriter.Serialize(json)), BrainCloud.Internal.RelayComms.TO_ALL_PLAYERS, true, true, 0);
        }

        private void initGame()
        {
            StopCoroutine(initGameRoutine());
            StartCoroutine(initGameRoutine());
        }

        private IEnumerator initGameRoutine()
        {
            yield return YieldFactory.GetWaitForSeconds(0.15f);

            GStateManager.Instance.ChangeState(GameState.STATE_NAME);
            yield return StartCoroutine(InitializeGameInfo(LobbyInfo.Settings));

            sendJoinedRoomServerMessage();
        }

        public void CreateRoomServerMatch()
        {
            ConnectToRoomServerService();
        }

        public void ConnectToGlobalChat()
        {
            GCore.Wrapper.RTTService.RegisterRTTChatCallback(chatCallback);
            GCore.Wrapper.RTTService.RegisterRTTEventCallback(eventCallback);

            // do a get channel call instead of manually appending these, this is for demo purposes
            GCore.Wrapper.ChatService.ChannelConnect(GCore.Wrapper.Client.AppId + ":gl:main", 25, onChannelConnected);
        }

        private void onChannelConnected(string in_json, object obj)
        {
            MainMenuState mainMenu = FindObjectOfType<MainMenuState>();
            if (mainMenu == null) return;

            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_json);

            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            Array firstChatMessagesData = (Array)jsonData["messages"];

            Dictionary<string, object> messageData = null;
            for (int i = 0; i < firstChatMessagesData.Length; ++i)
            {
                messageData = firstChatMessagesData.GetValue(i) as Dictionary<string, object>;
                mainMenu.AddGlobalChatMessage(messageData);
            }
        }

        public void DisconnectGlobalChat()
        {
            // cache the channel Id that you are connected to in order to disconnect, this is for demo purposes
            GCore.Wrapper.ChatService.ChannelDisconnect(GCore.Wrapper.Client.AppId + ":gl:main");
            GCore.Wrapper.RTTService.DeregisterRTTPresenceCallback();
            GCore.Wrapper.RTTService.DeregisterRTTChatCallback();
            GCore.Wrapper.RTTService.DeregisterRTTEventCallback();
        }

        private void chatCallback(string in_message)
        {
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_message);
            if (jsonMessage.ContainsKey("operation"))
            {
                MainMenuState mainMenu = FindObjectOfType<MainMenuState>();

                string operation = jsonMessage["operation"] as string;

                switch (operation)
                {
                    case "INCOMING":
                        {
                            if (mainMenu != null)
                            {
                                mainMenu.AddGlobalChatMessage(jsonMessage);
                            }
                        }
                        break;

                    case "DELETE":
                        {
                            if (mainMenu != null)
                            {
                                mainMenu.OnChatMessageDeleted(jsonMessage);
                            }
                        }
                        break;

                    case "UPDATE":
                        {
                            if (mainMenu != null)
                            {
                                mainMenu.OnChatMessageUpdated(jsonMessage);
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
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_message);
            switch (jsonMessage["operation"] as string)
            {
                case "GET_EVENTS":
                    {
                        Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
                        GCore.Wrapper.Client.EventService.DeleteIncomingEvent(jsonData["evId"] as string);

                        switch (jsonData["eventType"] as string)
                        {
                            case "OFFER_JOIN_LOBBY":
                                {
                                    // bring up the offer to join display
                                    // on the requested client
                                    MainMenuState mainMenu = GStateManager.Instance.CurrentState as MainMenuState;
                                    // only bring up the offer to join, when in the main menu, and not in a lobby already or in the process of getting in one.
                                    if (mainMenu && GStateManager.Instance.FindSubState(LobbySubState.STATE_NAME) == null
                                                 && GStateManager.Instance.FindSubState(ConfirmJoinLobbySubState.STATE_NAME) == null
                                                 && GStateManager.Instance.FindSubState(CreateGameSubState.STATE_NAME) == null
                                                 && GStateManager.Instance.FindSubState(JoiningGameSubState.STATE_NAME) == null)
                                    {
                                        Dictionary<string, object> eventData = (Dictionary<string, object>)jsonData["eventData"];
                                        mainMenu.DisplayJoinLobbyOffer(eventData[BrainCloudConsts.JSON_PROFILE_ID] as string, eventData[BrainCloudConsts.JSON_USER_NAME] as string);
                                    }
                                }
                                break;

                            case "CONFIRM_JOIN_LOBBY":
                                {
                                    // they confirmed to join the lobby!
                                    Dictionary<string, object> eventData = (Dictionary<string, object>)jsonData["eventData"];
                                    string[] cxIds = { eventData["lastConnectionId"] as string };

                                    Dictionary<string, object> matchOptions = new Dictionary<string, object>();
                                    matchOptions["gameTime"] = GConfigManager.GetIntValue("DefaultGameTime");
                                    matchOptions["gameTimeSel"] = GConfigManager.GetIntValue("DefaultGameTimeIndex");
                                    matchOptions["isPlaying"] = 0;
                                    matchOptions["mapLayout"] = 0;
                                    matchOptions["mapSize"] = 1;
                                    matchOptions["gameName"] = GPlayerMgr.Instance.PlayerData.PlayerName + "'s Room";
                                    matchOptions["maxPlayers"] = 8;
                                    matchOptions["lightPosition"] = 0;

                                    WaitOnLobbyJoin();
                                    // Find or create with someone else!
                                    CreateLobby(matchOptions, cxIds);
                                }
                                break;

                            case "REFUSED_JOIN_LOBBY":
                                {
                                    GEventManager.TriggerEvent(GEventManager.ON_REFUSED_INVITE_FRIEND);
                                }
                                break;
                            default: break;
                        }
                    }
                    break;

                default: break;
            }
        }

        public static void WaitOnLobbyJoin()
        {
            // if matchmaking, go to find lobby state
            MainMenuState mainMenu = FindObjectOfType<MainMenuState>();
            if (mainMenu)
            {
                BombersNetworkManager thisInstance = BombersNetworkManager.singleton as BombersNetworkManager;
                GStateManager.Instance.PushSubState(JoiningGameSubState.STATE_NAME);
                GCore.Wrapper.RTTService.RegisterRTTLobbyCallback(thisInstance.LobbyCallback);
            }
        }

        public static Dictionary<string, object> s_matchOptions;

        public override void OnApplicationQuit()
        {
            DisconnectGlobalChat();

            // force whatever is aroudn to be sent out
            GCore.Wrapper.Update();

            base.OnApplicationQuit();
        }

        // these must be unique
        public const string BULLET = "bu";
        public const string BOMBERS_PLANE_CONTROLLER = "bc";
        public const string BULLET_HIT = "bh";
        public const string BOMB = "bo";

        public const string BULLET_CONTROLLER = "bb";
        public const string BOMB_CONTROLLER = "cc";
        public const string GAME = "ga";
        public const string GAME_CONTROLLER = "gc";

        public const string HIT_TARGET = "ht";
        public const string LOBBY_UP = "lu";

        public const string MEMBER_ADD = "ma";
        public const string PICKUP = "pi";
        public const string PLANE_CONTROLLER = "pc";
        public const string ROCK = "ro";
        public const string SHIP = "sh";

        public const char SPECIAL_INNER_SPLIT = '^';
        public const char SPECIAL_INNER_JOIN = '@';
    }

    /// <summary>
    /// /////
    /// </summary>
    public class BCRoomServerInfo
    {
        public string UserPassCode;
        public string LobbyId;
        public string RoomId;
        public string Url;
        public int Port;
    }

    /// <summary>
    /// /////
    /// </summary>
    public class BCLobbyInfo
    {
        public string LobbyId;
        public string LobbyType;
        public string State;
        public string OwnerProfileId;

        public int Rating;
        public int Version;

        public Dictionary<string, object> LobbyJsonDataRaw;
        public Dictionary<string, object> LobbyDefinition;
        public Dictionary<string, object> Settings;
        public List<LobbyMemberInfo> Members;

        public bool IsOwner(string in_profile)
        {
            return OwnerProfileId == in_profile;
        }

        public int GetMemberLobbyIndex(string in_profileId)
        {
            int index = -1;
            for (int i = 0; i < Members.Count; ++i)
            {
                if (in_profileId == Members[i].ProfileId)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        public string GetTeamWithLeastPeople()
        {
            int numOnGreen = 0;
            int numOnRed = 0;
            for (int i = 0; i < Members.Count; ++i)
            {
                if (Members[i].Team == "green") ++numOnGreen;
                else ++numOnRed;
            }
            return numOnGreen > numOnRed ? "red" : numOnGreen < numOnRed ? "green" : "";
        }

        public LobbyMemberInfo RemoveMemberWithProfileId(string in_profileId)
        {
            LobbyMemberInfo toReturn = GetMemberWithProfileId(in_profileId);
            if (toReturn != null) Members.Remove(toReturn);

            return toReturn;
        }

        public LobbyMemberInfo GetMemberWithProfileId(string in_profileId)
        {
            LobbyMemberInfo toReturn = null;

            foreach (LobbyMemberInfo member in Members)
            {
                if (in_profileId == member.ProfileId)
                {
                    toReturn = member;
                    break;
                }
            }

            return toReturn;
        }

        public LobbyMemberInfo GetMemberWithNetId(short in_netId)
        {
            LobbyMemberInfo toReturn = null;

            foreach (LobbyMemberInfo member in Members)
            {
                if (in_netId == member.NetId)
                {
                    toReturn = member;
                    break;
                }
            }
            return toReturn;
        }

        public string GetTeamCodeWithProfileId(string in_profileId)
        {
            LobbyMemberInfo member = GetMemberWithProfileId(in_profileId);
            return member.Team;
        }

        public string GetOppositeTeamCodeWithProfileId(string in_profileId)
        {
            return GetTeamCodeWithProfileId(in_profileId) == "green" ? "red" : "green";
        }

        public bool IsMemberAlreadyInLobby(string in_profileId, out int in_index)
        {
            bool bToReturn = false;
            in_index = -1;
            LobbyMemberInfo member;
            for (int i = 0; i < Members.Count; ++i)
            {
                member = Members[i];
                if (in_profileId == member.ProfileId)
                {
                    bToReturn = true;
                    in_index = i;
                    break;
                }
            }

            return bToReturn;
        }
    }
}
