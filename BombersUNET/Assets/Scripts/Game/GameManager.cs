/*
 * The GameManager class is mainly used by the server, as none of its functions can be called on the client side. It 
 * controls the basic progress of the game, populating the scoreboards and dealing with players connecting.
 * 
 */

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using BrainCloudUNETExample.Connection;
using BrainCloudUNETExample.Game.PlayerInput;
using UnityEngine.SceneManagement;

namespace BrainCloudUNETExample.Game
{
    public class GameManager : NetworkBehaviour
    {

        public enum eGameState
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

        [SerializeField]
        public eGameState m_gameState = eGameState.GAME_STATE_INITIALIZE_GAME;

        public GUISkin m_skin;

        public int m_respawnTime = 3;

        public List<BulletController.BulletInfo> m_spawnedBullets;
        public List<BombController.BombInfo> m_spawnedBombs;

        public float m_gameTime = 10 * 60;

        public int m_mapLayout = 0;
        public int m_mapSize = 1;

        public List<MapPresets.Preset> m_mapPresets;
        public List<MapPresets.MapSize> m_mapSizes;

        public float m_currentRespawnTime = 0;

        public float m_team1Score = 0;
        public float m_team2Score = 0;
        public int m_shotsFired = 0;
        public int m_bombsDropped = 0;
        public int m_bombsHit = 0;
        public int m_planesDestroyed = 0;
        public int m_carriersDestroyed = 0;
        public int m_timesDestroyed = 0;

        public bool m_once = false;

        [SerializeField]
        public Collider m_team1SpawnBounds;

        [SerializeField]
        public Collider m_team2SpawnBounds;

        public List<BombPickup> m_bombPickupsSpawned;
        public int m_bombID;

        public List<ShipController> m_spawnedShips;

        public GameObject m_gameStartButton;

        public GameObject m_resultsWindow;
        public GameObject m_greenLogo;
        public GameObject m_redLogo;
        public GameObject m_enemyWinText;
        public GameObject m_allyWinText;
        public GameObject m_resetButton;
        public GameObject m_quitButton;
        public GameObject m_greenChevron;
        public GameObject m_redChevron;

        public GameObject m_HUD;

        public GameObject m_allyShipSunk;
        public GameObject m_enemyShipSunk;
        public GameObject m_greenShipLogo;
        public GameObject m_redShipLogo;

        public GameObject m_quitMenu;
        public bool m_showQuitMenu;
        public GameObject m_blackScreen;

        public bool m_showScores = false;

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

        private Text m_countDownText;

        void Awake()
        {
            BombersNetworkManager.RefreshBCVariable();
            if (!BombersNetworkManager._BC.Client.Initialized)
            {
                SceneManager.LoadScene("Connect");
                return;
            }

            m_allyShipSunk = GameObject.Find("ShipSink").transform.Find("AllyShipSunk").gameObject;
            m_enemyShipSunk = GameObject.Find("ShipSink").transform.Find("EnemyShipSunk").gameObject;
            m_redShipLogo = GameObject.Find("ShipSink").transform.Find("RedLogo").gameObject;
            m_greenShipLogo = GameObject.Find("ShipSink").transform.Find("GreenLogo").gameObject;
            m_blackScreen = GameObject.Find("BlackScreen");
            m_countDownText = GameObject.Find("CountDown").GetComponent<Text>();

            m_allyShipSunk.SetActive(false);
            m_enemyShipSunk.SetActive(false);
            m_redShipLogo.SetActive(false);
            m_greenShipLogo.SetActive(false);
            m_quitMenu = GameObject.Find("QuitMenu");
            m_quitMenu.SetActive(false);

            m_greenChevron = GameObject.Find("Team Green Score").transform.Find("Chevron").gameObject;
            m_redChevron = GameObject.Find("Team Red Score").transform.Find("Chevron").gameObject;
            m_greenLogo = GameObject.Find("Green Logo");
            m_greenLogo.SetActive(false);
            m_redLogo = GameObject.Find("Red Logo");
            m_redLogo.SetActive(false);
            m_enemyWinText = GameObject.Find("Window Title - Loss");
            m_allyWinText = GameObject.Find("Window Title - Win");
            m_resetButton = GameObject.Find("Continue");
            m_quitButton = GameObject.Find("ResultsQuit");
            m_gameStartButton = GameObject.Find("StartGame");
            m_gameTime = BrainCloudStats.Instance.m_defaultGameTime;
            m_mapPresets = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_presets;
            m_mapSizes = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_mapSizes;
            m_resultsWindow = GameObject.Find("Results");
            m_resultsWindow.SetActive(false);
            m_HUD = GameObject.Find("HUD");
            GameObject.Find("RespawnText").GetComponent<Text>().text = "";

            m_missionText = m_HUD.transform.Find("MissionText").gameObject;
            m_missionText.SetActive(false);
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

        public GameObject m_missionText;
        public GameInfo m_gameInfo;

        void Initialize()
        {
            m_mapLayout = m_gameInfo.GetMapLayout();
            m_mapSize = m_gameInfo.GetMapSize();

            if (isServer)
                RpcSetLightPosition(m_gameInfo.GetLightPosition());

            m_spawnedShips = new List<ShipController>();
            m_bombPickupsSpawned = new List<BombPickup>();

            m_spawnedBullets = new List<BulletController.BulletInfo>();
            m_spawnedBombs = new List<BombController.BombInfo>();
            m_skin = (GUISkin)Resources.Load("skin");
            StartCoroutine("UpdatePing");
            StartCoroutine("UpdateRoomDisplayName");

            m_team1Score = 0;
            m_team2Score = 0;

            {
                if (isServer)
                {
                    m_gameInfo.SetLightPosition(Random.Range(1, 5));
                    RpcSetLightPosition(m_gameInfo.GetLightPosition());
                }

                if (BombersNetworkManager.LobbyInfo != null)
                {
                    BCLobbyMemberInfo member = BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(BombersNetworkManager.LocalPlayer.m_profileId);
                    if (member.Team == "red")
                    {
                        // red
                        BombersNetworkManager.LocalPlayer.m_team = 2;
                        m_gameInfo.SetTeamPlayers(2, m_gameInfo.GetTeamPlayers(2) + 1);
                    }
                    else
                    {
                        // green
                        BombersNetworkManager.LocalPlayer.m_team = 1;
                        m_gameInfo.SetTeamPlayers(1, m_gameInfo.GetTeamPlayers(1) + 1);
                    }

                }
            }
            BombersNetworkManager.LocalPlayer.m_score = 0;

            if (m_gameInfo.GetPlaying() == 1)
            {
                //GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().IsPlaying();
                //LeaveRoom();
                m_gameState = eGameState.GAME_STATE_SPECTATING;
                updateSpectorFocusController();
                BombersNetworkManager.LocalPlayer.AnnounceJoinCommand();
            }
        }

        void Start()
        {
            StartCoroutine("WaitForGameInfo");
        }

        IEnumerator WaitForGameInfo()
        {
            while (GameObject.Find("GameInfo").GetComponent<GameInfo>() == null || BombersNetworkManager.LocalPlayer == null)
            {
                yield return new WaitForSeconds(0);
            }
            m_gameInfo = GameObject.Find("GameInfo").GetComponent<GameInfo>();
            Initialize();
        }

        [Command]
        void CmdAnnounceJoin(int aPlayerID, string aPlayerName, int aTeam)
        {
            StartCoroutine(RespawnPlayer(aPlayerID));
            RpcAnnounceJoin(aPlayerName, aTeam);
        }

        [ClientRpc]
        void RpcAnnounceJoin(string aPlayerName, int aTeam)
        {
            string message = aPlayerName + " has joined the fight\n on the ";
            message += (aTeam == 1) ? "green team!" : "red team!";
            GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog(message, true);
        }

        [ClientRpc]
        void RpcSetLightPosition(int aPosition)
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

        void OnApplicationQuit()
        {
            LeaveRoom();
        }

        public void LeaveRoom()
        {
            BombersNetworkManager networkMgr = BombersNetworkManager.singleton as BombersNetworkManager;
            if (BombersNetworkManager.LocalPlayer.m_team == 1)
            {
                m_gameInfo.SetTeamPlayers(1, m_gameInfo.GetTeamPlayers(1) - 1);
            }
            else if (BombersNetworkManager.LocalPlayer.m_team == 1)
            {
                m_gameInfo.SetTeamPlayers(2, m_gameInfo.GetTeamPlayers(2) - 1);
            }

            if (isServer)
            {
                networkMgr.matchMaker.DestroyMatch(networkMgr.matchInfo.networkId, 0, DestroyMatchCallback);
            }
            else
            {
                networkMgr.StopMatchMaker();
                networkMgr.StopClient();
                networkMgr.StartMatchMaker();
            }
            networkMgr.LeaveLobby();
        }

        [ClientRpc]
        public void RpcLeaveRoom()
        {
            LeaveRoom();
        }

        public void DestroyMatchCallback(bool success, string extendedInfo)
        {
            if (isServer)
            {
                NetworkManager.singleton.StopHost();
            }
            else
            {
                NetworkManager.singleton.client.Disconnect();
            }
            NetworkManager.singleton.StartMatchMaker();
        }

        [Command]
        public void CmdForceStartGame()
        {
            m_gameState = eGameState.GAME_STATE_STARTING_GAME;
            RpcForceStartGame();
        }

        [ClientRpc]
        public void RpcForceStartGame()
        {
            m_gameState = eGameState.GAME_STATE_STARTING_GAME;
        }

        public void CloseQuitMenu()
        {
            m_showQuitMenu = false;
        }

        void OnGUI()
        {
            GUI.skin = m_skin;
            switch (m_gameState)
            {
                case eGameState.GAME_STATE_WAITING_FOR_PLAYERS:
                    if (isServer)
                        CmdForceStartGame();
                    else
                        m_gameState = eGameState.GAME_STATE_STARTING_GAME;

                    /*
                        m_resultsWindow.GetComponent<CanvasGroup>().alpha = 0;

                        //m_lobbyWindow.gameObject.SetActive(true);
                        m_resultsWindow.gameObject.SetActive(false);
                        m_HUD.SetActive(false);
                        OnWaitingForPlayersWindow();
                        */
                    break;
                case eGameState.GAME_STATE_STARTING_GAME:
                    m_blackScreen.GetComponent<CanvasGroup>().alpha += Time.fixedDeltaTime * 3;
                    m_resultsWindow.GetComponent<CanvasGroup>().alpha = 0;
                    //m_lobbyWindow.gameObject.SetActive(true);
                    m_resultsWindow.gameObject.SetActive(false);
                    m_HUD.SetActive(false);
                    //OnWaitingForPlayersWindow();
                    break;
                case eGameState.GAME_STATE_SPECTATING:
                    m_blackScreen.SetActive(false);
                    //m_lobbyWindow.gameObject.SetActive(false);
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
                    m_HUD.SetActive(true);
                    OnHudWindow();
                    GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 20, 200, 20), "Spectating : " + (m_spectatorFocusController != null ? m_spectatorFocusController.m_displayName : ""));
                    break;
                case eGameState.GAME_STATE_GAME_OVER:
                    //m_lobbyWindow.gameObject.SetActive(false);
                    m_resultsWindow.gameObject.SetActive(true);
                    m_HUD.SetActive(false);

                    OnScoresWindow();
                    break;

                case eGameState.GAME_STATE_PLAYING_GAME:
                    m_blackScreen.GetComponent<CanvasGroup>().alpha -= Time.fixedDeltaTime * 3;
                    //m_lobbyWindow.gameObject.SetActive(false);
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
                    //m_lobbyWindow.gameObject.SetActive(false);
                    m_resultsWindow.gameObject.SetActive(false);
                    m_HUD.SetActive(false);
                    break;
            }
        }

