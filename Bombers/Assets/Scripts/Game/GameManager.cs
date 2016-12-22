using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using BrainCloudPhotonExample.Connection;
using BrainCloudPhotonExample.Game.PlayerInput;
using UnityEngine.SceneManagement;

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

        private int m_respawnTime = 3;

        private List<BulletController.BulletInfo> m_spawnedBullets;
        private List<BombController.BombInfo> m_spawnedBombs;

        private float m_gameTime = 10 * 60;

        private int m_mapLayout = 0;
        private int m_mapSize = 1;

        private List<MapPresets.Preset> m_mapPresets;
        private List<MapPresets.MapSize> m_mapSizes;

        private float m_currentRespawnTime = 0;

        private float m_team1Score = 0;
        private float m_team2Score = 0;
        private int m_shotsFired = 0;
        private int m_bombsDropped = 0;
        private int m_bombsHit = 0;
        private int m_planesDestroyed = 0;
        private int m_carriersDestroyed = 0;
        private int m_timesDestroyed = 0;

        private bool m_once = false;

        [SerializeField]
        private Collider m_team1SpawnBounds;

        [SerializeField]
        private Collider m_team2SpawnBounds;

        private List<BombPickup> m_bombPickupsSpawned = new List<BombPickup>();
        private int m_bombID;

        private List<ShipController> m_spawnedShips = new List<ShipController>();
        private List<ShipController> m_team1Ships = new List<ShipController>();
        private List<ShipController> m_team2Ships = new List<ShipController>();

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

        private GameObject m_HUD;

        private GameObject m_allyShipSunk;
        private GameObject m_enemyShipSunk;
        private GameObject m_greenShipLogo;
        private GameObject m_redShipLogo;

        private GameObject m_quitMenu;
        private bool m_showQuitMenu;
        private GameObject m_blackScreen;

        private bool m_showScores = false;

        private AudioSource m_backgroundMusic;
        private PlayerController m_playerController;
        private GameObject m_directionalLight;
        private DialogDisplay m_dialogueDisplay;
        private PhotonView m_photonView;

        //resources
        private GameObject m_carrierExplosion01;
        private GameObject m_carrierExplosion02;
        private GameObject m_battleshipExplosion01;
        private GameObject m_battleshipExplosion02;
        private GameObject m_cruiserExplosion02;
        private GameObject m_cruiserExplosion01;
        private GameObject m_patrolBoatExplosion02;
        private GameObject m_patrolBoatExplosion01;
        private GameObject m_destroyerExplosion02;
        private GameObject m_destroyerExplosion01;
        private GameObject m_flare;
        private GameObject m_bombPickup;
        private GameObject m_bulletHit;
        private GameObject m_playerExplosion;
        private GameObject m_weakpointExplosion;
        private GameObject m_carrier01;
        private GameObject m_bombExplosion;
        private GameObject m_bombWaterExplosion;
        private GameObject m_bombDud;

        void Awake()
        {
            if (!BrainCloudWrapper.GetBC().Initialized)
            {
                SceneManager.LoadScene("Connect");
                return;
            }

            m_backgroundMusic = GameObject.Find("BackgroundMusic").GetComponent<AudioSource>();
            m_playerController = GameObject.Find("PlayerController").GetComponent<PlayerController>();
            m_directionalLight = GameObject.Find("Directional Light");
            m_dialogueDisplay = GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>();
            m_photonView = GetComponent<PhotonView>();

            m_allyShipSunk = GameObject.Find("ShipSink").transform.FindChild("AllyShipSunk").gameObject;
            m_enemyShipSunk = GameObject.Find("ShipSink").transform.FindChild("EnemyShipSunk").gameObject;
            m_redShipLogo = GameObject.Find("ShipSink").transform.FindChild("RedLogo").gameObject;
            m_greenShipLogo = GameObject.Find("ShipSink").transform.FindChild("GreenLogo").gameObject;
            m_blackScreen = GameObject.Find("BlackScreen");

            m_allyShipSunk.SetActive(false);
            m_enemyShipSunk.SetActive(false);
            m_redShipLogo.SetActive(false);
            m_greenShipLogo.SetActive(false);
            m_quitMenu = GameObject.Find("QuitMenu");
            m_quitMenu.SetActive(false);

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
            m_gameTime = BrainCloudStats.Instance.m_defaultGameTime;
            m_mapPresets = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_presets;
            m_mapSizes = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_mapSizes;
            m_resultsWindow = GameObject.Find("Results");
            m_resultsWindow.SetActive(false);
            m_HUD = GameObject.Find("HUD");
            GameObject.Find("RespawnText").GetComponent<Text>().text = "";
            m_playerController.m_missionText = m_HUD.transform.FindChild("MissionText").gameObject;
            m_HUD.SetActive(false);

            //resources
            m_carrierExplosion01 = Resources.Load("CarrierExplosion01") as GameObject;
            m_carrierExplosion02 = Resources.Load("CarrierExplosion02") as GameObject;
            m_battleshipExplosion01 = Resources.Load("BattleshipExplosion01") as GameObject;
            m_battleshipExplosion02 = Resources.Load("BattleshipExplosion02") as GameObject;
            m_cruiserExplosion02 = Resources.Load("CruiserExplosion02") as GameObject;
            m_cruiserExplosion01 = Resources.Load("CruiserExplosion01") as GameObject;
            m_patrolBoatExplosion02 = Resources.Load("PatrolBoatExplosion02") as GameObject;
            m_patrolBoatExplosion01 = Resources.Load("PatrolBoatExplosion01") as GameObject;
            m_destroyerExplosion02 = Resources.Load("DestroyerExplosion02") as GameObject;
            m_destroyerExplosion01 = Resources.Load("DestroyerExplosion01") as GameObject;
            m_flare = Resources.Load("Flare") as GameObject;
            m_bombPickup = Resources.Load("BombPickup") as GameObject;
            m_bulletHit = Resources.Load("BulletHit") as GameObject;
            m_playerExplosion = Resources.Load("PlayerExplosion") as GameObject;
            m_weakpointExplosion = Resources.Load("WeakpointExplosion") as GameObject;
            m_carrier01 = Resources.Load("Carrier01") as GameObject;
            m_bombExplosion = Resources.Load("BombExplosion") as GameObject;
            m_bombWaterExplosion = Resources.Load("BombWaterExplosion") as GameObject;
            m_bombDud = Resources.Load("BombDud") as GameObject;
        }

        void Start()
        {
            m_mapLayout = (int)PhotonNetwork.room.customProperties["MapLayout"];
            m_mapSize = (int)PhotonNetwork.room.customProperties["MapSize"];

            if (PhotonNetwork.room.customProperties["LightPosition"] != null) SetLightPosition((int)PhotonNetwork.room.customProperties["LightPosition"]);

            m_spawnedBullets = new List<BulletController.BulletInfo>();
            m_spawnedBombs = new List<BombController.BombInfo>();
            m_skin = (GUISkin)Resources.Load("skin");
            m_room = PhotonNetwork.room;
            m_playerProperties = new ExitGames.Client.Photon.Hashtable();
            m_playerProperties = PhotonNetwork.player.customProperties;
            StartCoroutine("UpdatePing");
            StartCoroutine("UpdateRoomDisplayName");
            m_roomProperties = PhotonNetwork.room.customProperties;
            if (m_playerProperties["Team"] == null)
                m_playerProperties["Team"] = 0;

            m_team1Score = 0;
            m_team2Score = 0;

            /*if ((int)m_roomProperties["IsPlaying"] == 1)
            {
                m_gameState = eGameState.GAME_STATE_SPECTATING;
                m_roomProperties["Spectators"] = (int)PhotonNetwork.room.customProperties["Spectators"] + 1;
                PhotonPlayer[] playerList = PhotonNetwork.playerList;
                List<PhotonPlayer> playerListList = new List<PhotonPlayer>();
                for (int i = 0; i < playerList.Length; i++)
                {
                    playerListList.Add(playerList[i]);
                }

                int count = 0;
                while (count < playerListList.Count)
                {
                    if (playerListList[count].customProperties["Team"] == null || (int)playerListList[count].customProperties["Team"] == 0)
                    {
                        playerListList.RemoveAt(count);
                    }
                    else
                    {
                        count++;
                    }
                }
                playerList = playerListList.ToArray().OrderByDescending(x => (int)x.customProperties["Score"]).ToArray();
                m_spectatingTarget = playerList[0];
            }
            else*/



            {
                if (PhotonNetwork.isMasterClient)
                {
                    m_roomProperties["lastBulletID"] = -1;
                    m_roomProperties["lastBombID"] = -1;
                    m_roomProperties["GameTime"] = m_gameTime;
                    m_roomProperties["BombID"] = 0;
                    m_roomProperties["LightPosition"] = Random.Range(1, 5);
                    m_roomProperties["Team1Score"] = 0.0f;
                    m_roomProperties["Team2Score"] = 0.0f;
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
                    if ((int)m_playerProperties["Team"] == 0)
                    {
                        m_playerProperties["Team"] = 2;
                        m_roomProperties["Team2Players"] = (int)PhotonNetwork.room.customProperties["Team2Players"] + 1;
                    }
                }
                else
                {
                    if ((int)m_playerProperties["Team"] == 0)
                    {
                        m_playerProperties["Team"] = 1;
                        m_roomProperties["Team1Players"] = (int)PhotonNetwork.room.customProperties["Team1Players"] + 1;
                    }
                }
            }
            m_playerProperties["Score"] = 0;
            PhotonNetwork.player.SetCustomProperties(m_playerProperties);
            PhotonNetwork.room.SetCustomProperties(m_roomProperties);

            if ((int)PhotonNetwork.room.customProperties["IsPlaying"] == 1)
            {
                m_photonView.RPC("AnnounceJoin", PhotonTargets.All, m_playerProperties["RoomDisplayName"].ToString(), (int)m_playerProperties["Team"]);
            }
        }

        [PunRPC]
        void AnnounceJoin(string aPlayerName, int aTeam)
        {
            string message = aPlayerName + " has joined the fight\n on the ";
            message += (aTeam == 1) ? "green team!" : "red team!";
            m_dialogueDisplay.DisplayDialog(message, true);
        }

        [PunRPC]
        void SetLightPosition(int aPosition)
        {
            Vector3 position = Vector3.zero;
            switch (aPosition)
            {
                case 1:
                    position.Set(330, 0, 0);
                    break;
                case 2:
                    position.Set(354, 34, 0);
                    break;
                case 3:
                    position.Set(10, 325, 0);
                    break;
                case 4:
                    position.Set(30, 0, 0);
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
                m_directionalLight.transform.rotation = Quaternion.Slerp(m_directionalLight.transform.rotation, Quaternion.Euler(aPosition), 5 * Time.deltaTime);
                count++;
                if (count > 10000)
                {
                    m_directionalLight.transform.rotation = Quaternion.Euler(aPosition);
                    done = true;
                }
                yield return YieldFactory.GetWaitForSeconds(0.02f);
            }
        }

        public void OnPhotonSerializeView(PhotonStream aStream, PhotonMessageInfo aInfo)
        {

        }

        void OnApplicationQuit()
        {
            if (m_playerProperties != null)
            {
                m_playerProperties.Clear();
            }
            PhotonNetwork.player.SetCustomProperties(m_playerProperties);
            PhotonNetwork.Disconnect();
        }

        public void LeaveRoom()
        {
            if (PhotonNetwork.player.customProperties["Team"] != null && (int)PhotonNetwork.player.customProperties["Team"] == 1)
            {
                m_roomProperties = PhotonNetwork.room.customProperties;
                m_roomProperties["Team1Players"] = (int)m_roomProperties["Team1Players"] - 1;
                PhotonNetwork.room.SetCustomProperties(m_roomProperties);
            }
            else if (PhotonNetwork.player.customProperties["Team"] != null && (int)PhotonNetwork.player.customProperties["Team"] == 2)
            {
                m_roomProperties = PhotonNetwork.room.customProperties;
                m_roomProperties["Team2Players"] = (int)m_roomProperties["Team2Players"] - 1;
                PhotonNetwork.room.SetCustomProperties(m_roomProperties);
            }
            else if (PhotonNetwork.player.customProperties["Team"] == null || (int)PhotonNetwork.player.customProperties["Team"] == 0)
            {
                m_roomProperties = PhotonNetwork.room.customProperties;
                m_roomProperties["Spectators"] = (int)m_roomProperties["Spectators"] - 1;
                PhotonNetwork.room.SetCustomProperties(m_roomProperties);
            }

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
                m_photonView.RPC("ResetGame", PhotonTargets.All);
            }
        }

        public void CloseQuitMenu()
        {
            m_showQuitMenu = false;
        }

        void OnMasterClientSwitched(PhotonPlayer newMasterClient)
        {
            m_dialogueDisplay.HostLeft();
            PhotonNetwork.LeaveRoom();
            m_playerProperties.Clear();
            PhotonNetwork.player.SetCustomProperties(m_playerProperties);
        }

        void OnGUI()
        {
            GUI.skin = m_skin;
            if (PhotonNetwork.room == null) return;
            switch (m_gameState)
            {
                case eGameState.GAME_STATE_WAITING_FOR_PLAYERS:
                    m_resultsWindow.GetComponent<CanvasGroup>().alpha = 0;

                    m_lobbyWindow.gameObject.SetActive(true);
                    m_resultsWindow.gameObject.SetActive(false);
                    m_HUD.SetActive(false);
                    OnWaitingForPlayersWindow();
                    break;
                case eGameState.GAME_STATE_STARTING_GAME:
                    m_blackScreen.GetComponent<CanvasGroup>().alpha += Time.fixedDeltaTime * 3;
                    m_resultsWindow.GetComponent<CanvasGroup>().alpha = 0;
                    m_lobbyWindow.gameObject.SetActive(true);
                    m_resultsWindow.gameObject.SetActive(false);
                    m_HUD.SetActive(false);
                    OnWaitingForPlayersWindow();
                    break;
                case eGameState.GAME_STATE_SPECTATING:
                    m_lobbyWindow.gameObject.SetActive(false);
                    if (m_showScores)
                    {
                        m_resultsWindow.GetComponent<CanvasGroup>().alpha = 1;
                        m_resultsWindow.gameObject.SetActive(true);
                        OnMiniScoresWindow();
                    }
                    else
                    {
                        m_resultsWindow.GetComponent<CanvasGroup>().alpha = 0;
                        m_resultsWindow.gameObject.SetActive(false);
                    }
                    m_HUD.SetActive(false);
                    GUI.Label(new Rect(Screen.width / 2 - 100, 20, 200, 20), "Spectating");
                    break;
                case eGameState.GAME_STATE_GAME_OVER:
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_resultsWindow.gameObject.SetActive(true);
                    m_HUD.SetActive(false);

                    OnScoresWindow();
                    break;

                case eGameState.GAME_STATE_PLAYING_GAME:
                    m_blackScreen.GetComponent<CanvasGroup>().alpha -= Time.fixedDeltaTime * 3;
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_resultsWindow.gameObject.SetActive(false);
                    m_HUD.SetActive(true);
                    if (m_showScores)
                    {
                        m_resultsWindow.GetComponent<CanvasGroup>().alpha += Time.fixedDeltaTime * 4;
                        if (m_resultsWindow.GetComponent<CanvasGroup>().alpha > 1) m_resultsWindow.GetComponent<CanvasGroup>().alpha = 1;
                        m_resultsWindow.gameObject.SetActive(true);
                        OnMiniScoresWindow();
                    }
                    else
                    {
                        if (m_resultsWindow.GetComponent<CanvasGroup>().alpha > 0) m_resultsWindow.gameObject.SetActive(true);
                        m_resultsWindow.GetComponent<CanvasGroup>().alpha -= Time.fixedDeltaTime * 4;
                        if (m_resultsWindow.GetComponent<CanvasGroup>().alpha < 0) m_resultsWindow.GetComponent<CanvasGroup>().alpha = 0;
                    }
                    OnHudWindow();
                    break;

                default:
                    m_lobbyWindow.gameObject.SetActive(false);
                    m_resultsWindow.gameObject.SetActive(false);
                    m_HUD.SetActive(false);
                    break;
            }
        }

        void OnHudWindow()
        {
            if (PhotonNetwork.room == null) return;
            m_team1Score = (float)PhotonNetwork.room.customProperties["Team1Score"];
            m_team2Score = (float)PhotonNetwork.room.customProperties["Team2Score"];
            int score = 0;
            if (PhotonNetwork.player.customProperties["Score"] != null)
                score = (int)PhotonNetwork.player.customProperties["Score"];
            System.TimeSpan span = System.TimeSpan.FromSeconds(m_gameTime);
            string timeLeft = span.ToString().Substring(3, 5);

            int team1ShipsCount = ShipsAliveCount(m_team1Ships);
            int team2ShipsCount = ShipsAliveCount(m_team2Ships);

            m_HUD.transform.FindChild("PlayerScore").GetChild(0).GetComponent<Text>().text = score.ToString("n0");
            m_HUD.transform.FindChild("RedScore").GetChild(0).GetComponent<Text>().text = m_team2Score.ToString("n0");
            m_HUD.transform.FindChild("RedScore").GetChild(1).GetComponent<Text>().text = "Ships Left: " + team2ShipsCount.ToString();
            if (team2ShipsCount == 1)
                m_HUD.transform.FindChild("RedScore").GetChild(1).GetComponent<Text>().color = new Color(1, 0, 0, 1);
            else
                m_HUD.transform.FindChild("RedScore").GetChild(1).GetComponent<Text>().color = new Color(1, 1, 1, 1);
            m_HUD.transform.FindChild("GreenScore").GetChild(0).GetComponent<Text>().text = m_team1Score.ToString("n0");
            m_HUD.transform.FindChild("GreenScore").GetChild(1).GetComponent<Text>().text = "Ships Left: " + team1ShipsCount.ToString();
            if (team1ShipsCount == 1)
                m_HUD.transform.FindChild("GreenScore").GetChild(1).GetComponent<Text>().color = new Color(1, 0, 0, 1);
            else
                m_HUD.transform.FindChild("GreenScore").GetChild(1).GetComponent<Text>().color = new Color(1, 1, 1, 1);
            m_HUD.transform.FindChild("TimeLeft").GetChild(0).GetComponent<Text>().text = timeLeft;
        }

        private int ShipsAliveCount(List<ShipController> ships)
        {
            int count = 0;
            for (int i = 0; i < ships.Count; ++i)
            {
                if (ships[i].IsAlive()) count++;
            }
            return count;
        }

        void OnMiniScoresWindow()
        {
            m_quitButton.SetActive(false);
            m_resetButton.SetActive(false);
            m_allyWinText.SetActive(false);
            m_enemyWinText.SetActive(false);
            m_greenLogo.SetActive(false);
            m_redLogo.SetActive(false);

            m_team1Score = (float)PhotonNetwork.room.customProperties["Team1Score"];
            m_team2Score = (float)PhotonNetwork.room.customProperties["Team2Score"];
            GameObject team = GameObject.Find("Team Green Score");
            team.transform.FindChild("Team Score").GetComponent<Text>().text = m_team1Score.ToString("n0");
            team = GameObject.Find("Team Red Score");
            team.transform.FindChild("Team Score").GetComponent<Text>().text = m_team2Score.ToString("n0");

            PhotonPlayer[] playerList = PhotonNetwork.playerList;
            List<PhotonPlayer> playerListList = new List<PhotonPlayer>();
            for (int i = 0; i < playerList.Length; i++)
            {
                playerListList.Add(playerList[i]);
            }

            int count = 0;
            while (count < playerListList.Count)
            {
                if (playerListList[count].customProperties["Team"] == null || (int)playerListList[count].customProperties["Team"] == 0)
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
                        m_greenChevron.GetComponent<RectTransform>().localPosition = new Vector3(m_greenChevron.GetComponent<RectTransform>().localPosition.x, 21.8f - (greenPlayers * 17.7f), m_greenChevron.GetComponent<RectTransform>().localPosition.z);
                        greenNamesText += "\n";
                        greenKDText += "\n";
                        greenScoreText += "\n";
                    }
                    else
                    {
                        greenNamesText += playerList[i].customProperties["RoomDisplayName"].ToString() + "\n";
                        greenKDText += playerList[i].customProperties["Kills"].ToString() + "/" + playerList[i].customProperties["Deaths"].ToString() + "\n";
                        greenScoreText += ((int)playerList[i].customProperties["Score"]).ToString("n0") + "\n";
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
                        m_redChevron.GetComponent<RectTransform>().localPosition = new Vector3(m_redChevron.GetComponent<RectTransform>().localPosition.x, 21.8f - (redPlayers * 17.7f), m_redChevron.GetComponent<RectTransform>().localPosition.z);

                        redNamesText += "\n";
                        redKDText += "\n";
                        redScoreText += "\n";
                    }
                    else
                    {
                        redNamesText += playerList[i].customProperties["RoomDisplayName"].ToString() + "\n";
                        redKDText += playerList[i].customProperties["Kills"].ToString() + "/" + playerList[i].customProperties["Deaths"].ToString() + "\n";
                        redScoreText += ((int)playerList[i].customProperties["Score"]).ToString("n0") + "\n";
                    }
                    redPlayers++;
                }
            }

            team = GameObject.Find("Team Green Score");
            team.transform.FindChild("GreenPlayers").GetComponent<Text>().text = greenNamesText;
            team.transform.FindChild("GreenPlayerKD").GetComponent<Text>().text = greenKDText;
            team.transform.FindChild("GreenPlayerScores").GetComponent<Text>().text = greenScoreText;
            team = GameObject.Find("Team Red Score");
            team.transform.FindChild("RedPlayers").GetComponent<Text>().text = redNamesText;
            team.transform.FindChild("RedPlayerKD").GetComponent<Text>().text = redKDText;
            team.transform.FindChild("RedPlayerScores").GetComponent<Text>().text = redScoreText;
        }

        void OnScoresWindow()
        {
            if (PhotonNetwork.room == null) return;
            m_resultsWindow.GetComponent<CanvasGroup>().alpha += Time.fixedDeltaTime * 2;
            if (m_resultsWindow.GetComponent<CanvasGroup>().alpha > 1) m_resultsWindow.GetComponent<CanvasGroup>().alpha = 1;
            m_team1Score = (float)PhotonNetwork.room.customProperties["Team1Score"];
            m_team2Score = (float)PhotonNetwork.room.customProperties["Team2Score"];
            GameObject team = GameObject.Find("Team Green Score");
            team.transform.FindChild("Team Score").GetComponent<Text>().text = m_team1Score.ToString("n0");
            team = GameObject.Find("Team Red Score");
            team.transform.FindChild("Team Score").GetComponent<Text>().text = m_team2Score.ToString("n0");

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
            m_allyWinText.SetActive(false);
            m_enemyWinText.SetActive(false);
            if (m_gameState == eGameState.GAME_STATE_GAME_OVER)
            {
                if (m_team1Score > m_team2Score)
                {
                    m_greenLogo.SetActive(true);
                    m_redLogo.SetActive(false);
                    if (PhotonNetwork.player.customProperties["Team"] != null && (int)PhotonNetwork.player.customProperties["Team"] == 1)
                    {
                        m_allyWinText.SetActive(true);
                        m_enemyWinText.SetActive(false);
                    }
                    else if (PhotonNetwork.player.customProperties["Team"] != null && (int)PhotonNetwork.player.customProperties["Team"] == 2)
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
                if (playerListList[count].customProperties["Team"] == null || (int)playerListList[count].customProperties["Team"] == 0)
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
                        m_greenChevron.GetComponent<RectTransform>().localPosition = new Vector3(m_greenChevron.GetComponent<RectTransform>().localPosition.x, 21.8f - (greenPlayers * 17.7f), m_greenChevron.GetComponent<RectTransform>().localPosition.z);
                        greenNamesText += "\n";
                        greenKDText += "\n";
                        greenScoreText += "\n";
                    }
                    else
                    {
                        greenNamesText += playerList[i].customProperties["RoomDisplayName"].ToString() + "\n";
                        greenKDText += playerList[i].customProperties["Kills"].ToString() + "/" + playerList[i].customProperties["Deaths"].ToString() + "\n";
                        greenScoreText += ((int)playerList[i].customProperties["Score"]).ToString("n0") + "\n";
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
                        m_redChevron.GetComponent<RectTransform>().localPosition = new Vector3(m_redChevron.GetComponent<RectTransform>().localPosition.x, 21.8f - (redPlayers * 17.7f), m_redChevron.GetComponent<RectTransform>().localPosition.z);

                        redNamesText += "\n";
                        redKDText += "\n";
                        redScoreText += "\n";
                    }
                    else
                    {
                        redNamesText += playerList[i].customProperties["RoomDisplayName"].ToString() + "\n";
                        redKDText += playerList[i].customProperties["Kills"].ToString() + "/" + playerList[i].customProperties["Deaths"].ToString() + "\n";
                        redScoreText += ((int)playerList[i].customProperties["Score"]).ToString("n0") + "\n";
                    }
                    redPlayers++;
                }
            }

            team = GameObject.Find("Team Green Score");
            team.transform.FindChild("GreenPlayers").GetComponent<Text>().text = greenNamesText;
            team.transform.FindChild("GreenPlayerKD").GetComponent<Text>().text = greenKDText;
            team.transform.FindChild("GreenPlayerScores").GetComponent<Text>().text = greenScoreText;
            team = GameObject.Find("Team Red Score");
            team.transform.FindChild("RedPlayers").GetComponent<Text>().text = redNamesText;
            team.transform.FindChild("RedPlayerKD").GetComponent<Text>().text = redKDText;
            team.transform.FindChild("RedPlayerScores").GetComponent<Text>().text = redScoreText;
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
        }

        [PunRPC]
        void ChangeMapLayout(int aLayout)
        {
            m_mapLayout = aLayout;
        }

        [PunRPC]
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
                yield return YieldFactory.GetWaitForSeconds(1);
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

                yield return YieldFactory.GetWaitForSeconds(0.1f);
            }
        }

        IEnumerator RespawnPlayer()
        {
            m_currentRespawnTime = (float)m_respawnTime;
            while (m_currentRespawnTime > 0)
            {
                GameObject.Find("RespawnText").GetComponent<Text>().text = "Respawning in " + Mathf.CeilToInt(m_currentRespawnTime);
                yield return YieldFactory.GetWaitForSeconds(0.1f);
                m_currentRespawnTime -= 0.1f;
            }

            if (m_currentRespawnTime < 0)
            {
                m_currentRespawnTime = 0;
                GameObject.Find("RespawnText").GetComponent<Text>().text = "";
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
                m_playerController.SetPlayerPlane(playerPlane.GetComponent<PlaneController>());
                playerPlane.GetComponent<Rigidbody>().isKinematic = false;
            }
        }

        [PunRPC]
        void SetMapSizeRPC(Vector3 newScale)
        {
            GameObject mapBound = GameObject.Find("MapBounds");
            mapBound.transform.localScale = newScale;

            GameObject spawn = GameObject.Find("Team1Spawn");
            spawn.transform.position = new Vector3(mapBound.GetComponent<Collider>().bounds.min.x, 0, 22);
            spawn.transform.localScale = new Vector3(5, 1, newScale.z * 0.75f);
            spawn = GameObject.Find("Team2Spawn");
            spawn.transform.position = new Vector3(mapBound.GetComponent<Collider>().bounds.max.x, 0, 22);
            spawn.transform.localScale = new Vector3(5, 1, newScale.z * 0.75f);
            GameObject.Find("BorderClouds").GetComponent<MapCloudBorder>().SetCloudBorder();
        }

        IEnumerator SpawnGameStart()
        {
            MapPresets.MapSize mapSize = m_mapSizes[m_mapSize];
            GameObject mapBound = GameObject.Find("MapBounds");
            mapBound.transform.localScale = new Vector3(mapSize.m_horizontalSize, 1, mapSize.m_verticalSize);
            m_photonView.RPC("SetMapSizeRPC", PhotonTargets.OthersBuffered, new Vector3(mapSize.m_horizontalSize, 1, mapSize.m_verticalSize));
            m_photonView.RPC("GetReady", PhotonTargets.AllBuffered);
            yield return YieldFactory.GetWaitForSeconds(0.5f);
            m_roomProperties = PhotonNetwork.room.customProperties;
            m_roomProperties["IsPlaying"] = 1;
            PhotonNetwork.room.SetCustomProperties(m_roomProperties);

            int shipID = 0;
            bool done = false;
            GameObject ship = null;
            int shipIndex = 0;
            int tryCount = 0;


            GameObject spawn = GameObject.Find("Team1Spawn");
            spawn.transform.position = new Vector3(mapBound.GetComponent<Collider>().bounds.min.x, 0, 22);
            spawn.transform.localScale = new Vector3(5, 1, mapSize.m_verticalSize * 0.75f);
            spawn = GameObject.Find("Team2Spawn");
            spawn.transform.position = new Vector3(mapBound.GetComponent<Collider>().bounds.max.x, 0, 22);
            spawn.transform.localScale = new Vector3(5, 1, mapSize.m_verticalSize * 0.75f);
            GameObject.Find("BorderClouds").GetComponent<MapCloudBorder>().SetCloudBorder();
            if (m_mapLayout == 0)
            {
                GameObject testShip = (GameObject)Instantiate(m_carrier01, Vector3.zero, Quaternion.identity);
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
                    
                    var shipController = ship.GetComponent<ShipController>();
                    switch (shipIndex)
                    {
                        case 0:
                            shipController.SetShipType(ShipController.eShipType.SHIP_TYPE_CARRIER, (shipID % 2) + 1, shipID);
                            break;
                        case 1:
                            shipController.SetShipType(ShipController.eShipType.SHIP_TYPE_BATTLESHIP, (shipID % 2) + 1, shipID);
                            break;
                        case 2:
                            shipController.SetShipType(ShipController.eShipType.SHIP_TYPE_CRUISER, (shipID % 2) + 1, shipID);
                            break;
                        case 3:
                            shipController.SetShipType(ShipController.eShipType.SHIP_TYPE_PATROLBOAT, (shipID % 2) + 1, shipID);
                            break;
                        case 4:
                            shipController.SetShipType(ShipController.eShipType.SHIP_TYPE_DESTROYER, (shipID % 2) + 1, shipID);
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

            Bounds bounds = GameObject.Find("MapBounds").GetComponent<Collider>().bounds;

            for (int i = 0; i < (int)m_mapSizes[m_mapSize].m_horizontalSize / 80 + (int)m_mapSizes[m_mapSize].m_verticalSize / 80; i++)
            {
                Vector3 position = new Vector3(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y), 122);
                Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, Random.Range(0, 360.0f)));

                if (!Physics.CheckSphere(position, 15, (1 << 16 | 1 << 17 | 1 << 20)))
                {
                    PhotonNetwork.Instantiate("Rock0" + Random.Range(1, 5), position, rotation, 0);
                }
                else
                {
                    i--;
                }
            }
        }

        void Update()
        {
            switch (m_gameState)
            {
                case eGameState.GAME_STATE_WAITING_FOR_PLAYERS:
                    m_showScores = false;
                    if (m_room.playerCount == m_room.maxPlayers)
                    {
                        m_gameState = eGameState.GAME_STATE_STARTING_GAME;
                    }

                    if (m_backgroundMusic.isPlaying)
                    {
                        m_backgroundMusic.Stop();
                    }
                    Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(0, 0, -90), Time.deltaTime);
                    break;

                case eGameState.GAME_STATE_STARTING_GAME:
                    m_showScores = false;
                    if (PhotonNetwork.isMasterClient && !m_once)
                    {
                        m_once = true;
                        StartCoroutine("SpawnGameStart");
                        StartCoroutine("WaitForReadyPlayers");
                    }

                    break;
                case eGameState.GAME_STATE_SPAWN_PLAYERS:
                    m_showScores = false;
                    if (PhotonNetwork.isMasterClient && m_once)
                    {
                        m_once = false;
                        m_photonView.RPC("SpawnPlayer", PhotonTargets.AllBuffered);
                    }

                    break;

                case eGameState.GAME_STATE_PLAYING_GAME:

                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        m_showQuitMenu = !m_showQuitMenu;
                    }

                    if (Input.GetKey(KeyCode.Tab))
                    {
                        m_showScores = true;
                    }
                    else
                    {
                        m_showScores = false;
                    }

                    if (m_showQuitMenu)
                    {
                        m_quitMenu.SetActive(true);
                    }
                    else
                    {
                        m_quitMenu.SetActive(false);
                    }


                    if (!m_once)
                    {
                        m_backgroundMusic.Play();
                        m_once = true;
                    }
                    if (PhotonNetwork.isMasterClient)
                    {
                        m_gameTime -= Time.deltaTime;
                        m_roomProperties = PhotonNetwork.room.customProperties;
                        m_roomProperties["GameTime"] = m_gameTime;
                        PhotonNetwork.room.SetCustomProperties(m_roomProperties);

                        bool team1IsDestroyed = ShipsAliveCount(m_team1Ships) <= 0;
                        bool team2IsDestroyed = ShipsAliveCount(m_team2Ships) <= 0;

                        if (m_gameTime <= 0 || team1IsDestroyed || team2IsDestroyed)
                        {
                            m_photonView.RPC("EndGame", PhotonTargets.AllBuffered);
                        }
                    }
                    else
                    {
                        if (PhotonNetwork.room != null)
                            m_gameTime = (float)PhotonNetwork.room.customProperties["GameTime"];
                    }

                    break;

                case eGameState.GAME_STATE_GAME_OVER:
                    m_showScores = false;
                    if (m_once)
                    {
                        m_once = false;
                        m_playerController.EndGame();
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
                    }
                    else
                    {
                        if (PhotonNetwork.room != null)
                            m_gameTime = (float)PhotonNetwork.room.customProperties["GameTime"];
                    }
                    break;
                case eGameState.GAME_STATE_SPECTATING:
                    if (Input.GetKey(KeyCode.Tab))
                    {
                        m_showScores = true;
                    }
                    else
                    {
                        m_showScores = false;
                    }
                    if (PhotonNetwork.room != null)
                        m_gameTime = (float)PhotonNetwork.room.customProperties["GameTime"];

                    PhotonPlayer[] playerList = PhotonNetwork.playerList;
                    List<PhotonPlayer> playerListList = new List<PhotonPlayer>();
                    for (int i = 0; i < playerList.Length; i++)
                    {
                        playerListList.Add(playerList[i]);
                    }

                    int count = 0;
                    while (count < playerListList.Count)
                    {
                        if (playerListList[count].customProperties["Team"] == null || (int)playerListList[count].customProperties["Team"] == 0)
                        {
                            playerListList.RemoveAt(count);
                        }
                        else
                        {
                            count++;
                        }
                    }
                    playerList = playerListList.ToArray().OrderByDescending(x => (int)x.customProperties["Score"]).ToArray();

                    int playerIndex = -1;

                    if (m_spectatingTarget != null)
                    {
                        for (int i = 0; i < playerList.Length; i++)
                        {
                            if (playerList[i] == m_spectatingTarget)
                            {
                                playerIndex = i;
                                break;
                            }
                        }
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (playerIndex == 0 || playerIndex == -1)
                        {
                            playerIndex = playerList.Length - 1;
                        }
                        else
                        {
                            playerIndex--;
                        }
                    }
                    else if (Input.GetMouseButtonDown(1))
                    {
                        if (playerIndex == playerList.Length - 1 || playerIndex == -1)
                        {
                            playerIndex = 0;
                        }
                        else
                        {
                            playerIndex++;
                        }
                    }

                    if (playerIndex != -1)
                        m_spectatingTarget = playerList[playerIndex];

                    break;
            }
            if (!PhotonNetwork.isMasterClient && PhotonNetwork.room != null)
            {
                m_team1Score = (float)PhotonNetwork.room.customProperties["Team1Score"];
                m_team2Score = (float)PhotonNetwork.room.customProperties["Team2Score"];
            }
        }

        public void AddSpawnedShip(ShipController aShip)
        {
            m_spawnedShips.Add(aShip);

            if (aShip.m_team == 1)
            {
                m_team1Ships.Add(aShip);
            }
            else if (aShip.m_team == 2)
            {
                m_team2Ships.Add(aShip);
            }
        }

        private PhotonPlayer m_spectatingTarget;

        void LateUpdate()
        {
            if (m_gameState == eGameState.GAME_STATE_SPECTATING)
            {
                Vector3 camPos = Camera.main.transform.position;

                GameObject[] planes = GameObject.FindGameObjectsWithTag("Plane");
                Vector3 targetPos = Vector3.zero;
                if (planes.Length > 0)
                {
                    for (int i = 0; i < planes.Length; i++)
                    {
                        if (planes[i].GetComponent<PhotonView>().owner == m_spectatingTarget)
                        {
                            targetPos = planes[i].transform.position;
                        }
                        planes[i].transform.FindChild("NameTag").GetComponent<TextMesh>().characterSize = 0.14f;
                    }
                }
                else
                {
                    targetPos = camPos;
                }

                camPos = Vector3.Lerp(Camera.main.transform.position, new Vector3(targetPos.x, targetPos.y, -180), 5 * Time.deltaTime);

                Camera.main.transform.position = camPos;
            }
        }

        [PunRPC]
        void EndGame()
        {
            StopCoroutine("RespawnPlayer");
            m_gameState = eGameState.GAME_STATE_GAME_OVER;
            m_allyShipSunk.SetActive(false);
            m_enemyShipSunk.SetActive(false);
            m_redShipLogo.SetActive(false);
            m_greenShipLogo.SetActive(false);
            m_playerController.DestroyPlayerPlane();
        }

        [PunRPC]
        void ResetGame()
        {
            m_roomProperties = PhotonNetwork.room.customProperties;
            m_roomProperties["IsPlaying"] = 0;
            PhotonNetwork.room.SetCustomProperties(m_roomProperties);
            PhotonNetwork.LoadLevel("Game");
        }

        public void HitShipTargetPoint(ShipController.ShipTarget aShipTarget, BombController.BombInfo aBombInfo)
        {
            m_photonView.RPC("HitShipTargetPointRPC", PhotonTargets.AllBuffered, aShipTarget, aBombInfo);
        }

        public void RespawnShip(ShipController aShip)
        {
            if (PhotonNetwork.isMasterClient)
            {
                m_photonView.RPC("RespawnShipRPC", PhotonTargets.AllBuffered, aShip.m_shipID);
            }
        }

        [PunRPC]
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

        [PunRPC]
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
                m_playerProperties["Score"] = (int)m_playerProperties["Score"] + BrainCloudStats.Instance.m_pointsForWeakpointDestruction;
            }

            if (PhotonNetwork.isMasterClient)
            {
                if ((int)aBombInfo.m_shooter.customProperties["Team"] == 1)
                {
                    m_team1Score += BrainCloudStats.Instance.m_pointsForWeakpointDestruction;
                    m_roomProperties = PhotonNetwork.room.customProperties;
                    m_roomProperties["Team1Score"] = m_team1Score;
                    PhotonNetwork.room.SetCustomProperties(m_roomProperties);
                }
                else if ((int)aBombInfo.m_shooter.customProperties["Team"] == 2)
                {
                    m_team2Score += BrainCloudStats.Instance.m_pointsForWeakpointDestruction;
                    m_roomProperties = PhotonNetwork.room.customProperties;
                    m_roomProperties["Team2Score"] = m_team2Score;
                    PhotonNetwork.room.SetCustomProperties(m_roomProperties);
                }
            }

            Plane[] frustrum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(frustrum, ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Collider>().bounds))
            {
                m_playerController.ShakeCamera(BrainCloudStats.Instance.m_weakpointIntensity, BrainCloudStats.Instance.m_shakeTime);
            }

            if (shipTarget == null) return;
            GameObject explosion = (GameObject)Instantiate(m_weakpointExplosion, shipTarget.m_position.position, shipTarget.m_position.rotation);
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

        IEnumerator FadeOutShipMessage(GameObject aText, GameObject aLogo)
        {
            float time = 0.5f;
            m_allyShipSunk.SetActive(true);
            m_enemyShipSunk.SetActive(true);
            m_redShipLogo.SetActive(true);
            m_greenShipLogo.SetActive(true);
            m_allyShipSunk.GetComponent<Image>().color = new Color(1, 1, 1, 0);
            m_enemyShipSunk.GetComponent<Image>().color = new Color(1, 1, 1, 0);
            m_redShipLogo.GetComponent<Image>().color = new Color(1, 1, 1, 0);
            m_greenShipLogo.GetComponent<Image>().color = new Color(1, 1, 1, 0);
            Color fadeColor = new Color(1, 1, 1, 0);
            while (time > 0)
            {
                time -= Time.fixedDeltaTime;
                fadeColor = new Color(1, 1, 1, fadeColor.a + Time.fixedDeltaTime * 2.4f);
                aText.GetComponent<Image>().color = fadeColor;
                aLogo.GetComponent<Image>().color = fadeColor;
                yield return YieldFactory.GetWaitForFixedUpdate();
            }
            time = 2;
            aText.GetComponent<Image>().color = new Color(1, 1, 1, 1);
            aLogo.GetComponent<Image>().color = new Color(1, 1, 1, 1);
            fadeColor = new Color(1, 1, 1, 1);
            yield return YieldFactory.GetWaitForSeconds(2);

            while (time > 0)
            {
                time -= Time.fixedDeltaTime;
                fadeColor = new Color(1, 1, 1, fadeColor.a - Time.fixedDeltaTime);
                aText.GetComponent<Image>().color = fadeColor;
                aLogo.GetComponent<Image>().color = fadeColor;
                yield return YieldFactory.GetWaitForFixedUpdate();
            }
        }

        public void DestroyedShip(ShipController aShip, BombController.BombInfo aBombInfo)
        {
            m_photonView.RPC("DestroyedShipRPC", PhotonTargets.AllBuffered, aShip.m_shipID, aBombInfo);
        }

        [PunRPC]
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
                    m_team1Score += BrainCloudStats.Instance.m_pointsForShipDestruction;
                    m_roomProperties = PhotonNetwork.room.customProperties;
                    m_roomProperties["Team1Score"] = m_team1Score;
                    PhotonNetwork.room.SetCustomProperties(m_roomProperties);
                }
                else
                {
                    m_team2Score += BrainCloudStats.Instance.m_pointsForShipDestruction;
                    m_roomProperties = PhotonNetwork.room.customProperties;
                    m_roomProperties["Team2Score"] = m_team2Score;
                    PhotonNetwork.room.SetCustomProperties(m_roomProperties);
                }
            }

            Plane[] frustrum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(frustrum, ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Collider>().bounds))
            {
                m_playerController.ShakeCamera(BrainCloudStats.Instance.m_shipIntensity, BrainCloudStats.Instance.m_shakeTime);
            }

            if (ship == null) return;

            ship.m_isAlive = false;
            StopCoroutine("FadeOutShipMessage");
            if (ship.m_team == 1)
            {
                if ((int)PhotonNetwork.player.customProperties["Team"] == 1)
                {
                    StartCoroutine(FadeOutShipMessage(m_allyShipSunk, m_greenShipLogo));
                }
                else if ((int)PhotonNetwork.player.customProperties["Team"] == 2)
                {
                    StartCoroutine(FadeOutShipMessage(m_enemyShipSunk, m_greenShipLogo));
                }
            }
            else
            {
                if ((int)PhotonNetwork.player.customProperties["Team"] == 1)
                {
                    StartCoroutine(FadeOutShipMessage(m_enemyShipSunk, m_redShipLogo));
                }
                else if ((int)PhotonNetwork.player.customProperties["Team"] == 2)
                {
                    StartCoroutine(FadeOutShipMessage(m_allyShipSunk, m_redShipLogo));
                }
            }


            string shipName = "";
            ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().enabled = false;
            int children = ship.transform.childCount;
            for (int i = 1; i < children; i++)
            {
                var particleSys = ship.transform.GetChild(i).GetChild(0).GetChild(4).GetComponent<ParticleSystem>();
                if (particleSys)
                {
                    var emission = particleSys.emission;
                    emission.enabled = false;
                }
            }
            try
            {
                Destroy(ship.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject);
            }
            catch (System.Exception)
            {

            }

            int bomberTeam = (int)aBombInfo.m_shooter.customProperties["Team"];
            GameObject prefab = null;
            
            switch (ship.GetShipType())
            {
                case ShipController.eShipType.SHIP_TYPE_CARRIER:
                    prefab = bomberTeam == 1 ? m_carrierExplosion02 : m_carrierExplosion01;
                    break;
                case ShipController.eShipType.SHIP_TYPE_BATTLESHIP:
                    prefab = bomberTeam == 1 ? m_battleshipExplosion02 : m_battleshipExplosion01;
                    break;
                case ShipController.eShipType.SHIP_TYPE_CRUISER:
                    prefab = bomberTeam == 1 ? m_cruiserExplosion02 : m_cruiserExplosion01;
                    break;
                case ShipController.eShipType.SHIP_TYPE_PATROLBOAT:
                    prefab = bomberTeam == 1 ? m_patrolBoatExplosion02 : m_patrolBoatExplosion01;
                    break;
                case ShipController.eShipType.SHIP_TYPE_DESTROYER:
                    prefab = bomberTeam == 1 ? m_destroyerExplosion02 : m_destroyerExplosion01;
                    break;
            }

            if (prefab != null)
            {
                GameObject explosion = (GameObject)Instantiate(prefab, ship.transform.position, ship.transform.rotation);
                explosion.GetComponent<AudioSource>().Play();
            }

            if (PhotonNetwork.isMasterClient)
                ship.StartRespawn();

            if (aBombInfo.m_shooter == PhotonNetwork.player)
            {
                m_carriersDestroyed++;
                m_playerProperties["Score"] = (int)m_playerProperties["Score"] + BrainCloudStats.Instance.m_pointsForShipDestruction;
            }
        }

        void AwardExperience(int aWinningTeam)
        {
            m_photonView.RPC("AwardExperienceRPC", PhotonTargets.All, aWinningTeam);
        }

        [PunRPC]
        void AwardExperienceRPC(int aWinningTeam)
        {
            if ((int)PhotonNetwork.player.customProperties["Team"] == 0) return;

            m_timesDestroyed = (int)PhotonNetwork.player.customProperties["Deaths"];
            m_planesDestroyed = (int)PhotonNetwork.player.customProperties["Kills"];
            int gamesWon = ((int)PhotonNetwork.player.customProperties["Team"] == aWinningTeam) ? 1 : 0;
            if (m_planesDestroyed >= 5)
            {
                BrainCloudStats.Instance.Get5KillsAchievement();
            }
            BrainCloudStats.Instance.IncrementStatisticsToBrainCloud(1, gamesWon, m_timesDestroyed, m_shotsFired, m_bombsDropped, m_planesDestroyed, m_carriersDestroyed, m_bombsHit);
            BrainCloudStats.Instance.IncrementExperienceToBrainCloud(m_planesDestroyed * BrainCloudStats.Instance.m_expForKill);
            BrainCloudStats.Instance.SubmitLeaderboardData(m_planesDestroyed, m_bombsHit, m_timesDestroyed);
            m_shotsFired = 0;
            m_bombsDropped = 0;
            m_bombsHit = 0;
            m_planesDestroyed = 0;
            m_carriersDestroyed = 0;
            m_timesDestroyed = 0;
        }

        public void SpawnFlare(Vector3 aPosition, Vector3 aVelocity)
        {
            m_photonView.RPC("SpawnFlareRPC", PhotonTargets.All, aPosition, aVelocity, PhotonNetwork.player);
        }

        [PunRPC]
        void SpawnFlareRPC(Vector3 aPosition, Vector3 aVelocity, PhotonPlayer aPlayer)
        {
            GameObject flare = (GameObject)Instantiate(m_flare, aPosition, Quaternion.identity);
            flare.GetComponent<FlareController>().Activate(aPlayer);
            flare.GetComponent<Rigidbody>().velocity = aVelocity;
        }

        [PunRPC]
        void GetReady()
        {
            m_gameState = eGameState.GAME_STATE_STARTING_GAME;
            m_playerProperties["Deaths"] = 0;
            m_playerProperties["Kills"] = 0;
            m_playerProperties["IsReady"] = "true";
            PhotonNetwork.player.SetCustomProperties(m_playerProperties);
        }

        [PunRPC]
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
                m_playerController.SetPlayerPlane(playerPlane.GetComponent<PlaneController>());
                playerPlane.GetComponent<Rigidbody>().isKinematic = false;
                m_gameState = eGameState.GAME_STATE_PLAYING_GAME;
            }
        }

        public void DespawnBombPickup(int aPickupID)
        {
            m_photonView.RPC("DespawnBombPickupRPC", PhotonTargets.All, aPickupID);
        }

        [PunRPC]
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
            int bombID = Random.Range(-20000000, 20000000) * 100 + PhotonNetwork.player.ID;
            PhotonNetwork.room.SetCustomProperties(m_roomProperties);
            m_photonView.RPC("SpawnBombPickupRPC", PhotonTargets.All, aPosition, bombID);
        }

        [PunRPC]
        void SpawnBombPickupRPC(Vector3 aPosition, int bombID)
        {
            GameObject bombPickup = (GameObject)Instantiate(m_bombPickup, aPosition, Quaternion.identity);
            bombPickup.GetComponent<BombPickup>().Activate(bombID);
            m_bombPickupsSpawned.Add(bombPickup.GetComponent<BombPickup>());
        }

        public void BombPickedUp(PhotonPlayer aPlayer, int aPickupID)
        {
            m_photonView.RPC("BombPickedUpRPC", PhotonTargets.All, aPlayer, aPickupID);
        }

        [PunRPC]
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
                m_playerController.GetComponent<WeaponController>().AddBomb();
            }
        }

        public void SpawnBomb(BombController.BombInfo aBombInfo)
        {
            m_bombsDropped++;
            int id = GetNextBombID();
            aBombInfo.m_bombID = id;
            m_photonView.RPC("SpawnBombRPC", PhotonTargets.All, aBombInfo);
        }

        [PunRPC]
        void SpawnBombRPC(BombController.BombInfo aBombInfo)
        {
            if (PhotonNetwork.isMasterClient)
            {
                aBombInfo.m_isMaster = true;
            }

            GameObject bomb = m_playerController.GetComponent<WeaponController>().SpawnBomb(aBombInfo);
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
            m_photonView.RPC("DeleteBombRPC", PhotonTargets.All, aBombInfo, aHitSurface);
        }

        [PunRPC]
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
                        explosion = (GameObject)Instantiate(m_bombWaterExplosion, bomb.transform.position, Quaternion.identity);
                        explosion.GetComponent<AudioSource>().Play();
                    }
                    else if (aHitSurface == 1)
                    {
                        explosion = (GameObject)Instantiate(m_bombExplosion, bomb.transform.position, Quaternion.identity);
                        explosion.GetComponent<AudioSource>().Play();
                    }
                    else
                    {
                        explosion = (GameObject)Instantiate(m_bombDud, bomb.transform.position, Quaternion.identity);
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
            m_photonView.RPC("SpawnBulletRPC", PhotonTargets.All, aBulletInfo);
        }

        [PunRPC]
        void SpawnBulletRPC(BulletController.BulletInfo aBulletInfo)
        {
            if (PhotonNetwork.player == aBulletInfo.m_shooter)
            {
                aBulletInfo.m_isMaster = true;
            }

            GameObject bullet = m_playerController.GetComponent<WeaponController>().SpawnBullet(aBulletInfo);

            if (bullet == null) return;

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
            m_photonView.RPC("DeleteBulletRPC", PhotonTargets.All, aBulletInfo);
        }

        [PunRPC]
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
            m_photonView.RPC("BulletHitPlayerRPC", PhotonTargets.All, relativeHitPoint, aBulletInfo, shooter, hitPlayer);
        }

        [PunRPC]
        void BulletHitPlayerRPC(Vector3 aHitPoint, BulletController.BulletInfo aBulletInfo, PhotonPlayer aShooter, PhotonPlayer aHitPlayer)
        {
            var planes = GameObject.FindGameObjectsWithTag("Plane");
            for (int i = 0; i < planes.Length; ++i)
            {
                if (planes[i].GetComponent<PhotonView>().owner == aHitPlayer)
                {
                    Instantiate(m_bulletHit, planes[i].transform.position + aHitPoint, Quaternion.LookRotation(aBulletInfo.m_startDirection, -Vector3.forward));
                    break;
                }
            }

            if (aHitPlayer == PhotonNetwork.player)
            {
                m_playerController.TakeBulletDamage(aShooter);
            }
        }

        public void DestroyPlayerPlane(PhotonPlayer aVictim, PhotonPlayer aShooter = null)
        {
            m_photonView.RPC("DestroyPlayerPlaneRPC", PhotonTargets.All, aVictim, aShooter);
        }

        [PunRPC]
        void DestroyPlayerPlaneRPC(PhotonPlayer aVictim, PhotonPlayer aShooter)
        {
            var planes = GameObject.FindGameObjectsWithTag("Plane");
            for (int i = 0; i < planes.Length; ++i)
            {
                if (planes[i].GetComponent<PhotonView>().owner == aVictim)
                {
                    GameObject explosion = (GameObject)Instantiate(m_playerExplosion, planes[i].transform.position, planes[i].transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    break;
                }
            }

            if (m_gameState == eGameState.GAME_STATE_SPECTATING)
            {
                if (m_spectatingTarget == aVictim)
                    m_spectatingTarget = aShooter;
            }

            if (aShooter == null)
            {

                if (aVictim == PhotonNetwork.player)
                {
                    m_playerController.DestroyPlayerPlane();
                    m_playerProperties["Deaths"] = (int)PhotonNetwork.player.customProperties["Deaths"] + 1;
                    PhotonNetwork.player.SetCustomProperties(m_playerProperties);
                    StopCoroutine("RespawnPlayer");
                    StartCoroutine("RespawnPlayer");
                }
            }
            else
            {

                if (aVictim == PhotonNetwork.player)
                {
                    m_playerController.DestroyPlayerPlane();
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
            return Random.Range(-20000000, 20000000) * 100 + PhotonNetwork.player.ID;
        }

        int GetNextBombID()
        {
            return Random.Range(-20000000, 20000000) * 100 + PhotonNetwork.player.ID;
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

                yield return YieldFactory.GetWaitForSeconds(0.5f);
            }

            m_gameState = eGameState.GAME_STATE_SPAWN_PLAYERS;
        }

    }
}