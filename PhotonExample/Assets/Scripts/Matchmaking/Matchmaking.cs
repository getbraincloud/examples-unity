using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Linq;
using BrainCloudPhotonExample.Connection;


namespace BrainCloudPhotonExample.Matchmaking
{
    public class Matchmaking : MonoBehaviour
    {

        public class RoomButton
        {
            public RoomInfo m_room;
            public Button m_button;

            public RoomButton(RoomInfo aRoom, Button abutton)
            {
                m_room = aRoom;
                m_button = abutton;
            }
        }

        private Vector2 m_scrollPosition;

        private enum eMatchmakingState
        {
            GAME_STATE_SHOW_ROOMS,
            GAME_STATE_NEW_ROOM_OPTIONS,
            GAME_STATE_CREATE_NEW_ROOM,
            GAME_STATE_JOIN_ROOM,
            GAME_STATE_SHOW_LEADERBOARDS,
            GAME_STATE_SHOW_CONTROLS
        }
        private eMatchmakingState m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;

        private string m_roomName = "";
        private int m_roomMaxPlayers = 8;
        private int m_roomLevelRangeMin = 0;
        private int m_roomLevelRangeMax = 50;

        private GUISkin m_skin;
        private Rect m_windowRect;

        private GameObject m_showRoomsWindow;
        private List<RoomButton> m_roomButtons;
        private GameObject m_baseButton;

        private bool m_showPresetList = false;
        private bool m_showSizeList = false;
        private int m_presetListSelection = 0;
        private int m_sizeListSelection = 1;

        private GameObject m_createGameWindow;

        //private float m_roomYOffset = 304;

        private List<MapPresets.Preset> m_mapPresets;
        private List<MapPresets.MapSize> m_mapSizes;

        private GameObject m_basePresetButton;
        private GameObject m_baseSizeButton;

        private List<GameObject> m_presetButtons;
        private List<GameObject> m_sizeButtons;

        private GameObject m_leaderboardWindow;
        private GameObject m_scoreText;
        [SerializeField]
        private GameObject m_scoreRect;

        [SerializeField]
        private Sprite m_selectedTabSprite;
        [SerializeField]
        private Sprite m_tabSprite;

        private GameObject m_controlWindow;

        private Dictionary<string, bool> m_roomFilters = new Dictionary<string, bool>()
    {
        {"HideFull",false},
        {"HideLevelRange", false}
    };

        private string m_filterName = "";

        void Start()
        {
            GameObject.Find("Version Text").transform.SetParent(GameObject.Find("Canvas").transform);
            GameObject.Find("FullScreen").transform.SetParent(GameObject.Find("Canvas").transform);
            //m_scoreRect = GameObject.Find("Scores");
            
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
            m_createGameWindow.transform.FindChild("Room Name").GetComponent<InputField>().text = PlayerPrefs.GetString("LastRoomName");
            m_createGameWindow.SetActive(false);
            GameObject.Find("PlayerName").GetComponent<InputField>().text = PhotonNetwork.player.name;
            GameObject.Find("PlayerName").GetComponent<InputField>().interactable = false;
            m_skin = (GUISkin)Resources.Load("skin");
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadStatistics();
            //GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetLeaderboardPage(m_currentLeaderboardID, m_currentLeaderboardPage * m_leaderboardPageSize, m_currentLeaderboardPage * m_leaderboardPageSize + m_leaderboardPageSize);
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
            PhotonNetwork.player.name = GameObject.Find("PlayerName").GetComponent<InputField>().text;
            BrainCloudWrapper.GetBC().PlayerStateService.UpdatePlayerName(GameObject.Find("PlayerName").GetComponent<InputField>().text);
            GameObject.Find("PlayerName").GetComponent<InputField>().interactable = false;
            GameObject.Find("PlayerName").GetComponent<Image>().enabled = false;
        }

