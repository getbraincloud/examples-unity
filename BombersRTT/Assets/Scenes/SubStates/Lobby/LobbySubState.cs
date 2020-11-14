using Gameframework;
using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BrainCloudUNETExample.Connection;

namespace BrainCloudUNETExample
{
    public class LobbySubState : BaseSubState
    {
        public static string STATE_NAME = "lobby";

        [SerializeField]
        private RectTransform TeamGreenScrollView = null;

        [SerializeField]
        private RectTransform TeamRedScrollView = null;

        [SerializeField]
        private InputField LocalChatInputField = null;
        [SerializeField]
        private InputField GlobalChatInputField = null;

        #region BaseState
        protected override void Start()
        {
            m_initialized = false;

            m_teamGreenListItem = new List<FriendsListItem>();
            m_teamRedListItem = new List<FriendsListItem>();
            m_teamGreenItems = new List<PlayerData>();
            m_teamRedItems = new List<PlayerData>();

            m_currentMemberCount = 0;
            m_gameName = transform.FindDeepChild("GameName").transform.Find("Text").GetComponent<Text>();
            m_waitingForPlayers = transform.FindDeepChild("Waiting...").GetComponent<Text>();
            m_gameQuitButton = transform.FindDeepChild("ButtonQuitGame").gameObject;
            m_gameStartButton = transform.FindDeepChild("ButtonStartGame").gameObject;
            m_chatGroupLobby = transform.FindDeepChild("ChatGroupLocal").gameObject;
            m_chatGroupGlobal = transform.FindDeepChild("ChatGroupGlobal").gameObject;

            m_optionsAndGameGroup = transform.FindDeepChild("OptionsAndGameGroup").gameObject;
            m_gameButtons = transform.FindDeepChild("GameButtons").gameObject;
            m_panelLeft = transform.FindDeepChild("PanelLeft").GetComponent<Canvas>();

            m_titleField = transform.FindDeepChild("Title Field").gameObject;
            m_editButton = m_titleField.transform.Find("EditButton").gameObject;
            m_lobbyGameOptionsHost = m_optionsAndGameGroup.transform.Find("lobbyGameOptionsHost").gameObject;
            m_lobbyGameOptionsTester = m_optionsAndGameGroup.transform.Find("lobbyGameOptionsTester").gameObject;
            m_protocolDropdown = m_lobbyGameOptionsTester.transform.Find("dropdownButton1").GetComponent<Dropdown>();
            m_compressionDropdown = m_lobbyGameOptionsTester.transform.Find("dropdownButton2").GetComponent<Dropdown>();

            SetupLobbyDisplaySettings();
            SetupTesterSettings();

            m_changeTeamButton = transform.FindDeepChild("ButtonChangeTeam").gameObject;

            GameObject tabGlobal = transform.FindDeepChild("TabGlobal").gameObject;
            GameObject tabLocal = transform.FindDeepChild("TabLocal").gameObject;

            m_lobbyChatNotification = tabLocal.transform.FindDeepChild("notificationBadge").gameObject;
            m_globalChatNotification = tabGlobal.transform.FindDeepChild("notificationBadge").gameObject;

            LocalChatInputField.onEndEdit.AddListener(delegate { OnEndEditHelperLocal(); });
            GlobalChatInputField.onEndEdit.AddListener(delegate { OnEndEditHelperGlobal(); });

            GEventManager.StartListening("NEW_GLOBAL_CHAT", onNewGlobalChat);
            GEventManager.StartListening("NEW_LOBBY_CHAT", onNewLobbyChat);

            GPlayerMgr.Instance.UpdateActivity(GPlayerMgr.LOCATION_LOBBY, GPlayerMgr.STATUS_IDLE, BombersNetworkManager.LobbyInfo.LobbyId, BombersNetworkManager.LobbyInfo.LobbyType);
            m_initialized = true;

            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();

            populateGlobalChatWithExistingMessages();

            // start with lobby chat
            DisplayGlobalChat(false);
        }

