﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using BrainCloudPhotonExample.Connection;
using BrainCloudPhotonExample.Game.PlayerInput;

namespace BrainCloudPhotonExample.Game
{
    public class GameManager : Photon.MonoBehaviour
    {

        private enum eGameState
        {
            GAME_STATE_INITIALIZE_GAME,
            GAME_STATE_WAITING_FOR_PLAYERS,
            GAME_STATE_STARTING_GAME,
            GAME_STATE_SPAWN_PLAYERS,
            GAME_STATE_PLAYING_GAME,
            GAME_STATE_GAME_OVER,
            GAME_STATE_CLOSING_ROOM,
            GAME_STATE_RESETTING_GAME,
            GAME_STATE_SPECTATING
        }

        class KillMessage
        {
            public Color m_color = Color.white;
            public string m_message = "";

            public KillMessage(string aMessage, Color aColor)
            {
                m_color = aColor;
                m_message = aMessage;
            }
        }

        private eGameState m_gameState = eGameState.GAME_STATE_WAITING_FOR_PLAYERS;

        private ExitGames.Client.Photon.Hashtable m_playerProperties = null;
        private ExitGames.Client.Photon.Hashtable m_roomProperties = null;
        private Room m_room = null;

        private GUISkin m_skin;

        private bool m_showScores = false;

        private int m_respawnTime = 3;

        private List<BulletController.BulletInfo> m_spawnedBullets;
        private List<BombController.BombInfo> m_spawnedBombs;

        private List<KillMessage> m_killMessages;

        [SerializeField]
        private float m_gameTime = 10 * 60;

        private int m_mapLayout = 0;
        private int m_mapSize = 1;

        private List<MapPresets.Preset> m_mapPresets;
        private List<MapPresets.MapSize> m_mapSizes;
        //private bool m_showPresetList = false;
        //private bool m_showSizeList = false;
        //private int m_presetListSelection = 0;
        //private int m_sizeListSelection = 1;

        private float m_currentRespawnTime = 0;

        private float m_team1Score = 0;
        private float m_team2Score = 0;
        private int m_shotsFired = 0;
        private int m_bombsDropped = 0;
        private int m_bombsHit = 0;
        private int m_planesDestroyed = 0;
        private int m_carriersDestroyed = 0;
        private int m_timesDestroyed = 0;

        private bool m_once = true;
        private bool m_isRespawning = false;
        private bool m_showKillDialog = false;
        private float m_killDialogTimer = 0;
        private Color m_killDialogColor = Color.clear;

        [SerializeField]
        private Collider m_team1SpawnBounds;

        [SerializeField]
        private Collider m_team2SpawnBounds;

        private List<BombPickup> m_bombPickupsSpawned;
        private int m_bombID;

        private bool m_playerIsReady = false;
        private List<ShipController> m_spawnedShips;

        private GameObject m_lobbyWindow;
        private GameObject m_gameStartButton;

        private GameObject m_resultsWindow;
        private GameObject m_greenLogo;
        private GameObject m_redLogo;
        private GameObject m_enemyWinText;
        private GameObject m_allyWinText;
        private GameObject m_resetButton;
        private GameObject m_quitButton;
        private GameObject m_greenChevron;
        private GameObject m_redChevron;

        void Awake()
        {
            m_greenChevron = GameObject.Find("Team Green Score").transform.FindChild("Chevron").gameObject;
            m_redChevron = GameObject.Find("Team Red Score").transform.FindChild("Chevron").gameObject;
            m_greenLogo = GameObject.Find("Green Logo");
            m_greenLogo.SetActive(false);
            m_redLogo = GameObject.Find("Red Logo");
            m_redLogo.SetActive(false);
            m_enemyWinText = GameObject.Find("Window Title - Loss");
            m_allyWinText = GameObject.Find("Window Title - Win");
            m_resetButton = GameObject.Find("Continue");
            m_quitButton = GameObject.Find("ResultsQuit");
            m_lobbyWindow = GameObject.Find("Lobby");
            m_gameStartButton = GameObject.Find("StartGame");
            m_gameTime = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_defaultGameTime;
            m_mapPresets = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_presets;
            m_mapSizes = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_mapSizes;
            m_resultsWindow = GameObject.Find("Results");
            m_resultsWindow.SetActive(false);
            
        }

        void Start()
        {
            
            m_mapLayout = (int)PhotonNetwork.room.customProperties["MapLayout"];
            m_mapSize = (int)PhotonNetwork.room.customProperties["MapSize"];
            
            if (PhotonNetwork.room.customProperties["LightPosition"] != null) SetLightPosition((int)PhotonNetwork.room.customProperties["LightPosition"]);
            m_spawnedShips = new List<ShipController>();
            m_bombPickupsSpawned = new List<BombPickup>();
            
            StartCoroutine("LoadBackground");
            m_killMessages = new List<KillMessage>();
            m_spawnedBullets = new List<BulletController.BulletInfo>();
            m_spawnedBombs = new List<BombController.BombInfo>();
            m_skin = (GUISkin)Resources.Load("skin");
            m_room = PhotonNetwork.room;
            m_playerProperties = new ExitGames.Client.Photon.Hashtable();
            StartCoroutine("UpdatePing");
            StartCoroutine("UpdateRoomDisplayName");
            m_roomProperties = PhotonNetwork.room.customProperties;
            m_playerProperties["Team"] = 0;
            
            m_team1Score = 0;
            m_team2Score = 0;


            if ((int)m_roomProperties["IsPlaying"] == 1)
            {
                m_gameState = eGameState.GAME_STATE_SPECTATING;
                m_roomProperties["Spectators"] = (int)PhotonNetwork.room.customProperties["Spectators"] + 1;
            }
            else
            {
                if (PhotonNetwork.isMasterClient)
                {
                    m_roomProperties["lastBulletID"] = -1;
                    m_roomProperties["lastBombID"] = -1;
                    m_roomProperties["GameTime"] = m_gameTime;
                    m_roomProperties["BombID"] = 0;
                    m_roomProperties["LightPosition"] = Random.Range(1, 5);
                    m_roomProperties["Team1Score"] = 0;
                    m_roomProperties["Team2Score"] = 0;
                    SetLightPosition((int)m_roomProperties["LightPosition"]);
                }


                if (PhotonNetwork.room.customProperties["Team1Players"] == null || PhotonNetwork.room.customProperties["Team2Players"] == null)
                {
                    m_roomProperties["Team1Players"] = 0;
                    m_roomProperties["Team2Players"] = 0;
                    m_roomProperties["Spectators"] = 0;
                }

                if ((int)PhotonNetwork.room.customProperties["Team2Players"] < (int)PhotonNetwork.room.customProperties["Team1Players"])
                {
                    m_playerProperties["Team"] = 2;
                    m_roomProperties["Team2Players"] = (int)PhotonNetwork.room.customProperties["Team2Players"] + 1;
                }
                else
                {
                    m_playerProperties["Team"] = 1;
                    m_roomProperties["Team1Players"] = (int)PhotonNetwork.room.customProperties["Team1Players"] + 1;
                }
            }
            m_playerProperties["Score"] = 0;
            PhotonNetwork.player.SetCustomProperties(m_playerProperties);
            PhotonNetwork.room.SetCustomProperties(m_roomProperties);
        }

        [RPC]
        void SetLightPosition(int aPosition)
        {
            Vector3 position = Vector3.zero;
            switch (aPosition)
            {
                case 1:
                    position = new Vector3(330, 0, 0);
                    break;
                case 2:
                    position = new Vector3(354, 34, 0);
                    break;
                case 3:
                    position = new Vector3(10, 325, 0);
                    break;
                case 4:
                    position = new Vector3(30, 0, 0);
                    break;
            }
            StopCoroutine("MoveLight");
            StartCoroutine("MoveLight", position);
        }

        IEnumerator MoveLight(Vector3 aPosition)
        {
            bool done = false;
            int count = 0;
            while (!done)
            {
                GameObject.Find("Directional Light").transform.rotation = Quaternion.Slerp(GameObject.Find("Directional Light").transform.rotation, Quaternion.Euler(aPosition), 5 * Time.deltaTime);
                count++;
                if (count > 10000)
                {
                    GameObject.Find("Directional Light").transform.rotation = Quaternion.Euler(aPosition);
                    done = true;
                }
                yield return new WaitForSeconds(0.02f);
            }
        }

        IEnumerator LoadBackground()
        {
            AsyncOperation async = Application.LoadLevelAdditiveAsync("Background");
            yield return async;
        }

        public void OnPhotonSerializeView(PhotonStream aStream, PhotonMessageInfo aInfo)
        {

        }

        void OnApplicationQuit()
        {
            m_playerProperties.Clear();
            PhotonNetwork.player.SetCustomProperties(m_playerProperties);
            PhotonNetwork.Disconnect();
        }

