using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using BrainCloud;
using BrainCloudUNETExample.Connection;
using BrainCloud.JsonFx.Json;
using Gameframework;
using TMPro;

namespace BrainCloudUNETExample
{
    public class MainMenuState : BaseState
    {
        public static string SYSTEM_MESSAGE = "SYSTEM_MESSAGE";
        public static string STATE_NAME = "mainMenuState";

        [SerializeField]
        private PlayerRankIcon PlayerRankIcon = null;

        [SerializeField]
        private GameObject m_roomsScrollBar;

        [SerializeField]
        private GameObject ChatCellYou = null;
        [SerializeField]
        private GameObject ChatCellOther = null;
        [SerializeField]
        private GameObject ChatCellSystem = null;

        [SerializeField]
        private GameObject LobbyChatCellYou = null;
        [SerializeField]
        private GameObject LobbyChatCellOther = null;
        [SerializeField]
        private GameObject LobbyChatCellSystem = null;

        [SerializeField]
        private TextMeshProUGUI NoFriendsOnline = null;

        [SerializeField]
        private RectTransform FriendsScrollView = null;

        [SerializeField]
        private GameObject QuitButton = null;
        [SerializeField]
        private GameObject StoreButtonTop = null;
        [SerializeField]
        private GameObject StoreButtonBottom = null;
        [SerializeField]
        private GameObject QuitMenu = null;
        [SerializeField]
        private GameObject LeftButtonGroup = null;

        [SerializeField]
        private Button CustomGameButton = null;
        [SerializeField]
        private Button FindGameButton = null;
        [SerializeField]
        private Button QuickPlayButton = null;
        [SerializeField]
        private Button LeaderboardButton = null;
        [SerializeField]
        private Button AchievementButton = null;
        [SerializeField]
        private Button StoreButton = null;
        [SerializeField]
        private Button OptionsButton = null;
        [SerializeField]
        private Button FriendsButton = null;

        [SerializeField]
        private Image TextInputMaxIndicator = null;

        [SerializeField]
        private TMP_InputField ChatInputField = null;

        #region BaseState
        protected override void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            m_friendsListItem = new List<FriendsListItem>();
            GEventManager.StartListening(GFriendsManager.ON_FRIENDS_LIST_UPDATED, OnRefreshFriendsList);
            GEventManager.StartListening(GEventManager.ON_INVITED_FRIEND, OnInvitedFriend);
            GEventManager.StartListening(GEventManager.ON_REFUSED_INVITE_FRIEND, OnRefusedInviteFriend);
            GEventManager.StartListening(GEventManager.ON_RTT_ENABLED, OnEnableRTTSuccess);
            GEventManager.StartListening(GEventManager.ON_PLAYER_DATA_UPDATED, OnUpdateStats);

            m_statsPanelContentLeft = GameObject.Find("StatsPanel").transform.FindDeepChild("ContentLeft").gameObject;
            m_statsPanelContentRight = GameObject.Find("StatsPanel").transform.FindDeepChild("ContentRight").gameObject;
            m_joinFriendsPanel = GameObject.Find("JoinFriendsPanel");
            m_statText = GameObject.Find("StatText");
            m_statValue = GameObject.Find("StatValue");

            ChatInputField.onEndEdit.AddListener(delegate { OnEndEditHelper(); });

            BombersNetworkManager.Instance.ConnectRTT();

            GameObject playerName = GameObject.Find("PlayerName");
            m_inputField = playerName.GetComponent<TMP_InputField>();
            m_inputField.characterLimit = MAX_CHARACTERS_NAME;
            m_inputField.text = GPlayerMgr.Instance.PlayerData.PlayerName;
            m_inputField.interactable = false;
            PlayerRankIcon.UpdateIcon(GPlayerMgr.Instance.PlayerData.PlayerRank);
            BrainCloudStats.Instance.ReadStatistics();

            BaseNetworkBehavior.MSG_ENCODED = GConfigManager.GetIntValue("MSGENCODING");
            BaseNetworkBehavior.SEND_INTERVAL = GConfigManager.GetFloatValue("SEND_INTERVAL");
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();