        protected override void OnDestroy()
        {
            LocalChatInputField.onEndEdit.RemoveListener(delegate { OnEndEditHelperLocal(); });
            GlobalChatInputField.onEndEdit.RemoveListener(delegate { OnEndEditHelperGlobal(); });

            base.OnDestroy();
        }

        private void SetupLobbyDisplaySettings()
        {
            m_presetDropDownButton = m_lobbyGameOptionsHost.transform.Find("dropdownButton1").GetComponent<Dropdown>();
            m_sizeDropDownButton = m_lobbyGameOptionsHost.transform.Find("dropdownButton2").GetComponent<Dropdown>();
            m_gameDurationDropDownButton = m_lobbyGameOptionsHost.transform.Find("dropdownButton3").GetComponent<Dropdown>();

            m_mapPresets = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_presets;
            m_mapSizes = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_mapSizes;
            m_gameDurations = GameObject.Find("MapPresets").GetComponent<MapPresets>().GameDurations;

            m_inputField = GameObject.Find("GameName").GetComponent<InputField>();
            m_inputField.characterLimit = GPlayerMgr.MAX_CHARACTERS_GAME_NAME;
            m_inputField.interactable = false;

            m_editButton.SetActive(GCore.Wrapper.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId);
            m_lobbyGameOptionsHost.SetActive(true);

            List<string> items = new List<string>();
            for (int i = 0; i < m_mapPresets.Count; i++)
            {
                items.Add(m_mapPresets[i].m_name);
            }
            m_presetDropDownButton.ClearOptions();
            m_presetDropDownButton.AddOptions(items);
            items.Clear();

            for (int i = 0; i < m_mapSizes.Count; i++)
            {
                items.Add(m_mapSizes[i].m_name);
            }
            m_sizeDropDownButton.ClearOptions();
            m_sizeDropDownButton.AddOptions(items);
            items.Clear();

            for (int i = 0; i < m_gameDurations.Count; i++)
            {
                items.Add(m_gameDurations[i].Name);
            }
            m_gameDurationDropDownButton.ClearOptions();
            m_gameDurationDropDownButton.AddOptions(items);

            if (GCore.Wrapper.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId)
            {
                Dictionary<string, object> matchOptions = BombersNetworkManager.s_matchOptions;
                m_gameDurationListSelection = (int)matchOptions["gameTimeSel"];
                m_layoutListSelection = (int)matchOptions["mapLayout"];
                m_sizeListSelection = (int)matchOptions["mapSize"];
                m_initialLayoutListSelection = m_layoutListSelection;
                m_initialSizeListSelection = m_sizeListSelection;
                m_initialGameDurationListSelection = m_gameDurationListSelection;
            }

            m_inputField.text = BombersNetworkManager.LobbyInfo.Settings["gameName"] as string;
            m_initialGameName = m_inputField.text;

            OnNewRoomWindow();
        }

        private void SetupTesterSettings()
        {
            List<string> items = new List<string>();
            items.Clear();
            items.Add("WEBSOCKET");
#if !UNITY_WEBGL
            items.Add("TCP");
            items.Add("UDP");
#endif
            m_protocolDropdown.ClearOptions();
            m_protocolDropdown.AddOptions(items);

            items.Clear();
            items.Add("Json string");
            items.Add("KeyValuePair string");
            items.Add("DataStream byte[]");

            // Only the host can change the compression settings
            m_compressionDropdown.interactable = (GCore.Wrapper.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId);

            m_compressionDropdown.ClearOptions();
            m_compressionDropdown.AddOptions(items);

            m_protocolListSelection = GConfigManager.GetIntValue("RSConnectionType");
#if UNITY_WEBGL
            m_protocolDropdown.value = (int)RelayConnectionType.WEBSOCKET;
#else
            m_protocolDropdown.value = m_protocolListSelection;
#endif
            m_compressionDropdown.value = m_compressionListSelection;
            m_lobbyGameOptionsTester.SetActive(GPlayerMgr.Instance.PlayerData.IsTester);
        }