        public void LeaveRoom()
        {
            m_playerProperties.Clear();
            PhotonNetwork.player.SetCustomProperties(m_playerProperties);
            PhotonNetwork.LeaveRoom();
        }

        public void OnLeftRoom()
        {
            PhotonNetwork.LoadLevel("Matchmaking");
        }

        public void ForceStartGame()
        {
            m_gameState = eGameState.GAME_STATE_STARTING_GAME;
        }

        public void ReturnToWaitingRoom()
        {
            if (m_gameState == eGameState.GAME_STATE_GAME_OVER)
            {
                ResetGame();
            }
        }

        void OnMasterClientSwitched(PhotonPlayer newMasterClient)
        {
            PhotonNetwork.LeaveRoom();
            GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("The host has left the game!", "Disconnected");
            m_playerProperties.Clear();
            PhotonNetwork.player.SetCustomProperties(m_playerProperties);
        }

        void OnGUI()
        {
            GUI.skin = m_skin;

            switch (m_gameState)
            {
                case eGameState.GAME_STATE_WAITING_FOR_PLAYERS:
                case eGameState.GAME_STATE_STARTING_GAME:
                    m_lobbyWindow.gameObject.SetActive(true);
                    m_resultsWindow.gameObject.SetActive(false);
                    OnWaitingForPlayersWindow();
                    //GUILayout.Window(40, new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height), OnWaitingForPlayersWindow, m_room.name + " -- Waiting for Players " + m_room.playerCount + "/" + m_room.maxPlayers + "...");
                    //GUILayout.Window(40, new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height), OnWaitingForPlayersWindow, m_room.name + " -- Starting game...");
                    break;
                case eGameState.GAME_STATE_SPECTATING:
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_resultsWindow.gameObject.SetActive(false);
                    GUI.Label(new Rect(Screen.width / 2 - 100, 20, 200, 20), "Spectating");
                    break;
                case eGameState.GAME_STATE_GAME_OVER:
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_resultsWindow.gameObject.SetActive(true);
                    OnScoresWindow();
                    break;
                default:
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_resultsWindow.gameObject.SetActive(false);
                    break;
            }

            if (m_showScores)
            {

                //GUILayout.Window(41, new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height), OnScoresWindow, "Scoreboard - " + Mathf.FloorToInt(m_gameTime / 60) + ":" + Mathf.FloorToInt((m_gameTime % 60) / 10) + "" + Mathf.FloorToInt((m_gameTime % 10)));
            }

            if (m_isRespawning)
            {
                GUI.Box(new Rect(Screen.width / 2 - 70, Screen.height / 2 - 15, 140, 30), "");
                GUI.Label(new Rect(Screen.width / 2 - 60, Screen.height / 2 - 15, 120, 30), "Respawning in " + Mathf.CeilToInt(m_currentRespawnTime));
            }

            if (m_showKillDialog)
            {
                GUI.skin.label.normal.textColor = m_killDialogColor;
                for (int i = 0; i < m_killMessages.Count; i++)
                {
                    Color oldColor = GUI.skin.label.normal.textColor;
                    GUI.skin.label.normal.textColor = m_killMessages[i].m_color;
                    GUI.Label(new Rect(Screen.width - 200, 25 * (i + 1), 200, 20), m_killMessages[i].m_message);
                    GUI.skin.label.normal.textColor = oldColor;
                }
                GUI.skin.label.normal.textColor = Color.white;
            }
        }

        void OnScoresWindow()
        {

            GameObject team = GameObject.Find("Team Green Score");
            team.transform.FindChild("Team Score").GetComponent<Text>().text = m_team1Score.ToString("n0");
            team = GameObject.Find("Team Red Score");
            team.transform.FindChild("Team Score").GetComponent<Text>().text = m_team2Score.ToString("n0");

            // m_team1Score = (float)PhotonNetwork.room.customProperties["Team1Score"];
            //m_team2Score = (float)PhotonNetwork.room.customProperties["Team2Score"];
            if (m_gameState != eGameState.GAME_STATE_GAME_OVER)
            {
                m_quitButton.SetActive(false);
                m_resetButton.SetActive(false);
            }
            else if (!PhotonNetwork.isMasterClient)
            {
                m_quitButton.SetActive(false);
                m_resetButton.SetActive(true);
            }
            else
            {
                m_quitButton.SetActive(true);
                m_resetButton.SetActive(true);
            }

            if (m_gameState == eGameState.GAME_STATE_GAME_OVER)
            {
                if (m_team1Score > m_team2Score)
                {
                    m_greenLogo.SetActive(true);
                    m_redLogo.SetActive(false);
                    if ((int)PhotonNetwork.player.customProperties["Team"] == 1)
                    {
                        m_allyWinText.SetActive(true);
                        m_enemyWinText.SetActive(false);
                    }
                    else if ((int)PhotonNetwork.player.customProperties["Team"] == 2)
                    {
                        m_allyWinText.SetActive(false);
                        m_enemyWinText.SetActive(true);
                    }
                }
                else
                {
                    m_greenLogo.SetActive(false);
                    m_redLogo.SetActive(true);

                    if ((int)PhotonNetwork.player.customProperties["Team"] == 1)
                    {
                        m_allyWinText.SetActive(false);
                        m_enemyWinText.SetActive(true);
                    }
                    else if ((int)PhotonNetwork.player.customProperties["Team"] == 2)
                    {
                        m_allyWinText.SetActive(true);
                        m_enemyWinText.SetActive(false);
                    }
                }
            }
            else
            {
                m_greenLogo.SetActive(false);
                m_redLogo.SetActive(false);
                m_allyWinText.SetActive(false);
                m_enemyWinText.SetActive(false);
            }

            PhotonPlayer[] playerList = PhotonNetwork.playerList;
            List<PhotonPlayer> playerListList = new List<PhotonPlayer>();
            for (int i = 0; i < playerList.Length; i++)
            {
                playerListList.Add(playerList[i]);
            }

            int count = 0;
            while (count < playerListList.Count)
            {
                if ((int)playerListList[count].customProperties["Team"] == 0)
                {
                    playerListList.RemoveAt(count);
                }
                else
                {
                    count++;
                }
            }
            playerList = playerListList.ToArray().OrderByDescending(x => (int)x.customProperties["Score"]).ToArray();

            string greenNamesText = "";
            string greenKDText = "";
            string greenScoreText = "";

            string redNamesText = "";
            string redKDText = "";
            string redScoreText = "";

                    //default 21.8
        //17.7f per line
            int greenPlayers = 0;
            int redPlayers = 0;
            for (int i = 0; i < playerList.Length; i++)
            {
                if ((int)playerList[i].customProperties["Team"] == 1)
                {
                    if (playerList[i] == PhotonNetwork.player)
                    {
                        m_redChevron.SetActive(false);
                        m_greenChevron.SetActive(true);
                        m_greenChevron.transform.GetChild(0).GetComponent<Text>().text = PhotonNetwork.player.customProperties["RoomDisplayName"].ToString();
                        m_greenChevron.transform.GetChild(1).GetComponent<Text>().text = PhotonNetwork.player.customProperties["Kills"].ToString() + "/" + PhotonNetwork.player.customProperties["Deaths"].ToString();
                        m_greenChevron.transform.GetChild(2).GetComponent<Text>().text = ((int)PhotonNetwork.player.customProperties["Score"]).ToString("n0");
                        m_greenChevron.GetComponent<RectTransform>().localPosition = new Vector3(m_greenChevron.GetComponent<RectTransform>().localPosition.x, 21.8f + (greenPlayers * 17.7f), m_greenChevron.GetComponent<RectTransform>().localPosition.z);
                        greenNamesText += "\n";
                        greenKDText += "\n";
                        greenScoreText += "\n";
                    }
                    else
                    {
                        greenNamesText += playerList[i].customProperties["RoomDisplayName"].ToString() + "\n";
                        greenKDText += PhotonNetwork.player.customProperties["Kills"].ToString() + "/" + PhotonNetwork.player.customProperties["Deaths"].ToString() + "\n";
                        greenScoreText += ((int)PhotonNetwork.player.customProperties["Score"]).ToString("n0") + "\n";
                    }
                    greenPlayers++;
                }
                else
                {
                    if (playerList[i] == PhotonNetwork.player)
                    {
                        m_redChevron.SetActive(true);
                        m_greenChevron.SetActive(false);
                        m_redChevron.transform.GetChild(0).GetComponent<Text>().text = PhotonNetwork.player.customProperties["RoomDisplayName"].ToString();
                        m_redChevron.transform.GetChild(1).GetComponent<Text>().text = PhotonNetwork.player.customProperties["Kills"].ToString() + "/" + PhotonNetwork.player.customProperties["Deaths"].ToString();
                        m_redChevron.transform.GetChild(2).GetComponent<Text>().text = ((int)PhotonNetwork.player.customProperties["Score"]).ToString("n0");
                        m_redChevron.GetComponent<RectTransform>().localPosition = new Vector3(m_redChevron.GetComponent<RectTransform>().localPosition.x, 21.8f + (redPlayers * 17.7f), m_redChevron.GetComponent<RectTransform>().localPosition.z);

                        redNamesText += "\n";
                        redKDText += "\n";
                        redScoreText += "\n";
                    }
                    else
                    {
                        redNamesText += playerList[i].customProperties["RoomDisplayName"].ToString() + "\n";
                        redKDText += PhotonNetwork.player.customProperties["Kills"].ToString() + "/" + PhotonNetwork.player.customProperties["Deaths"].ToString() + "\n";
                        redScoreText += ((int)PhotonNetwork.player.customProperties["Score"]).ToString("n0") + "\n";
                    }
                    redPlayers++;
                }
            }

            team = GameObject.Find("Team Green Score");
            team.transform.FindChild("Players").GetComponent<Text>().text = greenNamesText;
            team.transform.FindChild("PlayerKD").GetComponent<Text>().text = greenKDText;
            team.transform.FindChild("PlayerScores").GetComponent<Text>().text = greenScoreText;
            team = GameObject.Find("Team Red Score");
            team.transform.FindChild("Players").GetComponent<Text>().text = redNamesText;
            team.transform.FindChild("PlayerKD").GetComponent<Text>().text = redKDText;
            team.transform.FindChild("PlayerScores").GetComponent<Text>().text = redScoreText;

            /*

            GUILayout.BeginScrollView(Vector2.zero);
            GUI.skin.label.normal.textColor = Color.green;
            GUILayout.Label(m_team1Score.ToString());
            GUI.skin.label.normal.textColor = Color.white;
            GUILayout.BeginHorizontal();
            for (int i = 0; i < m_room.maxPlayers; i++)
            {
                if (i < playerList.Length && (int)playerList[i].customProperties["Team"] == 1)
                {

                    GUI.skin.label.normal.textColor = Color.green;
                    if (playerList[i].customProperties["RoomDisplayName"] != null) GUILayout.Label(playerList[i].customProperties["RoomDisplayName"].ToString());
                    GUI.skin.label.normal.textColor = Color.white;

                    GUILayout.Label("K " + playerList[i].customProperties["Kills"].ToString() + "/" + playerList[i].customProperties["Deaths"].ToString() + " D");
                    if (playerList[i].customProperties["Ping"] != null) GUILayout.Label(playerList[i].customProperties["Ping"].ToString());

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.BeginScrollView(Vector2.zero);
            GUI.skin.label.normal.textColor = Color.red;
            GUILayout.Label(m_team2Score.ToString());
            GUI.skin.label.normal.textColor = Color.white;
            GUILayout.BeginHorizontal();

            for (int i = 0; i < m_room.maxPlayers; i++)
            {
                if (i < playerList.Length && (int)playerList[i].customProperties["Team"] == 2)
                {

                    GUI.skin.label.normal.textColor = Color.red;
                    if (playerList[i].customProperties["RoomDisplayName"] != null) GUILayout.Label(playerList[i].customProperties["RoomDisplayName"].ToString());
                    GUI.skin.label.normal.textColor = Color.white;

                    GUILayout.Label("K " + playerList[i].customProperties["Kills"].ToString() + "/" + playerList[i].customProperties["Deaths"].ToString() + " D");
                    if (playerList[i].customProperties["Ping"] != null) GUILayout.Label(playerList[i].customProperties["Ping"].ToString());

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
             * */
        }

