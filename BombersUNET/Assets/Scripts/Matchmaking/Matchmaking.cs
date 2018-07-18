/*
 * UNET does not yet allow for matchmaking information to be sent through the matchmaker, so all of the match filtering is non-functional, besides name-based filtering.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using BrainCloudUNETExample.Connection;
using UnityEngine.Networking.Match;
using UnityEngine.SceneManagement;

namespace BrainCloudUNETExample.Matchmaking
{
    public class Matchmaking : MonoBehaviour
    {
        public class RoomButton
        {
            public MatchInfoSnapshot m_room;
            public Button m_button;

            public RoomButton(MatchInfoSnapshot aRoom, Button abutton)
            {
                m_room = aRoom;
                m_button = abutton;
            }
        }

        private enum eMatchmakingState
        {
            GAME_STATE_SHOW_ROOMS,
            GAME_STATE_NEW_ROOM_OPTIONS,
            GAME_STATE_FIND_ROOM_OPTIONS,
            GAME_STATE_CREATE_NEW_ROOM,
            GAME_STATE_JOIN_ROOM,
            GAME_STATE_SHOW_LEADERBOARDS,
            GAME_STATE_SHOW_CONTROLS,
            GAME_STATE_SHOW_ACHIEVEMENTS,
            GAME_STATE_WAITING_FOR_PLAYERS,
            GAME_STATE_GLOBAL_CHAT,
            GAME_STATE_CONFIRM_LOBBY_OFFER
        }
        private eMatchmakingState m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;

        private int m_roomMaxPlayers = 8;
        private int m_roomLevelRangeMin = 0;
        private int m_roomLevelRangeMax = 50;

        private GameObject m_showRoomsWindow;
        private GameObject m_refreshLabel;
        private List<RoomButton> m_roomButtons;
        private GameObject m_baseButton;

        private bool m_showPresetList = false;
        private bool m_showSizeList = false;
        private int m_presetListSelection = 0;
        private int m_sizeListSelection = 1;

        private GameObject m_createGameWindow;

        private List<MapPresets.Preset> m_mapPresets;
        private List<MapPresets.MapSize> m_mapSizes;

        private GameObject m_basePresetButton;
        private GameObject m_baseSizeButton;

        private List<GameObject> m_presetButtons;
        private List<GameObject> m_sizeButtons;
        [SerializeField]
        private GameObject m_roomsScrollBar;

        private GameObject m_leaderboardWindow;
        private GameObject m_scoreText;
        [SerializeField]
        private GameObject m_scoreRect;

        [SerializeField]
        private Sprite m_selectedTabSprite;
        [SerializeField]
        private Sprite m_tabSprite;
        private Color m_selectedTabColor;
        private Color m_tabColor;

        [SerializeField]
        private GameObject m_playerChevron;

        [SerializeField]
        private GameObject ChatCell = null;

        [SerializeField]
        private GameObject GlobalChatCell = null;

        private GameObject m_joiningGameWindow;

        private GameObject m_controlWindow;
        private GameObject m_achievementsWindow;

        private GameObject m_lobbyWindow;
        private GameObject m_chatWindow;
        private GameObject m_gameStartButton;

        private GameObject m_confirmJoinLobbyWindow;
        List<MatchInfoSnapshot> m_roomList = null;

        private Dictionary<string, bool> m_roomFilters = new Dictionary<string, bool>()
        {
            {"HideFull",false},
            {"HideLevelRange", false}
        };

        private string m_filterName = "";
        private DialogDisplay m_dialogueDisplay;
        
        void Start()
        {
            BombersNetworkManager.RefreshBCVariable();
            BombersNetworkManager._BC.Client.EnableRTT();

            m_lobbyWindow = GameObject.Find("Lobby");
            m_chatWindow = GameObject.Find("GlobalChat");
            m_confirmJoinLobbyWindow = GameObject.Find("ConfirmJoinLobby");
            m_gameStartButton = GameObject.Find("StartGame");
            m_globaChatContent = GameObject.Find("globalChatContent");
            m_chatContent = GameObject.Find("lobbyChatContent");

            if (!BombersNetworkManager._BC.Client.Initialized)
            {
                SceneManager.LoadScene("Connect");
                return;
            }
            BombersNetworkManager.singleton.StartMatchMaker();
            m_selectedTabColor = GameObject.Find("Aces Tab").transform.GetChild(0).GetComponent<Text>().color;
            m_tabColor = GameObject.Find("Bombers Tab").transform.GetChild(0).GetComponent<Text>().color;

            m_dialogueDisplay = FindObjectOfType<DialogDisplay>();

            m_achievementsWindow = GameObject.Find("Achievements");
            m_refreshLabel = GameObject.Find("RefreshLabel");
            m_refreshLabel.GetComponent<Text>().text = "Refreshing list...";
            m_achievementsWindow.transform.GetChild(3).GetChild(0).gameObject.SetActive(false);
            m_achievementsWindow.transform.GetChild(4).GetChild(0).gameObject.SetActive(false);
            m_achievementsWindow.transform.GetChild(5).GetChild(0).gameObject.SetActive(false);
            m_achievementsWindow.SetActive(false);
            m_joiningGameWindow = GameObject.Find("JoiningGame");
            m_joiningGameWindow.SetActive(false);
            m_leaderboardWindow = GameObject.Find("Leaderboard");
            m_scoreText = GameObject.Find("SCORE");
            m_basePresetButton = GameObject.Find("PresetButton");
            m_baseSizeButton = GameObject.Find("SizeButton");
            m_basePresetButton.SetActive(false);
            m_baseSizeButton.SetActive(false);
            m_presetButtons = new List<GameObject>();
            m_sizeButtons = new List<GameObject>();
            m_mapPresets = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_presets;
            m_mapSizes = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_mapSizes;
            m_leaderboardWindow.SetActive(false);
            m_controlWindow = GameObject.Find("Controls");
            m_controlWindow.SetActive(false);


            for (int i = 0; i < m_mapPresets.Count; i++)
            {
                GameObject presetButton = (GameObject)Instantiate(m_basePresetButton, m_basePresetButton.transform.position, m_basePresetButton.transform.rotation);
                presetButton.transform.SetParent(m_basePresetButton.transform.parent);
                Vector3 position = presetButton.GetComponent<RectTransform>().position;
                position.y -= i * 23;
                presetButton.GetComponent<RectTransform>().position = position;
                int option = i;
                presetButton.GetComponent<Button>().onClick.AddListener(() => { SelectLayoutOption(option); });
                presetButton.transform.GetChild(0).GetComponent<Text>().text = m_mapPresets[i].m_name;
                m_presetButtons.Add(presetButton);
            }

            for (int i = 0; i < m_mapSizes.Count; i++)
            {
                GameObject sizeButton = (GameObject)Instantiate(m_baseSizeButton, m_baseSizeButton.transform.position, m_baseSizeButton.transform.rotation);
                sizeButton.transform.SetParent(m_baseSizeButton.transform.parent);
                Vector3 position = sizeButton.GetComponent<RectTransform>().position;
                position.y -= i * 23;
                sizeButton.GetComponent<RectTransform>().position = position;
                int option = i;
                sizeButton.GetComponent<Button>().onClick.AddListener(() => { SelectSizeOption(option); });
                sizeButton.transform.GetChild(0).GetComponent<Text>().text = m_mapSizes[i].m_name;
                m_sizeButtons.Add(sizeButton);
            }

            m_baseButton = GameObject.Find("Game Lineitem");
            m_baseButton.SetActive(false);
            m_roomButtons = new List<RoomButton>();
            m_showRoomsWindow = GameObject.Find("ShowRooms");
            m_createGameWindow = GameObject.Find("CreateGame");

            m_createGameWindow.SetActive(false);
            GameObject.Find("PlayerName").GetComponent<InputField>().text = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().PlayerName;
            GameObject.Find("PlayerName").GetComponent<InputField>().interactable = false;
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadStatistics();
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetLeaderboard(m_currentLeaderboardID);
            OnRoomsWindow();
        }

        public void EditName()
        {
            GameObject.Find("PlayerName").GetComponent<InputField>().interactable = true;
            GameObject.Find("PlayerName").GetComponent<InputField>().ActivateInputField();
            GameObject.Find("PlayerName").GetComponent<InputField>().Select();
            GameObject.Find("PlayerName").GetComponent<Image>().enabled = true;
        }

        public void FinishEditName()
        {
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().PlayerName = GameObject.Find("PlayerName").GetComponent<InputField>().text;
            BombersNetworkManager._BC.Client.PlayerStateService.UpdateUserName(GameObject.Find("PlayerName").GetComponent<InputField>().text);
            GameObject.Find("PlayerName").GetComponent<InputField>().interactable = false;
            GameObject.Find("PlayerName").GetComponent<Image>().enabled = false;
        }

        private void OnGUI()
        {
            switch (m_state)
            {
                case eMatchmakingState.GAME_STATE_CONFIRM_LOBBY_OFFER:
                    {
                        m_confirmJoinLobbyWindow.gameObject.SetActive(true);
                        m_chatWindow.gameObject.SetActive(true);
                        m_lobbyWindow.gameObject.SetActive(false);
                        m_achievementsWindow.SetActive(false);
                        m_showRoomsWindow.SetActive(false);
                        m_createGameWindow.SetActive(false);
                        m_leaderboardWindow.SetActive(false);
                        m_controlWindow.SetActive(false);
                        m_joiningGameWindow.SetActive(false);
                    }
                    break;

                case eMatchmakingState.GAME_STATE_GLOBAL_CHAT:
                    m_confirmJoinLobbyWindow.gameObject.SetActive(false);
                    m_chatWindow.gameObject.SetActive(true);
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(false);
                    break;

                case eMatchmakingState.GAME_STATE_WAITING_FOR_PLAYERS:
                    m_confirmJoinLobbyWindow.gameObject.SetActive(false);
                    m_chatWindow.gameObject.SetActive(false);
                    m_lobbyWindow.gameObject.SetActive(true);
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(false);

                    OnWaitingForPlayersWindow();
                    break;

                case eMatchmakingState.GAME_STATE_SHOW_ROOMS:
                    m_confirmJoinLobbyWindow.gameObject.SetActive(false);
                    m_chatWindow.gameObject.SetActive(false);
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(true);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(false);

                    OnStatsWindow();
                    OrderRoomButtons();
                    break;

                case eMatchmakingState.GAME_STATE_NEW_ROOM_OPTIONS:
                case eMatchmakingState.GAME_STATE_FIND_ROOM_OPTIONS:
                    m_confirmJoinLobbyWindow.gameObject.SetActive(false);
                    m_chatWindow.gameObject.SetActive(false);
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(true);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(false);

                    OnNewRoomWindow();
                    break;

                case eMatchmakingState.GAME_STATE_JOIN_ROOM:
                    m_confirmJoinLobbyWindow.gameObject.SetActive(false);
                    m_chatWindow.gameObject.SetActive(false);
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(true);

                    break;

                case eMatchmakingState.GAME_STATE_CREATE_NEW_ROOM:
                    m_confirmJoinLobbyWindow.gameObject.SetActive(false);
                    m_chatWindow.gameObject.SetActive(false);
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(true);

                    break;
                case eMatchmakingState.GAME_STATE_SHOW_LEADERBOARDS:
                    m_confirmJoinLobbyWindow.gameObject.SetActive(false);
                    m_chatWindow.gameObject.SetActive(false);
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(true);
                    m_controlWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(false);

                    OnLeaderboardWindow();

                    break;
                case eMatchmakingState.GAME_STATE_SHOW_CONTROLS:
                    m_confirmJoinLobbyWindow.gameObject.SetActive(false);
                    m_chatWindow.gameObject.SetActive(false);
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_controlWindow.SetActive(true);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(false);

                    break;
                case eMatchmakingState.GAME_STATE_SHOW_ACHIEVEMENTS:
                    m_confirmJoinLobbyWindow.gameObject.SetActive(false);
                    m_chatWindow.gameObject.SetActive(false);
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_achievementsWindow.SetActive(true);
                    m_showRoomsWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(false);
                    if (GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_achievements[2].m_achieved)
                    {
                        m_achievementsWindow.transform.GetChild(3).GetComponent<CanvasGroup>().alpha = 1;
                        m_achievementsWindow.transform.GetChild(3).GetChild(0).gameObject.SetActive(true);
                    }
                    else
                    {
                        m_achievementsWindow.transform.GetChild(3).GetComponent<CanvasGroup>().alpha = 0.3f;
                        m_achievementsWindow.transform.GetChild(3).GetChild(0).gameObject.SetActive(false);
                    }

                    if (GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_achievements[1].m_achieved)
                    {
                        m_achievementsWindow.transform.GetChild(4).GetComponent<CanvasGroup>().alpha = 1;
                        m_achievementsWindow.transform.GetChild(4).GetChild(0).gameObject.SetActive(true);
                    }
                    else
                    {
                        m_achievementsWindow.transform.GetChild(4).GetComponent<CanvasGroup>().alpha = 0.3f;
                        m_achievementsWindow.transform.GetChild(4).GetChild(0).gameObject.SetActive(false);
                    }

                    if (GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_achievements[0].m_achieved)
                    {
                        m_achievementsWindow.transform.GetChild(5).GetComponent<CanvasGroup>().alpha = 1;
                        m_achievementsWindow.transform.GetChild(5).GetChild(0).gameObject.SetActive(true);
                    }
                    else
                    {
                        m_achievementsWindow.transform.GetChild(5).GetComponent<CanvasGroup>().alpha = 0.3f;
                        m_achievementsWindow.transform.GetChild(5).GetChild(0).gameObject.SetActive(false);
                    }

                    break;
            }
        }

        public void ShowAchievements()
        {
            m_state = eMatchmakingState.GAME_STATE_SHOW_ACHIEVEMENTS;
        }

        public void ShowLobby()
        {
            m_state = eMatchmakingState.GAME_STATE_WAITING_FOR_PLAYERS;
        }

        public void ChangeTeam()
        {
            BombersNetworkManager._BC.LobbyService.SwitchTeam(BombersNetworkManager.LobbyInfo.LobbyId, BombersNetworkManager.LobbyInfo.GetOppositeTeamCodeWithProfileId(BombersNetworkManager._BC.Client.ProfileId));
        }

        public void StartGame()
        {
            BombersNetworkManager._BC.LobbyService.UpdateReady(BombersNetworkManager.LobbyInfo.LobbyId, true, BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(BombersNetworkManager._BC.Client.ProfileId).ExtraData);
            //BCLobbyMemberInfo member = BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(_bc.Client.ProfileId);
            //(BombersNetworkManager.singleton as BombersNetworkManager).CreateOrJoinUNETMatch(member);
        }

        public void OnSendLobbyChatSignal(InputField in_field)
        {
            if (in_field.text != "")
            {
                Dictionary<string, object> jsonData = new Dictionary<string, object>();
                jsonData["message"] = in_field.text;
                jsonData["name"] = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().PlayerName;

                BombersNetworkManager._BC.LobbyService.SendSignal(BombersNetworkManager.LobbyInfo.LobbyId, jsonData);
                in_field.text = "";
            }
        }

        private GameObject m_chatContent = null;
        public void AddLobbyChatMessage(Dictionary<string, object> in_jsonMessage)
        {
            Transform contentTransform = m_chatContent.transform;
            lock (contentTransform)
            {
                // populate based on the incoming data
                if (contentTransform.childCount >= 30)
                {
                    Destroy(contentTransform.transform.GetChild(0).gameObject);
                }

                ChatCell tempCell;
                GameObject tempObj;
                Dictionary<string, object> jsonData = in_jsonMessage.ContainsKey("data") ?
                                                        (Dictionary<string, object>)in_jsonMessage["data"] : in_jsonMessage;

                Dictionary<string, object> fromData = (Dictionary<string, object>)jsonData["from"];
                
                Dictionary<string, object> signalData = jsonData.ContainsKey("signalData") ?
                                                        (Dictionary<string, object>)jsonData["signalData"] : jsonData;

                tempObj = (GameObject)Instantiate(ChatCell, Vector3.zero, Quaternion.identity, contentTransform);
                tempCell = tempObj.GetComponent<ChatCell>();
                tempCell.Init(fromData["name"] as string, signalData["message"] as string, fromData["id"] as string, fromData["pic"] as string, "");
            }
        }

        private GameObject m_globaChatContent = null;
        public void AddGlobalChatMessage(Dictionary<string, object> in_jsonMessage)
        {
            Transform contentTransform = m_globaChatContent.transform;
            lock (contentTransform)
            {
                // populate based on the incoming data
                if (contentTransform.childCount >= 30)
                {
                    Destroy(contentTransform.transform.GetChild(0).gameObject);
                }

                ChatCell tempCell;
                GameObject tempObj;
                Dictionary<string, object> jsonData = in_jsonMessage.ContainsKey("data") ?
                                                                    (Dictionary<string, object>)in_jsonMessage["data"] : in_jsonMessage;

                Dictionary<string, object> fromData = (Dictionary<string, object>)jsonData["from"];
                Dictionary<string, object> contentData = (Dictionary<string, object>)jsonData["content"];
                Dictionary<string, object> richData = contentData.ContainsKey("rich") ? (Dictionary<string, object>)contentData["rich"] : null;

                tempObj = Instantiate(GlobalChatCell, Vector3.zero, Quaternion.identity, contentTransform);
                tempCell = tempObj.GetComponent<ChatCell>();

                tempCell.Init(fromData["name"] as string, contentData["text"] as string, fromData["id"] as string, fromData["pic"] as string,
                    richData.ContainsKey("lastConnectionId") ? richData["lastConnectionId"] as string : "",
                    Convert.ToUInt64(jsonData["msgId"]));
            }
        }

        public void OnGlobalChatEntered(InputField in_field)
        {
            in_field.text = in_field.text.Replace("\n", "").Trim();
            if (in_field.text.Length > 0)
            {
                Dictionary<string, object> jsonData = new Dictionary<string, object>();
                jsonData["lastConnectionId"] = BombersNetworkManager._BC.Client.RTTConnectionID;
                jsonData["timeSent"] = DateTime.UtcNow.ToLongTimeString();

                BombersNetworkManager._BC.ChatService.PostChatMessage(BombersNetworkManager._BC.Client.AppId + ":gl:main", in_field.text, 
                    BrainCloudUnity.BrainCloudPlugin.BCWrapped.JsonFx.Json.JsonWriter.Serialize(jsonData) );
            }

            in_field.text = "";
        }

        public void OnChatMessageDeleted(Dictionary<string, object> in_jsonMessage)
        {
            Transform contentTransform = m_chatContent.transform;
            lock (contentTransform)
            {
                Dictionary<string, object> jsonData = in_jsonMessage.ContainsKey("data") ?
                                                    (Dictionary<string, object>)in_jsonMessage["data"] : in_jsonMessage;

                for (int i = 0; i < contentTransform.childCount; ++i)
                {
                    if (Convert.ToUInt64(jsonData["msgId"]) == contentTransform.GetChild(i).GetComponent<ChatCell>().MessageId)
                    {
                        Destroy(contentTransform.GetChild(i).gameObject);
                        break;
                    }
                }
            }
        }

        public void OnChatMessageUpdated(Dictionary<string, object> in_jsonMessage)
        {
            Transform contentTransform = m_chatContent.transform;
            lock (contentTransform)
            {
                Dictionary<string, object> jsonData = in_jsonMessage.ContainsKey("data") ?
                                                                    (Dictionary<string, object>)in_jsonMessage["data"] : in_jsonMessage;

                Dictionary<string, object> fromData = (Dictionary<string, object>)jsonData["from"];
                Dictionary<string, object> contentData = (Dictionary<string, object>)jsonData["content"];
                Dictionary<string, object> richData = contentData.ContainsKey("rich") ? (Dictionary<string, object>)contentData["rich"] : null;

                ChatCell cell;
                for (int i = 0; i < contentTransform.childCount; ++i)
                {
                    cell = contentTransform.GetChild(i).GetComponent<ChatCell>();
                    if (Convert.ToUInt64(jsonData["msgId"]) == cell.MessageId)
                    {
                        cell.transform.SetParent(null);

                        Dictionary<string, object> richUpdated = new Dictionary<string, object>();
                        richUpdated["lastConnectionId"] = richData.ContainsKey("lastConnectionId") ? richData["lastConnectionId"] as string : "";
                        richUpdated["timeSent"] = DateTime.UtcNow.ToLongTimeString();

                        cell.Init(fromData["name"] as string, contentData["text"] as string, fromData["id"] as string, fromData["pic"] as string,
                            BrainCloudUnity.BrainCloudPlugin.BCWrapped.JsonFx.Json.JsonWriter.Serialize(jsonData), Convert.ToUInt64(jsonData["msgId"]));

                        cell.transform.SetParent(contentTransform);
                        cell.transform.SetSiblingIndex(i);
                        break;
                    }
                }
            }
        }
        
        private void OnWaitingForPlayersWindow()
        {
            List<BCLobbyMemberInfo> greenPlayers = new List<BCLobbyMemberInfo>();
            List<BCLobbyMemberInfo> redPlayers = new List<BCLobbyMemberInfo>();

            for (int i = 0; i < BombersNetworkManager.LobbyInfo.Members.Count; i++)
            {
                if (BombersNetworkManager.LobbyInfo.Members[i].Team == "green")
                {
                    greenPlayers.Add(BombersNetworkManager.LobbyInfo.Members[i]);
                }
                else
                {
                    redPlayers.Add(BombersNetworkManager.LobbyInfo.Members[i]);
                }
            }

            Text teamText = GameObject.Find("GreenPlayerNames").GetComponent<Text>();
            Text teamPingText = GameObject.Find("GreenPings").GetComponent<Text>();

            string nameText = "";
            string pingText = "";
            for (int i = 0; i < greenPlayers.Count; i++)
            {
                nameText += greenPlayers[i].Name + "\n";
                pingText += greenPlayers[i].Rating + "\n";
            }

            teamText.text = nameText;
            teamPingText.text = pingText;
            teamText = GameObject.Find("RedPlayerNames").GetComponent<Text>();
            teamPingText = GameObject.Find("RedPings").GetComponent<Text>();
            nameText = "";
            pingText = "";

            for (int i = 0; i < redPlayers.Count; i++)
            {
                nameText += redPlayers[i].Name + "\n";
                pingText += redPlayers[i].Rating + "\n";
            }
            teamText.text = nameText;
            teamPingText.text = pingText;

            float halfMax = Mathf.Floor((int)BombersNetworkManager.LobbyInfo.Settings["maxPlayers"] / 2.0f);
            GameObject.Find("GreenPlayers").GetComponent<Text>().text = greenPlayers.Count + "/" + halfMax;
            GameObject.Find("RedPlayers").GetComponent<Text>().text = redPlayers.Count + "/" + halfMax;
            GameObject.Find("GameName").GetComponent<Text>().text = BombersNetworkManager.LobbyInfo.Settings["gameName"] as string;
            m_gameStartButton.SetActive(BombersNetworkManager._BC.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId);

            if (!m_gameStartButton.activeInHierarchy)
            {
                if (m_changeTeamOrigPosition == Vector3.zero) m_changeTeamOrigPosition = GameObject.Find("ChangeTeam").transform.position;
                GameObject.Find("ChangeTeam").transform.position = m_gameStartButton.transform.position;
            }
            else if (m_changeTeamOrigPosition != Vector3.zero)
            {
                GameObject.Find("ChangeTeam").transform.position = m_changeTeamOrigPosition;
            }
        }

        private Vector3 m_changeTeamOrigPosition = Vector3.zero;

        private void OnStatsWindow()
        {
            List<BrainCloudStats.Stat> playerStats = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetStats();
            string rank = "";
            if (playerStats[0].m_statValue >= GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_playerLevelTitles.Length)
            {
                rank = "0" + "\n" + playerStats[1].m_statValue.ToString();
            }
            else
            {
                rank = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_playerLevelTitles[playerStats[0].m_statValue - 1] + " (" + (playerStats[0].m_statValue) + ")\n" + playerStats[1].m_statValue.ToString();
            }
            string stats = playerStats[3].m_statValue.ToString() + "\n" + playerStats[2].m_statValue.ToString() + "\n" + playerStats[4].m_statValue.ToString()
                + "\n" + playerStats[5].m_statValue.ToString() + "\n" + playerStats[6].m_statValue.ToString()
                + "\n" + playerStats[7].m_statValue.ToString() + "\n" + playerStats[8].m_statValue.ToString()
                + "\n" + playerStats[9].m_statValue.ToString();

            GameObject.Find("StatText").GetComponent<Text>().text = stats;
            GameObject.Find("RankText").GetComponent<Text>().text = rank;
        }

        public void CancelCreateGame()
        {
            CloseDropDowns();
            m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
            RefreshRoomsList();
        }

        public void ConfirmCreateGame()
        {
            CloseDropDowns();

            m_roomMaxPlayers = int.Parse(m_createGameWindow.transform.Find("Max Players").GetComponent<InputField>().text.ToString());
            m_roomLevelRangeMax = int.Parse(m_createGameWindow.transform.Find("Box 2").GetComponent<InputField>().text.ToString());
            m_roomLevelRangeMin = int.Parse(m_createGameWindow.transform.Find("Box 1").GetComponent<InputField>().text.ToString());

            var matchAttributes = new Dictionary<string, object>() { { "minLevel", m_roomLevelRangeMin }, { "maxLevel", m_roomLevelRangeMax } };

            CreateNewRoom(m_createGameWindow.transform.Find("Room Name").GetComponent<InputField>().text, (uint)m_roomMaxPlayers, matchAttributes);
        }

        public void DisplayJoinLobbyOffer(string in_profileId, string in_userName)
        {
            m_lastOfferedJoinLobby = in_profileId;
            Text label = m_confirmJoinLobbyWindow.transform.Find("Label").gameObject.GetComponent<Text>();
            label.text = "' " + in_userName + " ' is requesting to join a lobby with them.  Do you accept?"; 
            m_state = eMatchmakingState.GAME_STATE_CONFIRM_LOBBY_OFFER;
        }

        private string m_lastOfferedJoinLobby = "";
        public void ConfirmJoinGameWithOther()
        {
            // send event to confirm
            Dictionary<string, object> jsonData = new Dictionary<string, object>();
            jsonData["lastConnectionId"] = BombersNetworkManager._BC.Client.RTTConnectionID;
            jsonData["profileId"] = BombersNetworkManager._BC.Client.ProfileId;
            jsonData["userName"] = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().PlayerName;
            
            // send event to other person
            BombersNetworkManager._BC.Client.EventService.SendEvent(m_lastOfferedJoinLobby, "CONFIRM_JOIN_LOBBY",
                BrainCloudUnity.BrainCloudPlugin.BCWrapped.JsonFx.Json.JsonWriter.Serialize(jsonData));

            BombersNetworkManager.WaitOnLobbyJoin();
        }

        public void CancelJoinGameWithOther()
        {
            m_state = eMatchmakingState.GAME_STATE_GLOBAL_CHAT;
        }

        public void CloseDropDowns()
        {
            m_showPresetList = false;
            m_showSizeList = false;
        }

        public void OpenLayoutDropdown()
        {
            m_showPresetList = true;
            m_showSizeList = false;
        }

        public void OpenSizeDropdown()
        {
            m_showPresetList = false;
            m_showSizeList = true;
        }

        public void SelectLayoutOption(int aOption)
        {
            m_presetListSelection = aOption;
            CloseDropDowns();
        }

        public void SelectSizeOption(int aOption)
        {
            m_sizeListSelection = aOption;
            CloseDropDowns();
        }

        private void OnNewRoomWindow()
        {
            m_createGameWindow.transform.Find("Layout").Find("Selection").GetComponent<Text>().text = m_mapPresets[m_presetListSelection].m_name;
            m_createGameWindow.transform.Find("Size").Find("Selection").GetComponent<Text>().text = m_mapSizes[m_sizeListSelection].m_name;

            if (m_showPresetList)
            {
                for (int i = 0; i < m_presetButtons.Count; i++)
                {
                    m_presetButtons[i].SetActive(true);
                }
            }
            else
            {
                for (int i = 0; i < m_presetButtons.Count; i++)
                {
                    m_presetButtons[i].SetActive(false);
                }
            }


            if (m_showSizeList)
            {
                for (int i = 0; i < m_sizeButtons.Count; i++)
                {
                    m_sizeButtons[i].SetActive(true);
                }
            }
            else
            {
                for (int i = 0; i < m_sizeButtons.Count; i++)
                {
                    m_sizeButtons[i].SetActive(false);
                }
            }
        }

        public void OnJoinRoomState()
        {
            m_state = eMatchmakingState.GAME_STATE_JOIN_ROOM;
        }

        public void JoinRoom(MatchInfoSnapshot aRoomInfo)
        {
            int minLevel = 0;
            int maxLevel = 50;
            int playerLevel = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetStats()[0].m_statValue;

            if (playerLevel < minLevel || playerLevel > maxLevel)
            {
                m_dialogueDisplay.DisplayDialog("You're not in that room's\nlevel range!");
            }
            else
            {
                m_state = eMatchmakingState.GAME_STATE_JOIN_ROOM;
                try
                {
                    BombersNetworkManager.singleton.matchMaker.JoinMatch(aRoomInfo.networkId, "", "", "", 0, 0, OnMatchJoined);
                }
                catch (ArgumentException e)
                {
                    m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
                    m_dialogueDisplay.DisplayDialog("You just left that room!");
                    Debug.Log("caught ArgumentException " + e);
                }
                catch (Exception e)
                {
                    m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
                    m_dialogueDisplay.DisplayDialog("Error joining room! Try restarting.");
                    Debug.Log("caught Exception " + e);
                }
            }
        }

        public void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
        {
            (BombersNetworkManager.singleton as BombersNetworkManager).LeaveLobby();
            if (success)
            {
                try
                {
                    BombersNetworkManager.singleton.OnMatchJoined(success, extendedInfo, matchInfo);
                }
                catch (ArgumentException e)
                {
                    m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
                    m_dialogueDisplay.DisplayDialog("You just left that room!");
                    RefreshRoomsList();
                    Debug.Log("caught ArgumentException " + e);
                }
                catch (Exception e)
                {
                    m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
                    RefreshRoomsList();
                    m_dialogueDisplay.DisplayDialog("Error joining room! Try restarting.");
                    Debug.Log("caught Exception " + e);
                }
            }
            else
            {
                m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
                RefreshRoomsList();
                m_dialogueDisplay.DisplayDialog("Could not join room!");
                Debug.LogError("Join match failed");
            }
        }

        public void RefreshRoomsList()
        {
            m_refreshLabel.GetComponent<Text>().text = "Refreshing List...";
            OnRoomsWindow();
        }

        private void OrderRoomButtons()
        {
            m_roomFilters["HideFull"] = GameObject.Find("Toggle-Hide").GetComponent<Toggle>().isOn;
            m_roomFilters["HideLevelRange"] = GameObject.Find("Toggle-MyRank").GetComponent<Toggle>().isOn;
            m_filterName = GameObject.Find("InputField").GetComponent<InputField>().text;

            for (int i = 0; i < m_roomButtons.Count; i++)
            {

                if (m_filterName != "" && !m_roomButtons[i].m_room.name.ToLower().Contains(m_filterName.ToLower()))
                {
                    continue;
                }
            }
        }

        public void OnListRoomsCallback(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
        {
            //Debug.Log(aResponse.ToString());
            m_roomList = new List<MatchInfoSnapshot>();
            m_roomList.Clear();
            foreach (MatchInfoSnapshot match in matches)
            {
                m_roomList.Add(match);
            }

            if (m_roomList != null)
            {

                for (int i = 0; i < m_roomList.Count; i++)
                {
                    GameObject roomButton = (GameObject)Instantiate(m_baseButton, m_baseButton.transform.position, m_baseButton.transform.rotation);
                    roomButton.SetActive(true);
                    roomButton.transform.SetParent(m_baseButton.transform.parent);
                    Vector3 position = roomButton.GetComponent<RectTransform>().position;
                    position.y -= i * 30;
                    roomButton.GetComponent<RectTransform>().position = position;
                    MatchInfoSnapshot roomInfo = m_roomList[i];
                    roomButton.GetComponent<Button>().onClick.AddListener(() => { JoinRoom(roomInfo); });
                    roomButton.transform.GetChild(0).GetComponent<Text>().text = m_roomList[i].name;

                    roomButton.transform.GetChild(1).GetComponent<Text>().text = m_roomList[i].currentSize + "/" + m_roomList[i].maxSize;
                    m_roomButtons.Add(new RoomButton(roomInfo, roomButton.GetComponent<Button>()));
                }

                if (m_roomList.Count > 0)
                {
                    m_refreshLabel.GetComponent<Text>().text = "";
                }
                else
                {
                    m_refreshLabel.GetComponent<Text>().text = "No rooms found...";
                }

                if (m_roomList.Count < 9)
                {
                    m_roomsScrollBar.SetActive(false);
                }
                else
                {
                    m_roomsScrollBar.SetActive(true);
                }
            }
            else
            {
                m_refreshLabel.GetComponent<Text>().text = "No rooms found...";
                m_roomsScrollBar.SetActive(false);
            }
        }

        void OnRoomsWindow()
        {
            for (int i = 0; i < m_roomButtons.Count; i++)
            {
                Destroy(m_roomButtons[i].m_button.gameObject);
            }
            m_refreshLabel.GetComponent<Text>().text = "Refreshing...";
            m_roomButtons.Clear();
            BombersNetworkManager.singleton.matchMaker.ListMatches(0, 100, "", true, 0, 0, OnListRoomsCallback);
        }

        private string m_currentLeaderboardID = "KDR";
        private bool m_leaderboardReady = false;

        public void ShowLeaderboard()
        {
            m_state = eMatchmakingState.GAME_STATE_SHOW_LEADERBOARDS;
            m_leaderboardReady = false;
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetLeaderboard(m_currentLeaderboardID);
        }

        public void CloseLeaderboard()
        {
            m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
            RefreshRoomsList();
        }

        public void ShowKDRLeaderboard()
        {
            if (m_currentLeaderboardID != "KDR")
            {
                GameObject.Find("Aces Tab").GetComponent<Image>().sprite = m_selectedTabSprite;
                GameObject.Find("Bombers Tab").GetComponent<Image>().sprite = m_tabSprite;
                GameObject.Find("Aces Tab").transform.GetChild(0).GetComponent<Text>().color = m_selectedTabColor;
                GameObject.Find("Bombers Tab").transform.GetChild(0).GetComponent<Text>().color = m_tabColor;
                m_leaderboardReady = false;
                m_currentLeaderboardID = "KDR";
                GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetLeaderboard(m_currentLeaderboardID);
            }
        }

        public void ShowBDRLeaderboard()
        {
            if (m_currentLeaderboardID != "BDR")
            {
                GameObject.Find("Bombers Tab").GetComponent<Image>().sprite = m_selectedTabSprite;
                GameObject.Find("Aces Tab").GetComponent<Image>().sprite = m_tabSprite;
                GameObject.Find("Aces Tab").transform.GetChild(0).GetComponent<Text>().color = m_tabColor;
                GameObject.Find("Bombers Tab").transform.GetChild(0).GetComponent<Text>().color = m_selectedTabColor;
                m_leaderboardReady = false;
                m_currentLeaderboardID = "BDR";
                GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetLeaderboard(m_currentLeaderboardID);
            }
        }

        public void ShowControls()
        {
            m_state = eMatchmakingState.GAME_STATE_SHOW_CONTROLS;
        }

        public void HideControls()
        {
            (BombersNetworkManager.singleton as BombersNetworkManager).LeaveLobby();
            (BombersNetworkManager.singleton as BombersNetworkManager).DisconnectGlobalChat();
            m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
            RefreshRoomsList();
        }

        private bool m_once = true;

        void OnLeaderboardWindow()
        {
            if (GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_leaderboardReady)
            {
                if (!m_leaderboardReady) m_once = false;
                m_leaderboardReady = true;
            }
            else
            {
                m_leaderboardReady = false;
                m_scoreRect.GetComponent<RectTransform>().localPosition = new Vector3(m_scoreRect.GetComponent<RectTransform>().localPosition.x, -(m_scoreRect.GetComponent<RectTransform>().sizeDelta.y / 2), m_scoreRect.GetComponent<RectTransform>().localPosition.z);
            }

            if (m_currentLeaderboardID == "KDR")
            {
                m_scoreText.GetComponent<Text>().text = "KILLS";
            }
            else
            {
                m_scoreText.GetComponent<Text>().text = "TARGETS HIT";
            }

            LitJson.JsonData leaderboardData = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_leaderboardData;

            string leaderboardRankText = "";
            string leaderboardNameText = "";
            string leaderboardScoreText = "";
            string leaderboardLevelText = "";

            int players = 1;
            bool playerListed = false;
            int playerChevronPosition = 0;
            if (m_leaderboardReady)
            {

                players = leaderboardData["leaderboard"].Count;

                for (int i = 0; i < players; i++)
                {
                    if (leaderboardData["leaderboard"][i]["name"].ToString() == GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().PlayerName)
                    {
                        playerListed = true;
                        playerChevronPosition = i;
                        leaderboardRankText += "\n";
                        leaderboardNameText += "\n";
                        leaderboardLevelText += "\n";
                        leaderboardScoreText += "\n";
                        m_playerChevron.transform.Find("PlayerPlace").GetComponent<Text>().text = (i + 1) + "";
                        m_playerChevron.transform.Find("PlayerName").GetComponent<Text>().text = leaderboardData["leaderboard"][i]["name"].ToString() + "\n"; ;
                        m_playerChevron.transform.Find("PlayerLevel").GetComponent<Text>().text = leaderboardData["leaderboard"][i]["data"]["rank"].ToString() + " (" + leaderboardData["leaderboard"][i]["data"]["level"].ToString() + ")\n"; ;
                        m_playerChevron.transform.Find("PlayerScore").GetComponent<Text>().text = (Mathf.Floor(float.Parse(leaderboardData["leaderboard"][i]["score"].ToString()) / 10000) + 1).ToString("n0") + "\n";
                        //96.6
                        //17.95
                    }
                    else
                    {
                        leaderboardRankText += (i + 1) + "\n";
                        leaderboardNameText += leaderboardData["leaderboard"][i]["name"].ToString() + "\n";
                        leaderboardLevelText += leaderboardData["leaderboard"][i]["data"]["rank"].ToString() + " (" + leaderboardData["leaderboard"][i]["data"]["level"].ToString() + ")\n";
                        leaderboardScoreText += (Mathf.Floor(float.Parse(leaderboardData["leaderboard"][i]["score"].ToString()) / 10000) + 1).ToString("n0") + "\n";
                    }
                }
                if (players == 0)
                {
                    leaderboardNameText = "No entries found...";
                    leaderboardRankText = "";
                    leaderboardLevelText = "";
                    leaderboardScoreText = "";
                }
            }
            else
            {
                leaderboardNameText = "Please wait...";
                leaderboardRankText = "";
                leaderboardLevelText = "";
                leaderboardScoreText = "";
            }


            m_scoreRect.transform.Find("List").GetComponent<Text>().text = leaderboardNameText;
            m_scoreRect.transform.Find("List Ranks").GetComponent<Text>().text = leaderboardRankText;
            m_scoreRect.transform.Find("List Count").GetComponent<Text>().text = leaderboardScoreText;
            m_scoreRect.transform.Find("List Level").GetComponent<Text>().text = leaderboardLevelText;
            m_scoreRect.GetComponent<RectTransform>().sizeDelta = new Vector2(m_scoreRect.GetComponent<RectTransform>().sizeDelta.x, 18.2f * players);
            if (!m_once)
            {
                m_once = true;
                m_scoreRect.transform.parent.parent.Find("Scrollbar").GetComponent<Scrollbar>().value = 1;
                m_scoreRect.transform.parent.parent.Find("Scrollbar").GetComponent<Scrollbar>().value = 0.99f;
                m_scoreRect.transform.parent.parent.Find("Scrollbar").GetComponent<Scrollbar>().value = 1;
            }
            if (!playerListed)
            {
                m_playerChevron.SetActive(false);
            }
            else
            {
                m_playerChevron.GetComponent<RectTransform>().localPosition = new Vector3(m_playerChevron.GetComponent<RectTransform>().localPosition.x, -(19f * playerChevronPosition), m_playerChevron.GetComponent<RectTransform>().localPosition.z);

                m_playerChevron.SetActive(true);
            }
        }

        void OnJoinRoomFailed()
        {
            m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
            m_dialogueDisplay.DisplayDialog("Could not join room!");
        }

        public void QuitToLogin()
        {
            BombersNetworkManager._BC.Client.PlayerStateService.Logout();
            BombersNetworkManager._BC.Client.AuthenticationService.ClearSavedProfileID();
            SceneManager.LoadScene("Connect");
        }

        public void CreateGame()
        {
            m_state = eMatchmakingState.GAME_STATE_NEW_ROOM_OPTIONS;
            m_createGameWindow.transform.Find("Room Name").GetComponent<InputField>().text = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_previousGameName;
        }

        public void FindLobby()
        {
            m_state = eMatchmakingState.GAME_STATE_FIND_ROOM_OPTIONS;
        }

        public void GlobalChat()
        {
            m_state = eMatchmakingState.GAME_STATE_GLOBAL_CHAT;

            // clear all previous messages
            for (int i = 0; i < m_globaChatContent.transform.childCount; ++i)
            {
                Destroy(m_globaChatContent.transform.GetChild(i).gameObject);
            }

            (BombersNetworkManager.singleton as BombersNetworkManager).ConnectToGlobalChat();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) &&
                (m_state == eMatchmakingState.GAME_STATE_NEW_ROOM_OPTIONS ||
                m_state == eMatchmakingState.GAME_STATE_FIND_ROOM_OPTIONS))
            {
                ConfirmCreateGame();
            }
        }

        void CreateNewRoom(string aName, uint size, Dictionary<string, object> matchAttributes)
        {
            BombersNetworkManager networkMgr = BombersNetworkManager.singleton as BombersNetworkManager;
            List<MatchInfoSnapshot> rooms = networkMgr.matches;
            bool roomExists = false;
            string roomName = aName;

            if (aName == "")
            {
                roomName = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().PlayerName + "'s Room";
            }

            if (rooms != null)
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].name == aName)
                    {
                        roomExists = true;
                    }
                }
            }
            if (roomExists)
            {
                m_dialogueDisplay.DisplayDialog("There's already a room named " + aName + "!");
                return;
            }

            int playerLevel = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetStats()[0].m_statValue;

            if (m_roomLevelRangeMin < 0)
            {
                m_roomLevelRangeMin = 0;
            }
            else if (m_roomLevelRangeMin > playerLevel)
            {
                m_roomLevelRangeMin = playerLevel;
            }

            if (m_roomLevelRangeMax > 50)
            {
                m_roomLevelRangeMax = 50;
            }

            if (m_roomLevelRangeMax < m_roomLevelRangeMin)
            {
                m_roomLevelRangeMax = m_roomLevelRangeMin;
            }

            if (size > 8)
            {
                size = 8;
            }
            else if (size < 2)
            {
                size = 2;
            }

            matchAttributes["minLevel"] = m_roomLevelRangeMin;
            matchAttributes["maxLevel"] = m_roomLevelRangeMax;
            matchAttributes["StartGameTime"] = 300;
            matchAttributes["IsPlaying"] = 0;
            matchAttributes["MapLayout"] = m_presetListSelection;
            matchAttributes["MapSize"] = m_sizeListSelection;

            BombersNetworkManager._BC.Client.EntityService.UpdateSingleton("gameName", "{\"gameName\": \"" + roomName + "\"}", null, -1, null, null, null);
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadStatistics();

            Dictionary<string, object> matchOptions = new Dictionary<string, object>();
            matchOptions.Add("gameTime", 300);
            matchOptions.Add("isPlaying", 0);
            matchOptions.Add("mapLayout", m_presetListSelection);
            matchOptions.Add("mapSize", m_sizeListSelection);
            matchOptions.Add("gameName", roomName);
            matchOptions.Add("maxPlayers", size);
            matchOptions.Add("lightPosition", 0);

            // clear all previous messages
            for (int i = 0; i < m_chatContent.transform.childCount; ++i)
            {
                Destroy(m_chatContent.transform.GetChild(i).gameObject);
            }

            switch (m_state)
            {
                default:
                case eMatchmakingState.GAME_STATE_NEW_ROOM_OPTIONS:
                    {
                        networkMgr.CreateLobby(matchOptions);
                        m_state = eMatchmakingState.GAME_STATE_CREATE_NEW_ROOM;
                    }
                    break;

                case eMatchmakingState.GAME_STATE_FIND_ROOM_OPTIONS:
                    {
                        networkMgr.FindLobby(matchOptions);
                        m_state = eMatchmakingState.GAME_STATE_JOIN_ROOM;
                    }
                    break;
            }
        }
    }
}