        private void onNewGlobalChat()
        {
            if (!m_chatGroupGlobal.activeInHierarchy) m_globalChatNotification.SetActive(true);
        }
        private void onNewLobbyChat()
        {
            if (!m_chatGroupLobby.activeInHierarchy) m_lobbyChatNotification.SetActive(true);
        }

        void Update()
        {
            if (!m_panelLeft.enabled)
                OnWaitingForPlayersWindow();

            // Deselect dropdowns after a mouse click 
            if (Input.GetMouseButtonUp(0))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        override public void ExitSubState()
        {
            GEventManager.StopListening("NEW_GLOBAL_CHAT", onNewGlobalChat);
            GEventManager.StopListening("NEW_LOBBY_CHAT", onNewLobbyChat);

            BombersNetworkManager.Instance.LeaveLobby();

            base.ExitSubState();
        }
        #endregion

        #region Public
        private void OnWaitingForPlayersWindow()
        {
            if (BombersNetworkManager.LobbyInfo != null && BombersNetworkManager.LobbyInfo.Members != null)
            {
                List<LobbyMemberInfo> greenPlayers = new List<LobbyMemberInfo>();
                List<LobbyMemberInfo> redPlayers = new List<LobbyMemberInfo>();
                foreach (LobbyMemberInfo member in BombersNetworkManager.LobbyInfo.Members)
                {
                    if (member.Team == "green")
                    {
                        greenPlayers.Add(member);
                    }
                    else
                    {
                        redPlayers.Add(member);
                    }

                    if (member.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId &&
                        member.IsReady)
                    {
                        setLaunchingDisplay();
                    }
                }

                if (greenPlayers.Count != m_teamGreenCount)
                {
                    m_teamGreenCount = greenPlayers.Count;
                    m_teamGreenItems.Clear();
                    for (int i = 0; i < greenPlayers.Count; i++)
                    {
                        PlayerData playerData = new PlayerData();
                        playerData.PlayerName = greenPlayers[i].Name;
                        playerData.ProfileId = greenPlayers[i].ProfileId;
                        playerData.PlayerPictureUrl = greenPlayers[i].PictureURL;
                        m_teamGreenItems.Add(playerData);
                    }
                    PopulateFriendsScrollView(m_teamGreenItems, m_teamGreenListItem, TeamGreenScrollView, false, false);
                }

                if (redPlayers.Count != m_teamRedCount)
                {
                    m_teamRedCount = redPlayers.Count;
                    m_teamRedItems.Clear();
                    for (int i = 0; i < redPlayers.Count; i++)
                    {
                        PlayerData playerData = new PlayerData();
                        playerData.PlayerName = redPlayers[i].Name;
                        playerData.ProfileId = redPlayers[i].ProfileId;
                        playerData.PlayerPictureUrl = redPlayers[i].PictureURL;
                        m_teamRedItems.Add(playerData);
                    }
                    PopulateFriendsScrollView(m_teamRedItems, m_teamRedListItem, TeamRedScrollView, false, false);
                }

                float halfMax = Mathf.Floor((int)BombersNetworkManager.LobbyInfo.Settings["maxPlayers"] / 2.0f);
                GameObject green = GameObject.Find("GreenPlayers");
                if (green != null) green.GetComponent<Text>().text = greenPlayers.Count + "/" + halfMax;

                GameObject red = GameObject.Find("RedPlayers");
                if (red != null) red.GetComponent<Text>().text = redPlayers.Count + "/" + halfMax;

                if (GStateManager.Instance.CurrentSubStateId != JoiningGameSubState.STATE_NAME)
                {
                    bool isHost = GCore.Wrapper.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId;
                    m_gameStartButton.SetActive(isHost);
                    m_compressionDropdown.interactable = isHost;
                    m_editButton.SetActive(isHost);
                    m_presetDropDownButton.interactable = isHost;
                    m_sizeDropDownButton.interactable = isHost;
                    m_gameDurationDropDownButton.interactable = isHost;

                    if (BombersNetworkManager.s_matchOptions == null)
                    {
                        BombersNetworkManager.s_matchOptions = BombersNetworkManager.LobbyInfo.Settings;
                    }
                }

                if (BombersNetworkManager.LobbyInfo.Members.Count > 1 && BombersNetworkManager.LobbyInfo.Members.Count != m_currentMemberCount)
                {
                    // New member just arrived, update their game settings
                    UpdateAllSettings();
                }
                m_currentMemberCount = BombersNetworkManager.LobbyInfo.Members.Count;
            }
            else
            {
                GStateManager.Instance.PopSubState(_stateInfo);
                BombersNetworkManager.Instance.LeaveLobby();
                HudHelper.DisplayMessageDialog("ERROR", "THERE WAS A CONNECTION ERROR.  PLEASE TRY AGAIN SOON.", "OK");
            }
        }

        public void EditName()
        {
            m_inputField.DeactivateInputField();
            m_inputField.interactable = true;
            m_inputField.ActivateInputField();
        }

        public void FinishEditName()
        {
            m_inputField.DeactivateInputField();

            SendSignal("gameName", m_inputField.text);
            SetupOwnerLobbySettings();
        }

        public void ChangeTeam()
        {
            GCore.Wrapper.LobbyService.SwitchTeam(BombersNetworkManager.LobbyInfo.LobbyId, BombersNetworkManager.LobbyInfo.GetOppositeTeamCodeWithProfileId(GCore.Wrapper.Client.ProfileId));
        }

        public void StartGame()
        {
            m_inputField.text = m_inputField.text.Trim();
            GPlayerMgr.Instance.ValidateString(m_inputField.text, OnValidateStringSuccess, OnValidateStringError);
        }

        public void DisplayGlobalChat(bool in_value)
        {
            StopCoroutine("delayedDisplayGlobalChat");
            StartCoroutine(delayedDisplayGlobalChat(in_value));
        }

        public void SendLobbyChatSignal()
        {
            bool resetEntry = true;
            if (LocalChatInputField.text != "")
            {
                Dictionary<string, object> jsonData = new Dictionary<string, object>();
                jsonData["message"] = LocalChatInputField.text;
                jsonData["rank"] = GPlayerMgr.Instance.PlayerData.PlayerRank;

                GCore.Wrapper.LobbyService.SendSignal(BombersNetworkManager.LobbyInfo.LobbyId, jsonData);
#if UNITY_WEBGL || UNITY_STANDALONE || UNITY_EDITOR
                LocalChatInputField.text = "";
                resetEntry = false;
                if (GStateManager.Instance.CurrentSubStateId != GStateManager.UNDEFINED_STATE)
                    StartCoroutine(delayedSelect(LocalChatInputField));
#endif
            }

            if (resetEntry)
            {
                LocalChatInputField.text = "";
                LocalChatInputField.placeholder.enabled = true;
            }
        }

        private void OnEndEditHelperLocal()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                SendLobbyChatSignal();
        }