        void OnGUI()
        {
            // button.onClick.AddListener(() => { CreateGame(); });
            GUI.skin = m_skin;
            int width = 200;
            int height = 200;

            switch (m_state)
            {
                case eMatchmakingState.GAME_STATE_SHOW_ROOMS:

                    m_showRoomsWindow.SetActive(true);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    OnStatsWindow();
                    OrderRoomButtons();
                    //width = 500;
                    //height = 400;

                    //m_windowRect = new Rect(Screen.width / 2 - (width / 2 + 100), Screen.height / 2 - (height / 2), width, height);


                    //GUILayout.Window(20, m_windowRect, OnRoomsWindow, "Welcome " + PhotonNetwork.player.name + "!");
                    //GUILayout.Window(22, new Rect(m_windowRect.x + m_windowRect.width, m_windowRect.y, 200, m_windowRect.height), OnStatsWindow, "Statistics");

                    break;

                case eMatchmakingState.GAME_STATE_NEW_ROOM_OPTIONS:
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(true);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);

                    //m_windowRect = new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height);
                    OnNewRoomWindow();
                    //GUILayout.Window(21, m_windowRect, OnNewRoomWindow, "Create Room");

                    break;

                case eMatchmakingState.GAME_STATE_JOIN_ROOM:
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    height = 30;
                    GUI.TextArea(new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height), "Joining room...");

                    break;