            // should we push the connect screen
            if (!GCore.Wrapper.Client.Initialized)
            {
                GStateManager.Instance.PushSubState(ConnectingSubState.STATE_NAME);
            }

#if STEAMWORKS_ENABLED
            QuitButton.SetActive(true);
            StoreButtonTop.SetActive(false);
            StoreButtonBottom.SetActive(true);
#else
            QuitButton.SetActive(false);
            StoreButtonTop.SetActive(true);
            StoreButtonBottom.SetActive(false);
#endif

#if UNITY_WEBGL
            LeftButtonGroup.SetActive(false);
#endif

            GPlayerMgr.Instance.GetXpData();
        }

        protected override void OnResumeStateImpl(bool wasPaused)
        {
            base.OnResumeStateImpl(wasPaused);
            BaseNetworkBehavior.MSG_ENCODED = GConfigManager.GetIntValue("MSGENCODING");
            BaseNetworkBehavior.SEND_INTERVAL = GConfigManager.GetFloatValue("SEND_INTERVAL");
            GCore.Wrapper.RTTService.RegisterRTTPresenceCallback(OnPresenceCallback);
            GPlayerMgr.Instance.UpdateActivity(GPlayerMgr.LOCATION_MAIN_MENU, GPlayerMgr.STATUS_IDLE, "", "");
            OnRefreshFriendsList();
        }

        protected override void OnDestroy()
        {
            // stop listening to presence, once we go into gameplay
            if (!GCore.ApplicationIsQuitting)
            {
                GEventManager.StopListening(GFriendsManager.ON_FRIENDS_LIST_UPDATED, OnRefreshFriendsList);
                GEventManager.StopListening(GEventManager.ON_INVITED_FRIEND, OnInvitedFriend);
                GEventManager.StopListening(GEventManager.ON_REFUSED_INVITE_FRIEND, OnRefusedInviteFriend);
                GEventManager.StopListening(GEventManager.ON_RTT_ENABLED, OnEnableRTTSuccess);
                GEventManager.StopListening(GEventManager.ON_PLAYER_DATA_UPDATED, OnUpdateStats);
                (BombersNetworkManager.singleton as BombersNetworkManager).DisconnectGlobalChat();
            }
            ChatInputField.onEndEdit.RemoveListener(delegate { OnEndEditHelper(); });

            base.OnDestroy();
        }
        #endregion

        private void OnEnableRTTSuccess()
        {
            GCore.Wrapper.RTTService.RegisterRTTPresenceCallback(OnPresenceCallback);
            GCore.Wrapper.Client.PresenceService.RegisterListenersForFriends(platform, true, presenceSuccess);
            OnUpdateStats();
        }

        private void presenceSuccess(string in_data, object obj)
        {
            GFriendsManager.Instance.GetPresenceOfFriends(OnGetPresenceOfFriendsSuccess);
            GCore.Wrapper.Client.PresenceService.SetVisibility(true);
            GPlayerMgr.Instance.UpdateActivity(GPlayerMgr.LOCATION_MAIN_MENU, GPlayerMgr.STATUS_IDLE, "", "", true);
        }

        #region Public
        public void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                QuitMenu.SetActive(true);
            }
#endif
            // Currently joining a game, disable all buttons except chat.
            if (GStateManager.Instance.CurrentSubStateId == JoiningGameSubState.STATE_NAME || GStateManager.Instance.CurrentSubStateId == LobbySubState.STATE_NAME)
            {
                SetButtonsInteractable(false);
            }
            else
            {
                SetButtonsInteractable(true);
            }
        }

        public void QuitButtonAction()
        {
            QuitMenu.SetActive(true);
        }

        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE || STEAMWORKS_ENABLED
            Application.Quit();