        public void OnChatValueChanged(InputField in_field)
        {
            if (in_field.isFocused)
            {
                in_field.placeholder.enabled = false;
            }
            in_field.transform.parent.FindDeepChild("FillAmount").GetComponent<Image>().fillAmount = in_field.text.Length / (float)in_field.characterLimit;
        }

        public void GlobalChatEntered()
        {
            GlobalChatInputField.text = GlobalChatInputField.text.Replace("\n", "").Trim();
            bool resetEntry = true;
            if (GlobalChatInputField.text.Length > 0)
            {
                Dictionary<string, object> jsonData = new Dictionary<string, object>();
                jsonData["lastConnectionId"] = GCore.Wrapper.Client.RTTConnectionID;
                jsonData["timeSent"] = DateTime.UtcNow.ToLongTimeString();
                jsonData["rank"] = GPlayerMgr.Instance.PlayerData.PlayerRank;

                // TODO read this in correctly! 
                GCore.Wrapper.ChatService.PostChatMessage(GCore.Wrapper.Client.AppId + ":gl:main", GlobalChatInputField.text, JsonWriter.Serialize(jsonData));

#if UNITY_WEBGL || UNITY_STANDALONE || UNITY_EDITOR
                GlobalChatInputField.text = "";
                resetEntry = false;
                StartCoroutine(delayedSelect(GlobalChatInputField));
#endif
            }

            if (resetEntry)
            {
                GlobalChatInputField.text = "";
                GlobalChatInputField.placeholder.enabled = true;
            }
        }

