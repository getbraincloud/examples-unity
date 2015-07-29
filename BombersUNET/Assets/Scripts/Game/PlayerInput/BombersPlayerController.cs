/*
 * Most game logic and spawning of items is controlled through the player controller, 
 * which also controls the movement and display of the plane, and collision with billet and bomb pickups
 * 
 * UNET doesn't allow non-player objects to communicate with the server, so the play object has to implement all of the same game logic code
 * that is normally done in the GameManager.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloudUNETExample.Connection;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace BrainCloudUNETExample.Game.PlayerInput
{
    public class BombersPlayerController : NetworkBehaviour
    {

        public static BombersPlayerController GetPlayer(int aID)
        {
            GameObject[] playerList = GameObject.FindGameObjectsWithTag("PlayerController");

            for (int i = 0; i < playerList.Length; i++)
            {
                if (playerList[i].GetComponent<BombersPlayerController>().m_playerID == aID)
                {
                    return playerList[i].GetComponent<BombersPlayerController>();
                }
            }
            return null;
        }

        GameManager m_gMan;

        [SyncVar]
        public int m_score;

        [SyncVar]
        public int m_playerID;

        [SyncVar]
        public int m_team;

        [SyncVar]
        public int m_kills;

        [SyncVar]
        public int m_deaths;

        [SyncVar]
        public int m_ping;

        [SyncVar]
        public string m_displayName;

        private bool m_isAccelerating = false;

        private bool m_isTurningRight = false;

        private bool m_isTurningLeft = false;

        public PlaneController m_playerPlane;

        private float m_turnSpeed = 1;
        private float m_acceleration = 1;

        private float m_currentRotation = 0;

        private bool m_isActive = false;

        private int m_baseHealth = 3;

        private bool m_respawning = true;

        [SyncVar]
        public int m_health = 0;

        private float m_speedMultiplier = 1;

        private float m_maxSpeedMultiplier = 2.5f;
        private float m_leftBoundsTimer = 0;
        private bool m_leftBounds = false;

        private Vector3 m_originalCamPosition = Vector3.zero;
        private float m_shakeDecay = 0;
        private float m_shakeIntensity = 0;
        public GameObject m_missionText;

        [SyncVar]
        public bool m_planeActive = false;

        void Awake()
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
        }

        void Start()
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }


            m_gMan = GameObject.Find("GameManager").GetComponent<GameManager>();
            StartCoroutine("UpdateVarsClient");
            CmdUpdateSyncVars();
            BombersNetworkManager.m_localPlayer = this;
            m_displayName = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_playerName;
            m_playerID = (int)netId.Value;

            m_missionText = m_gMan.m_missionText;
            m_turnSpeed = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_planeTurnSpeed;
            m_acceleration = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_planeAcceleration;
            m_baseHealth = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_basePlaneHealth;
            m_maxSpeedMultiplier = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_maxPlaneSpeedMultiplier;
        }

        [Command(channel=1)]
        void CmdUpdateSyncVars()
        {
            StartCoroutine("UpdateVars");
        }

        [Command(channel=1)]
        void CmdUpdateSyncVarsFromClient(int aScore, int aPlayerID, int aTeam, int aKills, int aDeaths, int aPing, string aDisplayName, bool isActive, int aHealth)
        {
            m_score = aScore;
            m_playerID = aPlayerID;
            m_team = aTeam;
            m_kills = aKills;
            m_deaths = aDeaths;
            m_ping = aPing;
            m_displayName = aDisplayName;
            m_planeActive = isActive;
            m_health = aHealth;
        }

        IEnumerator UpdateVars()
        {
            while (true)
            {
                SetDirtyBit(syncVarDirtyBits);
                yield return new WaitForSeconds(1);
            }
        }

        IEnumerator UpdateVarsClient()
        {
            if (isServer) yield break;

            while (true)
            {
                CmdUpdateSyncVarsFromClient(m_score, m_playerID, m_team, m_kills, m_deaths, m_ping, m_displayName, m_planeActive, m_health);
                yield return new WaitForSeconds(GetNetworkSendInterval());
            }
        }

        public void SetPlayerPlane(PlaneController playerPlane)
        {
            m_planeActive = true;
            ActivatePlane();
            m_leftBoundsTimer = 4;
            if (isLocalPlayer)
                GameObject.Find("MapBounds").GetComponent<MapBoundsCheck>().m_playerPlane = m_playerPlane.gameObject;
            StartCoroutine("PulseMissionText");
            m_leftBounds = false;
            m_currentRotation = m_playerPlane.gameObject.transform.rotation.eulerAngles.z;
            m_isActive = true;
            GetComponent<WeaponController>().SetPlayerPlane(m_playerPlane);
            m_health = m_baseHealth;
            m_playerPlane.m_playerID = m_playerID;
        }

        IEnumerator PulseMissionText()
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                yield break;
            }
            bool goingToColor1 = true;
            float time = 0;
            while (true)
            {
                while (goingToColor1)
                {
                    m_missionText.GetComponent<Text>().color = Color.Lerp(m_missionText.GetComponent<Text>().color, new Color(1, 0, 0, 1), 4 * Time.fixedDeltaTime);
                    time += Time.fixedDeltaTime;
                    if (time > 0.3f)
                    {
                        goingToColor1 = !goingToColor1;
                    }
                    yield return new WaitForFixedUpdate();
                }
                time = 0;
                while (!goingToColor1)
                {
                    m_missionText.GetComponent<Text>().color = Color.Lerp(m_missionText.GetComponent<Text>().color, new Color(0.3f, 0, 0, 1), 4 * Time.fixedDeltaTime);
                    time += Time.fixedDeltaTime;
                    if (time > 0.3f)
                    {
                        goingToColor1 = !goingToColor1;
                    }
                    yield return new WaitForFixedUpdate();
                }
                time = 0;
            }
        }

        public void ActivatePlane()
        {
            GetComponent<PlaneController>().enabled = true;
            transform.GetChild(0).gameObject.SetActive(true);
            transform.GetChild(1).gameObject.SetActive(true);
            if (!GetComponent<AudioSource>().isPlaying) GetComponent<AudioSource>().Play();
            GetComponent<Collider>().enabled = true;
        }

        public void DeactivatePlane()
        {
            GetComponent<PlaneController>().enabled = false;
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<AudioSource>().Stop();
            GetComponent<Collider>().enabled = false;
        }

        void Update()
        {
            m_gMan = GameObject.Find("GameManager").GetComponent<GameManager>();
            if (!m_planeActive && !m_respawning)
            {
                m_respawning = false;
                CmdSpawnMyself();
            }

            if (m_planeActive)
            {
                GetComponent<PlaneController>().enabled = true;
                transform.GetChild(0).gameObject.SetActive(true);
                transform.GetChild(1).gameObject.SetActive(true);
                if (!GetComponent<AudioSource>().isPlaying) GetComponent<AudioSource>().Play();
                GetComponent<Collider>().enabled = true;
            }
            else
            {
                GetComponent<PlaneController>().enabled = false;
                transform.GetChild(0).gameObject.SetActive(false);
                transform.GetChild(1).gameObject.SetActive(false);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<AudioSource>().Stop();
                GetComponent<Collider>().enabled = false;

            }

            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }

            if (m_leftBounds)
            {
                if (m_leftBoundsTimer > 0)
                {
                    m_leftBoundsTimer -= Time.deltaTime;
                    if (m_leftBoundsTimer <= 0 && m_leftBounds)
                    {
                        SuicidePlayer();
                    }
                }
            }

            if (m_leftBounds)
            {
                m_missionText.SetActive(true);
                m_missionText.GetComponent<Text>().text = "0:0" + Mathf.CeilToInt(m_leftBoundsTimer);
            }
            else
            {
                m_missionText.GetComponent<Text>().text = "";
                m_missionText.SetActive(false);
            }
            if (!m_planeActive)
            {
                m_isAccelerating = false;
                m_isTurningLeft = false;
                m_isTurningRight = false;
                return;
            }

            if (!m_isActive) return;
            m_playerPlane.GetComponent<AudioSource>().pitch = 1 + ((m_speedMultiplier - 1) / m_maxSpeedMultiplier) * 0.5f;

            if (Input.GetAxis("Horizontal") > 0 && !m_isTurningLeft)
            {
                m_isTurningRight = true;
            }
            else
            {
                m_isTurningRight = false;
            }

            if (Input.GetAxis("Horizontal") < 0 && !m_isTurningRight)
            {
                m_isTurningLeft = true;
            }
            else
            {
                m_isTurningLeft = false;
            }

            if (Input.GetAxis("Vertical") > 0)
            {
                m_isAccelerating = true;
            }
            else
            {
                m_isAccelerating = false;
            }

            if (Input.GetButton("Fire1"))
            {
                GetComponent<WeaponController>().FireWeapon(m_isAccelerating);
            }

            if (Input.GetButtonDown("Fire2"))
            {
                GetComponent<WeaponController>().DropBomb();
            }

            if (Input.GetButtonDown("Fire3"))
            {
                GetComponent<WeaponController>().FireFlare(m_playerPlane.transform.position, m_playerPlane.GetComponent<Rigidbody>().velocity);
            }

            if (m_isAccelerating)
            {
                m_speedMultiplier += 3 * Time.deltaTime;
            }
            else
            {
                m_speedMultiplier -= 3 * Time.deltaTime;
            }

            if (m_speedMultiplier > m_maxSpeedMultiplier)
            {
                m_speedMultiplier = m_maxSpeedMultiplier;
            }
            else if (m_speedMultiplier < 1)
            {
                m_speedMultiplier = 1;
            }

            m_playerPlane.GetComponent<AudioSource>().pitch = 1 + ((m_speedMultiplier - 1) / m_maxSpeedMultiplier) * 0.8f;
        }

        void FixedUpdate()
        {
            if (!isLocalPlayer || !hasAuthority || !m_planeActive || !m_isActive)
            {
                return;
            }

            if (m_isTurningLeft && m_isAccelerating)
            {
                m_currentRotation += m_turnSpeed * 0.5f * Time.deltaTime;
            }
            else if (m_isTurningLeft)
            {
                m_currentRotation += m_turnSpeed * Time.deltaTime;
            }
            else if (m_isTurningRight && m_isAccelerating)
            {
                m_currentRotation -= m_turnSpeed * 0.5f * Time.deltaTime;
            }
            else if (m_isTurningRight)
            {
                m_currentRotation -= m_turnSpeed * Time.deltaTime;
            }

            m_playerPlane.Accelerate(m_acceleration, m_speedMultiplier);
            m_playerPlane.SetRotation(m_currentRotation);
        }

        void LateUpdate()
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            if (m_playerPlane != null)
            {
                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(m_playerPlane.transform.FindChild("CameraPosition").position.x, m_playerPlane.transform.FindChild("CameraPosition").position.y, -110), 0.5f);
                Camera.main.transform.GetChild(0).position = m_playerPlane.transform.position;
                m_playerPlane.GetComponent<AudioSource>().spatialBlend = 0;
            }

            Vector3 cameraPosition = Camera.main.transform.position;
            float height = 2 * 132 * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * Camera.main.aspect;
            Bounds bounds = new Bounds(new Vector3(cameraPosition.x, cameraPosition.y, 22), new Vector3(width, height, 0));
            Bounds mapBounds = GameObject.Find("MapBounds").GetComponent<Collider>().bounds;

            if (bounds.min.x < mapBounds.min.x)
            {
                cameraPosition.x = mapBounds.min.x - (bounds.min.x - bounds.center.x);
            }
            else if (bounds.max.x > mapBounds.max.x)
            {
                cameraPosition.x = mapBounds.max.x - (bounds.max.x - bounds.center.x);
            }

            if (bounds.min.y < mapBounds.min.y)
            {
                cameraPosition.y = mapBounds.min.y - (bounds.min.y - bounds.center.y);
            }
            else if (bounds.max.y > mapBounds.max.y)
            {
                cameraPosition.y = mapBounds.max.y - (bounds.max.y - bounds.center.y);
            }

            m_originalCamPosition = cameraPosition;
            if (m_shakeIntensity > 0)
            {
                cameraPosition = m_originalCamPosition + Random.insideUnitSphere * m_shakeIntensity;
                m_shakeIntensity -= (m_shakeIntensity / m_shakeDecay) * Time.deltaTime;
            }
            else
            {
                cameraPosition = m_originalCamPosition;
            }

            Camera.main.transform.position = cameraPosition;
        }

        public void ShakeCamera(float aIntensity, float aDecay)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            m_shakeIntensity = aIntensity;
            m_shakeDecay = aDecay;
        }

        public void SuicidePlayer()
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            m_leftBounds = false;
            m_health = 0;
            Vector3 position = m_playerPlane.transform.position;
            int bombs = GetComponent<WeaponController>().GetBombs();
            CmdDestroyPlayerPlane(m_playerID, -1);
            CmdSpawnBombPickup(position, NetworkManager.singleton.client.connection.connectionId);
            for (int i = 0; i < bombs; i++)
            {

                CmdSpawnBombPickup(position, NetworkManager.singleton.client.connection.connectionId);
            }
        }


        public void DestroyPlayerPlane()
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            if (!m_planeActive)
            {
                return;
            }
            GameObject.Find("MapBounds").GetComponent<MapBoundsCheck>().m_playerPlane = null;
            m_leftBounds = false;
            m_isActive = false;
            m_planeActive = false;
            DeactivatePlane();
            GetComponent<WeaponController>().DestroyPlayerPlane();
            m_health = 0;
        }

        public GameObject GetPlayerPlane()
        {
            return m_playerPlane.gameObject;
        }

        public void EndGame()
        {
            m_isActive = false;
        }

        public void DespawnBombPickupCommand(int aPickupID)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            CmdDespawnBombPickup(aPickupID);
        }

        [Command]
        public void CmdDespawnBombPickup(int aPickupID)
        {
            RpcDespawnBombPickup(aPickupID);
        }

        [ClientRpc]
        void RpcDespawnBombPickup(int aPickupID)
        {
            for (int i = 0; i < m_gMan.m_bombPickupsSpawned.Count; i++)
            {
                if (m_gMan.m_bombPickupsSpawned[i].m_pickupID == aPickupID)
                {
                    if (m_gMan.m_bombPickupsSpawned[i] != null && m_gMan.m_bombPickupsSpawned[i].gameObject != null) Destroy(m_gMan.m_bombPickupsSpawned[i].gameObject);
                    m_gMan.m_bombPickupsSpawned.RemoveAt(i);
                    break;
                }
            }
        }

        public void BombPickedUpCommand(int aPlayerID, int aPickupID)
        {

            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            CmdBombPickedUp(aPlayerID, aPickupID);
        }

        [Command]
        public void CmdBombPickedUp(int aPlayer, int aPickupID)
        {
            RpcBombPickedUp(aPlayer, aPickupID);
        }

        [ClientRpc]
        void RpcBombPickedUp(int aPlayer, int aPickupID)
        {
            for (int i = 0; i < m_gMan.m_bombPickupsSpawned.Count; i++)
            {
                if (m_gMan.m_bombPickupsSpawned[i].m_pickupID == aPickupID)
                {
                    if (m_gMan.m_bombPickupsSpawned[i] != null && m_gMan.m_bombPickupsSpawned[i].gameObject != null) Destroy(m_gMan.m_bombPickupsSpawned[i].gameObject);
                    m_gMan.m_bombPickupsSpawned.RemoveAt(i);
                    break;
                }
            }

            if (aPlayer == BombersNetworkManager.m_localPlayer.m_playerID)
            {
                BombersNetworkManager.m_localPlayer.GetComponent<WeaponController>().AddBomb();
            }
        }

        public void DestroyShipCommand(int shipID, string aJson)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            CmdDestroyedShip(shipID, aJson);
        }

        [Command]
        public void CmdDestroyedShip(int aShipID, string aBombInfoString)
        {
            BombController.BombInfo aBombInfo = BombController.BombInfo.GetBombInfo(aBombInfoString);

            if (isServer)
            {
                if (BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 1)
                {
                    m_gMan.m_gameInfo.SetTeamScore(1, m_gMan.m_gameInfo.GetTeamScore(1) + GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForShipDestruction);

                }
                else
                {
                    m_gMan.m_gameInfo.SetTeamScore(2, m_gMan.m_gameInfo.GetTeamScore(2) + GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForShipDestruction);

                }
            }

            RpcDestroyedShip(aShipID, aBombInfoString);
        }

        [ClientRpc]
        void RpcDestroyedShip(int aShipID, string aBombInfoString)
        {
            BombController.BombInfo aBombInfo = BombController.BombInfo.GetBombInfo(aBombInfoString);
            ShipController ship = null;
            for (int i = 0; i < m_gMan.m_spawnedShips.Count; i++)
            {
                if (m_gMan.m_spawnedShips[i].m_shipID == aShipID)
                {
                    ship = m_gMan.m_spawnedShips[i];
                    break;
                }
            }

            Plane[] frustrum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(frustrum, ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Collider>().bounds))
            {
                BombersNetworkManager.m_localPlayer.ShakeCamera(GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_shipIntensity, GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_shakeTime);
            }

            if (ship == null) return;

            ship.m_isAlive = false;
            StopCoroutine("FadeOutShipMessage");
            if (ship.m_team == 1)
            {
                if (BombersNetworkManager.m_localPlayer.m_team == 1)
                {
                    StartCoroutine(m_gMan.FadeOutShipMessage(m_gMan.m_allyShipSunk, m_gMan.m_greenShipLogo));
                }
                else if (BombersNetworkManager.m_localPlayer.m_team == 2)
                {
                    StartCoroutine(m_gMan.FadeOutShipMessage(m_gMan.m_enemyShipSunk, m_gMan.m_greenShipLogo));
                }
            }
            else
            {
                if (BombersNetworkManager.m_localPlayer.m_team == 1)
                {
                    StartCoroutine(m_gMan.FadeOutShipMessage(m_gMan.m_enemyShipSunk, m_gMan.m_redShipLogo));
                }
                else if (BombersNetworkManager.m_localPlayer.m_team == 2)
                {
                    StartCoroutine(m_gMan.FadeOutShipMessage(m_gMan.m_allyShipSunk, m_gMan.m_redShipLogo));
                }
            }


            string shipName = "";
            ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().enabled = false;
            int children = ship.transform.childCount;
            for (int i = 1; i < children; i++)
            {
                ship.transform.GetChild(i).GetChild(0).GetChild(4).GetComponent<ParticleSystem>().enableEmission = false;
            }
            GameObject explosion;
            string path = "";
            switch (ship.GetShipType())
            {
                case ShipController.eShipType.SHIP_TYPE_CARRIER:


                    if (BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 1)
                    {
                        shipName += "Red ";
                        path = "CarrierExplosion02";
                    }
                    else
                    {
                        shipName += "Green ";
                        path = "CarrierExplosion01";
                    }
                    explosion = (GameObject)Instantiate((GameObject)Resources.Load(path), ship.transform.position, ship.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    shipName += "Carrier";
                    break;
                case ShipController.eShipType.SHIP_TYPE_BATTLESHIP:

                    if (BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 1)
                    {
                        shipName += "Red ";
                        path = "BattleshipExplosion02";
                    }
                    else
                    {
                        shipName += "Green ";
                        path = "BattleshipExplosion01";
                    }
                    explosion = (GameObject)Instantiate((GameObject)Resources.Load(path), ship.transform.position, ship.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    shipName += "Battleship";
                    break;
                case ShipController.eShipType.SHIP_TYPE_CRUISER:
                    if (BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 1)
                    {
                        shipName += "Red ";
                        path = "CruiserExplosion02";
                    }
                    else
                    {
                        shipName += "Green ";
                        path = "CruiserExplosion01";
                    }
                    explosion = (GameObject)Instantiate((GameObject)Resources.Load(path), ship.transform.position, ship.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    shipName += "Cruiser";
                    break;
                case ShipController.eShipType.SHIP_TYPE_PATROLBOAT:
                    if (BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 1)
                    {
                        shipName += "Red ";
                        path = "PatrolBoatExplosion02";
                    }
                    else
                    {
                        shipName += "Green ";
                        path = "PatrolBoatExplosion01";
                    }
                    explosion = (GameObject)Instantiate((GameObject)Resources.Load(path), ship.transform.position, ship.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    shipName += "Patrol Boat";
                    break;
                case ShipController.eShipType.SHIP_TYPE_DESTROYER:
                    if (BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 1)
                    {
                        shipName += "Red ";
                        path = "DestroyerExplosion02";
                    }
                    else
                    {
                        shipName += "Green ";
                        path = "DestroyerExplosion01";
                    }
                    explosion = (GameObject)Instantiate((GameObject)Resources.Load(path), ship.transform.position, ship.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    shipName += "Destroyer";
                    break;
            }

            if (isServer)
                ship.StartRespawn();

            if (aBombInfo.m_shooter == BombersNetworkManager.m_localPlayer.m_playerID)
            {
                m_gMan.m_carriersDestroyed++;
                BombersNetworkManager.m_localPlayer.m_score += GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForShipDestruction;
            }
        }

        public void HitShipTargetPointCommand(int aID, int aIndex, string aJson)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            CmdHitShipTargetPoint(aID, aIndex, aJson);
        }

        [Command]
        public void CmdHitShipTargetPoint(int aShipID, int aTargetIndex, string aBombInfoString)
        {
            BombController.BombInfo aBombInfo = BombController.BombInfo.GetBombInfo(aBombInfoString);
            if (BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 1)
            {
                m_gMan.m_gameInfo.SetTeamScore(1, m_gMan.m_gameInfo.GetTeamScore(1) + GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForWeakpointDestruction);
            }
            else if (BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 2)
            {
                m_gMan.m_gameInfo.SetTeamScore(2, m_gMan.m_gameInfo.GetTeamScore(2) + GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForWeakpointDestruction);
            }

            RpcHitShipTargetPoint(aShipID, aTargetIndex, aBombInfoString);
        }

        [ClientRpc]
        void RpcHitShipTargetPoint(int aShipID, int aTargetIndex, string aBombInfoString)
        {
            ShipController.ShipTarget shipTarget = null;
            GameObject ship = null;
            ShipController.ShipTarget aShipTarget = new ShipController.ShipTarget(aShipID, aTargetIndex);
            BombController.BombInfo aBombInfo = BombController.BombInfo.GetBombInfo(aBombInfoString);
            for (int i = 0; i < m_gMan.m_spawnedShips.Count; i++)
            {
                if (m_gMan.m_spawnedShips[i].ContainsShipTarget(aShipTarget))
                {
                    shipTarget = m_gMan.m_spawnedShips[i].GetShipTarget(aShipTarget);
                    ship = m_gMan.m_spawnedShips[i].gameObject;
                    break;
                }
            }

            if (aBombInfo.m_shooter == BombersNetworkManager.m_localPlayer.m_playerID)
            {
                m_gMan.m_bombsHit++;
                BombersNetworkManager.m_localPlayer.m_score += GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_pointsForWeakpointDestruction;
            }

            Plane[] frustrum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(frustrum, ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Collider>().bounds))
            {
                BombersNetworkManager.m_localPlayer.ShakeCamera(GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_weakpointIntensity, GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_shakeTime);
            }

            if (shipTarget == null) return;
            GameObject explosion = (GameObject)Instantiate((GameObject)Resources.Load("WeakpointExplosion"), shipTarget.m_position.position, shipTarget.m_position.rotation);
            explosion.transform.parent = ship.transform;
            explosion.GetComponent<AudioSource>().Play();
            foreach (Transform child in shipTarget.gameObject.transform)
            {
                Destroy(child.gameObject);
            }

        }

        public void DeleteBombCommand(string aJson, int aID)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            CmdDeleteBomb(aJson, aID);
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
            if (m_gMan.m_spawnedBombs.Contains(aBombInfo))
            {
                int index = m_gMan.m_spawnedBombs.IndexOf(aBombInfo);
                GameObject bomb = m_gMan.m_spawnedBombs[index].gameObject;
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
                    }
                }
                Destroy(bomb);
                m_gMan.m_spawnedBombs.Remove(aBombInfo);
            }
        }

        public void DeleteBulletCommand(string aJson)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            CmdDeleteBullet(aJson);
        }

        [Command(channel = 1)]
        public void CmdDeleteBullet(string aBulletInfo)
        {
            RpcDeleteBullet(aBulletInfo);
        }

        [ClientRpc(channel = 1)]
        void RpcDeleteBullet(string aBulletInfoString)
        {
            BulletController.BulletInfo aBulletInfo = BulletController.BulletInfo.GetBulletInfo(aBulletInfoString);
            if (m_gMan.m_spawnedBullets.Contains(aBulletInfo))
            {
                int index = m_gMan.m_spawnedBullets.IndexOf(aBulletInfo);
                GameObject bullet = m_gMan.m_spawnedBullets[index].gameObject;
                Destroy(bullet);
                m_gMan.m_spawnedBullets.Remove(aBulletInfo);
            }
        }

        public void BulletHitPlayerCommand(string aJson, Vector3 aHitPoint, int aID)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            CmdBulletHitPlayer(aJson, aHitPoint, aID);
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
                    Instantiate((GameObject)Resources.Load("BulletHit"), plane.transform.position + aHitPoint, Quaternion.LookRotation(aBulletInfo.m_startDirection, -Vector3.forward));
                    break;
                }
            }

            if (aHitPlayer == BombersNetworkManager.m_localPlayer.m_playerID)
            {
                BombersNetworkManager.m_localPlayer.TakeBulletDamage(aShooter);
            }
        }

        public void FireBulletCommand(string aJson)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }

            CmdSpawnBullet(aJson);

        }

        [Command(channel = 1)]
        void CmdSpawnBullet(string aBulletInfo)
        {
            m_gMan.m_shotsFired++;
            int id = Random.Range(-20000000, 20000000) * 100 + m_playerID;
            BulletController.BulletInfo bulletInfo = BulletController.BulletInfo.GetBulletInfo(aBulletInfo);
            bulletInfo.m_bulletID = id;
            RpcSpawnBullet(bulletInfo.GetJson());
        }

        [ClientRpc(channel = 1)]
        void RpcSpawnBullet(string aBulletInfoString)
        {
            BulletController.BulletInfo aBulletInfo = BulletController.BulletInfo.GetBulletInfo(aBulletInfoString);
            if (BombersNetworkManager.m_localPlayer.m_playerID == aBulletInfo.m_shooter)
            {
                aBulletInfo.m_isMaster = true;
            }

            GameObject bullet = BombersNetworkManager.m_localPlayer.GetComponent<WeaponController>().SpawnBullet(aBulletInfo);
            m_gMan.m_spawnedBullets.Add(bullet.GetComponent<BulletController>().GetBulletInfo());
            int playerTeam = BombersPlayerController.GetPlayer(aBulletInfo.m_shooter).m_team;

            if (BombersNetworkManager.m_localPlayer.m_playerID != aBulletInfo.m_shooter)
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

        public void FireFlareCommand(Vector3 aPosition, Vector3 aVelocity)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            CmdSpawnFlare(aPosition, aVelocity, BombersNetworkManager.m_localPlayer.m_playerID);
        }

        [Command(channel = 1)]
        public void CmdSpawnFlare(Vector3 aPosition, Vector3 aVelocity, int aPlayerID)
        {
            RpcSpawnFlare(aPosition, aVelocity, aPlayerID);
        }

        [ClientRpc(channel = 1)]
        void RpcSpawnFlare(Vector3 aPosition, Vector3 aVelocity, int aPlayer)
        {
            GameObject flare = (GameObject)Instantiate((GameObject)Resources.Load("Flare"), aPosition, Quaternion.identity);
            flare.GetComponent<FlareController>().Activate(aPlayer);
            flare.GetComponent<Rigidbody>().velocity = aVelocity;
        }

        public void SpawnBombCommand(string aJson)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            CmdSpawnBomb(aJson);
        }

        [Command]
        public void CmdSpawnBomb(string aBombInfo)
        {
            m_gMan.m_bombsDropped++;
            int id = m_gMan.GetNextBombID();
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

            GameObject bomb = BombersNetworkManager.m_localPlayer.GetComponent<WeaponController>().SpawnBomb(aBombInfo);
            m_gMan.m_spawnedBombs.Add(bomb.GetComponent<BombController>().GetBombInfo());
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

        public void TakeBulletDamage(int aShooter)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            if (m_health == 0) return;
            m_health--;
            m_playerPlane.m_health = m_health;
            if (m_health <= 0)
            {
                m_health = 0;
                Vector3 position = m_playerPlane.transform.position;
                int bombs = GetComponent<WeaponController>().GetBombs();
                CmdDestroyPlayerPlane(m_playerID, aShooter);
                CmdSpawnBombPickup(position, NetworkManager.singleton.client.connection.connectionId);
                for (int i = 0; i < bombs; i++)
                {
                    CmdSpawnBombPickup(position, NetworkManager.singleton.client.connection.connectionId);
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
            GameObject bombPickup = (GameObject)Instantiate((GameObject)Resources.Load("BombPickup"), aPosition, Quaternion.identity);
            bombPickup.GetComponent<BombPickup>().Activate(bombID);
            m_gMan.m_bombPickupsSpawned.Add(bombPickup.GetComponent<BombPickup>());
        }

        [Command]
        public void CmdDestroyPlayerPlane(int aVictim, int aShooterID)
        {
            m_respawning = true;
            RpcDestroyPlayerPlane(aVictim, aShooterID);
        }

        [ClientRpc]
        void RpcDestroyPlayerPlane(int aVictim, int aShooter)
        {
            
            foreach (GameObject plane in GameObject.FindGameObjectsWithTag("PlayerController"))
            {
                if (plane.GetComponent<PlaneController>().m_playerID == aVictim)
                {
                    GameObject explosion = (GameObject)Instantiate((GameObject)Resources.Load("PlayerExplosion"), plane.transform.position, plane.transform.rotation);
                    explosion.GetComponent<AudioSource>().Play();
                    break;
                }
            }

            if (aShooter == -1)
            {
                if (aVictim == BombersNetworkManager.m_localPlayer.m_playerID)
                {
                    m_respawning = true;
                    BombersNetworkManager.m_localPlayer.DestroyPlayerPlane();
                    BombersNetworkManager.m_localPlayer.m_deaths += 1;
                    CmdStartRespawn(m_playerID);
                }
            }
            else
            {

                if (aVictim == BombersNetworkManager.m_localPlayer.m_playerID)
                {
                    m_respawning = true;
                    BombersNetworkManager.m_localPlayer.DestroyPlayerPlane();
                    BombersNetworkManager.m_localPlayer.m_deaths += 1;
                    CmdStartRespawn(m_playerID);
                }
                else if (aShooter == BombersNetworkManager.m_localPlayer.m_playerID)
                {
                    BombersNetworkManager.m_localPlayer.m_kills += 1;
                }
            }
        }

        public void CmdStartRespawn(int aPlayerID)
        {
            StopCoroutine(m_gMan.RespawnPlayer(aPlayerID));
            StartCoroutine(m_gMan.RespawnPlayer(aPlayerID));
        }

        public void EnteredBounds()
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            if (m_leftBounds)
            {
                m_leftBounds = false;
                m_leftBoundsTimer = 4;
            }
        }

        public void LeftBounds()
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }

            m_leftBounds = true;
        }

        [Command(channel = 1)]
        public void CmdSpawnMyself()
        {
            RpcSpawnPlayers();
        }

        [Server]
        public void SpawnPlayers()
        {
            RpcSpawnPlayers();
            return;
        }

        public void SpawnPlayerLocal(int aPlayerID)
        {
            RpcSpawnPlayerLocal();
        }

        [Server]
        public void SpawnPlayer(int aPlayerID)
        {
            RpcSpawnPlayer(aPlayerID);
        }

        [ClientRpc(channel = 1)]
        public void RpcSpawnPlayer(int aPlayerID)
        {
            Vector3 spawnPoint = Vector3.zero;
            spawnPoint.z = 22;

            if (BombersNetworkManager.m_localPlayer.m_team == 1)
            {
                spawnPoint.x = Random.Range(m_gMan.m_team1SpawnBounds.bounds.center.x - m_gMan.m_team1SpawnBounds.bounds.size.x / 2, m_gMan.m_team1SpawnBounds.bounds.center.x + m_gMan.m_team1SpawnBounds.bounds.size.x / 2) - 10;
                spawnPoint.y = Random.Range(m_gMan.m_team1SpawnBounds.bounds.center.y - m_gMan.m_team1SpawnBounds.bounds.size.y / 2, m_gMan.m_team1SpawnBounds.bounds.center.y + m_gMan.m_team1SpawnBounds.bounds.size.y / 2);
            }
            else if (BombersNetworkManager.m_localPlayer.m_team == 2)
            {
                spawnPoint.x = Random.Range(m_gMan.m_team2SpawnBounds.bounds.center.x - m_gMan.m_team2SpawnBounds.bounds.size.x / 2, m_gMan.m_team2SpawnBounds.bounds.center.x + m_gMan.m_team2SpawnBounds.bounds.size.x / 2) + 10;
                spawnPoint.y = Random.Range(m_gMan.m_team2SpawnBounds.bounds.center.y - m_gMan.m_team2SpawnBounds.bounds.size.y / 2, m_gMan.m_team2SpawnBounds.bounds.center.y + m_gMan.m_team2SpawnBounds.bounds.size.y / 2);
            }

            m_planeActive = true;
            ActivatePlane();
            m_playerPlane.transform.position = spawnPoint;
            m_playerPlane.transform.rotation = Quaternion.LookRotation(Vector3.forward, (new Vector3(0, 0, 22) - spawnPoint));
            if (m_team == 1)
            {
                m_playerPlane.gameObject.layer = 8;
            }
            else if (m_team == 2)
            {
                m_playerPlane.gameObject.layer = 9;
            }
            SetPlayerPlane(m_playerPlane.GetComponent<PlaneController>());
            m_playerPlane.GetComponent<Rigidbody>().isKinematic = false;
            m_gMan.m_gameState = GameManager.eGameState.GAME_STATE_PLAYING_GAME;
            return;
        }

        public void RpcSpawnPlayerLocal()
        {
            Vector3 spawnPoint = Vector3.zero;
            spawnPoint.z = 22;

            if (BombersNetworkManager.m_localPlayer.m_team == 1)
            {
                spawnPoint.x = Random.Range(m_gMan.m_team1SpawnBounds.bounds.center.x - m_gMan.m_team1SpawnBounds.bounds.size.x / 2, m_gMan.m_team1SpawnBounds.bounds.center.x + m_gMan.m_team1SpawnBounds.bounds.size.x / 2) - 10;
                spawnPoint.y = Random.Range(m_gMan.m_team1SpawnBounds.bounds.center.y - m_gMan.m_team1SpawnBounds.bounds.size.y / 2, m_gMan.m_team1SpawnBounds.bounds.center.y + m_gMan.m_team1SpawnBounds.bounds.size.y / 2);
            }
            else if (BombersNetworkManager.m_localPlayer.m_team == 2)
            {
                spawnPoint.x = Random.Range(m_gMan.m_team2SpawnBounds.bounds.center.x - m_gMan.m_team2SpawnBounds.bounds.size.x / 2, m_gMan.m_team2SpawnBounds.bounds.center.x + m_gMan.m_team2SpawnBounds.bounds.size.x / 2) + 10;
                spawnPoint.y = Random.Range(m_gMan.m_team2SpawnBounds.bounds.center.y - m_gMan.m_team2SpawnBounds.bounds.size.y / 2, m_gMan.m_team2SpawnBounds.bounds.center.y + m_gMan.m_team2SpawnBounds.bounds.size.y / 2);
            }

            m_planeActive = true;
            ActivatePlane();
            m_playerPlane.transform.position = spawnPoint;
            m_playerPlane.transform.rotation = Quaternion.LookRotation(Vector3.forward, (new Vector3(0, 0, 22) - spawnPoint));
            if (m_team == 1)
            {
                m_playerPlane.gameObject.layer = 8;
            }
            else if (m_team == 2)
            {
                m_playerPlane.gameObject.layer = 9;
            }
            SetPlayerPlane(m_playerPlane.GetComponent<PlaneController>());
            m_playerPlane.GetComponent<Rigidbody>().isKinematic = false;
            m_gMan.m_gameState = GameManager.eGameState.GAME_STATE_PLAYING_GAME;
            return;
        }

        [ClientRpc(channel = 1)]
        public void RpcSpawnPlayers()
        {
            Vector3 spawnPoint = Vector3.zero;
            spawnPoint.z = 22;

            if (BombersNetworkManager.m_localPlayer.m_team == 1)
            {
                spawnPoint.x = Random.Range(m_gMan.m_team1SpawnBounds.bounds.center.x - m_gMan.m_team1SpawnBounds.bounds.size.x / 2, m_gMan.m_team1SpawnBounds.bounds.center.x + m_gMan.m_team1SpawnBounds.bounds.size.x / 2) - 10;
                spawnPoint.y = Random.Range(m_gMan.m_team1SpawnBounds.bounds.center.y - m_gMan.m_team1SpawnBounds.bounds.size.y / 2, m_gMan.m_team1SpawnBounds.bounds.center.y + m_gMan.m_team1SpawnBounds.bounds.size.y / 2);
            }
            else if (BombersNetworkManager.m_localPlayer.m_team == 2)
            {
                spawnPoint.x = Random.Range(m_gMan.m_team2SpawnBounds.bounds.center.x - m_gMan.m_team2SpawnBounds.bounds.size.x / 2, m_gMan.m_team2SpawnBounds.bounds.center.x + m_gMan.m_team2SpawnBounds.bounds.size.x / 2) + 10;
                spawnPoint.y = Random.Range(m_gMan.m_team2SpawnBounds.bounds.center.y - m_gMan.m_team2SpawnBounds.bounds.size.y / 2, m_gMan.m_team2SpawnBounds.bounds.center.y + m_gMan.m_team2SpawnBounds.bounds.size.y / 2);
            }

            m_planeActive = true;
            ActivatePlane();
            m_playerPlane.transform.position = spawnPoint;
            m_playerPlane.transform.rotation = Quaternion.LookRotation(Vector3.forward, (new Vector3(0, 0, 22) - spawnPoint));
            if (m_team == 1)
            {
                m_playerPlane.gameObject.layer = 8;
            }
            else if (m_team == 2)
            {
                m_playerPlane.gameObject.layer = 9;
            }
            SetPlayerPlane(m_playerPlane.GetComponent<PlaneController>());
            m_playerPlane.GetComponent<Rigidbody>().isKinematic = false;
            m_gMan.m_gameState = GameManager.eGameState.GAME_STATE_PLAYING_GAME;
            return;
        }

        public void AnnounceJoinCommand()
        {
            CmdAnnounceJoin(m_playerID, m_displayName, m_team);
        }

        [Command(channel = 1)]
        void CmdAnnounceJoin(int aPlayerID, string aPlayerName, int aTeam)
        {
            SpawnPlayers();
            foreach (GameObject plane in GameObject.FindGameObjectsWithTag("PlayerController"))
            {
                plane.GetComponent<BombersPlayerController>().RpcAnnounceJoin(aPlayerID, aPlayerName, aTeam);
            }
        }

        [ClientRpc(channel = 1)]
        void RpcAnnounceJoin(int aPlayerID, string aPlayerName, int aTeam)
        {
            string message = aPlayerName + " has joined the fight\n on the ";
            message += (aTeam == 1) ? "green team!" : "red team!";
            GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog(message, true);
        }
    }
}