#endif
        }

        public void QuitMenuCancel()
        {
            QuitMenu.SetActive(false);
        }

        public void EditName()
        {
            m_inputField.interactable = true;
            m_inputField.ActivateInputField();
            m_inputField.Select();
            GameObject.Find("PlayerName").GetComponent<Image>().enabled = true;
        }

        public void FinishEditName()
        {
            m_inputField.interactable = false;
            GameObject playerName = GameObject.Find("PlayerName");
            playerName.GetComponent<Image>().enabled = false;
            if (GPlayerMgr.Instance.IsUniversalIdAttached())
                ValidateName();
            else
                ConnectingSubState.PushConnectingSubState("You need a UniversalId for this, please create one now.", "Create");
        }

        public void RestoreName()
        {
            m_inputField.text = GPlayerMgr.Instance.PlayerData.PlayerName;
        }

        public void ShowAchievements()
        {
            //m_state = eMatchmakingState.GAME_STATE_SHOW_ACHIEVEMENTS;
            GStateManager.Instance.PushSubState(AchievementsSubState.STATE_NAME);
        }

        public void ShowLobby()
        {
            //m_state = eMatchmakingState.GAME_STATE_WAITING_FOR_PLAYERS;
            GStateManager.Instance.PushSubState(LobbySubState.STATE_NAME);

            BaseState state = GStateManager.Instance.FindSubState(CreateGameSubState.STATE_NAME);
            if (state != null) GStateManager.Instance.PopSubState(state.StateInfo);
        }

        private void OnEndEditHelper()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                GlobalChatEntered();
        }

        public void OnGlobalChatValueChanged(TMP_InputField in_field)
        {
            if (in_field.isFocused)
            {
                in_field.placeholder.enabled = false;
            }
            TextInputMaxIndicator.fillAmount = in_field.text.Length / (float)in_field.characterLimit;
        }

        public void GlobalChatEntered()
        {
            ChatInputField.text = ChatInputField.text.Replace("\n", "").Trim();
            bool resetEntry = true;
            if (ChatInputField.text.Length > 0)
            {
                Dictionary<string, object> jsonData = new Dictionary<string, object>();
                jsonData[BrainCloudConsts.JSON_LAST_CONNECTION_ID] = GCore.Wrapper.Client.RTTConnectionID;
                jsonData["timeSent"] = DateTime.UtcNow.ToLongTimeString();
                jsonData[BrainCloudConsts.JSON_RANK] = GPlayerMgr.Instance.PlayerData.PlayerRank;

                // TODO read this in correctly! 
                GCore.Wrapper.ChatService.PostChatMessage(GCore.Wrapper.Client.AppId + ":gl:main", ChatInputField.text,
                    JsonWriter.Serialize(jsonData));

#if UNITY_WEBGL || UNITY_STANDALONE || UNITY_EDITOR
                ChatInputField.text = "";
                resetEntry = false;
                StartCoroutine(delayedSelect(ChatInputField));
#endif
            }

            if (resetEntry)
            {
                ChatInputField.text = "";
                ChatInputField.placeholder.enabled = true;
            }
        }

        private IEnumerator delayedSelect(TMP_InputField in_field)
        {
            in_field.interactable = false;
            yield return YieldFactory.GetWaitForSeconds(0.15f);
            in_field.interactable = true;
            in_field.Select();
        }

        public void AddLobbyChatMessage(Dictionary<string, object> in_jsonMessage)
        {
            BaseState lobbyState = GStateManager.Instance.FindSubState(LobbySubState.STATE_NAME);
            if (lobbyState != null && m_lobbyChatContent == null)
            {
                m_lobbyChatContent = lobbyState.transform.FindDeepChild("lobbyChatContent").gameObject;
            }
            if (m_lobbyChatContent == null) return;

            Transform contentTransform = m_lobbyChatContent.transform;
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
                string profileId = fromData["id"] as string;
                int rank = (int)signalData["rank"];

                tempObj = Instantiate(profileId == GCore.Wrapper.Client.ProfileId ? LobbyChatCellYou :                       // is you!
                                       profileId != ChatCell.SYSTEM_MESSAGE ? LobbyChatCellOther : LobbyChatCellSystem,      // A non system meessage?
                                       Vector3.zero, Quaternion.identity, contentTransform);

                tempCell = tempObj.GetComponent<ChatCell>();
                tempCell.Init(fromData["name"] as string, signalData["message"] as string, profileId, fromData.ContainsKey("pic") ? fromData["pic"] as string : null, "", rank);

                GEventManager.TriggerEvent("NEW_LOBBY_CHAT");
            }
        }

        public void AddGlobalChatMessage(Dictionary<string, object> in_jsonMessage, bool bUpdateOnlyLobby = false)
        {
            if (m_globaChatContent == null) m_globaChatContent = GameObject.Find("globalChatContent");

            BaseState lobbyState = GStateManager.Instance.FindSubState(LobbySubState.STATE_NAME);
            if (lobbyState != null && m_lobbyChatContent == null)
            {
                m_globaLobbyChatContent = lobbyState.transform.FindDeepChild("lobbyGlobalChatContent").gameObject;
            }

            Transform contentTransform = m_globaChatContent.transform;
            Transform contentTransform2 = m_globaLobbyChatContent != null ? m_globaLobbyChatContent.transform : null;
            if (!bUpdateOnlyLobby) addChatMessageToContent(in_jsonMessage, contentTransform);
            if (contentTransform2 != null) addChatMessageToContent(in_jsonMessage, contentTransform2, true);
        }

        private void addChatMessageToContent(Dictionary<string, object> in_jsonMessage, Transform contentTransform, bool in_lobbyView = false)
        {
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

                string profileId = fromData["id"] as string;
                string lastConnectionId = richData != null ? richData.ContainsKey("lastConnectionId") ? richData["lastConnectionId"] as string : "" : "";
                int rank = richData != null ? richData.ContainsKey("rank") ? (int)richData["rank"] : 0 : 0;

                tempObj = Instantiate(profileId == GCore.Wrapper.Client.ProfileId ? in_lobbyView ? LobbyChatCellYou : ChatCellYou :       // is you!
                                      profileId != ChatCell.SYSTEM_MESSAGE ? in_lobbyView ? LobbyChatCellOther : ChatCellOther : in_lobbyView ? LobbyChatCellSystem : ChatCellSystem,           // A non system meessage?   // haven't done these yet, probably from presence we will do this [TODO]
                                      Vector3.zero, Quaternion.identity, contentTransform);
                tempCell = tempObj.GetComponent<ChatCell>();

                tempCell.Init(fromData["name"] as string, contentData["text"] as string, profileId, fromData.ContainsKey("pic") ? fromData["pic"] as string : null,
                    lastConnectionId,
                    rank,
                    Convert.ToUInt64(jsonData["msgId"]),
                    (int)jsonData["ver"], in_jsonMessage, in_lobbyView);

                if (in_lobbyView)
                {
                    GEventManager.TriggerEvent("NEW_GLOBAL_CHAT");
                }
            }
        }

        public void OnChatMessageDeleted(Dictionary<string, object> in_jsonMessage)
        {
            if (m_globaChatContent == null) m_globaChatContent = GameObject.Find("globalChatContent");
            BaseState lobbyState = GStateManager.Instance.FindSubState(LobbySubState.STATE_NAME);
            if (lobbyState != null && m_lobbyChatContent == null)
            {
                m_globaLobbyChatContent = lobbyState.transform.FindDeepChild("lobbyGlobalChatContent").gameObject;
            }

            Transform contentTransform = m_globaChatContent.transform;
            Transform contentTransform2 = m_globaLobbyChatContent != null ? m_globaLobbyChatContent.transform : null;
            deleteChatMessage(in_jsonMessage, contentTransform);

            addChatMessageToContent(in_jsonMessage, contentTransform);
            if (contentTransform2 != null) deleteChatMessage(in_jsonMessage, contentTransform2);
        }

        private void deleteChatMessage(Dictionary<string, object> in_jsonMessage, Transform contentTransform)
        {
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
            if (m_globaChatContent == null) m_globaChatContent = GameObject.Find("globalChatContent");

            Transform contentTransform = m_globaChatContent.transform;
            Transform contentTransform2 = m_globaLobbyChatContent != null ? m_globaLobbyChatContent.transform : null;
            updateChatMessage(in_jsonMessage, contentTransform);
            if (contentTransform2 != null) updateChatMessage(in_jsonMessage, contentTransform2);
        }

        private void updateChatMessage(Dictionary<string, object> in_jsonMessage, Transform contentTransform)
        {
            lock (contentTransform)
            {
                Dictionary<string, object> jsonData = in_jsonMessage.ContainsKey("data") ?
                                                                    (Dictionary<string, object>)in_jsonMessage["data"] : in_jsonMessage;

                Dictionary<string, object> fromData = (Dictionary<string, object>)jsonData["from"];
                Dictionary<string, object> contentData = (Dictionary<string, object>)jsonData["content"];
                Dictionary<string, object> richData = contentData.ContainsKey("rich") ? (Dictionary<string, object>)contentData["rich"] : null;

                string lastConnectionId = richData != null ? richData.ContainsKey("lastConnectionId") ? richData["lastConnectionId"] as string : "" : "";
                int rank = richData != null ? richData.ContainsKey("rank") ? (int)richData["rank"] : 0 : 0;


                ChatCell cell;
                for (int i = 0; i < contentTransform.childCount; ++i)
                {
                    cell = contentTransform.GetChild(i).GetComponent<ChatCell>();
                    if (Convert.ToUInt64(jsonData["msgId"]) == cell.MessageId)
                    {
                        cell.transform.SetParent(null);
                        cell.Init(fromData["name"] as string, contentData["text"] as string,
                            fromData["id"] as string, fromData.ContainsKey("pic") ? fromData["pic"] as string : null,
                            lastConnectionId,
                            rank,
                            Convert.ToUInt64(jsonData["msgId"]), (int)jsonData["ver"], in_jsonMessage);

                        cell.transform.SetParent(contentTransform);
                        cell.transform.SetSiblingIndex(i);
                        break;
                    }
                }
            }
        }

        private string m_tempCacheProfileId = "";
        private string m_tempCacheUserName = "";
        public void DisplayJoinLobbyOffer(string in_profileId, string in_userName)
        {
            m_tempCacheProfileId = in_profileId;
            m_tempCacheUserName = in_userName;
            GStateManager.Instance.PushSubState(ConfirmJoinLobbySubState.STATE_NAME);
            GStateManager.Instance.OnInitializeDelegate += onConfirmJoinLobbyLateInit;
        }

        public void ShowLeaderboard()
        {
            GStateManager.Instance.PushSubState(LeaderboardSubState.STATE_NAME);
        }

        public void QuitToLogin()
        {
            GCore.Wrapper.Client.PlayerStateService.Logout();
            GCore.Wrapper.Client.AuthenticationService.ClearSavedProfileID();
            GStateManager.Instance.PushSubState(ConnectingSubState.STATE_NAME);
        }

        public void CreateGame()
        {
            GStateManager.Instance.PushSubState(CreateGameSubState.STATE_NAME);
        }

        public void FindLobby()
        {
            GStateManager.Instance.PushSubState(CreateGameSubState.STATE_NAME);
            GStateManager.Instance.OnInitializeDelegate += onFindLobbyLateInit;
        }

        public void OpenStore()
        {
            GStateManager.Instance.PushSubState(StoreSubState.STATE_NAME);
        }

        public void OpenFriends()
        {
            GStateManager.Instance.PushSubState(FriendsSubState.STATE_NAME);
        }

        public void ToggleStats()
        {
            OnUpdateStats();
            m_joinFriendsPanel.SetActive(!m_joinFriendsPanel.activeInHierarchy);
        }

        public void OpenOptions()
        {
            GStateManager.Instance.PushSubState(MainOptionsSubState.STATE_NAME);
        }

        public void QuickPlay()
        {
            GStateManager.Instance.PushSubState(CreateGameSubState.STATE_NAME, false, false);
            GStateManager.Instance.OnInitializeDelegate += onQuickPlayInit;
        }
        #endregion

        #region Private
        private void onFindLobbyLateInit(BaseState in_state)
        {
            CreateGameSubState createGameState = in_state as CreateGameSubState;
            if (createGameState != null)
            {
                GStateManager.Instance.OnInitializeDelegate -= onFindLobbyLateInit;
                createGameState.LateInit(false);
            }
        }

        private void onConfirmJoinLobbyLateInit(BaseState in_state)
        {
            ConfirmJoinLobbySubState subState = in_state as ConfirmJoinLobbySubState;
            if (subState != null)
            {
                GStateManager.Instance.OnInitializeDelegate -= onConfirmJoinLobbyLateInit;
                subState.LateInit(m_tempCacheProfileId, m_tempCacheUserName);

                m_tempCacheProfileId = "";
                m_tempCacheUserName = "";
            }
        }

        private void onQuickPlayInit(BaseState in_state)
        {
            CreateGameSubState createGameState = in_state as CreateGameSubState;
            if (createGameState != null)
            {
                GStateManager.Instance.OnInitializeDelegate -= onQuickPlayInit;
                createGameState.QuickPlay();
            }
        }

        private Image m_statsImage = null;
        private void OnUpdateStats()
        {
            if (m_statsPanelContentLeft != null &&
                BrainCloudStats.Instance.m_playerLevelTitles != null &&
                BrainCloudStats.Instance.m_playerLevelTitles.Length > 0)
            {
                // clear all stats
                for (int i = 0; i < m_statsPanelContentLeft.transform.childCount; ++i)
                {
                    Destroy(m_statsPanelContentLeft.transform.GetChild(i).gameObject);
                    Destroy(m_statsPanelContentRight.transform.GetChild(i).gameObject);
                }

                List<BrainCloudStats.Stat> playerStats = BrainCloudStats.Instance.GetStats();
                XPData xpData = GPlayerMgr.Instance.PlayerData.PlayerXPData;
                int currentLevel = xpData.CurrentLevel;

                if (GPlayerMgr.Instance.PlayerData.PlayerRank != currentLevel)
                {
                    // Update the PlayerSummaryData only if the Player's rank has changed
                    GPlayerMgr.Instance.PlayerData.PlayerRank = currentLevel;
                    GPlayerMgr.Instance.UpdatePlayerSummaryData();
                    PlayerRankIcon.UpdateIcon(GPlayerMgr.Instance.PlayerData.PlayerRank);
                }

                string rank = String.Empty;

                if (BrainCloudStats.Instance.m_playerLevelTitles.Length > 0)
                {
                    rank = BrainCloudStats.Instance.m_playerLevelTitles[0] + "(" + GPlayerMgr.Instance.PlayerData.PlayerXPData.CurrentLevel + ")";

                    if (currentLevel > 0 && currentLevel < BrainCloudStats.Instance.m_playerLevelTitles.Length)
                    {
                        rank = BrainCloudStats.Instance.m_playerLevelTitles[currentLevel - 1] + " (" + currentLevel + ")";
                    }
                    // over max
                    else if (currentLevel > 0)
                    {
                        rank = BrainCloudStats.Instance.m_playerLevelTitles[BrainCloudStats.Instance.m_playerLevelTitles.Length - 1] + " (" + currentLevel + ")";
                    }
                }

                if (m_statsImage == null)
                    m_statsImage = m_statsPanelContentLeft.transform.parent.parent.parent.FindDeepChild("XpBar").GetComponent<Image>();

                m_statsImage.fillAmount = Mathf.InverseLerp(xpData.PrevThreshold, xpData.NextThreshold, xpData.ExperiencePoints);

                TextMeshProUGUI tempText = null;
                for (int i = 2; i < playerStats.Count; ++i)
                {
                    tempText = Instantiate(m_statText, m_statsPanelContentLeft.transform).GetComponent<TextMeshProUGUI>();
                    tempText.alignment = TextAlignmentOptions.MidlineLeft;
                    tempText.text = "  " + playerStats[i].Name;
                    tempText = Instantiate(m_statValue, m_statsPanelContentRight.transform).GetComponent<TextMeshProUGUI>();
                    tempText.text = HudHelper.ToGUIString(playerStats[i].Value);
                }
                GameObject.Find("RankText").GetComponent<TextMeshProUGUI>().text = rank;
            }
        }

        private void ValidateName()
        {
            m_inputField.text = m_inputField.text.Trim();
            if (m_inputField.text.Length < MIN_CHARACTERS_NAME)
            {
                HudHelper.DisplayMessageDialog("DISALLOWED NAME", "THE NAME MUST BE AT LEAST " + MIN_CHARACTERS_NAME + " CHARACTERS LONG.", "OK");
                m_inputField.text = GPlayerMgr.Instance.PlayerData.PlayerName;
                return;
            }
            // Only update the username if it has changed.
            if (!m_inputField.text.Equals(GPlayerMgr.Instance.PlayerData.PlayerName))
                UpdateUserName(m_inputField.text);
        }

        private void UpdateUserName(string in_userName)
        {
            GStateManager.Instance.EnableLoadingSpinner(true);

            Dictionary<string, object> data = new Dictionary<string, object>();
            data[BrainCloudConsts.JSON_EXTERNAL_ID] = in_userName;
            GCore.Wrapper.ScriptService.RunScript("UpdateUniversalPlayerName", JsonWriter.Serialize(data), OnUpdateUserNameSuccess, OnUpdateUserNameError, data);
        }

        private void OnUpdateUserNameSuccess(string in_stringData, object in_obj)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            GDebug.Log(string.Format("Success | {0}", in_stringData));

            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_stringData);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            Dictionary<string, object> jsonResponse = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_RESPONSE];

            if ((int)jsonResponse["status"] == 200)
            {
                if (jsonResponse.ContainsKey("reason_code") && (int)jsonResponse["reason_code"] == ReasonCodes.NAME_CONTAINS_PROFANITY)
                    OnUpdateUserNameError((int)jsonResponse["status"], (int)jsonResponse["reason_code"], "", null);
                else
                    GPlayerMgr.Instance.PlayerData.PlayerName = m_inputField.text;
            }
            else if ((int)jsonResponse["status"] == 400 || (int)jsonResponse["status"] == 500)
            {
                OnUpdateUserNameError((int)jsonResponse["status"], (int)jsonResponse["reason_code"], (string)jsonResponse["status_message"], null);
            }
        }

        private void OnUpdateUserNameError(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            GDebug.Log(string.Format("Failed | {0}  {1}  {2}", statusCode, reasonCode, in_stringData));

            m_inputField.text = GPlayerMgr.Instance.PlayerData.PlayerName;
            switch (reasonCode)
            {
                case ReasonCodes.NAME_CONTAINS_PROFANITY:
                    HudHelper.DisplayMessageDialog("DISALLOWED NAME", "THIS NAME IS CONSIDERED INAPPROPRIATE. PLEASE ENTER ANOTHER ONE.", "OK");
                    break;
                case ReasonCodes.NEW_CREDENTIAL_IN_USE:
                    HudHelper.DisplayMessageDialog("WARNING", "THIS NAME IS ALREADY TAKEN, PLEASE TRY ANOTHER ONE.", "OK");
                    break;
                case ReasonCodes.WEBPURIFY_NOT_CONFIGURED:
                    HudHelper.DisplayMessageDialog("WEBPURIFY ERROR", "WEBPURIFY NOT CONFIGURED, PLEASE TRY AGAIN.", "OK");
                    break;
                case ReasonCodes.WEBPURIFY_EXCEPTION:
                    HudHelper.DisplayMessageDialog("WEBPURIFY ERROR", "WEBPURIFY EXCEPTION, PLEASE TRY AGAIN.", "OK");
                    break;
                case ReasonCodes.WEBPURIFY_FAILURE:
                    HudHelper.DisplayMessageDialog("WEBPURIFY ERROR", "WEBPURIFY FAILURE, PLEASE TRY AGAIN.", "OK");
                    break;
                case ReasonCodes.WEBPURIFY_NOT_ENABLED:
                    HudHelper.DisplayMessageDialog("WEBPURIFY ERROR", "WEBPURIFY IS NOT ENABLED", "OK");
                    break;
                case ReasonCodes.MISSING_IDENTITY_ERROR:
                    HudHelper.DisplayMessageDialog("ERROR", "MISSING IDENTITY ERROR", "OK");
                    break;
            }
        }
        public void OnPresenceCallback(string in_message)
        {
            PresenceData presenceData = new PresenceData();
            GFriendsManager.Instance.ParsePresenceCallback(in_message, ref presenceData);
            if (presenceData.ProfileId.Length > 0)
            {
                // Refresh our friend's online status
                UpdateFriendOnlineStatus(m_friendsListItem, presenceData);
            }
        }

        private void UpdateFriendOnlineStatus(List<FriendsListItem> in_listItem, PresenceData in_presenceData)
        {
            int nbrOnline = 0;
            for (int i = 0; i < in_listItem.Count; ++i)
            {
                if (in_listItem[i].ProfileId.Equals(in_presenceData.ProfileId))
                {
                    in_listItem[i].UpdateOnlineStatus(in_presenceData);
                    in_listItem[i].RefreshOnlineVisibility();
                }
                if (in_listItem[i].ItemData.Presence.IsOnline)
                    nbrOnline++;
            }
            NoFriendsOnline.gameObject.SetActive(nbrOnline == 0);
        }

        private void OnRefreshFriendsList()
        {
            OnGetPresenceOfFriendsSuccess("", null);
        }

        private FriendsListItem CreateFriendsListItem(Transform in_parent = null)
        {
            FriendsListItem toReturn = null;
            toReturn = (GEntityFactory.Instance.CreateResourceAtPath("Prefabs/UI/friendCell", in_parent.transform)).GetComponent<FriendsListItem>();
            toReturn.transform.SetParent(in_parent);
            toReturn.transform.localScale = Vector3.one;
            return toReturn;
        }

        private void RemoveAllCellsInView(List<FriendsListItem> in_friendsListItem)
        {
            FriendsListItem item;
            for (int i = 0; i < in_friendsListItem.Count; ++i)
            {
                item = in_friendsListItem[i];
                Destroy(item.gameObject);
            }
            in_friendsListItem.Clear();
        }

        private void PopulateFriendsScrollView(List<PlayerData> in_friendsItems, List<FriendsListItem> in_friendsListItem, RectTransform in_scrollView, bool in_add, bool in_remove)
        {
            RemoveAllCellsInView(in_friendsListItem);
            NoFriendsOnline.gameObject.SetActive(true);
            if (in_friendsItems.Count == 0)
            {
                return;
            }

            if (in_scrollView != null)
            {
                List<PlayerData> activeListData = in_friendsItems;
                for (int i = 0; i < activeListData.Count; ++i)
                {
                    FriendsListItem newItem = CreateFriendsListItem(in_scrollView);
                    newItem.Init(activeListData[i], in_add, in_remove);
                    newItem.transform.localPosition = new Vector3(0.0f, 0.0f);
                    newItem.RefreshOnlineVisibility();
                    if (newItem.ItemData.Presence.IsOnline)
                        NoFriendsOnline.gameObject.SetActive(false);

                    in_friendsListItem.Add(newItem);
                }
            }
        }

        private void OnGetPresenceOfFriendsSuccess(string in_stringData, object in_obj)
        {
            m_friendsItems = GFriendsManager.Instance.Friends;
            PopulateFriendsScrollView(m_friendsItems, m_friendsListItem, FriendsScrollView, false, false);
        }

        private void CreateSystemMessage(string in_message)
        {
            // put feedback into the global chat message
            Dictionary<string, object> systemMessage = new Dictionary<string, object>();

            Dictionary<string, object> from = new Dictionary<string, object>();
            from["id"] = SYSTEM_MESSAGE;
            from["name"] = SYSTEM_MESSAGE;
            from["pic"] = null;

            Dictionary<string, object> data = new Dictionary<string, object>();

            data["ver"] = 0;
            data["msgId"] = "123123123123";

            Dictionary<string, object> content = new Dictionary<string, object>();
            content["text"] = in_message;

            data["from"] = from;
            data["content"] = content;

            systemMessage["data"] = data;

            AddGlobalChatMessage(systemMessage);
        }

        private void OnInvitedFriend()
        {
            CreateSystemMessage("Sending Join Request to " + GFriendsManager.Instance.OriginalUserName);
        }

        private void OnRefusedInviteFriend()
        {
            CreateSystemMessage(GFriendsManager.Instance.OriginalUserName + " declined Join Request");
        }

        private void SetButtonsInteractable(bool in_active)
        {
            CustomGameButton.interactable = in_active;
            FindGameButton.interactable = in_active;
            QuickPlayButton.interactable = in_active;
            LeaderboardButton.interactable = in_active;
            AchievementButton.interactable = in_active;
            StoreButton.interactable = in_active;
            OptionsButton.interactable = in_active;
            FriendsButton.interactable = in_active;
        }

        private string platform = "";   // denotes All
        private TMP_InputField m_inputField = null;
        private int MIN_CHARACTERS_NAME = 3;
        private int MAX_CHARACTERS_NAME = 16;

        private GameObject m_statText = null;
        private GameObject m_statValue = null;
        private GameObject m_statsPanelContentLeft = null;
        private GameObject m_statsPanelContentRight = null;
        private GameObject m_joinFriendsPanel = null;
        private GameObject m_globaChatContent = null;
        private GameObject m_globaLobbyChatContent = null;
        private GameObject m_lobbyChatContent = null;

        private List<FriendsListItem> m_friendsListItem = null;
        private List<PlayerData> m_friendsItems = null;

        //private DialogDisplay m_dialogueDisplay;
        #endregion
    }
}