        public void ChangeTeam()
        {
            if ((int)PhotonNetwork.player.customProperties["Team"] != 1)
            {
                m_roomProperties = PhotonNetwork.room.customProperties;

                if ((int)PhotonNetwork.player.customProperties["Team"] == 2)
                {
                    if (PhotonNetwork.room != null)
                        m_roomProperties["Team2Players"] = (int)PhotonNetwork.room.customProperties["Team2Players"] - 1;
                }
                else
                {
                    if (PhotonNetwork.room != null)
                        m_roomProperties["Spectators"] = (int)PhotonNetwork.room.customProperties["Spectators"] - 1;
                }

                if (PhotonNetwork.room != null)
                    m_roomProperties["Team1Players"] = (int)PhotonNetwork.room.customProperties["Team1Players"] + 1;
                m_playerProperties["Team"] = 1;
                PhotonNetwork.player.SetCustomProperties(m_playerProperties);
                PhotonNetwork.room.SetCustomProperties(m_roomProperties);
            }
            else if ((int)PhotonNetwork.player.customProperties["Team"] != 2)
            {
                m_roomProperties = PhotonNetwork.room.customProperties;

                if ((int)PhotonNetwork.player.customProperties["Team"] == 1)
                {
                    if (PhotonNetwork.room != null)
                        m_roomProperties["Team1Players"] = (int)PhotonNetwork.room.customProperties["Team1Players"] - 1;
                }
                else
                {
                    if (PhotonNetwork.room != null)
                        m_roomProperties["Spectators"] = (int)PhotonNetwork.room.customProperties["Spectators"] - 1;
                }

                if (PhotonNetwork.room != null)
                    m_roomProperties["Team2Players"] = (int)PhotonNetwork.room.customProperties["Team2Players"] + 1;
                m_playerProperties["Team"] = 2;
                PhotonNetwork.player.SetCustomProperties(m_playerProperties);
                PhotonNetwork.room.SetCustomProperties(m_roomProperties);
            }
        }