        private void OnEndEditHelperGlobal()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                GlobalChatEntered();
        }

        public void OnSelectProtocol(Dropdown aOption)
        {
            m_protocolListSelection = aOption.value;
        }

        public void OnSelectCompression(Dropdown aOption)
        {
            m_compressionListSelection = aOption.value;

            SendSignal("compression", (float)m_compressionListSelection);
            SetupOwnerLobbySettings();
        }

        public int GetProtocol()
        {
            return m_protocolListSelection;
        }

        public int GetCompression()
        {
            return m_compressionListSelection;
        }

        public void UpdateCompressionDropdown(int in_compression)
        {
            m_compressionListSelection = in_compression;
            m_compressionDropdown.value = in_compression;
        }

        public void SelectLayoutOption(Dropdown aOption)
        {
            m_layoutListSelection = aOption.value;

            OnNewRoomWindow();

            if (GCore.Wrapper.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId)
            {
                SendSignal("maplayout", m_layoutListSelection);
                SetupOwnerLobbySettings();
            }
        }

        public void UpdateMapLayoutDropdown(int in_value)
        {
            m_layoutListSelection = in_value;
            m_presetDropDownButton.value = in_value;
        }

        public void SelectSizeOption(Dropdown aOption)
        {
            m_sizeListSelection = aOption.value;

            OnNewRoomWindow();

            if (GCore.Wrapper.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId)
            {
                SendSignal("mapsize", m_sizeListSelection);
                SetupOwnerLobbySettings();
            }
        }

        public void UpdateMapSizeDropdown(int in_value)
        {
            m_sizeListSelection = in_value;
            m_sizeDropDownButton.value = in_value;
        }

        public void SelectGameTime(Dropdown aOption)
        {
            m_gameDurationListSelection = aOption.value;

            if (GCore.Wrapper.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId)
            {
                SendSignal("gametime", m_gameDurationListSelection);
                SetupOwnerLobbySettings();
            }
        }

        public void UpdateGameTimeDropdown(int in_value)
        {
            m_gameDurationListSelection = in_value;
            m_gameDurationDropDownButton.value = in_value;
        }

        public void UpdateGameName(string in_value)
        {
            m_inputField.text = in_value;
        }
        #endregion

        #region Private
        private void OnStartGame()
        {
            setLaunchingDisplay();
            GCore.Wrapper.LobbyService.UpdateReady(BombersNetworkManager.LobbyInfo.LobbyId, true, BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(GCore.Wrapper.Client.ProfileId).ExtraData);
        }

        private void SendSignal(string in_key, object in_value)
        {
            if (m_initialized && GCore.Wrapper.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId)
            {
                Dictionary<string, object> jsonData = new Dictionary<string, object>();
                jsonData[in_key] = in_value;
                GCore.Wrapper.LobbyService.SendSignal(BombersNetworkManager.LobbyInfo.LobbyId, jsonData);
            }
        }

        private void UpdateAllSettings()
        {
            if (m_initialized && GCore.Wrapper.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId)
            {
                Dictionary<string, object> jsonData = new Dictionary<string, object>();
                jsonData["gametime"] = m_gameDurationListSelection;
                jsonData["compression"] = (float)m_compressionListSelection;
                jsonData["maplayout"] = m_layoutListSelection;
                jsonData["mapsize"] = m_sizeListSelection;
                jsonData["gameName"] = m_inputField.text;
                GCore.Wrapper.LobbyService.SendSignal(BombersNetworkManager.LobbyInfo.LobbyId, jsonData);
            }
        }

