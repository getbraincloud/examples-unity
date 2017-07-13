/*
 * UNET does not yet allow for matchmaking information to be sent through the matchmaker, so all of the match filtering is non-functional, besides name-based filtering.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using BrainCloudUNETExample.Connection;
using UnityEngine.Networking;
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
            GAME_STATE_CREATE_NEW_ROOM,
            GAME_STATE_JOIN_ROOM,
            GAME_STATE_SHOW_LEADERBOARDS,
            GAME_STATE_SHOW_CONTROLS,
            GAME_STATE_SHOW_ACHIEVEMENTS
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

        private GameObject m_joiningGameWindow;

        private GameObject m_controlWindow;
        private GameObject m_achievementsWindow;
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
            if (!BrainCloudWrapper.GetBC().Initialized)
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
            BrainCloudWrapper.GetBC().PlayerStateService.UpdatePlayerName(GameObject.Find("PlayerName").GetComponent<InputField>().text);
            GameObject.Find("PlayerName").GetComponent<InputField>().interactable = false;
            GameObject.Find("PlayerName").GetComponent<Image>().enabled = false;
        }

        private void OnGUI()
        {

            switch (m_state)
            {
                case eMatchmakingState.GAME_STATE_SHOW_ROOMS:
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
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(true);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(false);

                    OnNewRoomWindow();

                    break;

                case eMatchmakingState.GAME_STATE_JOIN_ROOM:
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(true);

                    break;

                case eMatchmakingState.GAME_STATE_CREATE_NEW_ROOM:
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(true);

                    break;
                case eMatchmakingState.GAME_STATE_SHOW_LEADERBOARDS:
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(true);
                    m_controlWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(false);

                    OnLeaderboardWindow();

                    break;
                case eMatchmakingState.GAME_STATE_SHOW_CONTROLS:
                    m_achievementsWindow.SetActive(false);
                    m_showRoomsWindow.SetActive(false);
                    m_controlWindow.SetActive(true);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_joiningGameWindow.SetActive(false);

                    break;
                case eMatchmakingState.GAME_STATE_SHOW_ACHIEVEMENTS:
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

            var matchAttributes = new Dictionary<string, long>() { { "minLevel", m_roomLevelRangeMin }, { "maxLevel", m_roomLevelRangeMax } };

            CreateNewRoom(m_createGameWindow.transform.Find("Room Name").GetComponent<InputField>().text, (uint)m_roomMaxPlayers, matchAttributes);
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

        public void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
        {
            if (success)
            {
                NetworkManager.singleton.OnMatchCreate(success, extendedInfo, matchInfo);
            }
            else
            {
                Debug.LogError("Create match failed");
            }
        }

        public void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
        {
            if (success)
            {
                try
                {
                    NetworkManager.singleton.OnMatchJoined(success, extendedInfo, matchInfo);
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
            else
            {
                m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
                m_dialogueDisplay.DisplayDialog("Could not join room!");
                Debug.LogError("Join match failed");
            }
        }

        public void QuitToLogin()
        {
            BrainCloudWrapper.GetBC().PlayerStateService.Logout();
            BrainCloudWrapper.GetBC().AuthenticationService.ClearSavedProfileID();
            SceneManager.LoadScene("Connect");
        }

        public void CreateGame()
        {
            m_state = eMatchmakingState.GAME_STATE_NEW_ROOM_OPTIONS;
            m_createGameWindow.transform.Find("Room Name").GetComponent<InputField>().text = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_previousGameName;
        }

        void Update()
        {
            if (m_state == eMatchmakingState.GAME_STATE_NEW_ROOM_OPTIONS && Input.GetKeyDown(KeyCode.Return))
            {
                ConfirmCreateGame();
            }
        }

        void CreateNewRoom(string aName, uint size, Dictionary<string, long> matchAttributes)
        {
            List<MatchInfoSnapshot> rooms = BombersNetworkManager.singleton.matches;
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
            matchAttributes["StartGameTime"] = 600;
            matchAttributes["IsPlaying"] = 0;
            matchAttributes["MapLayout"] = m_presetListSelection;
            matchAttributes["MapSize"] = m_sizeListSelection;

            BrainCloudWrapper.GetBC().EntityService.UpdateSingleton("gameName", "{\"gameName\": \"" + roomName + "\"}", null, -1, null, null, null);
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadStatistics();
            m_state = eMatchmakingState.GAME_STATE_CREATE_NEW_ROOM;


            Dictionary<string, string> matchOptions = new Dictionary<string, string>();
            matchOptions.Add("gameTime", 600.ToString());
            matchOptions.Add("isPlaying", 0.ToString());
            matchOptions.Add("mapLayout", m_presetListSelection.ToString());
            matchOptions.Add("mapSize", m_sizeListSelection.ToString());
            matchOptions.Add("gameName", roomName);
            matchOptions.Add("maxPlayers", size.ToString());
            matchOptions.Add("lightPosition", 0.ToString());

            BombersNetworkManager.m_matchOptions = matchOptions;
            BombersNetworkManager.singleton.matchMaker.CreateMatch(aName, size, true, "", "", "", 0, 0, OnMatchCreate);
        }
    }
}