        void OnWaitingForPlayersWindow()
        {
            if (PhotonNetwork.room == null) return;
            PhotonPlayer[] playerList = PhotonNetwork.playerList.OrderBy(x => x.ID).ToArray();
            List<PhotonPlayer> greenPlayers = new List<PhotonPlayer>();
            List<PhotonPlayer> redPlayers = new List<PhotonPlayer>();

            for (int i = 0; i < playerList.Length; i++)
            {
                if (playerList[i].customProperties["Team"] != null)
                {
                    if ((int)playerList[i].customProperties["Team"] == 1)
                    {
                        greenPlayers.Add(playerList[i]);
                    }
                    else if ((int)playerList[i].customProperties["Team"] == 2)
                    {
                        redPlayers.Add(playerList[i]);
                    }
                }
            }

            Text teamText = GameObject.Find("GreenPlayerNames").GetComponent<Text>();
            Text teamPingText = GameObject.Find("GreenPings").GetComponent<Text>();
            string nameText = "";
            string pingText = "";
            for (int i = 0; i < greenPlayers.Count; i++)
            {
                nameText += greenPlayers[i].customProperties["RoomDisplayName"] + "\n";
                pingText += greenPlayers[i].customProperties["Ping"] + "\n";
            }


            teamText.text = nameText;
            teamPingText.text = pingText;
            teamText = GameObject.Find("RedPlayerNames").GetComponent<Text>();
            teamPingText = GameObject.Find("RedPings").GetComponent<Text>();
            nameText = "";
            pingText = "";

            for (int i = 0; i < redPlayers.Count; i++)
            {
                nameText += redPlayers[i].customProperties["RoomDisplayName"] + "\n";
                pingText += redPlayers[i].customProperties["Ping"] + "\n";
            }
            teamText.text = nameText;
            teamPingText.text = pingText;

            GameObject.Find("GreenPlayers").GetComponent<Text>().text = greenPlayers.Count + "/" + Mathf.Floor(PhotonNetwork.room.maxPlayers / 2.0f);
            GameObject.Find("RedPlayers").GetComponent<Text>().text = redPlayers.Count + "/" + Mathf.Floor(PhotonNetwork.room.maxPlayers / 2.0f);
            GameObject.Find("GameName").GetComponent<Text>().text = PhotonNetwork.room.name;

            if (!PhotonNetwork.isMasterClient || m_gameState != eGameState.GAME_STATE_WAITING_FOR_PLAYERS)
            {
                m_gameStartButton.SetActive(false);

                if (m_gameState == eGameState.GAME_STATE_WAITING_FOR_PLAYERS)
                    GameObject.Find("ChangeTeam").transform.position = m_gameStartButton.transform.position;
            }
            else if (m_gameState == eGameState.GAME_STATE_WAITING_FOR_PLAYERS)
            {
                m_gameStartButton.SetActive(true);
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

            if (PhotonNetwork.isMasterClient && m_gameState == eGameState.GAME_STATE_WAITING_FOR_PLAYERS)
            {
                if (GUILayout.Button("Start game", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    ForceStartGame();
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

            }
            else if (!PhotonNetwork.isMasterClient)
            {
                GUI.Button(new Rect(Screen.width / 4 - 80, 25, 100, 25), presetList[m_mapLayout]);

                GUI.Button(new Rect(Screen.width / 4 + 60, 25, 100, 25), sizeList[m_mapSize]);
            }

            */
        }

        [RPC]
        void ChangeMapLayout(int aLayout)
        {
            m_mapLayout = aLayout;
        }

        [RPC]
        void ChangeMapSize(int aSize)
        {
            m_mapSize = aSize;
        }

        IEnumerator UpdatePing()
        {
            while (m_gameState != eGameState.GAME_STATE_CLOSING_ROOM)
            {
                m_playerProperties["Ping"] = PhotonNetwork.GetPing();
                PhotonNetwork.player.SetCustomProperties(m_playerProperties);
                yield return new WaitForSeconds(1);
            }
        }

        IEnumerator UpdateRoomDisplayName()
        {
            List<string> otherNames = new List<string>();
            while (true)
            {
                otherNames.Clear();
                foreach (PhotonPlayer player in PhotonNetwork.playerList)
                {
                    if (player != PhotonNetwork.player && player.customProperties["RoomDisplayName"] != null)
                    {
                        otherNames.Add(player.customProperties["RoomDisplayName"].ToString());
                    }
                    else if (player != PhotonNetwork.player && player.customProperties["RoomDisplayName"] != null)
                    {
                        otherNames.Add(player.name);
                    }
                }

                int count = 1;
                string displayName = PhotonNetwork.player.name;
                while (otherNames.Contains(displayName))
                {
                    displayName = PhotonNetwork.player.name + "(" + count + ")";
                    count++;
                }

                if (PhotonNetwork.player.customProperties["RoomDisplayName"] == null)
                {
                    m_playerProperties["RoomDisplayName"] = displayName;
                    PhotonNetwork.player.SetCustomProperties(m_playerProperties);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        IEnumerator RespawnPlayer()
        {
            m_currentRespawnTime = (float)m_respawnTime;
            while (m_currentRespawnTime > 0)
            {
                yield return new WaitForSeconds(0.1f);
                m_currentRespawnTime -= 0.1f;
            }

            if (m_currentRespawnTime < 0)
            {
                m_currentRespawnTime = 0;
            }

            if (m_gameState == eGameState.GAME_STATE_PLAYING_GAME)
            {
                Vector3 spawnPoint = Vector3.zero;
                spawnPoint.z = 22;

                if ((int)PhotonNetwork.player.customProperties["Team"] == 1)
                {
                    spawnPoint.x = Random.Range(m_team1SpawnBounds.bounds.center.x - m_team1SpawnBounds.bounds.size.x / 2, m_team1SpawnBounds.bounds.center.x + m_team1SpawnBounds.bounds.size.x / 2);
                    spawnPoint.y = Random.Range(m_team1SpawnBounds.bounds.center.y - m_team1SpawnBounds.bounds.size.y / 2, m_team1SpawnBounds.bounds.center.y + m_team1SpawnBounds.bounds.size.y / 2);
                }
                else if ((int)PhotonNetwork.player.customProperties["Team"] == 2)
                {
                    spawnPoint.x = Random.Range(m_team2SpawnBounds.bounds.center.x - m_team2SpawnBounds.bounds.size.x / 2, m_team2SpawnBounds.bounds.center.x + m_team2SpawnBounds.bounds.size.x / 2);
                    spawnPoint.y = Random.Range(m_team2SpawnBounds.bounds.center.y - m_team2SpawnBounds.bounds.size.y / 2, m_team2SpawnBounds.bounds.center.y + m_team2SpawnBounds.bounds.size.y / 2);
                }

                GameObject playerPlane = PhotonNetwork.Instantiate("Plane", spawnPoint, Quaternion.LookRotation(Vector3.forward, (Vector3.zero - spawnPoint)), 0);

                if ((int)PhotonNetwork.player.customProperties["Team"] == 1)
                {
                    playerPlane.layer = 8;
                }
                else if ((int)PhotonNetwork.player.customProperties["Team"] == 2)
                {
                    playerPlane.layer = 9;
                }
                GameObject.Find("PlayerController").GetComponent<PlayerController>().SetPlayerPlane(playerPlane.GetComponent<PlaneController>());
                playerPlane.GetComponent<Rigidbody>().isKinematic = false;
                m_isRespawning = false;
            }
            else
            {
                m_isRespawning = false;
            }
        }

        void Update()
        {

            if (m_killDialogTimer > 0)
            {
                m_killDialogTimer -= Time.deltaTime;
                if (m_killDialogTimer <= 0)
                {
                    m_killDialogTimer = 0;
                    StartCoroutine("FadeKillDialog");
                }
            }

            if ((Input.GetKey(KeyCode.Tab) && (m_gameState == eGameState.GAME_STATE_PLAYING_GAME || m_gameState == eGameState.GAME_STATE_SPECTATING)) || m_gameState == eGameState.GAME_STATE_GAME_OVER)
            {
                m_showScores = true;
            }
            else
            {
                m_showScores = false;
            }

            switch (m_gameState)
            {
                case eGameState.GAME_STATE_WAITING_FOR_PLAYERS:
                    if (m_room.playerCount == m_room.maxPlayers)
                    {
                        m_gameState = eGameState.GAME_STATE_STARTING_GAME;
                    }

                    if (GameObject.Find("BackgroundMusic").GetComponent<AudioSource>().isPlaying)
                    {
                        GameObject.Find("BackgroundMusic").GetComponent<AudioSource>().Stop();
                    }
                    Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(0, 0, -90), Time.deltaTime);
                    break;

                case eGameState.GAME_STATE_STARTING_GAME:

                    if (PhotonNetwork.isMasterClient && !m_playerIsReady)
                    {
                        m_roomProperties = PhotonNetwork.room.customProperties;
                        m_roomProperties["IsPlaying"] = 1;
                        PhotonNetwork.room.SetCustomProperties(m_roomProperties);

                        m_playerIsReady = true;
                        GetComponent<PhotonView>().RPC("GetReady", PhotonTargets.All);

                        int shipID = 0;
                        bool done = false;
                        GameObject ship = null;
                        int shipIndex = 0;
                        int tryCount = 0;

                        MapPresets.MapSize mapSize = m_mapSizes[m_mapSize];
                        GameObject mapBound = GameObject.Find("MapBounds");
                        mapBound.transform.localScale = new Vector3(mapSize.m_horizontalSize, 1, mapSize.m_verticalSize);
                        GameObject spawn = GameObject.Find("Team1Spawn");
                        spawn.transform.position = new Vector3(mapBound.GetComponent<Collider>().bounds.min.x, 0, 22);
                        spawn.transform.localScale = new Vector3(5, 1, mapSize.m_verticalSize * 0.75f);
                        spawn = GameObject.Find("Team2Spawn");
                        spawn.transform.position = new Vector3(mapBound.GetComponent<Collider>().bounds.max.x, 0, 22);
                        spawn.transform.localScale = new Vector3(5, 1, mapSize.m_verticalSize * 0.75f);

                        if (m_mapLayout == 0)
                        {
                            GameObject testShip = (GameObject)Instantiate((GameObject)Resources.Load("Carrier01"), Vector3.zero, Quaternion.identity);
                            while (!done)
                            {
                                tryCount = 0;
                                bool positionFound = false;
                                Vector3 position = new Vector3(0, 0, 122);

                                while (!positionFound)
                                {
                                    position.x = Random.Range(-340.0f, 340.0f);
                                    position.y = Random.Range(-220.0f, 220.0f);

                                    float minDistance = 10000;
                                    for (int i = 0; i < m_spawnedShips.Count; i++)
                                    {
                                        if ((m_spawnedShips[i].transform.position - position).magnitude < minDistance)
                                        {
                                            minDistance = (m_spawnedShips[i].transform.position - position).magnitude;
                                        }
                                    }

                                    if (minDistance > 170)
                                    {
                                        positionFound = true;
                                    }
                                    tryCount++;
                                    if (tryCount > 100000)
                                    {
                                        positionFound = true;
                                    }
                                }

                                float rotation = 0;
                                tryCount = 0;
                                bool done2 = false;
                                testShip.transform.position = position;
                                while (!done2)
                                {
                                    rotation = Random.Range(0.0f, 360.0f);
                                    testShip.transform.rotation = Quaternion.Euler(0, 0, rotation);

                                    bool collides = false;
                                    for (int i = 0; i < m_spawnedShips.Count; i++)
                                    {
                                        if (testShip.transform.FindChild("Graphic").GetComponent<Collider>().bounds.Intersects(m_spawnedShips[i].transform.FindChild("ShipGraphic").GetChild(0).FindChild("Graphic").GetComponent<Collider>().bounds))
                                        {
                                            collides = true;
                                            break;
                                        }
                                    }

                                    if (!collides)
                                    {
                                        done2 = true;
                                    }
                                    tryCount++;
                                    if (tryCount > 1000)
                                    {
                                        done2 = true;
                                    }

                                }

                                ship = PhotonNetwork.Instantiate("Ship", position, Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f)), 0);
                                switch (shipIndex)
                                {
                                    case 0:
                                        ship.GetComponent<ShipController>().SetShipType(ShipController.eShipType.SHIP_TYPE_CARRIER, (shipID % 2) + 1, shipID);
                                        break;
                                    case 1:
                                        ship.GetComponent<ShipController>().SetShipType(ShipController.eShipType.SHIP_TYPE_BATTLESHIP, (shipID % 2) + 1, shipID);
                                        break;
                                    case 2:
                                        ship.GetComponent<ShipController>().SetShipType(ShipController.eShipType.SHIP_TYPE_CRUISER, (shipID % 2) + 1, shipID);
                                        break;
                                    case 3:
                                        ship.GetComponent<ShipController>().SetShipType(ShipController.eShipType.SHIP_TYPE_SUBMARINE, (shipID % 2) + 1, shipID);
                                        break;
                                    case 4:
                                        ship.GetComponent<ShipController>().SetShipType(ShipController.eShipType.SHIP_TYPE_DESTROYER, (shipID % 2) + 1, shipID);
                                        break;
                                }

                                if (shipID % 2 == 1)
                                {
                                    shipIndex++;
                                }
                                shipID++;
                                if (m_spawnedShips.Count >= 10) done = true;
                            }
                            Destroy(testShip);
                        }
                        else
                        {
                            MapPresets.Preset preset = m_mapPresets[m_mapLayout];
                            Bounds mapBounds = GameObject.Find("MapBounds").GetComponent<Collider>().bounds;
                            for (int i = 0; i < preset.m_numShips; i++)
                            {
                                Vector3 position = new Vector3(mapBounds.min.x + (mapBounds.max.x - mapBounds.min.x) * preset.m_ships[i].m_xPositionPercent, mapBounds.min.y + (mapBounds.max.y - mapBounds.min.y) * preset.m_ships[i].m_yPositionPercent, 122);
                                ship = PhotonNetwork.Instantiate("Ship", position, Quaternion.Euler(0, 0, preset.m_ships[i].m_angle), 0);
                                ship.GetComponent<ShipController>().SetShipType(preset.m_ships[i].m_shipType, preset.m_ships[i].m_team, shipID, preset.m_ships[i].m_angle, position, preset.m_ships[i].m_respawnTime, preset.m_ships[i].m_path, preset.m_ships[i].m_pathSpeed);

                                shipID++;
                            }

                        }
                        StartCoroutine("WaitForReadyPlayers");
                    }

                    break;
                case eGameState.GAME_STATE_SPAWN_PLAYERS:
                    if (PhotonNetwork.isMasterClient && m_once)
                    {
                        m_once = false;
                        GetComponent<PhotonView>().RPC("SpawnPlayer", PhotonTargets.All);
                    }

                    break;

                case eGameState.GAME_STATE_PLAYING_GAME:

                    if (!m_once)
                    {
                        GameObject.Find("BackgroundMusic").GetComponent<AudioSource>().Play();
                        GameObject.Find("PlayerController").GetComponent<PlayerController>().SetCamLimits(m_mapSizes[m_mapSize].m_horizontalSize / 2.64f, m_mapSizes[m_mapSize].m_verticalSize / 2.64f);
                        m_once = true;
                    }
                    if (PhotonNetwork.isMasterClient)
                    {
                        m_gameTime -= Time.deltaTime;
                        m_roomProperties = PhotonNetwork.room.customProperties;
                        m_roomProperties["GameTime"] = m_gameTime;
                        PhotonNetwork.room.SetCustomProperties(m_roomProperties);
                        List<ShipController> team1Ships = new List<ShipController>();
                        List<ShipController> team2Ships = new List<ShipController>();
                        for (int i = 0; i < m_spawnedShips.Count; i++)
                        {
                            if (m_spawnedShips[i].m_team == 1)
                            {
                                team1Ships.Add(m_spawnedShips[i]);
                            }
                            else if (m_spawnedShips[i].m_team == 2)
                            {
                                team2Ships.Add(m_spawnedShips[i]);
                            }
                        }
                        bool team1IsDestroyed = true;
                        bool team2IsDestroyed = true;
                        for (int i = 0; i < team1Ships.Count; i++)
                        {
                            if (team1Ships[i].IsAlive())
                            {
                                team1IsDestroyed = false;
                                break;
                            }
                        }

                        for (int i = 0; i < team2Ships.Count; i++)
                        {
                            if (team2Ships[i].IsAlive())
                            {
                                team2IsDestroyed = false;
                                break;
                            }
                        }

                        if (m_gameTime <= 0 || team1IsDestroyed || team2IsDestroyed)
                        {
                            GetComponent<PhotonView>().RPC("EndGame", PhotonTargets.All);
                        }
                    }
                    else
                    {
                        if (PhotonNetwork.room != null)
                            m_gameTime = (float)PhotonNetwork.room.customProperties["GameTime"];
                    }


                    break;

                case eGameState.GAME_STATE_GAME_OVER:
                    if (m_once)
                    {
                        m_once = false;
                        GameObject.Find("PlayerController").GetComponent<PlayerController>().EndGame();
                        if (PhotonNetwork.isMasterClient)
                        {
                            if (m_team1Score > m_team2Score)
                            {
                                AwardExperience(1);
                            }
                            else if (m_team2Score > m_team1Score)
                            {
                                AwardExperience(2);
                            }
                            else
                            {
                                AwardExperience(0);
                            }
                        }
                    }

                    if (PhotonNetwork.isMasterClient)
                    {
                        m_gameTime -= Time.deltaTime;
                        m_roomProperties = PhotonNetwork.room.customProperties;
                        m_roomProperties["GameTime"] = m_gameTime;
                        PhotonNetwork.room.SetCustomProperties(m_roomProperties);


                        if (m_gameTime <= 0)
                        {
                            GetComponent<PhotonView>().RPC("ResetGame", PhotonTargets.All);

                        }
                    }
                    else
                    {
                        if (PhotonNetwork.room != null)
                            m_gameTime = (float)PhotonNetwork.room.customProperties["GameTime"];
                    }
                    break;
                case eGameState.GAME_STATE_SPECTATING:
                    if (PhotonNetwork.room != null)
                        m_gameTime = (float)PhotonNetwork.room.customProperties["GameTime"];
                    break;

            }
            if (!PhotonNetwork.isMasterClient && PhotonNetwork.room != null)
            {
                m_team1Score = (int)PhotonNetwork.room.customProperties["Team1Score"];
                m_team2Score = (int)PhotonNetwork.room.customProperties["Team2Score"];
            }

            if (Input.GetKeyDown("f") && m_gameState == eGameState.GAME_STATE_PLAYING_GAME)
            {
                GetComponent<PhotonView>().RPC("EndGame", PhotonTargets.All);
            }
        }

        public void AddSpawnedShip(ShipController aShip)
        {
            m_spawnedShips.Add(aShip);
        }

        void LateUpdate()
        {
            if (m_gameState == eGameState.GAME_STATE_SPECTATING)
            {

                Vector3 camPos = Camera.main.transform.position;
                GameObject[] planes = GameObject.FindGameObjectsWithTag("Plane");
                Vector3 total = Vector3.zero;
                if (planes.Length > 0)
                {
                    for (int i = 0; i < planes.Length; i++)
                    {
                        total += planes[i].transform.position;
                        planes[i].transform.FindChild("NameTag").GetComponent<TextMesh>().characterSize = 0.14f;
                    }
                    total /= planes.Length;
                }
                else
                {
                    total = new Vector3(0, 0, -180);
                }
                camPos = Vector3.Lerp(new Vector3(0, 0, -180), new Vector3(total.x, total.y, -180), 0.5f);

                if (camPos.x > 160)
                {
                    camPos.x = 160;
                }
                else if (camPos.x < -160)
                {
                    camPos.x = -160;
                }

                if (camPos.y > 105)
                {
                    camPos.y = 105;
                }
                else if (camPos.y < -105)
                {
                    camPos.y = -105;
                }

                Camera.main.transform.position = camPos;
            }
        }

        [RPC]
        void EndGame()
        {
            StopCoroutine("RespawnPlayer");
            m_gameState = eGameState.GAME_STATE_GAME_OVER;
            GameObject.Find("PlayerController").GetComponent<PlayerController>().DestroyPlayerPlane();
            if (PhotonNetwork.isMasterClient)
                m_gameTime = 15;
        }

        [RPC]
        void ResetGame()
        {
            m_gameState = eGameState.GAME_STATE_RESETTING_GAME;
            m_redLogo.SetActive(false);
            m_greenLogo.SetActive(false);
            if (PhotonNetwork.isMasterClient)
            {
                for (int i = 0; i < m_spawnedShips.Count; i++)
                {
                    PhotonNetwork.Destroy(m_spawnedShips[i].gameObject);
                }

                m_spawnedShips.Clear();

                if (PhotonNetwork.room != null)
                    m_gameTime = (int)PhotonNetwork.room.customProperties["StartGameTime"];
                m_roomProperties = PhotonNetwork.room.customProperties;
                m_playerProperties["Score"] = 0;
                m_playerProperties["Kills"] = 0;
                m_playerProperties["Deaths"] = 0;
                m_roomProperties["IsPlaying"] = 0;
                m_roomProperties["Team1Score"] = 0;
                m_roomProperties["Team2Score"] = 0;
                m_team1Score = 0;
                m_team2Score = 0;
                bool done = false;
                while (!done)
                {
                    int lastLight = (int)m_roomProperties["LightPosition"];
                    m_roomProperties["LightPosition"] = Random.Range(1, 5);
                    if ((int)m_roomProperties["LightPosition"] != lastLight)
                    {
                        done = true;
                    }
                }
                
                PhotonNetwork.player.SetCustomProperties(m_playerProperties);
                PhotonNetwork.room.SetCustomProperties(m_roomProperties);
                GetComponent<PhotonView>().RPC("SetLightPosition", PhotonTargets.All, (int)m_roomProperties["LightPosition"]);
            }

            m_spawnedShips.Clear();
            GameObject[] explosions = GameObject.FindGameObjectsWithTag("Effect");

            for (int i = 0; i < explosions.Length; i++)
            {
                Destroy(explosions[i]);
            }

            m_playerIsReady = false;
            m_once = true;
            m_isRespawning = false;
            m_showScores = false;
            m_gameState = eGameState.GAME_STATE_WAITING_FOR_PLAYERS;

        }

        public void HitShipTargetPoint(ShipController.ShipTarget aShipTarget, BombController.BombInfo aBombInfo)
        {
            GetComponent<PhotonView>().RPC("HitShipTargetPointRPC", PhotonTargets.AllBuffered, aShipTarget, aBombInfo);
        }

        public void RespawnShip(ShipController aShip)
        {
            if (PhotonNetwork.isMasterClient)
            {
                GetComponent<PhotonView>().RPC("RespawnShipRPC", PhotonTargets.AllBuffered, aShip.m_shipID);
            }
        }

        [RPC]
        void RespawnShipRPC(int aShipID)
        {
            ShipController ship = null;
            for (int i = 0; i < m_spawnedShips.Count; i++)
            {
                if (m_spawnedShips[i].m_shipID == aShipID)
                {
                    ship = m_spawnedShips[i];
                    break;
                }
            }

            if (ship == null)
            {
                return;
            }
            ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().enabled = true;
            m_spawnedShips.Remove(ship);
            if (PhotonNetwork.isMasterClient)
            {
                ship.SetShipType(ship.GetShipType(), ship.m_team, aShipID);
            }

        }

        [RPC]
        void HitShipTargetPointRPC(ShipController.ShipTarget aShipTarget, BombController.BombInfo aBombInfo)
        {
            ShipController.ShipTarget shipTarget = null;
            GameObject ship = null;

            for (int i = 0; i < m_spawnedShips.Count; i++)
            {
                if (m_spawnedShips[i].ContainsShipTarget(aShipTarget))
                {
                    shipTarget = m_spawnedShips[i].GetShipTarget(aShipTarget);
                    ship = m_spawnedShips[i].gameObject;
                    break;
                }
            }
            if (aBombInfo.m_shooter == PhotonNetwork.player)
            {
                m_bombsHit++;
                m_playerProperties["Score"] = (int)m_playerProperties["Score"] + GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForWeakpointDestruction;
            }

            if (PhotonNetwork.isMasterClient)
            {
                if ((int)aBombInfo.m_shooter.customProperties["Team"] == 1)
                {
                    m_team1Score += GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForWeakpointDestruction;
                    m_roomProperties = PhotonNetwork.room.customProperties;
                    m_roomProperties["Team1Score"] = m_team1Score;
                    PhotonNetwork.room.SetCustomProperties(m_roomProperties);
                }
                else if ((int)aBombInfo.m_shooter.customProperties["Team"] == 2)
                {
                    m_team2Score += GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForWeakpointDestruction;
                    m_roomProperties = PhotonNetwork.room.customProperties;
                    m_roomProperties["Team2Score"] = m_team2Score;
                    PhotonNetwork.room.SetCustomProperties(m_roomProperties);
                }
            }

            Plane[] frustrum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(frustrum, ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Collider>().bounds))
            {
                GameObject.Find("PlayerController").GetComponent<PlayerController>().ShakeCamera(GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_weakpointIntensity, GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_shakeTime);
            }

            if (shipTarget == null) return;

            GameObject explosion = (GameObject)Instantiate((GameObject)Resources.Load("WeakpointExplosion"), shipTarget.m_position.position, shipTarget.m_position.rotation);
            explosion.transform.parent = ship.transform;
            explosion.GetComponent<AudioSource>().Play();
            Destroy(shipTarget.m_targetGraphic);

        }

        public GameObject GetClosestEnemyShip(Vector3 aPosition, int aTeam)
        {
            GameObject ship = null;
            float minDistance = 100000;
            for (int i = 0; i < m_spawnedShips.Count; i++)
            {
                if (m_spawnedShips[i].IsAlive() && m_spawnedShips[i].m_team != aTeam && (m_spawnedShips[i].gameObject.transform.position - aPosition).magnitude < minDistance)
                {
                    minDistance = (m_spawnedShips[i].gameObject.transform.position - aPosition).magnitude;
                    ship = m_spawnedShips[i].gameObject;
                }
            }

            return ship;
        }

        public void DestroyedShip(ShipController aShip, BombController.BombInfo aBombInfo)
        {
            GetComponent<PhotonView>().RPC("DestroyedShipRPC", PhotonTargets.AllBuffered, aShip.m_shipID, aBombInfo);
        }

        [RPC]
        void DestroyedShipRPC(int aShipID, BombController.BombInfo aBombInfo)
        {
            ShipController ship = null;
            for (int i = 0; i < m_spawnedShips.Count; i++)
            {
                if (m_spawnedShips[i].m_shipID == aShipID)
                {
                    ship = m_spawnedShips[i];
                    break;
                }
            }

            if (PhotonNetwork.isMasterClient)
            {
                if ((int)aBombInfo.m_shooter.customProperties["Team"] == 1)
                {
                    m_team1Score += GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForShipDestruction;
                    m_roomProperties = PhotonNetwork.room.customProperties;
                    m_roomProperties["Team1Score"] = m_team1Score;
                    PhotonNetwork.room.SetCustomProperties(m_roomProperties);
                }
                else
                {
                    m_team2Score += GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForShipDestruction;
                    m_roomProperties = PhotonNetwork.room.customProperties;
                    m_roomProperties["Team2Score"] = m_team2Score;
                    PhotonNetwork.room.SetCustomProperties(m_roomProperties);
                }
            }

            Plane[] frustrum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(frustrum, ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Collider>().bounds))
            {
                GameObject.Find("PlayerController").GetComponent<PlayerController>().ShakeCamera(GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_shipIntensity, GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_shakeTime);
            }

            if (ship == null) return;

            string shipName = "";
            ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().enabled = false;
            GameObject explosion;
            switch (ship.GetShipType())
            {
                case ShipController.eShipType.SHIP_TYPE_CARRIER:
                    explosion = (GameObject)Instantiate((GameObject)Resources.Load("CarrierExplosion"), ship.transform.position, ship.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    if ((int)aBombInfo.m_shooter.customProperties["Team"] == 1)
                    {
                        shipName += "Red ";
                    }
                    else
                    {
                        shipName += "Green ";
                    }
                    shipName += "Carrier";
                    break;
                case ShipController.eShipType.SHIP_TYPE_BATTLESHIP:
                    explosion = (GameObject)Instantiate((GameObject)Resources.Load("BattleshipExplosion"), ship.transform.position, ship.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    if ((int)aBombInfo.m_shooter.customProperties["Team"] == 1)
                    {
                        shipName += "Red ";
                    }
                    else
                    {
                        shipName += "Green ";
                    }
                    shipName += "Battleship";
                    break;
                case ShipController.eShipType.SHIP_TYPE_CRUISER:
                    explosion = (GameObject)Instantiate((GameObject)Resources.Load("CruiserExplosion"), ship.transform.position, ship.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    if ((int)aBombInfo.m_shooter.customProperties["Team"] == 1)
                    {
                        shipName += "Red ";
                    }
                    else
                    {
                        shipName += "Green ";
                    }
                    shipName += "Cruiser";
                    break;
                case ShipController.eShipType.SHIP_TYPE_SUBMARINE:
                    explosion = (GameObject)Instantiate((GameObject)Resources.Load("SubmarineExplosion"), ship.transform.position, ship.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    if ((int)aBombInfo.m_shooter.customProperties["Team"] == 1)
                    {
                        shipName += "Red ";
                    }
                    else
                    {
                        shipName += "Green ";
                    }
                    shipName += "Submarine";
                    break;
                case ShipController.eShipType.SHIP_TYPE_DESTROYER:
                    explosion = (GameObject)Instantiate((GameObject)Resources.Load("DestroyerExplosion"), ship.transform.position, ship.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    if ((int)aBombInfo.m_shooter.customProperties["Team"] == 1)
                    {
                        shipName += "Red ";
                    }
                    else
                    {
                        shipName += "Green ";
                    }
                    shipName += "Destroyer";
                    break;
            }

            m_killMessages.Add(new KillMessage((string)aBombInfo.m_shooter.customProperties["RoomDisplayName"] + " blew up a " + shipName, ((int)aBombInfo.m_shooter.customProperties["Team"] == 1) ? Color.green : Color.red));
            m_showKillDialog = true;
            m_killDialogTimer = 2;
            StopCoroutine("FadeKillDialog");
            if (PhotonNetwork.isMasterClient)
                ship.StartRespawn();
            if (aBombInfo.m_shooter == PhotonNetwork.player)
            {
                m_carriersDestroyed++;
                m_playerProperties["Score"] = (int)m_playerProperties["Score"] + GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForShipDestruction;

            }
        }

        void AwardExperience(int aWinningTeam)
        {
            GetComponent<PhotonView>().RPC("AwardExperienceRPC", PhotonTargets.All, aWinningTeam);
        }

        [RPC]
        void AwardExperienceRPC(int aWinningTeam)
        {
            if ((int)PhotonNetwork.player.customProperties["Team"] == 0) return;

            m_timesDestroyed = (int)PhotonNetwork.player.customProperties["Deaths"];
            m_planesDestroyed = (int)PhotonNetwork.player.customProperties["Kills"];
            int gamesWon = ((int)PhotonNetwork.player.customProperties["Team"] == aWinningTeam) ? 1 : 0;
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().IncrementStatisticsToBrainCloud(1, gamesWon, m_timesDestroyed, m_shotsFired, m_bombsDropped, m_planesDestroyed, m_carriersDestroyed, m_bombsHit);
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().IncrementExperienceToBrainCloud(m_planesDestroyed * GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_expForKill);
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().SubmitLeaderboardData(m_planesDestroyed, m_bombsHit, m_timesDestroyed);
            m_shotsFired = 0;
            m_bombsDropped = 0;
            m_bombsHit = 0;
            m_planesDestroyed = 0;
            m_carriersDestroyed = 0;
            m_timesDestroyed = 0;
        }

        IEnumerator FadeKillDialog()
        {
            float fadeTimer = 1;
            while (fadeTimer > 0)
            {
                yield return null;
                fadeTimer -= Time.deltaTime;
                m_killDialogColor = Color.Lerp(m_killDialogColor, new Color(m_killDialogColor.r, m_killDialogColor.g, m_killDialogColor.b, 0), 0.66f * Time.deltaTime);
            }
            m_showKillDialog = false;
            m_killMessages.Clear();
        }

        [RPC]
        void GetReady()
        {
            m_playerProperties["Deaths"] = 0;
            m_playerProperties["Kills"] = 0;
            m_playerProperties["IsReady"] = "true";
            PhotonNetwork.player.SetCustomProperties(m_playerProperties);
        }

        [RPC]
        void SpawnPlayer()
        {
            if ((int)PhotonNetwork.player.customProperties["Team"] == 0)
            {
                m_gameState = eGameState.GAME_STATE_SPECTATING;
            }
            else
            {
                Vector3 spawnPoint = Vector3.zero;
                spawnPoint.z = 22;

                if ((int)PhotonNetwork.player.customProperties["Team"] == 1)
                {
                    spawnPoint.x = Random.Range(m_team1SpawnBounds.bounds.center.x - m_team1SpawnBounds.bounds.size.x / 2, m_team1SpawnBounds.bounds.center.x + m_team1SpawnBounds.bounds.size.x / 2) - 10;
                    spawnPoint.y = Random.Range(m_team1SpawnBounds.bounds.center.y - m_team1SpawnBounds.bounds.size.y / 2, m_team1SpawnBounds.bounds.center.y + m_team1SpawnBounds.bounds.size.y / 2);
                }
                else if ((int)PhotonNetwork.player.customProperties["Team"] == 2)
                {
                    spawnPoint.x = Random.Range(m_team2SpawnBounds.bounds.center.x - m_team2SpawnBounds.bounds.size.x / 2, m_team2SpawnBounds.bounds.center.x + m_team2SpawnBounds.bounds.size.x / 2) + 10;
                    spawnPoint.y = Random.Range(m_team2SpawnBounds.bounds.center.y - m_team2SpawnBounds.bounds.size.y / 2, m_team2SpawnBounds.bounds.center.y + m_team2SpawnBounds.bounds.size.y / 2);
                }

                GameObject playerPlane = PhotonNetwork.Instantiate("Plane", spawnPoint, Quaternion.LookRotation(Vector3.forward, (new Vector3(0, 0, 22) - spawnPoint)), 0);
                if ((int)PhotonNetwork.player.customProperties["Team"] == 1)
                {
                    playerPlane.layer = 8;
                }
                else if ((int)PhotonNetwork.player.customProperties["Team"] == 2)
                {
                    playerPlane.layer = 9;
                }
                GameObject.Find("PlayerController").GetComponent<PlayerController>().SetPlayerPlane(playerPlane.GetComponent<PlaneController>());
                playerPlane.GetComponent<Rigidbody>().isKinematic = false;
                m_gameState = eGameState.GAME_STATE_PLAYING_GAME;
            }
        }

        public void DespawnBombPickup(int aPickupID)
        {
            GetComponent<PhotonView>().RPC("DespawnBombPickupRPC", PhotonTargets.All, aPickupID);
        }

        [RPC]
        void DespawnBombPickupRPC(int aPickupID)
        {
            for (int i = 0; i < m_bombPickupsSpawned.Count; i++)
            {
                if (m_bombPickupsSpawned[i].m_pickupID == aPickupID)
                {
                    Destroy(m_bombPickupsSpawned[i].gameObject);
                    m_bombPickupsSpawned.RemoveAt(i);
                    break;
                }
            }
        }

        public void SpawnBombPickup(Vector3 aPosition)
        {
            int bombID = 0;
            m_roomProperties = PhotonNetwork.room.customProperties;
            m_roomProperties["BombID"] = (int)m_roomProperties["BombID"] + 1;
            bombID = (int)m_roomProperties["BombID"];
            PhotonNetwork.room.SetCustomProperties(m_roomProperties);
            GetComponent<PhotonView>().RPC("SpawnBombPickupRPC", PhotonTargets.All, aPosition, bombID);
        }

        [RPC]
        void SpawnBombPickupRPC(Vector3 aPosition, int bombID)
        {
            GameObject bombPickup = (GameObject)Instantiate((GameObject)Resources.Load("BombPickup"), aPosition, Quaternion.identity);
            bombPickup.GetComponent<BombPickup>().Activate(bombID);
            m_bombPickupsSpawned.Add(bombPickup.GetComponent<BombPickup>());
        }

        public void BombPickedUp(PhotonPlayer aPlayer, int aPickupID)
        {
            GetComponent<PhotonView>().RPC("BombPickedUpRPC", PhotonTargets.All, aPlayer, aPickupID);
        }

        [RPC]
        void BombPickedUpRPC(PhotonPlayer aPlayer, int aPickupID)
        {
            for (int i = 0; i < m_bombPickupsSpawned.Count; i++)
            {
                if (m_bombPickupsSpawned[i].m_pickupID == aPickupID)
                {
                    Destroy(m_bombPickupsSpawned[i].gameObject);
                    m_bombPickupsSpawned.RemoveAt(i);
                    break;
                }
            }

            if (aPlayer == PhotonNetwork.player)
            {
                GameObject.Find("PlayerController").GetComponent<WeaponController>().AddBomb();
            }
        }

        public void SpawnBomb(BombController.BombInfo aBombInfo)
        {
            m_bombsDropped++;
            int id = GetNextBombID();
            aBombInfo.m_bombID = id;
            GetComponent<PhotonView>().RPC("SpawnBombRPC", PhotonTargets.All, aBombInfo);
        }

        [RPC]
        void SpawnBombRPC(BombController.BombInfo aBombInfo)
        {
            if (PhotonNetwork.isMasterClient)
            {
                aBombInfo.m_isMaster = true;
            }

            GameObject bomb = GameObject.Find("PlayerController").GetComponent<WeaponController>().SpawnBomb(aBombInfo);
            m_spawnedBombs.Add(bomb.GetComponent<BombController>().GetBombInfo());
            int playerTeam = (int)aBombInfo.m_shooter.customProperties["Team"];

            switch (playerTeam)
            {
                case 1:
                    bomb.layer = 14;
                    break;
                case 2:
                    bomb.layer = 15;
                    break;
            }
        }

        public void DeleteBomb(BombController.BombInfo aBombInfo, int aHitSurface)
        {
            GetComponent<PhotonView>().RPC("DeleteBombRPC", PhotonTargets.All, aBombInfo, aHitSurface);
        }

        [RPC]
        void DeleteBombRPC(BombController.BombInfo aBombInfo, int aHitSurface)
        {
            if (m_spawnedBombs.Contains(aBombInfo))
            {
                int index = m_spawnedBombs.IndexOf(aBombInfo);
                GameObject bomb = m_spawnedBombs[index].gameObject;
                GameObject explosion;
                if (!bomb.GetComponent<BombController>().m_isActive)
                {
                    if (aHitSurface == 0)
                    {
                        explosion = (GameObject)Instantiate((GameObject)Resources.Load("BombWaterExplosion"), bomb.transform.position, Quaternion.identity);
                        explosion.GetComponent<AudioSource>().Play();
                    }
                    else if (aHitSurface == 1)
                    {
                        explosion = (GameObject)Instantiate((GameObject)Resources.Load("BombExplosion"), bomb.transform.position, Quaternion.identity);
                        explosion.GetComponent<AudioSource>().Play();
                    }
                    else
                    {
                        explosion = (GameObject)Instantiate((GameObject)Resources.Load("BombDud"), bomb.transform.position, Quaternion.identity);
                        //explosion.GetComponent<AudioSource>().Play();
                    }
                }
                Destroy(bomb);
                m_spawnedBombs.Remove(aBombInfo);
            }
        }

        public void SpawnBullet(BulletController.BulletInfo aBulletInfo)
        {
            m_shotsFired++;
            int id = GetNextBulletID();
            aBulletInfo.m_bulletID = id;
            GetComponent<PhotonView>().RPC("SpawnBulletRPC", PhotonTargets.All, aBulletInfo);
        }

        [RPC]
        void SpawnBulletRPC(BulletController.BulletInfo aBulletInfo)
        {
            if (PhotonNetwork.player == aBulletInfo.m_shooter)
            {
                aBulletInfo.m_isMaster = true;
            }

            GameObject bullet = GameObject.Find("PlayerController").GetComponent<WeaponController>().SpawnBullet(aBulletInfo);
            m_spawnedBullets.Add(bullet.GetComponent<BulletController>().GetBulletInfo());
            int playerTeam = (int)aBulletInfo.m_shooter.customProperties["Team"];

            if (PhotonNetwork.player != aBulletInfo.m_shooter)
            {
                bullet.GetComponent<Collider>().isTrigger = true;
            }

            switch (playerTeam)
            {
                case 1:
                    bullet.layer = 10;
                    break;
                case 2:
                    bullet.layer = 11;
                    break;
            }
        }

        public void DeleteBullet(BulletController.BulletInfo aBulletInfo)
        {
            GetComponent<PhotonView>().RPC("DeleteBulletRPC", PhotonTargets.All, aBulletInfo);
        }

        [RPC]
        void DeleteBulletRPC(BulletController.BulletInfo aBulletInfo)
        {
            if (m_spawnedBullets.Contains(aBulletInfo))
            {
                int index = m_spawnedBullets.IndexOf(aBulletInfo);
                GameObject bullet = m_spawnedBullets[index].gameObject;
                Destroy(bullet);
                m_spawnedBullets.Remove(aBulletInfo);
            }
        }

        public void BulletHitPlayer(BulletController.BulletInfo aBulletInfo, Collision aCollision)
        {
            aBulletInfo.gameObject.transform.parent = aCollision.gameObject.transform;
            Vector3 relativeHitPoint = aBulletInfo.gameObject.transform.localPosition;
            PhotonPlayer hitPlayer = aCollision.gameObject.GetComponent<PhotonView>().owner;
            PhotonPlayer shooter = aBulletInfo.m_shooter;
            aBulletInfo.gameObject.transform.parent = null;
            DeleteBullet(aBulletInfo);
            GetComponent<PhotonView>().RPC("BulletHitPlayerRPC", PhotonTargets.All, relativeHitPoint, aBulletInfo, shooter, hitPlayer);
        }

        [RPC]
        void BulletHitPlayerRPC(Vector3 aHitPoint, BulletController.BulletInfo aBulletInfo, PhotonPlayer aShooter, PhotonPlayer aHitPlayer)
        {
            foreach (GameObject plane in GameObject.FindGameObjectsWithTag("Plane"))
            {
                if (plane.GetComponent<PhotonView>().owner == aHitPlayer)
                {
                    Instantiate((GameObject)Resources.Load("BulletHit"), plane.transform.position + aHitPoint, Quaternion.LookRotation(aBulletInfo.m_startDirection, -Vector3.forward));
                    break;
                }
            }

            if (aHitPlayer == PhotonNetwork.player)
            {
                GameObject.Find("PlayerController").GetComponent<PlayerController>().TakeBulletDamage(aShooter);
            }
        }

        public void DestroyPlayerPlane(PhotonPlayer aVictim, PhotonPlayer aShooter = null)
        {
            GetComponent<PhotonView>().RPC("DestroyPlayerPlaneRPC", PhotonTargets.All, aVictim, aShooter);
        }

        [RPC]
        void DestroyPlayerPlaneRPC(PhotonPlayer aVictim, PhotonPlayer aShooter)
        {
            foreach (GameObject plane in GameObject.FindGameObjectsWithTag("Plane"))
            {
                if (plane.GetComponent<PhotonView>().owner == aVictim)
                {
                    GameObject explosion = (GameObject)Instantiate((GameObject)Resources.Load("PlayerExplosion"), plane.transform.position, plane.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    break;
                }
            }

            if (aShooter == null)
            {
                m_killMessages.Add(new KillMessage((string)aVictim.customProperties["RoomDisplayName"] + " went down", ((int)aVictim.customProperties["Team"] == 1) ? Color.green : Color.red));
                m_killDialogTimer = 2;
                StopCoroutine("FadeKillDialog");

                if (aVictim == PhotonNetwork.player)
                {
                    GameObject.Find("PlayerController").GetComponent<PlayerController>().DestroyPlayerPlane();
                    m_isRespawning = true;
                    m_playerProperties["Deaths"] = (int)PhotonNetwork.player.customProperties["Deaths"] + 1;
                    PhotonNetwork.player.SetCustomProperties(m_playerProperties);
                    StopCoroutine("RespawnPlayer");
                    StartCoroutine("RespawnPlayer");
                }
            }
            else
            {
                m_killMessages.Add(new KillMessage((string)aShooter.customProperties["RoomDisplayName"] + " shot down " + (string)aVictim.customProperties["RoomDisplayName"], ((int)aShooter.customProperties["Team"] == 1) ? Color.green : Color.red));
                m_showKillDialog = true;
                m_killDialogTimer = 2;
                StopCoroutine("FadeKillDialog");

                if (aVictim == PhotonNetwork.player)
                {
                    GameObject.Find("PlayerController").GetComponent<PlayerController>().DestroyPlayerPlane();
                    m_isRespawning = true;
                    m_playerProperties["Deaths"] = (int)PhotonNetwork.player.customProperties["Deaths"] + 1;
                    PhotonNetwork.player.SetCustomProperties(m_playerProperties);
                    StopCoroutine("RespawnPlayer");
                    StartCoroutine("RespawnPlayer");
                }
                else if (aShooter == PhotonNetwork.player)
                {
                    m_playerProperties["Kills"] = (int)PhotonNetwork.player.customProperties["Kills"] + 1;
                    PhotonNetwork.player.SetCustomProperties(m_playerProperties);
                }
            }
        }

        int GetNextBulletID()
        {
            m_roomProperties = PhotonNetwork.room.customProperties;
            int lastId = (int)PhotonNetwork.room.customProperties["lastBulletID"];

            lastId++;
            m_roomProperties["lastBulletID"] = lastId;
            PhotonNetwork.room.SetCustomProperties(m_roomProperties);
            return lastId;
        }

        int GetNextBombID()
        {
            m_roomProperties = PhotonNetwork.room.customProperties;
            int lastId = (int)PhotonNetwork.room.customProperties["lastBombID"];

            lastId++;
            m_roomProperties["lastBombID"] = lastId;
            PhotonNetwork.room.SetCustomProperties(m_roomProperties);
            return lastId;
        }

        IEnumerator WaitForReadyPlayers()
        {
            bool playersReady = false;

            while (!playersReady)
            {
                PhotonPlayer[] playerList = PhotonNetwork.playerList.OrderBy(x => x.ID).ToArray();

                playersReady = true;
                for (int i = 0; i < m_room.maxPlayers; i++)
                {
                    if (i < playerList.Length)
                    {
                        if (playerList[i].customProperties["IsReady"] == null) playersReady = false;
                        break;
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            m_gameState = eGameState.GAME_STATE_SPAWN_PLAYERS;
        }

    }
}