        private void SetUIInteractable(bool in_enabled)
        {
            m_compressionDropdown.interactable = in_enabled;
            m_protocolDropdown.interactable = in_enabled;
            m_presetDropDownButton.interactable = in_enabled;
            m_sizeDropDownButton.interactable = in_enabled;
            m_gameDurationDropDownButton.interactable = in_enabled;
        }

        private void SetupOwnerLobbySettings()
        {
            if (m_initialized && GCore.Wrapper.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId)
            {
                Dictionary<string, object> matchOptions = BombersNetworkManager.s_matchOptions;
                matchOptions["gameTime"] = m_gameDurations[m_gameDurationListSelection].Duration;
                matchOptions["gameTimeSel"] = m_gameDurationListSelection;
                matchOptions["mapLayout"] = m_layoutListSelection;
                matchOptions["mapSize"] = m_sizeListSelection;
                matchOptions["gameName"] = m_inputField.text;

                GCore.Wrapper.LobbyService.UpdateSettings(BombersNetworkManager.LobbyInfo.LobbyId, matchOptions);
            }
        }

        private void OnNewRoomWindow()
        {
            m_presetDropDownButton.captionText.text = m_mapPresets[m_layoutListSelection].m_name;
            m_presetDropDownButton.value = m_layoutListSelection;

            m_sizeDropDownButton.captionText.text = m_mapSizes[m_sizeListSelection].m_name;
            m_sizeDropDownButton.value = m_sizeListSelection;

            m_gameDurationDropDownButton.captionText.text = m_gameDurations[m_gameDurationListSelection].Name;
            m_gameDurationDropDownButton.value = m_gameDurationListSelection;
        }

        private void setLaunchingDisplay()
        {
            m_titleField.SetActive(false);
            m_optionsAndGameGroup.SetActive(false);
            m_gameButtons.SetActive(false);
            m_panelLeft.enabled = true;

            m_waitingForPlayers.text = "LAUNCHING...";

            m_gameStartButton.GetComponent<Button>().interactable = false;
            m_gameQuitButton.GetComponent<Button>().interactable = false;
            m_changeTeamButton.GetComponent<Button>().interactable = false;
            SetUIInteractable(false);
            if (!GStateManager.Instance.IsLoadingState && !GStateManager.Instance.IsLoadingSubState &&
                GStateManager.Instance.FindSubState(JoiningGameSubState.STATE_NAME) == null &&
                GStateManager.Instance.FindSubState(STATE_NAME) != null)
            {
                GStateManager.Instance.PushSubState(JoiningGameSubState.STATE_NAME, false, false);
            }
        }

        private void populateGlobalChatWithExistingMessages()
        {
            Transform contentTransform = GameObject.Find("globalChatContent").transform;
            MainMenuState mainMenu = GStateManager.Instance.CurrentState as MainMenuState;
            if (mainMenu != null && contentTransform != null)
            {
                ChatCell cell;
                for (int i = 0; i < contentTransform.childCount; ++i)
                {
                    cell = contentTransform.GetChild(i).GetComponent<ChatCell>();
                    mainMenu.AddGlobalChatMessage(cell.RawJson, true);
                }
            }
        }

        private IEnumerator delayedDisplayGlobalChat(bool in_value)
        {
            yield return YieldFactory.GetWaitForEndOfFrame();

            m_chatGroupLobby.SetActive(!in_value);
            m_chatGroupGlobal.SetActive(in_value);

            m_lobbyChatNotification.SetActive(false);
            m_globalChatNotification.SetActive(false);

            GameObject tabGlobal = GameObject.Find("TabGlobal");
            GameObject tabLocal = GameObject.Find("TabLocal");

            Image lobbyButton = tabLocal.transform.FindDeepChild("Button").GetComponent<Image>();
            Image globalButton = tabGlobal.transform.FindDeepChild("Button").GetComponent<Image>();

            lobbyButton.color = !in_value ? Color.white : notSelected;
            globalButton.color = in_value ? Color.white : notSelected;
        }

