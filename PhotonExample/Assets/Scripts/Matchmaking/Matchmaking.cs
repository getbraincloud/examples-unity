﻿using UnityEngine;
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
            GAME_STATE_SHOW_LEADERBOARDS
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

        private Dictionary<string, bool> m_roomFilters = new Dictionary<string, bool>()
    {
        {"HideFull",false},
        {"HideLevelRange", false}
    };

        private string m_filterName = "";

        void Start()
        {
            m_basePresetButton = GameObject.Find("PresetButton");
            m_baseSizeButton = GameObject.Find("SizeButton");
            m_basePresetButton.SetActive(false);
            m_baseSizeButton.SetActive(false);
            m_presetButtons = new List<GameObject>();
            m_sizeButtons = new List<GameObject>();
            m_mapPresets = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_presets;
            m_mapSizes = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_mapSizes;

            for (int i = 0; i < m_mapPresets.Count;i++)
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
            m_skin = (GUISkin)Resources.Load("skin");
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadStatistics();
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetLeaderboardPage(m_currentLeaderboardID, m_currentLeaderboardPage * m_leaderboardPageSize, m_currentLeaderboardPage * m_leaderboardPageSize + m_leaderboardPageSize);
            OnRoomsWindow();
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

                    //m_windowRect = new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height);
                    OnNewRoomWindow();
                    //GUILayout.Window(21, m_windowRect, OnNewRoomWindow, "Create Room");

                    break;

                case eMatchmakingState.GAME_STATE_JOIN_ROOM:
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    height = 30;
                    GUI.TextArea(new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height), "Joining room...");

                    break;

                case eMatchmakingState.GAME_STATE_CREATE_NEW_ROOM:
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    height = 30;
                    GUI.TextArea(new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height), "Creating and joining room...");

                    break;
                case eMatchmakingState.GAME_STATE_SHOW_LEADERBOARDS:
                    m_showRoomsWindow.SetActive(false);
                    m_createGameWindow.SetActive(false);
                    width = 500;
                    height = 400;

                    m_windowRect = new Rect(Screen.width / 2 - (width / 2 + 100), Screen.height / 2 - (height / 2), width, height);

                    GUILayout.Window(23, m_windowRect, OnLeaderboardWindow, "Leaderboards");
                    //GUILayout.Window(22, new Rect(m_windowRect.x + m_windowRect.width, m_windowRect.y, 200, m_windowRect.height), OnStatsWindow, "Statistics");
                    break;
            }
        }

        void OnStatsWindow()
        {
            List<BrainCloudStats.Stat> playerStats = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetStats();

            string rank = playerStats[0].m_statValue.ToString() + "\n" + playerStats[1].m_statValue.ToString();
            string stats = playerStats[3].m_statValue.ToString() + "\n" + playerStats[2].m_statValue.ToString() + "\n" + playerStats[4].m_statValue.ToString()
                + "\n" + playerStats[5].m_statValue.ToString() + "\n" + playerStats[6].m_statValue.ToString()
                + "\n" + playerStats[7].m_statValue.ToString() + "\n" + playerStats[8].m_statValue.ToString()
                + "\n" + playerStats[9].m_statValue.ToString();

            GameObject.Find("StatText").GetComponent<Text>().text = stats;
            GameObject.Find("RankText").GetComponent<Text>().text = rank;
            GameObject.Find("PlayerName").GetComponent<Text>().text = PhotonNetwork.player.name;

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
                for (int i=0;i<m_presetButtons.Count;i++)
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
                GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("You're not in that room's\nlevel range!", "Error");
            }
            else if (aRoomInfo.playerCount < aRoomInfo.maxPlayers)
            {
                m_state = eMatchmakingState.GAME_STATE_JOIN_ROOM;
                if (!PhotonNetwork.JoinRoom(aRoomInfo.name))
                {
                    m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
                    GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("Could not join room!", "Error");
                }
            }
            else
            {
                GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("That room is full!", "Error");
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

        private int m_currentLeaderboardPage = 0;
        private int m_leaderboardPageSize = 6;
        private string m_currentLeaderboardID = "KDR";
        private bool m_leaderboardReady = false;


        void OnLeaderboardWindow(int windowID)
        {
            if (GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_leaderboardReady)
            {
                m_leaderboardReady = true;
            }
            else
            {
                m_leaderboardReady = false;
            }

            LitJson.JsonData leaderboardData = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_leaderboardData;
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            if (m_currentLeaderboardID == "KDR")
            {
                if (GUILayout.Button("Kills") && m_leaderboardReady)
                {
                    m_leaderboardReady = false;
                    m_currentLeaderboardID = "BDR";
                    m_currentLeaderboardPage = 0;
                    GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetLeaderboardPage(m_currentLeaderboardID, m_currentLeaderboardPage * m_leaderboardPageSize, m_currentLeaderboardPage * m_leaderboardPageSize + m_leaderboardPageSize);
                }
            }
            else
            {
                if (GUILayout.Button("Bomb Hits") && m_leaderboardReady)
                {
                    m_leaderboardReady = false;
                    m_currentLeaderboardID = "KDR";
                    m_currentLeaderboardPage = 0;
                    GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetLeaderboardPage(m_currentLeaderboardID, m_currentLeaderboardPage * m_leaderboardPageSize, m_currentLeaderboardPage * m_leaderboardPageSize + m_leaderboardPageSize);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Previous Page") && m_leaderboardReady)
            {
                if (m_currentLeaderboardPage > 0)
                {
                    m_leaderboardReady = false;
                    m_currentLeaderboardPage--;
                    GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetLeaderboardPage(m_currentLeaderboardID, m_currentLeaderboardPage * m_leaderboardPageSize, m_currentLeaderboardPage * m_leaderboardPageSize + m_leaderboardPageSize);
                }
            }


            string totalPages = "";
            if (leaderboardData == null)
                totalPages = "1";
            else
                totalPages = (Mathf.CeilToInt(float.Parse(leaderboardData["leaderboardSize"].ToString()) / (float)m_leaderboardPageSize)).ToString();


            GUILayout.Label("Page " + (m_currentLeaderboardPage + 1) + "/" + totalPages);

            if (GUILayout.Button("Next Page") && m_leaderboardReady)
            {
                if ((int.Parse(leaderboardData["leaderboardSize"].ToString()) > m_currentLeaderboardPage * m_leaderboardPageSize + m_leaderboardPageSize))
                {
                    m_leaderboardReady = false;
                    m_currentLeaderboardPage++;
                    GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().GetLeaderboardPage(m_currentLeaderboardID, m_currentLeaderboardPage * m_leaderboardPageSize, m_currentLeaderboardPage * m_leaderboardPageSize + m_leaderboardPageSize - 1);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            GUI.Box(new Rect(m_windowRect.width / 2 - 240, 80, 403, m_windowRect.height - 110), "");

            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, GUILayout.Width(400), GUILayout.Height(m_windowRect.height - 120));
            if (m_leaderboardReady)
            {
                int players = m_currentLeaderboardPage * m_leaderboardPageSize;
                if (players > int.Parse(leaderboardData["leaderboardSize"].ToString()))
                {
                    players = int.Parse(leaderboardData["leaderboardSize"].ToString());
                }

                int maxPlayers = int.Parse(leaderboardData["leaderboardSize"].ToString());

                int loopCount = maxPlayers - players;

                if (loopCount > m_leaderboardPageSize)
                {
                    loopCount = m_leaderboardPageSize;
                }

                for (int i = 0; i < loopCount; i++)
                {
                    string scoreType = "Kills";
                    if (m_currentLeaderboardID == "KDR")
                    {
                        scoreType = "Planes Destroyed: ";
                    }
                    else
                    {
                        scoreType = "Weakpoints Destroyed: ";
                    }
                    string playerInfo = "";
                    if (leaderboardData["leaderboard"][i] == null)
                    {
                        Debug.Log(i + " " + loopCount + " " + maxPlayers);
                    }
                    playerInfo = leaderboardData["leaderboard"][i]["rank"].ToString() + ": " + leaderboardData["leaderboard"][i]["name"].ToString() + " -- " + scoreType + (Mathf.Floor(float.Parse(leaderboardData["leaderboard"][i]["score"].ToString()) / 10000) + 1);
                    GUILayout.Button(playerInfo, GUILayout.Width(370), GUILayout.Height(42));
                }
                if (maxPlayers == 0)
                {
                    GUILayout.Label("No entries found...");
                }
            }
            else
            {
                GUILayout.Label("Please wait...");
            }

            GUILayout.EndScrollView();

            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }

        void OnPhotonJoinRoomFailed()
        {
            m_state = eMatchmakingState.GAME_STATE_SHOW_ROOMS;
            GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("Could not join room!", "Error");
        }

        void OnJoinedRoom()
        {
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
                
                GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("There's already a room named " + aName + "!", "Error");
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

            m_state = eMatchmakingState.GAME_STATE_CREATE_NEW_ROOM;
            PhotonNetwork.CreateRoom(aName, aOptions, TypedLobby.Default);

        }
    }
}