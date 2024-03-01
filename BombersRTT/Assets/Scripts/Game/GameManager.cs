/*
 * The GameManager class is mainly used by the server, as none of its functions can be called on the client side. It 
 * controls the basic progress of the game, populating the scoreboards and dealing with players connecting.
 * 
 */

using BrainCloudUNETExample.Connection;
using Gameframework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrainCloudUNETExample.Game
{
    public class GameManager : BaseNetworkBehavior
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

        public eGameState m_gameState = eGameState.GAME_STATE_INITIALIZE_GAME;

        public int m_respawnTime = 3;

        public List<BombInfo> m_spawnedBombs;

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

        public Collider m_team1SpawnBounds;
        public Collider m_team2SpawnBounds;

        [SerializeField]
        private RectTransform m_greenChevron;
        [SerializeField]
        private RectTransform m_redChevron;

        public GameObject MissionText { get { return m_missionText; } }

        public bool IsQuitMenuVisible { get { return m_showQuitMenu; } }

        public bool IsResultsMenuVisible { get { return m_resultsWindow.gameObject.activeInHierarchy; } }

#if (UNITY_IOS || UNITY_ANDROID)
        public void ToggleQuitMenu() { m_showQuitMenu = !m_showQuitMenu; }
#endif
        void Awake()
        {
            _classType = BombersNetworkManager.GAME_CONTROLLER;
            if (!GCore.Wrapper.Client.Initialized)
            {
                GStateManager.Instance.PushSubState(ConnectingSubState.STATE_NAME);
                return;
            }

            m_greenTeamScore = GameObject.Find("Team Green Score").transform.Find("Team Score").GetComponent<TextMeshProUGUI>();
            m_redTeamScore = GameObject.Find("Team Red Score").transform.Find("Team Score").GetComponent<TextMeshProUGUI>();

            m_greenTeamResultsTransform = GameObject.Find("Team Green Score Inner").transform.FindDeepChild("PlayerResultsGroup").transform;
            m_redTeamResultsTransform = GameObject.Find("Team Red Score Inner").transform.FindDeepChild("PlayerResultsGroup").transform;

            m_allyShipSunk = GameObject.Find("ShipSink").transform.Find("AllyShipSunk").gameObject;
            m_enemyShipSunk = GameObject.Find("ShipSink").transform.Find("EnemyShipSunk").gameObject;
            m_redShipLogo = GameObject.Find("ShipSink").transform.Find("RedLogo").gameObject;
            m_greenShipLogo = GameObject.Find("ShipSink").transform.Find("GreenLogo").gameObject;
            m_initialLoadingScreen = GameObject.Find("InitialLoadingScreen");
            m_countDownText = GameObject.Find("CountDown").GetComponent<TextMeshProUGUI>();

            m_allyShipSunk.SetActive(false);
            m_enemyShipSunk.SetActive(false);
            m_redShipLogo.SetActive(false);
            m_greenShipLogo.SetActive(false);
            m_quitMenu = GameObject.Find("QuitMenu");
            m_quitMenu.SetActive(false);

            m_greenLogo = GameObject.Find("Green Logo");
            m_greenLogo.SetActive(false);
            m_redLogo = GameObject.Find("Red Logo");
            m_redLogo.SetActive(false);
            m_enemyWinText = GameObject.Find("Window Title - Loss");
            m_allyWinText = GameObject.Find("Window Title - Win");
            m_resetButton = GameObject.Find("Continue");
            m_quitButton = GameObject.Find("ResultsQuit");
            m_gameTime = GConfigManager.GetFloatValue("DefaultGameTime");
            m_mapPresets = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_presets;
            m_mapSizes = GameObject.Find("MapPresets").GetComponent<MapPresets>().m_mapSizes;
            m_resultsWindow = GameObject.Find("Results").GetComponent<CanvasGroup>();
            m_resultsWindow.gameObject.SetActive(false);

            m_displayDialog = GameObject.Find("DisplayDialog");
            m_displayDialog.SetActive(false);

            m_HUD = GameObject.Find("HUD");
            m_redScoreTrans = m_HUD.transform.GetChild(0).GetChild(2).Find("RedScore");
            m_greenScoreTrans = m_HUD.transform.GetChild(0).GetChild(1).Find("GreenScore");
            m_redShipAnimators = m_redScoreTrans.transform.parent.FindDeepChildren<GenericAnimator>();

            // flip the green animators 
            m_greenShipAnimators = m_greenScoreTrans.transform.parent.FindDeepChildren<GenericAnimator>();
            m_greenShipAnimators.Reverse();

            m_timeLeft = m_HUD.transform.GetChild(0).Find("TimeLeft").GetChild(0).GetComponent<TextMeshProUGUI>();
            m_respawnText = GameObject.Find("RespawnText").GetComponent<TextMeshProUGUI>();

            m_respawnText.text = "";

            m_missionText = m_HUD.transform.Find("MissionText").gameObject;
            m_missionText.SetActive(false);
            m_HUD.SetActive(false);

            //resources
            m_carrierExplosion01 = Resources.Load("Prefabs/Game/CarrierExplosion01") as GameObject;
            m_carrierExplosion02 = Resources.Load("Prefabs/Game/CarrierExplosion02") as GameObject;
            m_battleshipExplosion01 = Resources.Load("Prefabs/Game/BattleshipExplosion01") as GameObject;
            m_battleshipExplosion02 = Resources.Load("Prefabs/Game/BattleshipExplosion02") as GameObject;
            m_cruiserExplosion02 = Resources.Load("Prefabs/Game/CruiserExplosion02") as GameObject;
            m_cruiserExplosion01 = Resources.Load("Prefabs/Game/CruiserExplosion01") as GameObject;
            m_patrolBoatExplosion02 = Resources.Load("Prefabs/Game/PatrolBoatExplosion02") as GameObject;
            m_patrolBoatExplosion01 = Resources.Load("Prefabs/Game/PatrolBoatExplosion01") as GameObject;
            m_destroyerExplosion02 = Resources.Load("Prefabs/Game/DestroyerExplosion02") as GameObject;
            m_destroyerExplosion01 = Resources.Load("Prefabs/Game/DestroyerExplosion01") as GameObject;
            //m_flare = Resources.Load("Prefabs/Game/Flare") as GameObject;
            m_bombPickup = Resources.Load("Prefabs/Game/BombPickup") as GameObject;
            m_weakpointExplosion = Resources.Load("Prefabs/Game/WeakpointExplosion") as GameObject;
            m_carrier01 = Resources.Load("Prefabs/Game/Carrier01") as GameObject;
            m_bombExplosion = Resources.Load("Prefabs/Game/BombExplosion") as GameObject;
            m_bombWaterExplosion = Resources.Load("Prefabs/Game/BombWaterExplosion") as GameObject;
            m_bombDud = Resources.Load("Prefabs/Game/BombDud") as GameObject;
            m_offscreenPrefab = Resources.Load("Prefabs/Game/OffScreenBomberIndicator") as GameObject;
            m_offscreenShipPrefab = Resources.Load("Prefabs/Game/OffScreenShipIndicator") as GameObject;

            m_resultCellPrefab = Resources.Load("Prefabs/UI/resultsPlayerCellOther") as GameObject;
            m_resultsCellPrefabYou = Resources.Load("Prefabs/UI/resultsPlayerCellYou") as GameObject;
        }

        void Initialize()
        {
            m_mapLayout = m_gameInfo.GetMapLayout();
            m_mapSize = m_gameInfo.GetMapSize();

            if (IsServer)
                RpcSetLightPosition(m_gameInfo.GetLightPosition());

            m_spawnedShips = new List<ShipController>();
            m_bombPickupsSpawned = new List<BombPickup>();

            m_spawnedBullets = new List<BulletInfo>();
            m_spawnedBombs = new List<BombInfo>();

            m_team1Score = 0;
            m_team2Score = 0;

            if (IsServer)
            {
                m_gameInfo.SetLightPosition(Random.Range(1, 5));
                RpcSetLightPosition(m_gameInfo.GetLightPosition());
            }

            if (m_gameInfo.GetPlaying() == 1)
            {
                m_gameState = eGameState.GAME_STATE_SPECTATING;
                updateSpectorFocusController();
            }
        }

        public void ForcedStart()
        {
            StartCoroutine("WaitForGameInfoForced");
        }

        IEnumerator WaitForGameInfoForced()
        {
            m_gameInfo = GameObject.Find("GameInfo").GetComponent<GameInfo>();
            while (m_gameInfo == null)
            {
                yield return YieldFactory.GetWaitForEndOfFrame();
                m_gameInfo = GameObject.Find("GameInfo").GetComponent<GameInfo>();
            }
            Initialize();
            CmdForceStartGame();
            StartGameStart();
            yield return null;
        }

        IEnumerator WaitForGameInfo()
        {
            while (GameObject.Find("GameInfo").GetComponent<GameInfo>() == null || BombersNetworkManager.LocalPlayer == null)
            {
                yield return YieldFactory.GetWaitForSeconds(0);
            }
            m_gameInfo = GameObject.Find("GameInfo").GetComponent<GameInfo>();
            Initialize();
        }

        //[ClientRpc]
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
                yield return YieldFactory.GetWaitForSeconds(0.02f);
            }
        }

        void OnApplicationQuit()
        {
            LeaveRoom();
        }

        // small gameplay messages for people joining, leaving etc
        public void DisplayDialogMessage(string in_message)
        {
            if (m_displayDialog != null)
                StartCoroutine(DialogDisplayRoutine(in_message));
        }

        private IEnumerator DialogDisplayRoutine(string in_message)
        {
            int numDisplayMessages = m_displayDialog.transform.parent.FindDeepChildren("DisplayDialog").Count;

            if (numDisplayMessages > 0) numDisplayMessages -= 1;

            GameObject newDisplay = GEntityFactory.Instance.CreateObject(m_displayDialog,
                                                                         m_displayDialog.transform.position,
                                                                         m_displayDialog.transform.rotation,
                                                                         m_displayDialog.transform.parent);
            // ensure they are in the right spot
            newDisplay.name = "DisplayDialog";
            newDisplay.transform.position = m_displayDialog.transform.position;
            newDisplay.transform.rotation = m_displayDialog.transform.rotation;

            newDisplay.SetActive(true);
            CanvasGroup canvasGroup = newDisplay.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            TextMeshProUGUI text = newDisplay.transform.FindDeepChild<TextMeshProUGUI>();
            text.text = in_message;
            Vector3 newPos = newDisplay.transform.position;
            while (canvasGroup.alpha < 1.0f)
            {
                canvasGroup.alpha += Time.deltaTime * 2.0f;
                newPos.y += Time.deltaTime * (numDisplayMessages * 125.0f);
                newDisplay.transform.position = newPos;
                yield return YieldFactory.GetWaitForEndOfFrame();
            }

            yield return YieldFactory.GetWaitForSeconds(2.0f);
            newDisplay.name = "";
            while (canvasGroup.alpha > 0)
            {
                canvasGroup.alpha -= Time.deltaTime * 2.0f;
                yield return YieldFactory.GetWaitForEndOfFrame();
            }

            Destroy(newDisplay);
        }

        public int GetNextBulletID()
        {
            return Random.Range(-20000000, 20000000) * 100;
        }

        public int GetNextBombID()
        {
            return Random.Range(-20000000, 20000000) * 100;
        }

        public void AddBulletInfo(BulletInfo in_info)
        {
            m_spawnedBullets.Add(in_info);
        }
        public int GetIndexOfBulletInfo(BulletInfo in_info)
        {
            int iToReturn = -1;
            BulletInfo bInfo = null;
            for (int i = 0; i < m_spawnedBullets.Count; ++i)
            {
                bInfo = m_spawnedBullets[i];
                if (in_info.m_bulletID == bInfo.m_bulletID)
                {
                    iToReturn = i;
                    break;
                }
            }

            return iToReturn;
        }

        public void DestroyBulletAtIndex(int in_index)
        {
            BulletInfo bInfo = m_spawnedBullets[in_index];
            GameObject bullet = bInfo.gameObject;
            Destroy(bullet);
            m_spawnedBullets.Remove(bInfo);
        }

        public int GetIndexOfBombInfo(BombInfo in_info)
        {
            int iToReturn = -1;
            BombInfo bInfo = null;
            for (int i = 0; i < m_spawnedBombs.Count; ++i)
            {
                bInfo = m_spawnedBombs[i];
                if (in_info.m_bombID == bInfo.m_bombID)
                {
                    iToReturn = i;
                    break;
                }
            }

            return iToReturn;
        }

        public void DestroyBombAtIndex(int in_index, int aHitSurface)
        {
            GameObject bomb = m_spawnedBombs[in_index].gameObject;
            GameObject explosion;
            if (!bomb.GetComponent<BombController>().m_isActive)
            {
                // hit water
                if (aHitSurface == 0)
                {
                    explosion = (GameObject)Instantiate(m_bombWaterExplosion, bomb.transform.position, Quaternion.identity);
                    explosion.GetComponent<AudioSource>().Play();
                }
                // hit something good
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
            m_spawnedBombs.RemoveAt(in_index);
        }

        public void JoinGameContinue()
        {
            SendStart(BombersNetworkManager.LOBBY_UP, GCore.Wrapper.Client.ProfileId, "true", transform);
            LobbyMemberInfo myMemberInfo = BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(GCore.Wrapper.Client.ProfileId);
            myMemberInfo.LobbyReadyUp = true;
            RefreshQuitGameDisplay();
        }

        public void LeaveRoom()
        {
            StopCoroutine("startingInCountDown");
            SendStart(BombersNetworkManager.LOBBY_UP, GCore.Wrapper.Client.ProfileId, "false", transform);
            // this leaves to the main menu
            BombersNetworkManager.singleton.DestroyMatch();
        }

        public void DestroyMatch()
        {
            BombersNetworkManager.singleton.DestroyMatch();
            // force an update
            GCore.Wrapper.Client.Update();
        }

        //[Command]
        public void CmdForceStartGame()
        {
            m_gameState = eGameState.GAME_STATE_STARTING_GAME;
        }

        public void CloseQuitMenu()
        {
            m_showQuitMenu = false;
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

        private void createOffScreenIndicator(BombersPlayerController in_targetTransfrom)
        {
            OffScreenController screenController = GEntityFactory.Instance.CreateObject(m_offscreenPrefab,
                                                                                Vector3.zero, Quaternion.identity,
                                                                                BombersNetworkManager.LocalPlayer.m_playerPlane.transform).GetComponent<OffScreenController>();
            screenController.Init(in_targetTransfrom, BombersNetworkManager.LocalPlayer);
        }

        private void createOffScreenShipIndicator(ShipController in_controller)
        {
            OffScreenController screenController = GEntityFactory.Instance.CreateObject(m_offscreenShipPrefab,
                                                                                Vector3.zero, Quaternion.identity,
                                                                                BombersNetworkManager.LocalPlayer.m_playerPlane.transform).GetComponent<OffScreenController>();
            screenController.Init(in_controller, BombersNetworkManager.LocalPlayer);
        }

        private void updateSpectorFocusController()
        {
            GameObject[] playerList = GameObject.FindGameObjectsWithTag("PlayerController");

            foreach (GameObject item in playerList)
            {
                if (item.GetComponent<BombersPlayerController>().ProfileId == GCore.Wrapper.Client.ProfileId)
                {
                    item.tag = "Untagged";
                    item.SetActive(false);
                    //Destroy(item);
                    break;
                }
            }
            GameObject playerController = playerList.GetValue(m_spectatingIndex) as GameObject;
            m_spectatorFocusController = playerController != null ? playerController.GetComponent<BombersPlayerController>() : null;
            if (m_spectatorFocusController == null || m_spectatorFocusController.ProfileId == GCore.Wrapper.Client.ProfileId)
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
            if (BombersNetworkManager.LocalPlayer == null) return;

            m_team1Score = m_gameInfo.GetTeamScore(1);
            m_team2Score = m_gameInfo.GetTeamScore(2);
            System.TimeSpan span = System.TimeSpan.FromSeconds(m_gameTime);

            string timeLeft = span.ToString().Substring(3, 5);
            m_timeLeft.text = timeLeft;

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
            // update red hud
            int count = 0;
            m_redScoreTrans.GetChild(0).GetComponent<TextMeshProUGUI>().text = m_team2Score.ToString("n0");
            foreach (GenericAnimator animator in m_redShipAnimators)
            {
                animator.PlayAnimation("Alive", ++count <= team2Ships.Count);
                animator.PlayAnimation("Flashing", team2Ships.Count == 1);
            }
            // update green hud
            count = 0;
            m_greenScoreTrans.GetChild(0).GetComponent<TextMeshProUGUI>().text = m_team1Score.ToString("n0");

            foreach (GenericAnimator animator in m_greenShipAnimators)
            {
                animator.PlayAnimation("Alive", ++count <= team1Ships.Count);
                animator.PlayAnimation("Flashing", team1Ships.Count == 1);
            }
        }

        void OnMiniScoresWindow()
        {
            m_quitButton.SetActive(false);
            m_resetButton.SetActive(false);
            m_allyWinText.SetActive(false);
            m_enemyWinText.SetActive(false);
            m_greenLogo.SetActive(false);
            m_redLogo.SetActive(false);

            updatePlayerDisplay();
        }

        private void populateResultsGroups()
        {
            // create them for the first time
            if (m_greenTeamResultsTransform.childCount == 0 && m_redTeamResultsTransform.childCount == 0)
            {
                m_updatedDisplay = true;
                List<BombersPlayerController> playerListList = new List<BombersPlayerController>();
                List<LobbyMemberInfo> members = BombersNetworkManager.LobbyInfo.Members;

                for (int i = 0; i < members.Count; i++)
                {
                    if (members[i].PlayerController != null && members[i].PlayerController.m_team != 0)
                        playerListList.Add(members[i].PlayerController);
                }

                BombersPlayerController[] playerList = playerListList.ToArray().OrderByDescending(x => x.m_score).ToArray();

                ResultsCell resultsCellTemp = null;
                bool isYou = false;
                bool isGreenTeam = false;

                for (int i = 0; i < playerList.Length; i++)
                {
                    isGreenTeam = playerList[i].m_team == 1;
                    isYou = playerList[i].ProfileId == GCore.Wrapper.Client.ProfileId;

                    resultsCellTemp = GEntityFactory.Instance.CreateObject(isYou ? m_resultsCellPrefabYou : m_resultCellPrefab,
                                                                                    Vector3.zero, Quaternion.identity,
                                                                                    isGreenTeam ? m_greenTeamResultsTransform : m_redTeamResultsTransform).GetComponent<ResultsCell>();
                    resultsCellTemp.UpdateDisplay(
                                        new ResultsData(playerList[i].m_displayName,                                                // display name
                                                playerList[i].m_kills + "/" + playerList[i].m_deaths,                               // kd ratio
                                                HudHelper.ToGUIString(playerList[i].m_score),                                       // score display
                                                HudHelper.ToGUIString(isYou ?
                                                                        GCore.Wrapper.Client.RelayService.LastPing * 0.0001f :
                                                                        (playerList[i].m_playerPlane as BaseNetworkBehavior).LastSyncedPing),   // ping display
                                                m_gameState == eGameState.GAME_STATE_GAME_OVER,                                                 // only display when in game over
                                                BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(playerList[i].ProfileId).LobbyReadyUp)   // confirmed if ready up
                                   );
                }
            }
        }

        private bool m_updatedDisplay = false;
        private void updatePlayerDisplay()
        {
            if (!m_updatedDisplay)
            {
                m_team1Score = m_gameInfo.GetTeamScore(1);
                m_team2Score = m_gameInfo.GetTeamScore(2);
                m_greenTeamScore.text = HudHelper.ToGUIString(m_team1Score);
                m_redTeamScore.text = HudHelper.ToGUIString(m_team2Score);

                populateResultsGroups();
            }
        }

        public void RefreshQuitGameDisplay()
        {
            removeResultsGroupCells();
            updatePlayerDisplay();
        }

        private void removeResultsGroupCells()
        {
            m_updatedDisplay = false;
            // remove all
            for (int i = 0; i < m_greenTeamResultsTransform.childCount; ++i)
            {
                Destroy(m_greenTeamResultsTransform.GetChild(i).gameObject);
            }
            for (int i = 0; i < m_redTeamResultsTransform.childCount; ++i)
            {
                Destroy(m_redTeamResultsTransform.GetChild(i).gameObject);
            }
        }


        IEnumerator startingInCountDown()
        {
            TextMeshProUGUI startingIn = m_resultsWindow.transform.FindDeepChild("startingInCountdownText").GetComponent<TextMeshProUGUI>();
            int countDown = GConfigManager.GetIntValue("playAgainTime");
            bool bAllHumansReadiedUp = false;
            bool bPreviousOwnerJoinedUp = false;
            List<string> cxIds = new List<string>();
            while (countDown > 0 && !bAllHumansReadiedUp)
            {
                yield return YieldFactory.GetWaitForSeconds(1.0f);
                startingIn.text = "" + (--countDown);

                bAllHumansReadiedUp = true;
                foreach (LobbyMemberInfo member in BombersNetworkManager.LobbyInfo.Members)
                {
                    if (member.Name.Contains(SERVER_BOT)) continue;

                    // if they are readied up 
                    if (member.LobbyReadyUp)
                    {
                        cxIds.Add(member.CXId);
                        if (BombersNetworkManager.LobbyInfo.IsOwner(member.ProfileId))
                            bPreviousOwnerJoinedUp = true;
                    }

                    if (!member.LobbyReadyUp)
                    {
                        bAllHumansReadiedUp = false;
                    }
                }
            }
            yield return YieldFactory.GetWaitForEndOfFrame();

            BombersNetworkManager.singleton.ContinueJoinRoom(bPreviousOwnerJoinedUp ?
                BombersNetworkManager.LobbyInfo.OwnerProfileId : cxIds.Count > 0 ? cxIds[cxIds.Count - 1] : "");

            yield return null;
        }

        void OnScoresWindow()
        {
            CanvasGroup group = m_resultsWindow;
            if (group.alpha < 1.0f)
            {
                group.alpha += Time.deltaTime * 2.0f;
            }
            else if (group.alpha != 1.0f)
            {
                group.alpha = 1.0f;
            }

            m_resetButton.SetActive(m_gameState == eGameState.GAME_STATE_GAME_OVER);
            m_quitButton.SetActive(m_gameState == eGameState.GAME_STATE_GAME_OVER);
            m_allyWinText.SetActive(false);
            m_enemyWinText.SetActive(false);
            if (m_gameState == eGameState.GAME_STATE_GAME_OVER)
            {
                if (m_team1Score > m_team2Score)
                {
                    m_greenLogo.SetActive(true);
                    m_redLogo.SetActive(false);

                    if (BombersNetworkManager.LocalPlayer != null && BombersNetworkManager.LocalPlayer.m_team == 1)
                    {
                        m_allyWinText.SetActive(true);
                        m_enemyWinText.SetActive(false);
                    }
                    else if (BombersNetworkManager.LocalPlayer != null && BombersNetworkManager.LocalPlayer.m_team == 2)
                    {
                        m_allyWinText.SetActive(false);
                        m_enemyWinText.SetActive(true);
                    }
                }
                else
                {
                    m_greenLogo.SetActive(false);
                    m_redLogo.SetActive(true);

                    if (BombersNetworkManager.LocalPlayer != null && BombersNetworkManager.LocalPlayer.m_team == 1)
                    {
                        m_allyWinText.SetActive(false);
                        m_enemyWinText.SetActive(true);
                    }
                    else if (BombersNetworkManager.LocalPlayer != null && BombersNetworkManager.LocalPlayer.m_team == 2)
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
            updatePlayerDisplay();
        }

        public void ChangeTeam()
        {
            GCore.Wrapper.LobbyService.SwitchTeam(BombersNetworkManager.LobbyInfo.LobbyId, BombersNetworkManager.LobbyInfo.GetOppositeTeamCodeWithProfileId(GCore.Wrapper.Client.ProfileId));
        }

        public BombersPlayerController FindPlayerWithID(int aID)
        {
            return BombersNetworkManager.LocalPlayer;
        }

        public IEnumerator RespawnPlayer(BombersPlayerController in_bomber)
        {
            if (m_gameState < eGameState.GAME_STATE_GAME_OVER)
            {
                if (in_bomber.ProfileId == BombersNetworkManager.LocalPlayer.ProfileId)
                {
                    m_currentRespawnTime = (float)m_respawnTime;
                    while (m_currentRespawnTime > 0)
                    {
                        m_respawnText.text = "Respawning in " + Mathf.CeilToInt(m_currentRespawnTime);
                        yield return YieldFactory.GetWaitForSeconds(0.1f);
                        m_currentRespawnTime -= 0.1f;
                    }

                    if (m_currentRespawnTime < 0)
                    {
                        m_currentRespawnTime = 0;
                        m_respawnText.text = "";
                    }
                }
                else
                {
                    yield return YieldFactory.GetWaitForSeconds(m_respawnTime);
                }

                if (m_gameState == eGameState.GAME_STATE_PLAYING_GAME &&
                    BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(in_bomber.ProfileId) != null)
                {
                    in_bomber.SpawnPlayer(in_bomber.ProfileId);
                }
            }
            else
            {
                yield return null;
            }
        }

        //[ClientRpc]
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
            yield return YieldFactory.GetWaitForSeconds(1.0f);

            // set up map dimensions
            MapPresets.MapSize mapSize = m_mapSizes[m_mapSize];
            GameObject mapBound = GameObject.Find("MapBounds");
            mapBound.transform.localScale = new Vector3(mapSize.m_horizontalSize, 1, mapSize.m_verticalSize);

            RpcSetMapSize(new Vector3(mapSize.m_horizontalSize, 1, mapSize.m_verticalSize));

            m_countDownText.text = "2";
            yield return YieldFactory.GetWaitForSeconds(1.0f);
            m_countDownText.text = "1";
            yield return YieldFactory.GetWaitForSeconds(1.0f);

            float numTime = 0.0f;
            while (!BombersNetworkManager.Instance.AllMembersJoined() && numTime < 15.0f && m_gameState != eGameState.GAME_STATE_PLAYING_GAME)
            {
                yield return YieldFactory.GetWaitForSeconds(0.1f);
                numTime += 0.1f;
            }

            yield return null;

            // close out, there was an issue
            if (numTime >= 15.0f)
            {
                HudHelper.DisplayMessageDialog("ERROR", "THERE WAS A CONNECTION ERROR.  PLEASE TRY AGAIN SOON.", "OK", BombersNetworkManager.singleton.DestroyMatch);
                yield return null;
            }
            else if (m_gameState != eGameState.GAME_STATE_PLAYING_GAME)
            {
                m_gameInfo.SetPlaying(1);

                if (IsServer)
                {
                    yield return YieldFactory.GetWaitForSeconds(0.5f);

                    int shipID = 0;
                    bool done = false;
                    GameObject ship = null;
                    int shipIndex = 0;
                    int tryCount = 0;
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

                            ship = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/Ship"), position, Quaternion.Euler(0, 0, rotation));

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

                            SendStart(BombersNetworkManager.SHIP, shipController.GetShipType().ToString() + "***" + shipID.ToString() + "^^^" + ((shipID % 2) + 1).ToString(), ship.name.Replace("(Clone)", ""), ship.transform);
                            //yield return YieldFactory.GetWaitForEndOfFrame();
                            if (shipID % 2 == 1)
                            {
                                shipIndex++;
                            }
                            shipID++;
                            if (shipID >= 10 || m_spawnedShips.Count >= 10) done = true;
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
                            ship = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/Ship"), position, Quaternion.Euler(0, 0, preset.m_ships[i].m_angle));
                            ShipController controller = ship.GetComponent<ShipController>();

                            controller.SetShipType(preset.m_ships[i].m_shipType, preset.m_ships[i].m_team, shipID, preset.m_ships[i].m_angle, position, preset.m_ships[i].m_respawnTime, preset.m_ships[i].m_path, preset.m_ships[i].m_pathSpeed);
                            SendStart(BombersNetworkManager.SHIP, controller.GetShipType().ToString() + "***" + shipID.ToString() + "^^^" + preset.m_ships[i].m_team.ToString(), ship.name.Replace("(Clone)", ""), ship.transform);
                            // yield return YieldFactory.GetWaitForEndOfFrame();
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
                            GameObject rock = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/Rock0" + Random.Range(1, 5)), position, rotation);

                            SendStart(BombersNetworkManager.ROCK, "", rock.name.Replace("(Clone)", ""), rock.transform);
                            //yield return YieldFactory.GetWaitForEndOfFrame();
                        }
                        else
                        {
                            i--;
                        }
                    }

                    m_once = true;
                }
                m_gameState = eGameState.GAME_STATE_SPAWN_PLAYERS;
            }
            yield return null;
        }

        public const string SERVER_BOT = "serverBot";
        IEnumerator SpawnAllPlayers()
        {
            yield return YieldFactory.GetWaitForEndOfFrame();
            // now spawn all the players!
            int i = 0;
            List<LobbyMemberInfo> members = BombersNetworkManager.LobbyInfo.Members;
            for (; i < members.Count; ++i)
            {
                RpcSpawnPlayer(members[i].ProfileId);
                yield return YieldFactory.GetWaitForEndOfFrame();
            }
            //yield return YieldFactory.GetWaitForEndOfFrame();

            // spawn server controlled bots!
            int maxPlayers = m_gameInfo.GetMaxPlayers();
            Dictionary<string, object> botDict = new Dictionary<string, object>();

            botDict["pic"] = "";
            botDict["team"] = BombersNetworkManager.LobbyInfo.GetTeamWithLeastPeople();
            botDict["cxId"] = "";
            botDict["rating"] = 0;
            botDict["isReady"] = true;

            LobbyMemberInfo newMember = null;
            for (; i < maxPlayers; ++i)
            {
                botDict["team"] = BombersNetworkManager.LobbyInfo.GetTeamWithLeastPeople();
                botDict["profileId"] = SERVER_BOT + i;
                botDict["name"] = botDict["profileId"];
                botDict["netId"] = System.Convert.ToInt16(i + 8);

                newMember = new LobbyMemberInfo(botDict);
                BombersNetworkManager.LobbyInfo.Members.Add(newMember);
                string infoStr = SerializeDict(botDict, BombersNetworkManager.SPECIAL_INNER_JOIN, BombersNetworkManager.SPECIAL_INNER_SPLIT);
                SendStart(BombersNetworkManager.MEMBER_ADD, newMember.NetId, infoStr, null);
                RpcSpawnPlayer(newMember.ProfileId);

                yield return YieldFactory.GetWaitForEndOfFrame();
            }

            m_gameState = eGameState.GAME_STATE_PLAYING_GAME;
            yield return null;
        }

        public void StartGameStart()
        {
            StopCoroutine("SpawnGameStart");
            StartCoroutine("SpawnGameStart");
        }

        void Update()
        {
            /*
            if (Input.GetKeyUp(KeyCode.L))
            {
                for (int i = 0; i < BombersNetworkManager.LobbyInfo.Members.Count; ++i)
                {
                    if (BombersNetworkManager.LobbyInfo.Members[i].ProfileId != BombersNetworkManager.LocalPlayer.ProfileId)
                        createOffScreenIndicator(BombersNetworkManager.LobbyInfo.Members[i].PlayerController);
                }
            }
            if (Input.GetKeyUp(KeyCode.L))
            {
                foreach (ShipController controller in m_spawnedShips)
                {
                    createOffScreenShipIndicator(controller);
                }
            }
            */

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
                    break;
                case eGameState.GAME_STATE_SPAWN_PLAYERS:
                    m_showScores = false;
                    if (IsServer && m_once)
                    {
                        m_once = false;
                        StopCoroutine("SpawnAllPlayers");
                        StartCoroutine("SpawnAllPlayers");
                    }

                    break;

                case eGameState.GAME_STATE_PLAYING_GAME:

                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        m_showQuitMenu = !m_showQuitMenu;
                    }
                    if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyUp(KeyCode.Tab))
                    {
                        removeResultsGroupCells();
                    }
                    m_showScores = Input.GetKey(KeyCode.Tab);
                    m_quitMenu.SetActive(m_showQuitMenu);

                    if (!m_once)
                    {
                        m_once = true;
                        //GameManager.SERVER_BOT
                        List<LobbyMemberInfo> members = BombersNetworkManager.LobbyInfo.Members.FindAll(x => !x.Name.Contains(SERVER_BOT));
                        GFriendsManager.Instance.SetRecentlyViewedData(BombersNetworkManager.LobbyInfo.Members);
                    }

                    if (m_initialLoadingScreen.GetComponent<CanvasGroup>().alpha <= 0)
                    {
                        m_gameTime = m_gameInfo.GetGameTime();
                        m_gameTime -= Time.deltaTime;
                        m_gameInfo.SetGameTime(m_gameTime);
                    }

                    if (IsServer)
                    {
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
                            CmdEndGame();
                        }
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

                        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyUp(KeyCode.Tab))
                        {
                            removeResultsGroupCells();
                        }

                        m_showScores = Input.GetKey(KeyCode.Tab);
                        m_quitMenu.SetActive(m_showQuitMenu);
                        if (!m_once)
                        {
                            //GameObject.Find("BackgroundMusic").GetComponent<AudioSource>().Play();
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
                        if (BombersNetworkManager.LocalPlayer != null)
                            BombersNetworkManager.LocalPlayer.EndGame();

                        StartCoroutine("startingInCountDown");
                        //if (_isServer)
                        {
                            if (m_team1Score > m_team2Score)
                            {
                                RpcAwardExperience(1);
                            }
                            else if (m_team2Score > m_team1Score)
                            {
                                RpcAwardExperience(2);
                            }
                            else
                            {
                                RpcAwardExperience(0);
                            }
                        }
                    }
                    break;
            }

            switch (m_gameState)
            {
                case eGameState.GAME_STATE_WAITING_FOR_PLAYERS:
                    CmdForceStartGame();
                    break;
                case eGameState.GAME_STATE_STARTING_GAME:
                    m_initialLoadingScreen.GetComponent<CanvasGroup>().alpha += Time.fixedDeltaTime * 3;
                    m_resultsWindow.alpha = 0;
                    m_resultsWindow.gameObject.SetActive(false);
                    m_HUD.SetActive(false);
                    break;
                case eGameState.GAME_STATE_SPECTATING:
                    m_initialLoadingScreen.SetActive(false);
                    m_resultsWindow.gameObject.SetActive(m_showScores);
                    m_resultsWindow.alpha = m_showScores ? 1 : 0;
                    if (m_showScores)
                    {
                        OnMiniScoresWindow();
                    }
                    m_HUD.SetActive(true);
                    OnHudWindow();
                    break;
                case eGameState.GAME_STATE_GAME_OVER:
                    m_resultsWindow.gameObject.SetActive(true);
                    m_HUD.SetActive(false);
                    OnScoresWindow();
                    break;

                case eGameState.GAME_STATE_PLAYING_GAME:
                    m_initialLoadingScreen.GetComponent<CanvasGroup>().alpha -= Time.fixedDeltaTime * 3;
                    m_resultsWindow.gameObject.SetActive(false);
                    m_HUD.SetActive(true);
                    if (m_showScores)
                    {
                        m_resultsWindow.alpha += Time.fixedDeltaTime * 4;
                        if (m_resultsWindow.alpha > 1) m_resultsWindow.alpha = 1;
                        m_resultsWindow.gameObject.SetActive(true);
                        OnMiniScoresWindow();
                    }
                    else
                    {
                        if (m_resultsWindow.alpha > 0) m_resultsWindow.gameObject.SetActive(true);
                        m_resultsWindow.alpha -= Time.fixedDeltaTime * 4;
                        if (m_resultsWindow.alpha < 0) m_resultsWindow.alpha = 0;
                    }
                    OnHudWindow();
                    break;

                default:
                    m_resultsWindow.gameObject.SetActive(false);
                    m_HUD.SetActive(false);
                    break;
            }
