using BrainCloud;
using BrainCloudUNETExample.Connection;
using Gameframework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace BrainCloudUNETExample
{
    public class CreateGameSubState : BaseSubState
    {
        public static Dictionary<string, object> gmatchOptions = new Dictionary<string, object>();
        public static string STATE_NAME = "createGame";

        #region BaseState
        protected override void Start()
        {

            Dropdown presetDropDownButton = GameObject.Find("PresetDropDownButton").GetComponent<Dropdown>();
            Dropdown sizeDropDownButton = GameObject.Find("SizeDropDownButton").GetComponent<Dropdown>();
            Dropdown gameDurationDropDownButton = GameObject.Find("TimeDropDownButton").GetComponent<Dropdown>();
            Dropdown regionDurationDropDownButton = GameObject.Find("RegionDropDownButton").GetComponent<Dropdown>();

            m_mapPresets = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_presets;
            m_mapSizes = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_mapSizes;
            m_gameDurations = GameObject.Find("MapPresets").GetComponent<MapPresets>().GameDurations;
            m_regions = GameObject.Find("MapPresets").GetComponent<MapPresets>().Regions;
            m_textMesh = transform.FindDeepChild("TitleBarText").GetComponent<TextMeshProUGUI>();
            m_inputField = transform.FindDeepChild("Room Name").GetComponent<InputField>();
            m_inputField.characterLimit = GPlayerMgr.MAX_CHARACTERS_GAME_NAME;

            // update the Max Players default and max values.
            Slider maxPlayersSlider = transform.FindDeepChild("MaxPlayersSlider").GetComponent<Slider>();
            maxPlayersSlider.value = GConfigManager.GetIntValue("MaxPlayersDefault") / 2;
            maxPlayersSlider.maxValue = GConfigManager.GetIntValue("MaxPlayers") / 2;

            // updated region selection
            BombersNetworkManager.Instance.SetSelectedRegion(m_regions[m_regionListSelection].Lobby);

            string str = m_state == eCreateGameState.NEW_ROOM ? "Create Game" : "Find Game";
            transform.FindDeepChild("CreateButton").FindDeepChild("Text").GetComponent<Text>().text = str;
            m_textMesh.text = str.ToUpper();

            List<string> items = new List<string>();
            for (int i = 0; i < m_mapPresets.Count; i++)
            {
                items.Add(m_mapPresets[i].m_name);
            }
            presetDropDownButton.ClearOptions();
            presetDropDownButton.AddOptions(items);
            items.Clear();

            for (int i = 0; i < m_mapSizes.Count; i++)
            {
                items.Add(m_mapSizes[i].m_name);
            }
            sizeDropDownButton.ClearOptions();
            sizeDropDownButton.AddOptions(items);

            // game durations
            items.Clear();
            for (int i = 0; i < m_gameDurations.Count; i++)
            {
                items.Add(m_gameDurations[i].Name);
            }
            gameDurationDropDownButton.ClearOptions();
            gameDurationDropDownButton.AddOptions(items);

            // regions
            items.Clear();
            for (int i = 0; i < m_regions.Count; i++)
            {
                items.Add(m_regions[i].Name);
            }
            regionDurationDropDownButton.ClearOptions();
            regionDurationDropDownButton.AddOptions(items);

            m_sizeListSelection = 1;
            m_gameDurationListSelection = GConfigManager.GetIntValue("DefaultGameTimeIndex");

            OnNewRoomWindow();

            transform.FindDeepChild("Room Name").GetComponent<InputField>().text = GPlayerMgr.Instance.PlayerData.PlayerName + "'s Room";

            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();
        }


        void Update()
        {
            if (m_inputWasFocused && Input.GetKeyDown(KeyCode.Return))
                m_inputWasFocused = false;
            else if (Input.GetKeyDown(KeyCode.Return))
                ConfirmCreateGame();

            // Deselect dropdowns after a mouse click 
            if (Input.GetMouseButtonUp(0))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
        #endregion

        #region Public 
        public void QuickPlay()
        {
            m_state = eCreateGameState.QUICK_PLAY;
            ConfirmCreateGame();
            gameObject.SetActive(false);
        }

        public void LateInit(bool in_bCreateGame = true)
        {
            m_state = in_bCreateGame ? eCreateGameState.NEW_ROOM : eCreateGameState.FIND_ROOM;
        }

        public void ConfirmCreateGame()
        {
            m_inputField.text = m_inputField.text.Trim();
            GPlayerMgr.Instance.ValidateString(m_inputField.text, OnValidateStringSuccess, OnValidateStringError);
        }

        public void OpenLayoutDropdown()
        {
            OnNewRoomWindow();
        }

        public void OpenSizeDropdown()
        {
            OnNewRoomWindow();
        }

        public void SelectLayoutOption(Dropdown aOption)
        {
            m_layoutListSelection = aOption.value;

            OnNewRoomWindow();
        }

        public void SelectSizeOption(Dropdown aOption)
        {
            m_sizeListSelection = aOption.value;

            OnNewRoomWindow();
        }

        public void SelectGameTime(Dropdown aOption)
        {
            m_gameDurationListSelection = aOption.value;
        }

        public void SelectRegion(Dropdown aOption)
        {
            m_regionListSelection = aOption.value;
            BombersNetworkManager.Instance.SetSelectedRegion(m_regions[m_regionListSelection].Lobby);
        }

        public void FinishedEditing()
        {
            m_inputWasFocused = true;
        }

        public void OnMaxPlayers(float in_slider)
        {
            m_roomMaxPlayers = (int)in_slider * 2;
        }
        #endregion

        #region Private
        private void OnValidateStringSuccess(string in_stringData, object in_obj)
        {
            // Room name is valid, we can now create the game.
            OnConfirmCreateGame(in_stringData);
        }

        private void OnValidateStringError(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            // An inappropriate name was detected, reset to default name.
            m_inputField.text = GPlayerMgr.Instance.PlayerData.PlayerName + "'s Room";
        }

        private void OnConfirmCreateGame(string in_name)
        {
            m_roomLevelRangeMax = int.Parse(this.transform.FindDeepChild("Box 2").GetComponent<InputField>().text.ToString());
            m_roomLevelRangeMin = int.Parse(this.transform.FindDeepChild("Box 1").GetComponent<InputField>().text.ToString());

            var matchAttributes = new Dictionary<string, object>() { { "minLevel", m_roomLevelRangeMin }, { "maxLevel", m_roomLevelRangeMax } };

            CreateNewRoom(in_name, (uint)m_roomMaxPlayers, matchAttributes);
        }

        private void OnNewRoomWindow()
        {
            Dropdown presetDropDownButton = GameObject.Find("PresetDropDownButton").GetComponent<Dropdown>();
            Dropdown sizeDropDownButton = GameObject.Find("SizeDropDownButton").GetComponent<Dropdown>();
            Dropdown timeDropDownButton = GameObject.Find("TimeDropDownButton").GetComponent<Dropdown>();
            Dropdown regionDropDownButton = GameObject.Find("RegionDropDownButton").GetComponent<Dropdown>();

            presetDropDownButton.captionText.text = m_mapPresets[m_layoutListSelection].m_name;
            presetDropDownButton.value = m_layoutListSelection;

            sizeDropDownButton.captionText.text = m_mapSizes[m_sizeListSelection].m_name;
            sizeDropDownButton.value = m_sizeListSelection;

            timeDropDownButton.captionText.text = m_gameDurations[m_gameDurationListSelection].Name;
            timeDropDownButton.value = m_gameDurationListSelection;

            regionDropDownButton.captionText.text = m_regions[m_regionListSelection].Name;
            regionDropDownButton.value = m_regionListSelection;
        }


        void CreateNewRoom(string aName, uint size, Dictionary<string, object> matchAttributes)
        {
            BombersNetworkManager networkMgr = BombersNetworkManager.singleton as BombersNetworkManager;
            //List<MatchInfoSnapshot> rooms = null;// networkMgr.matches;
            bool roomExists = false;
            string roomName = aName;

            if (aName == "")
            {
                roomName = GPlayerMgr.Instance.PlayerData.PlayerName + "'s Room";
            }
            if (roomExists)
            {
                //m_dialogueDisplay.DisplayDialog("There's already a room named " + aName + "!");
                return;
            }

            int playerLevel = BrainCloudStats.Instance.GetStats()[0].Value;

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
            matchAttributes["StartGameTime"] = m_gameDurations[m_gameDurationListSelection].Duration;
            matchAttributes["IsPlaying"] = 0;
            matchAttributes["MapLayout"] = m_layoutListSelection;
            matchAttributes["MapSize"] = m_sizeListSelection;

            GCore.Wrapper.Client.EntityService.UpdateSingleton("gameName", "{\"gameName\": \"" + roomName + "\"}", null, -1, null, null, null);
            BrainCloudStats.Instance.ReadStatistics();

            Dictionary<string, object> matchOptions = new Dictionary<string, object>();
            matchOptions.Add("gameTime", matchAttributes["StartGameTime"]);
            matchOptions.Add("gameTimeSel", m_gameDurationListSelection);
            matchOptions.Add("isPlaying", 0);
            matchOptions.Add("mapLayout", m_layoutListSelection);
            matchOptions.Add("mapSize", m_sizeListSelection);
            matchOptions.Add("gameName", roomName);
            matchOptions.Add("maxPlayers", size);
            matchOptions.Add("lightPosition", 0);

            matchOptions.Add("minLevel", m_roomLevelRangeMin);
            matchOptions.Add("maxLevel", m_roomLevelRangeMax);

            switch (m_state)
            {
                case eCreateGameState.NEW_ROOM:
                default:
                    {
                        // Make sure that the JoiningGameSubState is loaded before calling CreateLobby
                        GStateManager stateMgr = GStateManager.Instance;
                        stateMgr.OnInitializeDelegate += onPushJoiningGameSubStateCreateLobbyLoaded;
                        gmatchOptions = matchOptions;
                        stateMgr.PushSubState(JoiningGameSubState.STATE_NAME);
                    }
                    break;

                case eCreateGameState.QUICK_PLAY:
                case eCreateGameState.FIND_ROOM:
                    {
                        // Make sure that the JoiningGameSubState is loaded before calling FindLobby
                        GStateManager stateMgr = GStateManager.Instance;
                        stateMgr.OnInitializeDelegate += onPushJoiningGameSubStateFindLobbyLoaded;
                        gmatchOptions = matchOptions;
                        stateMgr.PushSubState(JoiningGameSubState.STATE_NAME);
                    }
                    break;
            }
        }

        private static void onPushJoiningGameSubStateCreateLobbyLoaded(BaseState in_state)
        {
            GStateManager stateMgr = GStateManager.Instance;
            if (in_state as JoiningGameSubState)
            {
                stateMgr.OnInitializeDelegate -= onPushJoiningGameSubStateCreateLobbyLoaded;

                BombersNetworkManager networkMgr = BombersNetworkManager.singleton as BombersNetworkManager;
                networkMgr.CreateLobby(gmatchOptions);
            }
        }

        private static void onPushJoiningGameSubStateFindLobbyLoaded(BaseState in_state)
        {
            GStateManager stateMgr = GStateManager.Instance;
            if (in_state as JoiningGameSubState)
            {
                stateMgr.OnInitializeDelegate -= onPushJoiningGameSubStateFindLobbyLoaded;

                BombersNetworkManager networkMgr = BombersNetworkManager.singleton as BombersNetworkManager;
                networkMgr.FindLobby(gmatchOptions);
            }
        }

        private enum eCreateGameState
        {
            NEW_ROOM,
            FIND_ROOM,
            QUICK_PLAY
        }

        private eCreateGameState m_state = eCreateGameState.NEW_ROOM;

        private int m_roomMaxPlayers = 8;
        private int m_roomLevelRangeMin = 0;
        private int m_roomLevelRangeMax = 50;

        private int m_layoutListSelection = 0;
        private int m_sizeListSelection = 1;
        private int m_gameDurationListSelection = 3;
        private int m_regionListSelection = 0;

        private bool m_inputWasFocused = false;
        private InputField m_inputField = null;
        private List<MapPresets.Preset> m_mapPresets;
        private List<MapPresets.MapSize> m_mapSizes;
        private List<MapPresets.GameDuration> m_gameDurations;
        private List<MapPresets.RegionInfo> m_regions;

        private TextMeshProUGUI m_textMesh;
        #endregion
    }
}