        private int m_spectatingIndex = 0;
        private BombersPlayerController m_spectatorFocusController = null;

        private void incrementSpectatorIndex(int in_increment)
        {
            GameObject[] playerList = GameObject.FindGameObjectsWithTag("PlayerController");
            if (in_increment > 0)
            {
                if (m_spectatingIndex < playerList.Length - 1)
                {
                    ++m_spectatingIndex;
                }
                else
                {
                    m_spectatingIndex = 0;
                }
            }
            else
            {
                if (m_spectatingIndex > 0)
                {
                    --m_spectatingIndex;
                }
                else
                {
                    m_spectatingIndex = playerList.Length - 1;
                }
            }
        }

        private void updateSpectorFocusController()
        {
            GameObject[] playerList = GameObject.FindGameObjectsWithTag("PlayerController");

            foreach (GameObject item in playerList)
            {
                if (item.GetComponent<BombersPlayerController>().m_profileId == BombersNetworkManager._BC.Client.ProfileId)
                {
                    item.tag = "Untagged";
                    item.SetActive(false);
                    //Destroy(item);
                    break;
                }
            }
            GameObject playerController = playerList.GetValue(m_spectatingIndex) as GameObject;
            m_spectatorFocusController = playerController != null ? playerController.GetComponent<BombersPlayerController>() : null;
            if (m_spectatorFocusController == null || m_spectatorFocusController.m_profileId == BombersNetworkManager._BC.Client.ProfileId)
            {
                incrementSpectatorIndex(1);
                updateSpectorFocusController();
            }
        }