                case eMatchmakingState.GAME_STATE_CREATE_NEW_ROOM:
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);
                    m_controlWindow.SetActive(false);
                    height = 30;
                    GUI.TextArea(new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height), "Creating and joining room...");

                    break;
                case eMatchmakingState.GAME_STATE_SHOW_LEADERBOARDS:
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(true);
                    m_controlWindow.SetActive(false);
                    //width = 500;
                    //height = 400;

                    //m_windowRect = new Rect(Screen.width / 2 - (width / 2 + 100), Screen.height / 2 - (height / 2), width, height);
                    OnLeaderboardWindow();
                    //GUILayout.Window(23, m_windowRect, OnLeaderboardWindow, "Leaderboards");
                    //GUILayout.Window(22, new Rect(m_windowRect.x + m_windowRect.width, m_windowRect.y, 200, m_windowRect.height), OnStatsWindow, "Statistics");
                    break;
                case eMatchmakingState.GAME_STATE_SHOW_CONTROLS:
                    m_showRoomsWindow.SetActive(false);
                    m_controlWindow.SetActive(true);
                    m_createGameWindow.SetActive(false);
                    m_leaderboardWindow.SetActive(false);

                    break;
            }
        }

        void OnStatsWindow()
        {
            List<BrainCloudStats.Stat> playerStats = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetStats();
            string rank = "";
            if (playerStats[0].m_statValue >= GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_playerLevelTitles.Length)
            {
                rank = "0" + "\n" + playerStats[1].m_statValue.ToString();
            }
            else
            {
                rank = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_playerLevelTitles[playerStats[0].m_statValue-1] + " (" + (playerStats[0].m_statValue) + ")\n" + playerStats[1].m_statValue.ToString();
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

            RoomOptions options = new RoomOptions();

            m_roomMaxPlayers = int.Parse(m_createGameWindow.transform.FindChild("Max Players").GetComponent<InputField>().text.ToString());
            m_roomLevelRangeMax = int.Parse(m_createGameWindow.transform.FindChild("Box 2").GetComponent<InputField>().text.ToString());
            m_roomLevelRangeMin = int.Parse(m_createGameWindow.transform.FindChild("Box 1").GetComponent<InputField>().text.ToString());

            options.maxPlayers = (byte)m_roomMaxPlayers;
            options.isOpen = true;
            options.isVisible = true;

            CreateNewRoom(m_createGameWindow.transform.FindChild("Room Name").GetComponent<InputField>().text, options);
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

        void OnNewRoomWindow()
        {
            m_createGameWindow.transform.FindChild("Layout").FindChild("Selection").GetComponent<Text>().text = m_mapPresets[m_presetListSelection].m_name;
            m_createGameWindow.transform.FindChild("Size").FindChild("Selection").GetComponent<Text>().text = m_mapSizes[m_sizeListSelection].m_name;

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

            /*
            GUIContent[] presetList = new GUIContent[m_mapPresets.Count];
            GUIContent[] sizeList = new GUIContent[m_mapSizes.Count];
            for (int i = 0; i < m_mapPresets.Count; i++)
            {
                presetList[i] = new GUIContent(m_mapPresets[i].m_name);
            }
            for (int i = 0; i < m_mapSizes.Count; i++)
            {
                sizeList[i] = new GUIContent(m_mapSizes[i].m_name);
            }

                if (Popup.List(new Rect(Screen.width / 4 - 80, 25, 100, 25), ref m_showPresetList, ref m_presetListSelection, presetList[m_presetListSelection], presetList, GUI.skin.button))
                {
                    m_mapLayout = m_presetListSelection;
                    GetComponent<PhotonView>().RPC("ChangeMapLayout", PhotonTargets.OthersBuffered, m_mapLayout);
                }

                if (Popup.List(new Rect(Screen.width / 4 + 60, 25, 100, 25), ref m_showSizeList, ref m_sizeListSelection, sizeList[m_sizeListSelection], sizeList, GUI.skin.button))
                {
                    m_mapSize = m_sizeListSelection;
                    GetComponent<PhotonView>().RPC("ChangeMapSize", PhotonTargets.OthersBuffered, m_mapSize);
                }

          
            else if (!PhotonNetwork.isMasterClient)
            {
                GUI.Button(new Rect(Screen.width / 4 - 80, 25, 100, 25), presetList[m_mapLayout]);

                GUI.Button(new Rect(Screen.width / 4 + 60, 25, 100, 25), sizeList[m_mapSize]);
            }

            */

            /*
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();

            GUILayout.Label("Room Name");
            m_roomName = GUILayout.TextField(m_roomName, GUILayout.MinWidth(100));

            GUILayout.Label("Max Players (2-8)");
            m_roomMaxPlayers = int.Parse(GUILayout.TextField(m_roomMaxPlayers.ToString(), GUILayout.MinWidth(100)));

            GUILayout.Label("Level Range");
            GUILayout.BeginHorizontal();
            m_roomLevelRangeMin = int.Parse(GUILayout.TextField(m_roomLevelRangeMin.ToString(), GUILayout.MinWidth(30)));
            GUILayout.Space(10);
            GUILayout.Label("-");
            GUILayout.Space(10);
            m_roomLevelRangeMax = int.Parse(GUILayout.TextField(m_roomLevelRangeMax.ToString(), GUILayout.MinWidth(30)));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Done"))
            {
                RoomOptions options = new RoomOptions();

                options.maxPlayers = m_roomMaxPlayers;
                options.isOpen = true;
                options.isVisible = true;

                CreateNewRoom(m_roomName, options);
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
             * */

        }

        public void JoinRoom(RoomInfo aRoomInfo)
        {
            int minLevel = 0;
            int maxLevel = 50;
            int playerLevel = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetStats()[0].m_statValue;

            if (aRoomInfo.customProperties["roomMinLevel"] != null)
            {
                minLevel = (int)aRoomInfo.customProperties["roomMinLevel"];
            }

            if (aRoomInfo.customProperties["roomMaxLevel"] != null)
            {
                maxLevel = (int)aRoomInfo.customProperties["roomMaxLevel"];
            }

            if (playerLevel < minLevel || playerLevel > maxLevel)
            {
                GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("You're not in that room's\nlevel range!");
            }
            else if (aRoomInfo.playerCount < aRoomInfo.maxPlayers)
            {
                m_state = eMatchmakingState.GAME_STATE_JOIN_ROOM;
                if (!PhotonNetwork.JoinRoom(aRoomInfo.name))
                {
                    m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
                    GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("Could not join room!");
                }
            }
            else
            {
                GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("That room is full!");
            }
        }

        public void RefreshRoomsList()
        {
            OnRoomsWindow();
        }

        void OrderRoomButtons()
        {
            m_roomFilters["HideFull"] = GameObject.Find("Toggle-Hide").GetComponent<Toggle>().isOn;
            m_roomFilters["HideLevelRange"] = GameObject.Find("Toggle-MyRank").GetComponent<Toggle>().isOn;
            m_filterName = GameObject.Find("InputField").GetComponent<InputField>().text;

            int minLevel = 0;
            int maxLevel = 50;
            int playerLevel = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetStats()[0].m_statValue;

            for (int i = 0; i < m_roomButtons.Count; i++)
            {
                if (m_roomButtons[i].m_room.customProperties["roomMinLevel"] != null)
                {
                    minLevel = (int)m_roomButtons[i].m_room.customProperties["roomMinLevel"];
                    if (playerLevel < minLevel && m_roomFilters["HideLevelRange"])
                    {
                        continue;
                    }
                }

                if (m_roomButtons[i].m_room.customProperties["roomMaxLevel"] != null)
                {
                    maxLevel = (int)m_roomButtons[i].m_room.customProperties["roomMaxLevel"];
                    if (playerLevel > maxLevel && m_roomFilters["HideLevelRange"])
                    {
                        continue;
                    }
                }


                if (m_filterName != "" && !m_roomButtons[i].m_room.name.ToLower().Contains(m_filterName.ToLower()))
                {
                    continue;
                }

                //m_roomButtons[i].m_button;
            }


        }

        void OnRoomsWindow()
        {
            for (int i = 0; i < m_roomButtons.Count; i++)
            {
                Destroy(m_roomButtons[i].m_button.gameObject);
            }

            m_roomButtons.Clear();
            RoomInfo[] rooms = PhotonNetwork.GetRoomList();
            //Debug.Log(rooms.Length);

            for (int i = 0; i < rooms.Length; i++)
            {
                GameObject roomButton = (GameObject)Instantiate(m_baseButton, m_baseButton.transform.position, m_baseButton.transform.rotation);
                roomButton.SetActive(true);
                roomButton.transform.SetParent(m_baseButton.transform.parent);
                Vector3 position = roomButton.GetComponent<RectTransform>().position;
                position.y -= i * 30;
                roomButton.GetComponent<RectTransform>().position = position;
                RoomInfo roomInfo = rooms[i];
                roomButton.GetComponent<Button>().onClick.AddListener(() => { JoinRoom(roomInfo); });
                roomButton.transform.GetChild(0).GetComponent<Text>().text = rooms[i].name;
                if ((int)rooms[i].customProperties["IsPlaying"] == 1)
                {
                    roomButton.transform.GetChild(0).GetComponent<Text>().text = rooms[i].name + " -- In Progress";
                }

                roomButton.transform.GetChild(1).GetComponent<Text>().text = rooms[i].playerCount + "/" + rooms[i].maxPlayers;
                m_roomButtons.Add(new RoomButton(roomInfo, roomButton.GetComponent<Button>()));
            }
            /*
            RoomInfo[] rooms = PhotonNetwork.GetRoomList();
            List<RoomInfo> roomList = rooms.ToList<RoomInfo>();
            for (int i = 0; i < m_roomButtons.Count; i++)
            {
                if (!roomList.Contains(m_roomButtons[i].m_room))
                {
                    Destroy(m_roomButtons[i].m_button.gameObject);
                    i--;
                }
            }

            for (int i = 0; i < roomList.Count; i++)
            {
                bool hasButton = false;
                for (int j=0;i<m_roomButtons.Count;j++)
                {
                    if (m_roomButtons[j].m_room == roomList[i])
                    {
                        hasButton = true;
                    }
                }

                if (!hasButton)
                {
                    GameObject roomButton = (GameObject)Instantiate(m_baseButton, m_baseButton.transform.position, m_baseButton.transform.rotation);
                    roomButton.transform.parent = m_baseButton.transform.parent;
                    Vector3 position = roomButton.GetComponent<RectTransform>().position;
                    position.y -= (roomList.Count - 1) * 30;
                    roomButton.GetComponent<RectTransform>().position = position;
                
                }
            }
             * 
             */

            /*
                for (int i = 0; i < rooms.Length; i++)
                {
                    if (rooms[i].removedFromList) continue;

                    if (rooms[i].playerCount == rooms[i].maxPlayers && m_roomFilters["HideFull"])
                    {
                        continue;
                    }
                    int minLevel = 0;
                    int maxLevel = 50;
                    int playerLevel = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetStats()[0].m_statValue;


                    if (rooms[i].customProperties["roomMinLevel"] != null)
                    {
                        minLevel = (int)rooms[i].customProperties["roomMinLevel"];
                        if (playerLevel < minLevel && m_roomFilters["HideLevelRange"])
                        {
                            continue;
                        }
                    }

                    if (rooms[i].customProperties["roomMaxLevel"] != null)
                    {
                        maxLevel = (int)rooms[i].customProperties["roomMaxLevel"];
                        if (playerLevel > maxLevel && m_roomFilters["HideLevelRange"])
                        {
                            continue;
                        }
                    }


                    if (m_filterName != "" && !rooms[i].name.ToLower().Contains(m_filterName.ToLower()))
                    {
                        continue;
                    }


                    string roomInfo = rooms[i].name + " -- " + rooms[i].playerCount + "/" + rooms[i].maxPlayers + " players" + "\nRanks: " + minLevel + " - " + maxLevel;

                    if ((int)rooms[i].customProperties["IsPlaying"] == 1)
                    {
                        roomInfo = rooms[i].name + " -- " + rooms[i].playerCount + "/" + rooms[i].maxPlayers + " players" + " -- In Progress\nRanks: " + minLevel + " - " + maxLevel;
                    }

                    if (GUILayout.Button(roomInfo, GUILayout.Width(370), GUILayout.Height(50)))
                    {
                        if (playerLevel < minLevel || playerLevel > maxLevel)
                        {
                            GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("You're not in that room's\nlevel range!", "Error");

                            break;
                        }
                        else if (rooms[i].playerCount < rooms[i].maxPlayers)
                        {
                            m_state = eMatchmakingState.GAME_STATE_JOIN_ROOM;
                            if (!PhotonNetwork.JoinRoom(rooms[i].name))
                            {
                                m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
                                GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("Could not join room!", "Error");
                            }
                            break;
                        }
                        else
                        {
                            GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("That room is full!", "Error");
                        }
                    }
                }
             * */

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
            if (m_leaderboardReady)
            {
                
                players = int.Parse(leaderboardData["social_leaderboard"].Count.ToString());

                for (int i = 0; i < players; i++)
                {
                    leaderboardRankText += (i + 1) + "\n";
                    leaderboardNameText += leaderboardData["social_leaderboard"][i]["name"].ToString() + "\n";
                    leaderboardLevelText += leaderboardData["social_leaderboard"][i]["data"]["rank"].ToString() + " (" + leaderboardData["social_leaderboard"][i]["data"]["level"].ToString() + ")\n";
                    leaderboardScoreText += (Mathf.Floor(float.Parse(leaderboardData["social_leaderboard"][i]["score"].ToString()) / 10000) + 1).ToString("n0") + "\n";

                   // playerInfo = leaderboardData["leaderboard"][i]["rank"].ToString() + ": " + leaderboardData["leaderboard"][i]["name"].ToString() + " -- " + scoreType + (Mathf.Floor(float.Parse(leaderboardData["leaderboard"][i]["score"].ToString()) / 10000) + 1);

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

            m_scoreRect.transform.FindChild("List").GetComponent<Text>().text = leaderboardNameText;
            m_scoreRect.transform.FindChild("List Ranks").GetComponent<Text>().text = leaderboardRankText;
            m_scoreRect.transform.FindChild("List Count").GetComponent<Text>().text = leaderboardScoreText;
            m_scoreRect.transform.FindChild("List Level").GetComponent<Text>().text = leaderboardLevelText;
            m_scoreRect.GetComponent<RectTransform>().sizeDelta = new Vector2(m_scoreRect.GetComponent<RectTransform>().sizeDelta.x, 18.2f * players);
            if (!m_once)
            {
                m_once = true;
                //m_scoreRect.GetComponent<RectTransform>().localPosition = new Vector3(m_scoreRect.GetComponent<RectTransform>().localPosition.x, -((18.2f * players) / 2), m_scoreRect.GetComponent<RectTransform>().localPosition.z);
                m_scoreRect.transform.parent.parent.FindChild("Scrollbar").GetComponent<Scrollbar>().value = 1;
                m_scoreRect.transform.parent.parent.FindChild("Scrollbar").GetComponent<Scrollbar>().value = 0.99f;
                m_scoreRect.transform.parent.parent.FindChild("Scrollbar").GetComponent<Scrollbar>().value = 1;
            }
            
        }

        void OnPhotonJoinRoomFailed()
        {
            m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
            GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("Could not join room!");
        }

        void OnJoinedRoom()
        {
            GameObject.Find("Version Text").transform.SetParent(null);
            GameObject.Find("FullScreen").transform.SetParent(null);
            PhotonNetwork.automaticallySyncScene = true;
            PhotonNetwork.LoadLevel("Game");
        }

        public void QuitToLogin()
        {
            BrainCloudWrapper.GetBC().PlayerStateService.Logout();
            BrainCloudWrapper.GetBC().AuthenticationService.ClearSavedProfileID();
            PhotonNetwork.LoadLevel("Connect");
        }

        public void CreateGame()
        {
            m_state = eMatchmakingState.GAME_STATE_NEW_ROOM_OPTIONS;
        }

        void Update()
        {
            if (m_state == eMatchmakingState.GAME_STATE_NEW_ROOM_OPTIONS && Input.GetKeyDown(KeyCode.Return))
            {
                RoomOptions options = new RoomOptions();

                options.maxPlayers = (byte)m_roomMaxPlayers;
                options.isOpen = true;
                options.isVisible = true;

                CreateNewRoom(m_roomName, options);
            }
        }

        void CreateNewRoom(string aName, RoomOptions aOptions)
        {
            RoomInfo[] rooms = PhotonNetwork.GetRoomList();
            bool roomExists = false;

            if (aName == "")
            {
                aName = PhotonNetwork.player.name + "'s Room";
            }

            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i].name == aName)
                {
                    roomExists = true;
                }
            }

            if (roomExists)
            {

                GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("There's already a room named " + aName + "!");
                m_roomName = "";
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

            if (aOptions.maxPlayers > 8)
            {
                aOptions.maxPlayers = 8;
            }
            else if (aOptions.maxPlayers < 1)
            {
                aOptions.maxPlayers = 1;
            }



            ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
            customProperties["roomMinLevel"] = m_roomLevelRangeMin;
            customProperties["roomMaxLevel"] = m_roomLevelRangeMax;
            customProperties["StartGameTime"] = 10 * 60;
            customProperties["Team1Score"] = 0;
            customProperties["Team2Score"] = 0;
            customProperties["IsPlaying"] = 0;
            customProperties["MapLayout"] = m_presetListSelection;
            customProperties["MapSize"] = m_sizeListSelection;
            aOptions.customRoomProperties = customProperties;
            aOptions.customRoomPropertiesForLobby = new string[] { "roomMinLevel", "roomMaxLevel", "IsPlaying" };
            PlayerPrefs.SetString("LastRoomName", aName);
            m_state = eMatchmakingState.GAME_STATE_CREATE_NEW_ROOM;
            PhotonNetwork.CreateRoom(aName, aOptions, TypedLobby.Default);

        }
    }
}