#if !(UNITY_IOS || UNITY_ANDROID)
            Cursor.visible = m_quitMenu.gameObject.activeInHierarchy ||
                             m_resultsWindow.gameObject.activeInHierarchy ||
                             GStateManager.Instance.CurrentSubStateId != GStateManager.UNDEFINED_STATE;

            Cursor.lockState = !Cursor.visible ? CursorLockMode.Confined : CursorLockMode.None;
#endif
        }

        public void AddSpawnedShip(ShipController aShip)
        {
            m_spawnedShips.Add(aShip);
        }

        void CmdEndGame()
        {
            RpcEndGame();
            SendDestroy(BombersNetworkManager.GAME, "", "");
        }

        //[ClientRpc]
        public void RpcEndGame()
        {
            StopCoroutine("RespawnPlayer");
            m_gameState = eGameState.GAME_STATE_GAME_OVER;
            removeResultsGroupCells();
            m_allyShipSunk.SetActive(false);
            m_enemyShipSunk.SetActive(false);
            m_redShipLogo.SetActive(false);
            m_greenShipLogo.SetActive(false);
            if (BombersNetworkManager.LocalPlayer != null)
                BombersNetworkManager.LocalPlayer.DestroyPlayerPlane();
        }

        //[Command]
        public void CmdHitShipTargetPoint(int aShipID, int aTargetIndex, string aBombInfoString)
        {
            RpcHitShipTargetPoint(aShipID, aTargetIndex, aBombInfoString);
            SendStart(BombersNetworkManager.HIT_TARGET, aShipID + "***" + aTargetIndex, aBombInfoString, this.transform);
        }

        //[ClientRpc]
        public void RpcHitShipTargetPoint(int aShipID, int aTargetIndex, string aBombInfoString)
        {
            ShipTarget shipTarget = null;
            GameObject ship = null;
            ShipTarget aShipTarget = new ShipTarget(aShipID, aTargetIndex);
            BombInfo aBombInfo = BombInfo.GetBombInfo(aBombInfoString);
            for (int i = 0; i < m_spawnedShips.Count; i++)
            {
                if (m_spawnedShips[i].ContainsShipTarget(aShipTarget))
                {
                    shipTarget = m_spawnedShips[i].GetShipTarget(aShipTarget);
                    ship = m_spawnedShips[i].gameObject;
                    break;
                }
            }

            BombersPlayerController shooterController = BombersPlayerController.GetPlayer(aBombInfo.m_shooter);
            m_gameInfo.SetTeamScore(shooterController.m_team, m_gameInfo.GetTeamScore(shooterController.m_team) + GConfigManager.GetIntValue("ScoreForWeakpointDestruction"));

            if (aBombInfo.m_shooter == BombersNetworkManager.LocalPlayer.NetId)
            {
                m_bombsHit++;
            }
            shooterController.m_score += GConfigManager.GetIntValue("ScoreForWeakpointDestruction");

            Plane[] frustrum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(frustrum, ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Collider>().bounds))
            {
                BombersNetworkManager.LocalPlayer.ShakeCamera(GConfigManager.GetFloatValue("WeakpointDestructionShakeIntensity"), GConfigManager.GetFloatValue("ScreenShakeTime"));
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


        //[Command]
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
            if (IsServer)
            {
                ship.SetShipType(ship.GetShipType(), ship.m_team, aShipID);
            }
            RpcRespawnShip(aShipID);
        }

        //[ClientRpc]
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
            yield return YieldFactory.GetWaitForSeconds(2);

            while (time > 0)
            {
                time -= Time.fixedDeltaTime;
                fadeColor = new Color(1, 1, 1, fadeColor.a - Time.fixedDeltaTime);
                aText.GetComponent<Image>().color = fadeColor;
                aLogo.GetComponent<Image>().color = fadeColor;
                yield return new WaitForFixedUpdate();
            }
        }

        //[Command]
        public void CmdDestroyedShip(int aShipID, string aBombInfoString)
        {
            RpcDestroyedShip(aShipID, aBombInfoString);
            SendDestroy(BombersNetworkManager.SHIP, aShipID.ToString(), aBombInfoString);
        }

        //[ClientRpc]
        public void RpcDestroyedShip(int aShipID, string aBombInfoString)
        {
            BombInfo aBombInfo = BombInfo.GetBombInfo(aBombInfoString);
            ShipController ship = null;
            for (int i = 0; i < m_spawnedShips.Count; i++)
            {
                if (m_spawnedShips[i].m_shipID == aShipID)
                {
                    ship = m_spawnedShips[i];
                    break;
                }
            }
            BombersPlayerController shooterController = BombersPlayerController.GetPlayer(aBombInfo.m_shooter);
            m_gameInfo.SetTeamScore(shooterController.m_team, m_gameInfo.GetTeamScore(shooterController.m_team) + GConfigManager.GetIntValue("ScoreForShipDestruction"));

            Plane[] frustrum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(frustrum, ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Collider>().bounds))
            {
                BombersNetworkManager.LocalPlayer.ShakeCamera(GConfigManager.GetFloatValue("ShipDestructionShakeIntensity"), GConfigManager.GetFloatValue("ScreenShakeTime"));
            }

            if (ship == null) return;

            ship.m_isAlive = false;
            StopCoroutine("FadeOutShipMessage");
            int playerTeam = BombersNetworkManager.LocalPlayer.m_team;
            // simplify
            if (ship.m_team == 1)
            {
                if (playerTeam == 1)
                {
                    StartCoroutine(FadeOutShipMessage(m_allyShipSunk, m_greenShipLogo));
                }
                else if (playerTeam == 2)
                {
                    StartCoroutine(FadeOutShipMessage(m_enemyShipSunk, m_greenShipLogo));
                }
            }
            else
            {
                if (playerTeam == 1)
                {
                    StartCoroutine(FadeOutShipMessage(m_enemyShipSunk, m_redShipLogo));
                }
                else if (playerTeam == 2)
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
            int bomberTeam = shooterController.m_team;
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

            if (IsServer)
                ship.StartRespawn();

            if (aBombInfo.m_shooter == BombersNetworkManager.LocalPlayer.NetId)
            {
                m_carriersDestroyed++;
            }
            shooterController.m_score += GConfigManager.GetIntValue("ScoreForShipDestruction");
        }

        //[ClientRpc]
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
            BrainCloudStats.Instance.IncrementExperienceToBrainCloud(m_planesDestroyed * GConfigManager.GetIntValue("ExpForKill"));
            BrainCloudStats.Instance.SubmitLeaderboardData(m_planesDestroyed, m_bombsHit, m_timesDestroyed);
            m_shotsFired = 0;
            m_bombsDropped = 0;
            m_bombsHit = 0;
            m_planesDestroyed = 0;
            m_carriersDestroyed = 0;
            m_timesDestroyed = 0;
        }
        /*
        //[Command]
        public void CmdSpawnFlare(Vector3 aPosition, Vector3 aVelocity, string aPlayerID)
        {
            RpcSpawnFlare(aPosition, aVelocity, aPlayerID);
        }

        //[ClientRpc]
        void RpcSpawnFlare(Vector3 aPosition, Vector3 aVelocity, string aPlayer)
        {
            GameObject flare = (GameObject)Instantiate(m_flare, aPosition, Quaternion.identity);
            flare.GetComponent<FlareController>().Activate(aPlayer);
            flare.GetComponent<Rigidbody>().velocity = aVelocity;
        }
        */

        //[ClientRpc]
        public void RpcSpawnPlayer(string in_profileId)
        {
            Vector3 spawnPoint = Vector3.zero;
            spawnPoint.z = 22.0f;
            LobbyMemberInfo member = BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(in_profileId);

            int playerTeam = member.Team == "green" ? 1 : 2;
            if (playerTeam == 1)
            {
                spawnPoint.x = Random.Range(m_team1SpawnBounds.bounds.center.x - m_team1SpawnBounds.bounds.size.x / 2, m_team1SpawnBounds.bounds.center.x + m_team1SpawnBounds.bounds.size.x / 2) - 10;
                spawnPoint.y = Random.Range(m_team1SpawnBounds.bounds.center.y - m_team1SpawnBounds.bounds.size.y / 2, m_team1SpawnBounds.bounds.center.y + m_team1SpawnBounds.bounds.size.y / 2);
            }
            else if (playerTeam == 2)
            {
                spawnPoint.x = Random.Range(m_team2SpawnBounds.bounds.center.x - m_team2SpawnBounds.bounds.size.x / 2, m_team2SpawnBounds.bounds.center.x + m_team2SpawnBounds.bounds.size.x / 2) + 10;
                spawnPoint.y = Random.Range(m_team2SpawnBounds.bounds.center.y - m_team2SpawnBounds.bounds.size.y / 2, m_team2SpawnBounds.bounds.center.y + m_team2SpawnBounds.bounds.size.y / 2);
            }

            GameObject playerPlane = Instantiate(Resources.Load<GameObject>("Prefabs/Game/PlayerController"), spawnPoint, Quaternion.LookRotation(Vector3.forward, new Vector3(0, 0, 22.0f)));

            BombersPlayerController controller = playerPlane.GetComponent<BombersPlayerController>();
            if (playerTeam == 1)
            {
                playerPlane.layer = 8;
            }
            else if (playerTeam == 2)
            {
                playerPlane.layer = 9;
            }
            controller.m_team = playerTeam;
            if (in_profileId == GCore.Wrapper.Client.ProfileId)
            {
                BombersNetworkManager.LocalPlayer = controller;
            }
            controller.NetId = BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(in_profileId).NetId;

            member.PlayerController = controller;
            controller.m_displayName = member.Name;
            controller.SpawnPlayer(in_profileId);
            m_gameState = eGameState.GAME_STATE_PLAYING_GAME;
        }

        public void RpcSpawnPlayer(string in_profileId, Vector3 in_spawnPoint)
        {
            Vector3 spawnPoint = new Vector3(in_spawnPoint.x, in_spawnPoint.y, in_spawnPoint.z);
            LobbyMemberInfo member = BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(in_profileId);

            GameObject playerPlane = Instantiate(Resources.Load<GameObject>("Prefabs/Game/PlayerController"), spawnPoint, Quaternion.LookRotation(Vector3.forward, new Vector3(0, 0, 22.0f)));

            BombersPlayerController controller = playerPlane.GetComponent<BombersPlayerController>();

            int playerTeam = member.Team == "green" ? 1 : 2;
            if (playerTeam == 1)
            {
                playerPlane.layer = 8;
            }
            else if (playerTeam == 2)
            {
                playerPlane.layer = 9;
            }
            controller.m_team = playerTeam;
            if (in_profileId == GCore.Wrapper.Client.ProfileId)
            {
                BombersNetworkManager.LocalPlayer = controller;
            }

            controller.NetId = member.NetId;
            member.PlayerController = controller;
            controller.m_displayName = member.Name;
            controller.SpawnPlayer(in_profileId, in_spawnPoint);
            m_gameState = eGameState.GAME_STATE_PLAYING_GAME;
        }

        //[Command]
        void CmdSpawnBombPickup(Vector3 aPosition)
        {
            int bombID = GetNextBombID();
            RpcSpawnBombPickup(aPosition, bombID);
        }

        //[ClientRpc]
        public void RpcSpawnBombPickup(Vector3 aPosition, int bombID)
        {
            GameObject bombPickup = (GameObject)Instantiate(m_bombPickup, aPosition, Quaternion.identity);
            bombPickup.GetComponent<BombPickup>().Activate(bombID);
            m_bombPickupsSpawned.Add(bombPickup.GetComponent<BombPickup>());

            if (IsServer)
                SendStart(BombersNetworkManager.PICKUP, bombID, bombPickup.transform);
        }

        //[Command]
        public void CmdBombPickedUp(short aPlayer, int aPickupID)
        {
            RpcBombPickedUp(aPlayer, aPickupID);
            SendDestroy(BombersNetworkManager.PICKUP, aPlayer, aPickupID);
        }

        //[ClientRpc]
        public void RpcBombPickedUp(short aPlayer, int aPickupID)
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

            BombersPlayerController playerController = BombersNetworkManager.LocalPlayer;
            foreach (LobbyMemberInfo member in BombersNetworkManager.LobbyInfo.Members)
            {
                if (member.PlayerController.NetId == aPlayer)
                {
                    member.PlayerController.WeaponController.AddBomb();

                    // when picked up
                    if (aPlayer != playerController.NetId &&
                        member.PlayerController.m_team != playerController.m_team)
                    {
                        createOffScreenIndicator(member.PlayerController);
                    }

                    // all ships on other team
                    List<ShipController> ships = m_spawnedShips.FindAll(x => x.m_team != BombersNetworkManager.LocalPlayer.m_team && x.IsAlive());

                    if (ships.Count == 1 &&
                        BombersNetworkManager.LocalPlayer.transform.FindDeepChild("OffScreenShipIndicator(Clone)") == null)// expensive
                    {
                        createOffScreenShipIndicator(ships[0]);
                    }
                    break;
                }
            }
        }

        public void CmdSpawnBomb(Vector3 aStartPos, Vector3 aDirection, Vector3 aSpeed, short aShooter)
        {
            if (aShooter == BombersNetworkManager.LocalPlayer.NetId)
                m_bombsDropped++;
            int id = GetNextBombID();
            BombInfo aBombInfo = new BombInfo(aStartPos, aDirection, aShooter, aSpeed, id);
            string jsonBomb = aBombInfo.GetJson();
            GameObject bomb = RpcSpawnBomb(aBombInfo);
            SendProjectileStart(BombersNetworkManager.BOMB, aBombInfo.GetDict());
        }

        //[ClientRpc]
        public GameObject RpcSpawnBomb(BombInfo aBombInfo)
        {
            GameObject bomb = null;
            int playerTeam = BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team;
            foreach (LobbyMemberInfo member in BombersNetworkManager.LobbyInfo.Members)
            {
                if (member.PlayerController.NetId == aBombInfo.m_shooter)
                {
                    bomb = member.PlayerController.WeaponController.SpawnBomb(aBombInfo);
                    m_spawnedBombs.Add(bomb.GetComponent<BombController>().BombInfo);

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
            }
            return bomb;
        }

        //[Command]
        public void CmdDeleteBomb(string aBombInfo, int aHitSurface)
        {
            BombInfo aBombInfo2 = BombInfo.GetBombInfo(aBombInfo);
            RpcDeleteBomb(aBombInfo2, aHitSurface);
            //SendDestroy(BombersNetworkManager.BOMB, aBombInfo2.GetJson(), aHitSurface.ToString());
            SendProjectileDestroy(BombersNetworkManager.BOMB, aBombInfo2.GetDict());
        }

        //[ClientRpc]
        public void RpcDeleteBomb(BombInfo aBombInfo, int aHitSurface)
        {
            int index = GetIndexOfBombInfo(aBombInfo);
            if (index >= 0)
            {
                DestroyBombAtIndex(index, aHitSurface);
            }
        }

        //[Command]
        public void CmdSpawnBullet(string aBulletInfo)
        {
            BulletInfo bulletInfo = BulletInfo.GetBulletInfo(aBulletInfo);
            if (bulletInfo.m_shooter == BombersNetworkManager.LocalPlayer.NetId)
                m_shotsFired++;
            RpcSpawnBullet(bulletInfo);
        }

        // We create a bullet from a remote packet. We couldn't add the bullet prediction to 
        // RpcSpawnBullet because that function is also called by local player from CmdSpawnBullet.
        public void CreateRemoteBullet(BulletInfo bulletInfo)
        {
            var bullet = RpcSpawnBullet(bulletInfo);

            // We need to move the bullet forward a bit since it got shot and the delay of receiving that packet
            // This alone doesn't work well! Bullets appear to "spawn" out of thin air.
            // Solution to make it feel better is to speed up the bullets, and shorten their alive time
            var bulletControler = bullet.GetComponent<BulletController>();
            var rigidbody = bullet.GetComponent<Rigidbody>();
            var bulletLifeTime = GConfigManager.GetFloatValue("BulletLifeTime"); // Bullet start() function not called yet, so we can't fetch that value from it. Read it directly from configs

            // Compute combined ping
            var myPing = (float)(GCore.Wrapper.Client.RelayService.LastPing * 0.0001);
            var hisPing = (float)bulletInfo.m_lastPing;
            var combinedPings = (myPing + hisPing) * 0.001f;
            combinedPings *= 0.5f; // Ping are round trip, here we only care about the one way time.

            // Shorter life based, faster velocity. So we process the same distance, in a shorter time
            var originalTravelDistance = (rigidbody.velocity * bulletLifeTime).magnitude;
            var truncatedTravelDistance = originalTravelDistance - (rigidbody.velocity * combinedPings).magnitude;
            bulletControler.setLifeTimeOffset(-combinedPings);
            rigidbody.velocity *= originalTravelDistance / truncatedTravelDistance;
        }

        //[ClientRpc]
        public GameObject RpcSpawnBullet(BulletInfo in_bulletInfo)
        {
            GameObject bullet = BombersNetworkManager.LocalPlayer.WeaponController.SpawnBullet(in_bulletInfo);
            m_spawnedBullets.Add(in_bulletInfo);
            return bullet;
        }

        //[ClientRpc]
        public void DeleteBullet(BulletInfo in_info)
        {
            int index = GetIndexOfBulletInfo(in_info);
            if (index >= 0)
            {
                DestroyBulletAtIndex(index);
            }
        }

        //[Command]
        public void RpcBulletHitPlayer(BulletInfo bulletInfo)
        {
            DeleteBullet(bulletInfo);
            Instantiate((GameObject)Resources.Load("Prefabs/Game/BulletHit"), bulletInfo.m_startPosition, Quaternion.LookRotation(bulletInfo.m_startDirection, -Vector3.forward));

            foreach (LobbyMemberInfo member in BombersNetworkManager.LobbyInfo.Members)
            {
                if (member.PlayerController.NetId == bulletInfo.m_hitId)
                {
                    member.PlayerController.TakeBulletDamage(bulletInfo.m_shooter);
                    break;
                }
            }
        }

        //[ClientRpc]
        public void RpcDestroyPlayerPlane(short aVictim, short aShooter)
        {
            BombersPlayerController tempController = null;
            foreach (LobbyMemberInfo member in BombersNetworkManager.LobbyInfo.Members)
            {
                tempController = member.PlayerController;
                if (tempController == null) continue;
                if (tempController.NetId == aVictim)
                {
                    break;
                }
            }
            if (tempController == null) return;
            if (tempController.m_planeActive)
            {
                if (IsServer)
                {
                    // spawn a bomb pick up at this location and all the other bombs
                    // only do this if plane was shot by valid shooter
                    if (aShooter != -1)
                    {
                        Vector3 position = tempController.m_playerPlane.transform.position;
                        int bombs = tempController.WeaponController.GetBombs();
                        CmdSpawnBombPickup(position);
                        for (int i = 0; i < bombs; i++)
                        {
                            CmdSpawnBombPickup(position);
                        }
                    }

                    // send destroyed!
                    SendDestroy(BombersNetworkManager.PLANE_CONTROLLER, aVictim, aShooter);
                }

                tempController.m_deaths++;
                GameObject explosion = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "PlayerExplosion"),
                                   tempController.m_playerPlane.transform.position,
                                   tempController.m_playerPlane.transform.rotation);
                explosion.GetComponent<AudioSource>().Play();

                tempController.DestroyPlayerPlane();
                StartCoroutine(RespawnPlayer(tempController));

                foreach (LobbyMemberInfo member in BombersNetworkManager.LobbyInfo.Members)
                {
                    tempController = member.PlayerController;
                    if (tempController != null && tempController.NetId == aShooter)
                    {
                        tempController.m_kills++;
                        break;
                    }
                }
            }
        }

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
        private GameObject m_bombPickup;
        private GameObject m_weakpointExplosion;

        private GameObject m_carrier01;
        private GameObject m_bombExplosion;
        private GameObject m_bombWaterExplosion;
        private GameObject m_bombDud;

        private GameObject m_displayDialog;
        private CanvasGroup m_resultsWindow;
        private GameObject m_greenLogo;

        private GameObject m_redLogo;
        private GameObject m_enemyWinText;
        private GameObject m_allyWinText;
        private GameObject m_resetButton;

        private GameObject m_quitButton;
        private GameObject m_allyShipSunk;
        private GameObject m_enemyShipSunk;
        private GameObject m_redShipLogo;

        private GameObject m_greenShipLogo;
        private GameObject m_quitMenu;
        private GameObject m_initialLoadingScreen;
        private GameObject m_HUD;

        private GameObject m_offscreenPrefab;
        private GameObject m_offscreenShipPrefab;
        private GameObject m_resultCellPrefab;
        private GameObject m_resultsCellPrefabYou;
        private GameObject m_missionText;

        private TextMeshProUGUI m_timeLeft;
        private TextMeshProUGUI m_respawnText;
        private TextMeshProUGUI m_countDownText;
        private TextMeshProUGUI m_greenTeamScore;
        private TextMeshProUGUI m_redTeamScore;

        private Transform m_redScoreTrans;
        private Transform m_greenScoreTrans;

        private Transform m_greenTeamResultsTransform;
        private Transform m_redTeamResultsTransform;

        private GameInfo m_gameInfo;

        private bool m_showQuitMenu = false;
        private bool m_showScores = false;

        private List<GenericAnimator> m_redShipAnimators;
        private List<GenericAnimator> m_greenShipAnimators;
        private List<ShipController> m_spawnedShips;
        private List<BombPickup> m_bombPickupsSpawned;
        private List<BulletInfo> m_spawnedBullets;
    }
}