        private void focusOnCurrentPlane()
        {
            if (m_spectatorFocusController != null)
            {
                PlaneController playerPlane = m_spectatorFocusController.m_playerPlane;
                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(playerPlane.transform.Find("CameraPosition").position.x, playerPlane.transform.Find("CameraPosition").position.y, -110), 0.05f);
                Camera.main.transform.GetChild(0).position = playerPlane.transform.position;
                playerPlane.GetComponent<AudioSource>().spatialBlend = 0;
            }
        }

        void OnHudWindow()
        {
            m_team1Score = m_gameInfo.GetTeamScore(1);
            m_team2Score = m_gameInfo.GetTeamScore(2);
            int score = BombersNetworkManager.LocalPlayer.m_score;
            System.TimeSpan span = System.TimeSpan.FromSeconds(m_gameTime);
            string timeLeft = span.ToString().Substring(3, 5);

            List<ShipController> team1Ships = new List<ShipController>();
            List<ShipController> team2Ships = new List<ShipController>();
            for (int i = 0; i < m_spawnedShips.Count; i++)
            {
                if (m_spawnedShips[i].m_team == 1 && m_spawnedShips[i].IsAlive())
                {
                    team1Ships.Add(m_spawnedShips[i]);
                }
                else if (m_spawnedShips[i].m_team == 2 && m_spawnedShips[i].IsAlive())
                {
                    team2Ships.Add(m_spawnedShips[i]);
                }
            }

            Transform playerScore = m_HUD.transform.GetChild(0).GetChild(0).Find("PlayerScore");
            playerScore.GetChild(0).GetComponent<Text>().text = score.ToString("n0");
            playerScore.gameObject.SetActive(m_gameState != eGameState.GAME_STATE_SPECTATING);

            m_HUD.transform.GetChild(0).GetChild(1).Find("RedScore").GetChild(0).GetComponent<Text>().text = m_team2Score.ToString("n0");
            m_HUD.transform.GetChild(0).GetChild(1).Find("RedScore").GetChild(1).GetComponent<Text>().text = m_gameState != eGameState.GAME_STATE_SPECTATING ? "Ships Left: " + (team2Ships.Count / 2).ToString() : "";
            if (team2Ships.Count == 2)
                m_HUD.transform.GetChild(0).GetChild(1).Find("RedScore").GetChild(1).GetComponent<Text>().color = new Color(1, 0, 0, 1);
            else
                m_HUD.transform.GetChild(0).GetChild(1).Find("RedScore").GetChild(1).GetComponent<Text>().color = new Color(1, 1, 1, 1);
            m_HUD.transform.GetChild(0).GetChild(1).Find("GreenScore").GetChild(0).GetComponent<Text>().text = m_team1Score.ToString("n0");
            m_HUD.transform.GetChild(0).GetChild(1).Find("GreenScore").GetChild(1).GetComponent<Text>().text = m_gameState != eGameState.GAME_STATE_SPECTATING ? "Ships Left: " + (team1Ships.Count / 2).ToString() : "";
            if (team1Ships.Count == 2)
                m_HUD.transform.GetChild(0).GetChild(1).Find("GreenScore").GetChild(1).GetComponent<Text>().color = new Color(1, 0, 0, 1);
            else
                m_HUD.transform.GetChild(0).GetChild(1).Find("GreenScore").GetChild(1).GetComponent<Text>().color = new Color(1, 1, 1, 1);

            m_HUD.transform.GetChild(0).GetChild(0).Find("TimeLeft").GetChild(0).GetComponent<Text>().text = timeLeft;
        }

        void OnMiniScoresWindow()
        {
            m_quitButton.SetActive(false);
            m_resetButton.SetActive(false);
            m_allyWinText.SetActive(false);
            m_enemyWinText.SetActive(false);
            m_greenLogo.SetActive(false);
            m_redLogo.SetActive(false);

            m_team1Score = m_gameInfo.GetTeamScore(1);
            m_team2Score = m_gameInfo.GetTeamScore(2);
            GameObject team = GameObject.Find("Team Green Score");
            team.transform.Find("Team Score").GetComponent<Text>().text = m_team1Score.ToString("n0");
            team = GameObject.Find("Team Red Score");
            team.transform.Find("Team Score").GetComponent<Text>().text = m_team2Score.ToString("n0");

            GameObject[] playerList = GameObject.FindGameObjectsWithTag("PlayerController");
            List<GameObject> playerListList = new List<GameObject>();

            for (int i = 0; i < playerList.Length; i++)
            {
                playerListList.Add(playerList[i]);
            }

            int count = 0;
            while (count < playerListList.Count)
            {
                if (playerListList[count].GetComponent<BombersPlayerController>().m_team == 0)
                {
                    playerListList.RemoveAt(count);
                }
                else
                {
                    count++;
                }
            }
            playerList = playerListList.ToArray().OrderByDescending(x => x.GetComponent<BombersPlayerController>().m_score).ToArray();

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
                if ((int)playerList[i].GetComponent<BombersPlayerController>().m_team == 1)
                {
                    if (playerList[i] == BombersNetworkManager.LocalPlayer)
                    {
                        m_redChevron.SetActive(false);
                        m_greenChevron.SetActive(true);
                        m_greenChevron.transform.GetChild(0).GetComponent<Text>().text = playerList[i].GetComponent<BombersPlayerController>().m_displayName;
                        m_greenChevron.transform.GetChild(1).GetComponent<Text>().text = playerList[i].GetComponent<BombersPlayerController>().m_kills + "/" + playerList[i].GetComponent<BombersPlayerController>().m_deaths;
                        m_greenChevron.transform.GetChild(2).GetComponent<Text>().text = (playerList[i].GetComponent<BombersPlayerController>().m_score).ToString("n0");
                        m_greenChevron.GetComponent<RectTransform>().localPosition = new Vector3(m_greenChevron.GetComponent<RectTransform>().localPosition.x, 21.8f - (greenPlayers * 17.7f), m_greenChevron.GetComponent<RectTransform>().localPosition.z);
                        greenNamesText += "\n";
                        greenKDText += "\n";
                        greenScoreText += "\n";
                    }
                    else
                    {
                        greenNamesText += playerList[i].GetComponent<BombersPlayerController>().m_displayName + "\n";
                        greenKDText += playerList[i].GetComponent<BombersPlayerController>().m_kills + "/" + playerList[i].GetComponent<BombersPlayerController>().m_deaths + "\n";
                        greenScoreText += (playerList[i].GetComponent<BombersPlayerController>().m_score).ToString("n0") + "\n";
                    }
                    greenPlayers++;
                }
                else
                {
                    if (playerList[i] == BombersNetworkManager.LocalPlayer)
                    {
                        m_redChevron.SetActive(true);
                        m_greenChevron.SetActive(false);
                        m_redChevron.transform.GetChild(0).GetComponent<Text>().text = playerList[i].GetComponent<BombersPlayerController>().m_displayName;
                        m_redChevron.transform.GetChild(1).GetComponent<Text>().text = playerList[i].GetComponent<BombersPlayerController>().m_kills + "/" + playerList[i].GetComponent<BombersPlayerController>().m_deaths;
                        m_redChevron.transform.GetChild(2).GetComponent<Text>().text = (playerList[i].GetComponent<BombersPlayerController>().m_score).ToString("n0");
                        m_redChevron.GetComponent<RectTransform>().localPosition = new Vector3(m_redChevron.GetComponent<RectTransform>().localPosition.x, 21.8f - (redPlayers * 17.7f), m_redChevron.GetComponent<RectTransform>().localPosition.z);

                        redNamesText += "\n";
                        redKDText += "\n";
                        redScoreText += "\n";
                    }
                    else
                    {
                        redNamesText += playerList[i].GetComponent<BombersPlayerController>().m_displayName + "\n";
                        redKDText += playerList[i].GetComponent<BombersPlayerController>().m_kills + "/" + playerList[i].GetComponent<BombersPlayerController>().m_deaths + "\n";
                        redScoreText += (playerList[i].GetComponent<BombersPlayerController>().m_score).ToString("n0") + "\n";
                    }
                    redPlayers++;
                }
            }

            team = GameObject.Find("Team Green Score");
            team.transform.Find("GreenPlayers").GetComponent<Text>().text = greenNamesText;
            team.transform.Find("GreenPlayerKD").GetComponent<Text>().text = greenKDText;
            team.transform.Find("GreenPlayerScores").GetComponent<Text>().text = greenScoreText;
            team = GameObject.Find("Team Red Score");
            team.transform.Find("RedPlayers").GetComponent<Text>().text = redNamesText;
            team.transform.Find("RedPlayerKD").GetComponent<Text>().text = redKDText;
            team.transform.Find("RedPlayerScores").GetComponent<Text>().text = redScoreText;
        }

        void OnScoresWindow()
        {
            m_resultsWindow.GetComponent<CanvasGroup>().alpha += Time.fixedDeltaTime * 2;
            if (m_resultsWindow.GetComponent<CanvasGroup>().alpha > 1) m_resultsWindow.GetComponent<CanvasGroup>().alpha = 1;
            m_team1Score = m_gameInfo.GetTeamScore(1);
            m_team2Score = m_gameInfo.GetTeamScore(2);
            GameObject team = GameObject.Find("Team Green Score");
            team.transform.Find("Team Score").GetComponent<Text>().text = m_team1Score.ToString("n0");
            team = GameObject.Find("Team Red Score");
            team.transform.Find("Team Score").GetComponent<Text>().text = m_team2Score.ToString("n0");

            if (m_gameState != eGameState.GAME_STATE_GAME_OVER)
            {
                m_quitButton.SetActive(false);
                m_resetButton.SetActive(false);
            }
            else if (!isServer)
            {
                m_quitButton.SetActive(true);
                m_resetButton.SetActive(false);
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
                    if (BombersNetworkManager.LocalPlayer.m_team == 1)
                    {
                        m_allyWinText.SetActive(true);
                        m_enemyWinText.SetActive(false);
                    }
                    else if (BombersNetworkManager.LocalPlayer.m_team == 2)
                    {
                        m_allyWinText.SetActive(false);
                        m_enemyWinText.SetActive(true);
                    }
                }
                else
                {
                    m_greenLogo.SetActive(false);
                    m_redLogo.SetActive(true);

                    if (BombersNetworkManager.LocalPlayer.m_team == 1)
                    {
                        m_allyWinText.SetActive(false);
                        m_enemyWinText.SetActive(true);
                    }
                    else if (BombersNetworkManager.LocalPlayer.m_team == 2)
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

            GameObject[] playerList = GameObject.FindGameObjectsWithTag("PlayerController");
            List<GameObject> playerListList = new List<GameObject>();

            for (int i = 0; i < playerList.Length; i++)
            {
                playerListList.Add(playerList[i]);
            }

            int count = 0;
            while (count < playerListList.Count)
            {
                if (playerListList[count].GetComponent<BombersPlayerController>().m_team == 0)
                {
                    playerListList.RemoveAt(count);
                }
                else
                {
                    count++;
                }
            }
            playerList = playerListList.ToArray().OrderByDescending(x => x.GetComponent<BombersPlayerController>().m_score).ToArray();

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
                if ((int)playerList[i].GetComponent<BombersPlayerController>().m_team == 1)
                {
                    if (playerList[i] == BombersNetworkManager.LocalPlayer)
                    {
                        m_redChevron.SetActive(false);
                        m_greenChevron.SetActive(true);
                        m_greenChevron.transform.GetChild(0).GetComponent<Text>().text = playerList[i].GetComponent<BombersPlayerController>().m_displayName;
                        m_greenChevron.transform.GetChild(1).GetComponent<Text>().text = playerList[i].GetComponent<BombersPlayerController>().m_kills + "/" + playerList[i].GetComponent<BombersPlayerController>().m_deaths;
                        m_greenChevron.transform.GetChild(2).GetComponent<Text>().text = (playerList[i].GetComponent<BombersPlayerController>().m_score).ToString("n0");
                        m_greenChevron.GetComponent<RectTransform>().localPosition = new Vector3(m_greenChevron.GetComponent<RectTransform>().localPosition.x, 21.8f - (greenPlayers * 17.7f), m_greenChevron.GetComponent<RectTransform>().localPosition.z);
                        greenNamesText += "\n";
                        greenKDText += "\n";
                        greenScoreText += "\n";
                    }
                    else
                    {
                        greenNamesText += playerList[i].GetComponent<BombersPlayerController>().m_displayName + "\n";
                        greenKDText += playerList[i].GetComponent<BombersPlayerController>().m_kills + "/" + playerList[i].GetComponent<BombersPlayerController>().m_deaths + "\n";
                        greenScoreText += (playerList[i].GetComponent<BombersPlayerController>().m_score).ToString("n0") + "\n";
                    }
                    greenPlayers++;
                }
                else
                {
                    if (playerList[i] == BombersNetworkManager.LocalPlayer)
                    {
                        m_redChevron.SetActive(true);
                        m_greenChevron.SetActive(false);
                        m_redChevron.transform.GetChild(0).GetComponent<Text>().text = playerList[i].GetComponent<BombersPlayerController>().m_displayName;
                        m_redChevron.transform.GetChild(1).GetComponent<Text>().text = playerList[i].GetComponent<BombersPlayerController>().m_kills + "/" + playerList[i].GetComponent<BombersPlayerController>().m_deaths;
                        m_redChevron.transform.GetChild(2).GetComponent<Text>().text = (playerList[i].GetComponent<BombersPlayerController>().m_score).ToString("n0");
                        m_redChevron.GetComponent<RectTransform>().localPosition = new Vector3(m_redChevron.GetComponent<RectTransform>().localPosition.x, 21.8f - (redPlayers * 17.7f), m_redChevron.GetComponent<RectTransform>().localPosition.z);

                        redNamesText += "\n";
                        redKDText += "\n";
                        redScoreText += "\n";
                    }
                    else
                    {
                        redNamesText += playerList[i].GetComponent<BombersPlayerController>().m_displayName + "\n";
                        redKDText += playerList[i].GetComponent<BombersPlayerController>().m_kills + "/" + playerList[i].GetComponent<BombersPlayerController>().m_deaths + "\n";
                        redScoreText += (playerList[i].GetComponent<BombersPlayerController>().m_score).ToString("n0") + "\n";
                    }
                    redPlayers++;
                }
            }

            team = GameObject.Find("Team Green Score");
            team.transform.Find("GreenPlayers").GetComponent<Text>().text = greenNamesText;
            team.transform.Find("GreenPlayerKD").GetComponent<Text>().text = greenKDText;
            team.transform.Find("GreenPlayerScores").GetComponent<Text>().text = greenScoreText;
            team = GameObject.Find("Team Red Score");
            team.transform.Find("RedPlayers").GetComponent<Text>().text = redNamesText;
            team.transform.Find("RedPlayerKD").GetComponent<Text>().text = redKDText;
            team.transform.Find("RedPlayerScores").GetComponent<Text>().text = redScoreText;
        }

        public void ChangeTeam()
        {
            BombersNetworkManager._BC.LobbyService.SwitchTeam(BombersNetworkManager.LobbyInfo.LobbyId, BombersNetworkManager.LobbyInfo.GetOppositeTeamCodeWithProfileId(BombersNetworkManager._BC.Client.ProfileId));
        }

        public BombersPlayerController FindPlayerWithID(int aID)
        {
            return BombersNetworkManager.LocalPlayer;
        }

        void OnWaitingForPlayersWindow()
        {
            GameObject[] playerList = GameObject.FindGameObjectsWithTag("PlayerController");
            List<GameObject> playerListList = new List<GameObject>();

            for (int i = 0; i < playerList.Length; i++)
            {
                playerListList.Add(playerList[i]);
            }

            int count = 0;
            while (count < playerListList.Count)
            {
                if (playerListList[count].GetComponent<BombersPlayerController>().m_team == 0)
                {
                    playerListList.RemoveAt(count);
                }
                else
                {
                    count++;
                }
            }
            playerList = playerListList.ToArray().OrderByDescending(x => x.GetComponent<BombersPlayerController>().m_score).ToArray();

            List<GameObject> greenPlayers = new List<GameObject>();
            List<GameObject> redPlayers = new List<GameObject>();
            BCLobbyMemberInfo member = null;
            BombersPlayerController bomberController = null;
            for (int i = 0; i < playerList.Length; i++)
            {
                bomberController = playerList[i].GetComponent<BombersPlayerController>();

                member = BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(bomberController.m_profileId);
                if (member != null)
                {
                    // setup the member info
                    if (bomberController.MemberInfo == null) bomberController.MemberInfo = member;

                    if (member.Team == "green")
                    {
                        greenPlayers.Add(playerList[i]);
                    }
                    else //if (bomberController.m_team == 2)
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
                nameText += greenPlayers[i].GetComponent<BombersPlayerController>().MemberInfo.Name + "\n";
                pingText += greenPlayers[i].GetComponent<BombersPlayerController>().MemberInfo.Rating + "\n";
            }


            teamText.text = nameText;
            teamPingText.text = pingText;
            teamText = GameObject.Find("RedPlayerNames").GetComponent<Text>();
            teamPingText = GameObject.Find("RedPings").GetComponent<Text>();
            nameText = "";
            pingText = "";

            for (int i = 0; i < redPlayers.Count; i++)
            {
                nameText += redPlayers[i].GetComponent<BombersPlayerController>().MemberInfo.Name + "\n";
                pingText += redPlayers[i].GetComponent<BombersPlayerController>().MemberInfo.Rating + "\n";
            }
            teamText.text = nameText;
            teamPingText.text = pingText;

            GameObject.Find("GreenPlayers").GetComponent<Text>().text = greenPlayers.Count + "/" + Mathf.Floor(m_gameInfo.GetMaxPlayers() / 2.0f);
            GameObject.Find("RedPlayers").GetComponent<Text>().text = redPlayers.Count + "/" + Mathf.Floor(m_gameInfo.GetMaxPlayers() / 2.0f);
            GameObject.Find("GameName").GetComponent<Text>().text = m_gameInfo.GetGameName();

            if (!isServer || m_gameState != eGameState.GAME_STATE_WAITING_FOR_PLAYERS)
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

        IEnumerator UpdatePing()
        {
            while (m_gameState != eGameState.GAME_STATE_CLOSING_ROOM)
            {
                byte errorByte;
                if (BombersNetworkManager.LocalConnection.hostId != -1)
                    BombersNetworkManager.LocalPlayer.m_ping = NetworkTransport.GetCurrentRTT(BombersNetworkManager.LocalConnection.hostId, BombersNetworkManager.LocalConnection.connectionId, out errorByte);
                yield return new WaitForSeconds(5);
            }
        }

        IEnumerator UpdateRoomDisplayName()
        {
            List<string> otherNames = new List<string>();
            while (true)
            {

                while (BombersNetworkManager.LocalPlayer == null)
                {
                    yield return new WaitForSeconds(0);
                }
                otherNames.Clear();
                GameObject[] playerList = GameObject.FindGameObjectsWithTag("PlayerController");
                foreach (GameObject player in playerList)
                {
                    if (player != BombersNetworkManager.LocalPlayer && player.GetComponent<BombersPlayerController>().m_displayName != "")
                    {
                        otherNames.Add(player.GetComponent<BombersPlayerController>().m_displayName);
                    }
                }

                int count = 1;
                string displayName = BombersNetworkManager.LocalPlayer.m_displayName;
                while (otherNames.Contains(displayName))
                {
                    displayName = BombersNetworkManager.LocalPlayer.m_displayName + "(" + count + ")";
                    count++;
                }

                if (BombersNetworkManager.LocalPlayer.m_displayName == "")
                {
                    BombersNetworkManager.LocalPlayer.m_displayName = BrainCloudStats.Instance.PlayerName;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        public IEnumerator RespawnPlayer(int aPlayerID)
        {
            m_currentRespawnTime = (float)m_respawnTime;
            while (m_currentRespawnTime > 0)
            {
                GameObject.Find("RespawnText").GetComponent<Text>().text = "Respawning in " + Mathf.CeilToInt(m_currentRespawnTime);
                yield return new WaitForSeconds(0.1f);
                m_currentRespawnTime -= 0.1f;
            }

            if (m_currentRespawnTime < 0)
            {
                m_currentRespawnTime = 0;
                GameObject.Find("RespawnText").GetComponent<Text>().text = "";
            }

            if (m_gameState == eGameState.GAME_STATE_PLAYING_GAME)
            {
                BombersNetworkManager.LocalPlayer.SpawnPlayerLocal(aPlayerID);
            }
        }

        [ClientRpc]
        void RpcSetMapSize(Vector3 newScale)
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
            m_countDownText.text = "3";
            yield return new WaitForSeconds(1.0f);
            m_countDownText.text = "2";
            yield return new WaitForSeconds(1.0f);
            m_countDownText.text = "1";
            yield return new WaitForSeconds(1.0f);

            MapPresets.MapSize mapSize = m_mapSizes[m_mapSize];
            GameObject mapBound = GameObject.Find("MapBounds");
            mapBound.transform.localScale = new Vector3(mapSize.m_horizontalSize, 1, mapSize.m_verticalSize);

            RpcSetMapSize(new Vector3(mapSize.m_horizontalSize, 1, mapSize.m_verticalSize));
            RpcGetReady();

            m_gameInfo.SetPlaying(1);
            yield return new WaitForSeconds(0.5f);

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
                        if (tryCount > 10000)
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
                            if (testShip.transform.Find("Graphic").GetComponent<Collider>().bounds.Intersects(m_spawnedShips[i].transform.Find("ShipGraphic").GetChild(0).Find("Graphic").GetComponent<Collider>().bounds))
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

                    ship = (GameObject)Instantiate((GameObject)Resources.Load("Ship"), position, Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f)));
                    NetworkServer.Spawn(ship);

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
                    ship = (GameObject)Instantiate((GameObject)Resources.Load("Ship"), position, Quaternion.Euler(0, 0, preset.m_ships[i].m_angle));
                    NetworkServer.Spawn(ship);
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
                    GameObject rock = (GameObject)Instantiate((GameObject)Resources.Load("Rock0" + Random.Range(1, 5)), position, rotation);
                    NetworkServer.Spawn(rock);
                }
                else
                {
                    i--;
                }
            }

            (BombersNetworkManager.singleton as BombersNetworkManager).LeaveLobby();
            m_gameState = eGameState.GAME_STATE_SPAWN_PLAYERS;
        }

        IEnumerator SpawnAllPlayers()
        {
            bool done = false;
            GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerController");
            for (int i = 0; i < players.Length; i++)
            {
                players[i].GetComponent<BombersPlayerController>().SpawnPlayers();
                while (!done)
                {
                    if (players[i].GetComponent<BombersPlayerController>().m_planeActive)
                    {
                        done = true;
                    }
                    else
                    {
                        players[i].GetComponent<BombersPlayerController>().SpawnPlayers();
                        yield return new WaitForSeconds(0.5f);
                    }
                }
                done = false;
                yield return new WaitForSeconds(0);
            }
            m_gameState = eGameState.GAME_STATE_PLAYING_GAME;
        }

        public void StartGameStart()
        {
            StartCoroutine("SpawnGameStart");
        }

        void Update()
        {
            switch (m_gameState)
            {
                /*
                case eGameState.GAME_STATE_WAITING_FOR_PLAYERS:
                    m_showScores = false;

                    if (GameObject.Find("BackgroundMusic").GetComponent<AudioSource>().isPlaying)
                    {
                        GameObject.Find("BackgroundMusic").GetComponent<AudioSource>().Stop();
                    }
                    break;
                    */
                case eGameState.GAME_STATE_STARTING_GAME:
                    m_showScores = false;
                    if (isServer && !m_once)
                    {
                        m_once = true;
                        StartCoroutine("SpawnGameStart");
                    }

                    break;
                case eGameState.GAME_STATE_SPAWN_PLAYERS:
                    m_showScores = false;
                    if (isServer && m_once)
                    {
                        m_once = false;
                        StartCoroutine("SpawnAllPlayers");
                    }

                    break;

                case eGameState.GAME_STATE_PLAYING_GAME:

                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        m_showQuitMenu = !m_showQuitMenu;
                    }
                    m_showScores = Input.GetKey(KeyCode.Tab);
                    m_quitMenu.SetActive(m_showQuitMenu);

                    if (!m_once)
                    {
                        GameObject.Find("BackgroundMusic").GetComponent<AudioSource>().Play();
                        m_once = true;
                    }

                    if (isServer)
                    {
                        if (m_blackScreen.GetComponent<CanvasGroup>().alpha <= 0)
                        {
                            m_gameTime = m_gameInfo.GetGameTime();
                            m_gameTime -= Time.deltaTime;
                            m_gameInfo.SetGameTime(m_gameTime);
                        }

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
                            RpcEndGame();
                        }
                    }
                    else
                    {
                        m_gameTime = m_gameInfo.GetGameTime();
                    }
                    break;

                case eGameState.GAME_STATE_SPECTATING:
                    {
                        if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            m_showQuitMenu = !m_showQuitMenu;
                        }

                        if (Input.GetKeyDown(KeyCode.Plus))
                        {
                            incrementSpectatorIndex(1);
                            updateSpectorFocusController();
                        }

                        if (Input.GetKeyDown(KeyCode.Minus))
                        {
                            incrementSpectatorIndex(-1);
                            updateSpectorFocusController();
                        }

                        m_showScores = Input.GetKey(KeyCode.Tab);
                        m_quitMenu.SetActive(m_showQuitMenu);
                        if (!m_once)
                        {
                            GameObject.Find("BackgroundMusic").GetComponent<AudioSource>().Play();
                            m_once = true;
                        }
                        m_gameTime = m_gameInfo.GetGameTime();
                        focusOnCurrentPlane();
                    }
                    break;

                case eGameState.GAME_STATE_GAME_OVER:
                    m_showScores = false;
                    if (m_once)
                    {
                        m_once = false;
                        m_team1Score = m_gameInfo.GetTeamScore(1);
                        m_team2Score = m_gameInfo.GetTeamScore(2);
                        BombersNetworkManager.LocalPlayer.EndGame();
                        if (isServer)
                        {
                            if (m_team1Score > m_team2Score)
                            {
                                CmdAwardExperience(1);
                            }
                            else if (m_team2Score > m_team1Score)
                            {
                                CmdAwardExperience(2);
                            }
                            else
                            {
                                CmdAwardExperience(0);
                            }
                        }
                    }

                    if (isServer)
                    {

                    }
                    else
                    {
                        m_gameTime = m_gameInfo.GetGameTime();
                    }
                    break;
            }
            if (isClient)
            {
                m_team1Score = m_gameInfo.GetTeamScore(1);
                m_team2Score = m_gameInfo.GetTeamScore(2);
            }
        }

        public void AddSpawnedShip(ShipController aShip)
        {
            m_spawnedShips.Add(aShip);
        }

        [ClientRpc]
        void RpcEndGame()
        {
            StopCoroutine("RespawnPlayer");
            m_gameState = eGameState.GAME_STATE_GAME_OVER;
            m_allyShipSunk.SetActive(false);
            m_enemyShipSunk.SetActive(false);
            m_redShipLogo.SetActive(false);
            m_greenShipLogo.SetActive(false);
            BombersNetworkManager.LocalPlayer.DestroyPlayerPlane();
        }

        [Command]
        public void CmdReturnToWaitingRoom()
        {
            if (m_gameState == eGameState.GAME_STATE_GAME_OVER)
            {
                RpcResetGame();
                m_gameState = eGameState.GAME_STATE_WAITING_FOR_PLAYERS;

                GameObject.Find("GameInfo").GetComponent<GameInfo>().Reinitialize();
                m_spawnedBombs.Clear();
                m_spawnedBullets.Clear();
                m_spawnedShips.Clear();
            }
        }

        [ClientRpc]
        void RpcResetGame()
        {
            m_gameState = eGameState.GAME_STATE_WAITING_FOR_PLAYERS;
            BombersNetworkManager.LocalPlayer.m_score = 0;
            BombersNetworkManager.LocalPlayer.m_kills = 0;
            BombersNetworkManager.LocalPlayer.m_deaths = 0;

            GameObject[] effects = GameObject.FindGameObjectsWithTag("Effect");
            foreach (GameObject effect in effects)
            {
                Destroy(effect);
            }

            effects = GameObject.FindGameObjectsWithTag("Ship");
            foreach (GameObject effect in effects)
            {
                Destroy(effect);
            }

            m_spawnedBombs.Clear();
            m_spawnedBullets.Clear();
            m_spawnedShips.Clear();
        }

        [Command]
        public void CmdHitShipTargetPoint(int aShipID, int aTargetIndex, string aBombInfoString)
        {
            BombController.BombInfo aBombInfo = BombController.BombInfo.GetBombInfo(aBombInfoString);
            if (BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 1)
            {
                m_gameInfo.SetTeamScore(1, m_gameInfo.GetTeamScore(1) + BrainCloudStats.Instance.m_pointsForWeakpointDestruction);
            }
            else if (BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 2)
            {
                m_gameInfo.SetTeamScore(2, m_gameInfo.GetTeamScore(2) + BrainCloudStats.Instance.m_pointsForWeakpointDestruction);
            }

            RpcHitShipTargetPoint(aShipID, aTargetIndex, aBombInfoString);
        }

        [Command]
        public void CmdRespawnShip(int aShipID)
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
            if (isServer)
            {
                ship.SetShipType(ship.GetShipType(), ship.m_team, aShipID);
            }
            RpcRespawnShip(aShipID);
        }

        [ClientRpc]
        void RpcRespawnShip(int aShipID)
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
        }

        [ClientRpc]
        void RpcHitShipTargetPoint(int aShipID, int aTargetIndex, string aBombInfoString)
        {
            ShipController.ShipTarget shipTarget = null;
            GameObject ship = null;
            ShipController.ShipTarget aShipTarget = new ShipController.ShipTarget(aShipID, aTargetIndex);
            BombController.BombInfo aBombInfo = BombController.BombInfo.GetBombInfo(aBombInfoString);
            for (int i = 0; i < m_spawnedShips.Count; i++)
            {
                if (m_spawnedShips[i].ContainsShipTarget(aShipTarget))
                {
                    shipTarget = m_spawnedShips[i].GetShipTarget(aShipTarget);
                    ship = m_spawnedShips[i].gameObject;
                    break;
                }
            }

            if (aBombInfo.m_shooter == BombersNetworkManager.LocalPlayer.m_playerID)
            {
                m_bombsHit++;
                BombersNetworkManager.LocalPlayer.m_score += BrainCloudStats.Instance.m_pointsForWeakpointDestruction;
            }

            Plane[] frustrum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(frustrum, ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Collider>().bounds))
            {
                BombersNetworkManager.LocalPlayer.ShakeCamera(BrainCloudStats.Instance.m_weakpointIntensity, BrainCloudStats.Instance.m_shakeTime);
            }

            if (shipTarget == null) return;
            GameObject explosion = (GameObject)Instantiate(m_weakpointExplosion, shipTarget.m_position.position, shipTarget.m_position.rotation);
            explosion.transform.parent = ship.transform;
            explosion.GetComponent<AudioSource>().Play();
            foreach (Transform child in shipTarget.gameObject.transform)
            {
                Destroy(child.gameObject);
            }

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

        public IEnumerator FadeOutShipMessage(GameObject aText, GameObject aLogo)
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
                yield return new WaitForFixedUpdate();
            }
            time = 2;
            aText.GetComponent<Image>().color = new Color(1, 1, 1, 1);
            aLogo.GetComponent<Image>().color = new Color(1, 1, 1, 1);
            fadeColor = new Color(1, 1, 1, 1);
            yield return new WaitForSeconds(2);

            while (time > 0)
            {
                time -= Time.fixedDeltaTime;
                fadeColor = new Color(1, 1, 1, fadeColor.a - Time.fixedDeltaTime);
                aText.GetComponent<Image>().color = fadeColor;
                aLogo.GetComponent<Image>().color = fadeColor;
                yield return new WaitForFixedUpdate();
            }
        }

        [Command]
        public void CmdDestroyedShip(int aShipID, string aBombInfoString)
        {
            BombController.BombInfo aBombInfo = BombController.BombInfo.GetBombInfo(aBombInfoString);

            if (isServer)
            {
                if (BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 1)
                {
                    m_gameInfo.SetTeamScore(1, m_gameInfo.GetTeamScore(1) + BrainCloudStats.Instance.m_pointsForShipDestruction);

                }
                else
                {
                    m_gameInfo.SetTeamScore(2, m_gameInfo.GetTeamScore(2) + BrainCloudStats.Instance.m_pointsForShipDestruction);

                }
            }

            RpcDestroyedShip(aShipID, aBombInfoString);
        }

        [ClientRpc]
        void RpcDestroyedShip(int aShipID, string aBombInfoString)
        {
            BombController.BombInfo aBombInfo = BombController.BombInfo.GetBombInfo(aBombInfoString);
            ShipController ship = null;
            for (int i = 0; i < m_spawnedShips.Count; i++)
            {
                if (m_spawnedShips[i].m_shipID == aShipID)
                {
                    ship = m_spawnedShips[i];
                    break;
                }
            }

            Plane[] frustrum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(frustrum, ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Collider>().bounds))
            {
                BombersNetworkManager.LocalPlayer.ShakeCamera(BrainCloudStats.Instance.m_shipIntensity, BrainCloudStats.Instance.m_shakeTime);
            }

            if (ship == null) return;

            ship.m_isAlive = false;
            StopCoroutine("FadeOutShipMessage");
            if (ship.m_team == 1)
            {
                if (BombersNetworkManager.LocalPlayer.m_team == 1)
                {
                    StartCoroutine(FadeOutShipMessage(m_allyShipSunk, m_greenShipLogo));
                }
                else if (BombersNetworkManager.LocalPlayer.m_team == 2)
                {
                    StartCoroutine(FadeOutShipMessage(m_enemyShipSunk, m_greenShipLogo));
                }
            }
            else
            {
                if (BombersNetworkManager.LocalPlayer.m_team == 1)
                {
                    StartCoroutine(FadeOutShipMessage(m_enemyShipSunk, m_redShipLogo));
                }
                else if (BombersNetworkManager.LocalPlayer.m_team == 2)
                {
                    StartCoroutine(FadeOutShipMessage(m_allyShipSunk, m_redShipLogo));
                }
            }

            ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().enabled = false;
            int children = ship.transform.childCount;
            for (int i = 1; i < children; i++)
            {
                var ps = ship.transform.GetChild(i).GetChild(0).GetChild(4).GetComponent<ParticleSystem>();
                var em = ps.emission;
                em.enabled = false;
            }

            int bomberTeam = BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team;
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

            if (isServer)
                ship.StartRespawn();

            if (aBombInfo.m_shooter == BombersNetworkManager.LocalPlayer.m_playerID)
            {
                m_carriersDestroyed++;
                BombersNetworkManager.LocalPlayer.m_score += BrainCloudStats.Instance.m_pointsForShipDestruction;
            }
        }

        [Command]
        void CmdAwardExperience(int aWinningTeam)
        {
            RpcAwardExperience(aWinningTeam);
        }

        [ClientRpc]
        void RpcAwardExperience(int aWinningTeam)
        {
            if (BombersNetworkManager.LocalPlayer.m_team == 0) return;

            m_timesDestroyed = BombersNetworkManager.LocalPlayer.m_deaths;
            m_planesDestroyed = BombersNetworkManager.LocalPlayer.m_kills;
            int gamesWon = (BombersNetworkManager.LocalPlayer.m_team == aWinningTeam) ? 1 : 0;
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

        [Command]
        public void CmdSpawnFlare(Vector3 aPosition, Vector3 aVelocity, int aPlayerID)
        {
            RpcSpawnFlare(aPosition, aVelocity, aPlayerID);
        }

        [ClientRpc]
        void RpcSpawnFlare(Vector3 aPosition, Vector3 aVelocity, int aPlayer)
        {
            GameObject flare = (GameObject)Instantiate(m_flare, aPosition, Quaternion.identity);
            flare.GetComponent<FlareController>().Activate(aPlayer);
            flare.GetComponent<Rigidbody>().velocity = aVelocity;
        }

        [ClientRpc]
        void RpcGetReady()
        {
            m_gameState = eGameState.GAME_STATE_STARTING_GAME;
            BombersNetworkManager.LocalPlayer.m_deaths = 0;
            BombersNetworkManager.LocalPlayer.m_kills = 0;
        }

        [ClientRpc]
        void RpcSpawnPlayer()
        {
            {
                Vector3 spawnPoint = Vector3.zero;
                spawnPoint.z = 22;

                if (BombersNetworkManager.LocalPlayer.m_team == 1)
                {
                    spawnPoint.x = Random.Range(m_team1SpawnBounds.bounds.center.x - m_team1SpawnBounds.bounds.size.x / 2, m_team1SpawnBounds.bounds.center.x + m_team1SpawnBounds.bounds.size.x / 2) - 10;
                    spawnPoint.y = Random.Range(m_team1SpawnBounds.bounds.center.y - m_team1SpawnBounds.bounds.size.y / 2, m_team1SpawnBounds.bounds.center.y + m_team1SpawnBounds.bounds.size.y / 2);
                }
                else if (BombersNetworkManager.LocalPlayer.m_team == 2)
                {
                    spawnPoint.x = Random.Range(m_team2SpawnBounds.bounds.center.x - m_team2SpawnBounds.bounds.size.x / 2, m_team2SpawnBounds.bounds.center.x + m_team2SpawnBounds.bounds.size.x / 2) + 10;
                    spawnPoint.y = Random.Range(m_team2SpawnBounds.bounds.center.y - m_team2SpawnBounds.bounds.size.y / 2, m_team2SpawnBounds.bounds.center.y + m_team2SpawnBounds.bounds.size.y / 2);
                }

                GameObject playerPlane = (GameObject)Instantiate((GameObject)Resources.Load("PlayerController"), spawnPoint, Quaternion.LookRotation(Vector3.forward, (new Vector3(0, 0, 22) - spawnPoint)));
                NetworkServer.Spawn(playerPlane);
                if (BombersNetworkManager.LocalPlayer.m_team == 1)
                {
                    playerPlane.layer = 8;
                }
                else if (BombersNetworkManager.LocalPlayer.m_team == 2)
                {
                    playerPlane.layer = 9;
                }
                BombersNetworkManager.LocalPlayer.SetPlayerPlane(playerPlane.GetComponent<PlaneController>());
                playerPlane.GetComponent<Rigidbody>().isKinematic = false;
                m_gameState = eGameState.GAME_STATE_PLAYING_GAME;
            }
        }

        [Command]
        public void CmdDespawnBombPickup(int aPickupID)
        {
            RpcDespawnBombPickup(aPickupID);
        }

        [ClientRpc]
        void RpcDespawnBombPickup(int aPickupID)
        {
            for (int i = 0; i < m_bombPickupsSpawned.Count; i++)
            {
                if (m_bombPickupsSpawned[i].m_pickupID == aPickupID)
                {
                    if (m_bombPickupsSpawned[i] != null && m_bombPickupsSpawned[i].gameObject != null) Destroy(m_bombPickupsSpawned[i].gameObject);
                    m_bombPickupsSpawned.RemoveAt(i);
                    break;
                }
            }
        }

        [Command]
        public void CmdSpawnBombPickup(Vector3 aPosition, int playerID)
        {
            int bombID = Random.Range(-20000000, 20000000) * 100 + playerID;
            RpcSpawnBombPickup(aPosition, bombID);
        }

        [ClientRpc]
        void RpcSpawnBombPickup(Vector3 aPosition, int bombID)
        {
            GameObject bombPickup = (GameObject)Instantiate(m_bombPickup, aPosition, Quaternion.identity);
            bombPickup.GetComponent<BombPickup>().Activate(bombID);
            m_bombPickupsSpawned.Add(bombPickup.GetComponent<BombPickup>());
        }

        [Command]
        public void CmdBombPickedUp(int aPlayer, int aPickupID)
        {
            RpcBombPickedUp(aPlayer, aPickupID);
        }

        [ClientRpc]
        void RpcBombPickedUp(int aPlayer, int aPickupID)
        {
            for (int i = 0; i < m_bombPickupsSpawned.Count; i++)
            {
                if (m_bombPickupsSpawned[i].m_pickupID == aPickupID)
                {
                    if (m_bombPickupsSpawned[i] != null && m_bombPickupsSpawned[i].gameObject != null) Destroy(m_bombPickupsSpawned[i].gameObject);
                    m_bombPickupsSpawned.RemoveAt(i);
                    break;
                }
            }

            if (aPlayer == BombersNetworkManager.LocalPlayer.m_playerID)
            {
                BombersNetworkManager.LocalPlayer.GetComponent<WeaponController>().AddBomb();
            }
        }

        [Command]
        public void CmdSpawnBomb(string aBombInfo)
        {
            m_bombsDropped++;
            int id = GetNextBombID();
            BombController.BombInfo bombInfo = BombController.BombInfo.GetBombInfo(aBombInfo);
            bombInfo.m_bombID = id;
            RpcSpawnBomb(bombInfo.GetJson());
        }

        [ClientRpc]
        void RpcSpawnBomb(string aBombInfoString)
        {
            BombController.BombInfo aBombInfo = BombController.BombInfo.GetBombInfo(aBombInfoString);
            if (isServer)
            {
                aBombInfo.m_isMaster = true;
            }

            GameObject bomb = BombersNetworkManager.LocalPlayer.GetComponent<WeaponController>().SpawnBomb(aBombInfo);
            m_spawnedBombs.Add(bomb.GetComponent<BombController>().GetBombInfo());
            int playerTeam = BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team;

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

        [Command]
        public void CmdDeleteBomb(string aBombInfo, int aHitSurface)
        {
            RpcDeleteBomb(aBombInfo, aHitSurface);
        }

        [ClientRpc]
        void RpcDeleteBomb(string aBombInfoString, int aHitSurface)
        {
            BombController.BombInfo aBombInfo = BombController.BombInfo.GetBombInfo(aBombInfoString);
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

        [Command]
        public void CmdSpawnBullet(string aBulletInfo)
        {
            m_shotsFired++;
            int id = GetNextBulletID();
            BulletController.BulletInfo bulletInfo = BulletController.BulletInfo.GetBulletInfo(aBulletInfo);
            bulletInfo.m_bulletID = id;
            RpcSpawnBullet(bulletInfo.GetJson());
        }

        [ClientRpc]
        void RpcSpawnBullet(string aBulletInfoString)
        {
            BulletController.BulletInfo aBulletInfo = BulletController.BulletInfo.GetBulletInfo(aBulletInfoString);
            if (BombersNetworkManager.LocalPlayer.m_playerID == aBulletInfo.m_shooter)
            {
                aBulletInfo.m_isMaster = true;
            }

            GameObject bullet = BombersNetworkManager.LocalPlayer.GetComponent<WeaponController>().SpawnBullet(aBulletInfo);
            m_spawnedBullets.Add(bullet.GetComponent<BulletController>().GetBulletInfo());
            int playerTeam = BombersPlayerController.GetPlayer(aBulletInfo.m_shooter).m_team;

            if (BombersNetworkManager.LocalPlayer.m_playerID != aBulletInfo.m_shooter)
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

        [Command]
        public void CmdDeleteBullet(string aBulletInfo)
        {
            RpcDeleteBullet(aBulletInfo);
        }

        [ClientRpc]
        void RpcDeleteBullet(string aBulletInfoString)
        {
            BulletController.BulletInfo aBulletInfo = BulletController.BulletInfo.GetBulletInfo(aBulletInfoString);
            if (m_spawnedBullets.Contains(aBulletInfo))
            {
                int index = m_spawnedBullets.IndexOf(aBulletInfo);
                GameObject bullet = m_spawnedBullets[index].gameObject;
                Destroy(bullet);
                m_spawnedBullets.Remove(aBulletInfo);
            }
        }

        [Command]
        public void CmdBulletHitPlayer(string aBulletInfo, Vector3 aRelativeHitPoint, int aHitPlayerID)
        {
            BulletController.BulletInfo bulletInfo = BulletController.BulletInfo.GetBulletInfo(aBulletInfo);
            int hitPlayer = aHitPlayerID;
            int shooter = bulletInfo.m_shooter;
            CmdDeleteBullet(aBulletInfo);
            RpcBulletHitPlayer(aRelativeHitPoint, aBulletInfo, shooter, hitPlayer);
        }

        [ClientRpc]
        void RpcBulletHitPlayer(Vector3 aHitPoint, string aBulletInfoString, int aShooter, int aHitPlayer)
        {
            BulletController.BulletInfo aBulletInfo = BulletController.BulletInfo.GetBulletInfo(aBulletInfoString);
            foreach (GameObject plane in GameObject.FindGameObjectsWithTag("PlayerController"))
            {
                if (plane.GetComponent<PlaneController>().m_playerID == aHitPlayer)
                {
                    Instantiate(m_bulletHit, plane.transform.position + aHitPoint, Quaternion.LookRotation(aBulletInfo.m_startDirection, -Vector3.forward));
                    break;
                }
            }

            if (aHitPlayer == BombersNetworkManager.LocalPlayer.m_playerID)
            {
                BombersNetworkManager.LocalPlayer.TakeBulletDamage(aShooter);
            }
        }

        [Command]
        public void CmdDestroyPlayerPlane(int aVictim, int aShooterID)
        {
            RpcDestroyPlayerPlane(aVictim, aShooterID);
        }

        [ClientRpc]
        void RpcDestroyPlayerPlane(int aVictim, int aShooter)
        {
            foreach (GameObject plane in GameObject.FindGameObjectsWithTag("PlayerController"))
            {
                if (plane.GetComponent<PlaneController>().m_playerID == aVictim)
                {
                    GameObject explosion = (GameObject)Instantiate(m_playerExplosion, plane.transform.position, plane.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    break;
                }
            }

            if (aShooter == -1)
            {
                if (aVictim == BombersNetworkManager.LocalPlayer.m_playerID)
                {
                    BombersNetworkManager.LocalPlayer.DestroyPlayerPlane();
                    BombersNetworkManager.LocalPlayer.m_deaths += 1;
                    StopCoroutine("RespawnPlayer");
                    StartCoroutine("RespawnPlayer");
                }
            }
            else
            {

                if (aVictim == BombersNetworkManager.LocalPlayer.m_playerID)
                {
                    BombersNetworkManager.LocalPlayer.DestroyPlayerPlane();
                    BombersNetworkManager.LocalPlayer.m_deaths += 1;
                    StopCoroutine("RespawnPlayer");
                    StartCoroutine("RespawnPlayer");
                }
                else if (aShooter == BombersNetworkManager.LocalPlayer.m_playerID)
                {
                    BombersNetworkManager.LocalPlayer.m_kills += 1;
                }
            }
        }

        public int GetNextBulletID()
        {
            return Random.Range(-20000000, 20000000) * 100;
        }

        public int GetNextBombID()
        {
            return Random.Range(-20000000, 20000000) * 100;
        }

    }
}