        Color notSelected = new Color(0.5f, 0.5f, 0.5f, 1.0f);

        private IEnumerator delayedSelect(InputField in_field)
        {
            in_field.interactable = false;
            yield return YieldFactory.GetWaitForSeconds(0.15f);
            in_field.interactable = true;
            in_field.Select();
        }

        private void OnValidateStringSuccess(string in_stringData, object in_obj)
        {
            // Room name is valid, we can now start the game.
            OnStartGame();
        }

        private void OnValidateStringError(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            // An inappropriate name was detected, reset to default name.
            m_inputField.text = GPlayerMgr.Instance.PlayerData.PlayerName + "'s Room";
        }


        private FriendsListItem CreateFriendsListItem(Transform in_parent = null, bool isYou = false)
        {
            FriendsListItem toReturn = null;
            toReturn = (GEntityFactory.Instance.CreateResourceAtPath(isYou ? "Prefabs/UI/youCell" : "Prefabs/UI/friendCell", in_parent.transform)).GetComponent<FriendsListItem>();
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
            if (in_friendsItems.Count == 0)
            {
                return;
            }

            if (in_scrollView != null)
            {
                List<PlayerData> activeListData = in_friendsItems;
                for (int i = 0; i < activeListData.Count; ++i)
                {
                    FriendsListItem newItem = CreateFriendsListItem(in_scrollView, activeListData[i].ProfileId == GPlayerMgr.Instance.PlayerData.ProfileId);
                    newItem.Init(activeListData[i], in_add, in_remove);
                    newItem.transform.localPosition = new Vector3(0.0f, 0.0f);
                    newItem.RefreshOnlineVisibility();
                    newItem.gameObject.SetActive(true);
                    newItem.Status.gameObject.SetActive(false);
                    in_friendsListItem.Add(newItem);
                }
            }
        }
        private Text m_gameName = null;
        private Text m_waitingForPlayers = null;
        private GameObject m_gameQuitButton = null;
        private GameObject m_gameStartButton = null;
        private GameObject m_chatGroupLobby = null;
        private GameObject m_chatGroupGlobal = null;
        private GameObject m_optionsAndGameGroup = null;
        private GameObject m_lobbyGameOptionsHost = null;
        private GameObject m_lobbyGameOptionsTester = null;
        private GameObject m_editButton = null;
        private GameObject m_titleField = null;
        private GameObject m_gameButtons = null;
        private Canvas m_panelLeft = null;

        private GameObject m_changeTeamButton = null;
        private GameObject m_lobbyChatNotification = null;
        private GameObject m_globalChatNotification = null;

        private Dropdown m_presetDropDownButton = null;
        private Dropdown m_sizeDropDownButton = null;
        private Dropdown m_gameDurationDropDownButton = null;

        private Dropdown m_protocolDropdown = null;
        private Dropdown m_compressionDropdown = null;

        private InputField m_inputField = null;

        private List<MapPresets.Preset> m_mapPresets;
        private List<MapPresets.MapSize> m_mapSizes;
        private List<MapPresets.GameDuration> m_gameDurations;

        private int m_currentMemberCount = 0;

        private int m_layoutListSelection = 0;
        private int m_sizeListSelection = 1;
        private int m_gameDurationListSelection = 3;

        private int m_initialLayoutListSelection = 0;
        private int m_initialSizeListSelection = 0;
        private int m_initialGameDurationListSelection = 0;
        private string m_initialGameName = "";

        private int m_protocolListSelection = 0;
        private int m_compressionListSelection = (int)BaseNetworkBehavior.MSG_ENCODED;
        private bool m_initialized = false;

        private List<FriendsListItem> m_teamGreenListItem = null;
        private List<FriendsListItem> m_teamRedListItem = null;

        private List<PlayerData> m_teamGreenItems = null;
        private List<PlayerData> m_teamRedItems = null;

        private int m_teamGreenCount = 0;
        private int m_teamRedCount = 0;
        #endregion
    }
}
