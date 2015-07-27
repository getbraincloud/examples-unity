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

        private PlaneController m_playerPlane;

        private float m_turnSpeed = 1;
        private float m_acceleration = 1;

        private float m_currentRotation = 0;

        private bool m_isActive = false;

        private int m_baseHealth = 3;

        private int m_health = 0;
        private float m_speedMultiplier = 1;

        private float m_maxSpeedMultiplier = 2.5f;
        private float m_leftBoundsTimer = 0;
        private bool m_leftBounds = false;

        private Vector3 m_originalCamPosition = Vector3.zero;
        private float m_shakeDecay = 0;
        private float m_shakeIntensity = 0;
        public GameObject m_missionText;

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

            

            //if (BombersNetworkManager.m_localPlayer != null)
            //{
                StartCoroutine("UpdateVarsClient");
                CmdUpdateSyncVars();
                Debug.Log("hit");
                BombersNetworkManager.m_localPlayer = this;
                m_displayName = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_playerName;
                m_playerID = (int)netId.Value;
            //}

            m_missionText = GameObject.Find("GameManager").GetComponent<GameManager>().m_missionText;
            m_turnSpeed = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_planeTurnSpeed;
            m_acceleration = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_planeAcceleration;
            m_baseHealth = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_basePlaneHealth;
            m_maxSpeedMultiplier = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_maxPlaneSpeedMultiplier;
        }

        [Command]
        void CmdUpdateSyncVars()
        {
            StartCoroutine("UpdateVars");
        }

        [Command]
        void CmdUpdateSyncVarsFromClient(int aScore, int aPlayerID, int aTeam, int aKills, int aDeaths, int aPing, string aDisplayName)
        {
            m_score = aScore;
            m_playerID = aPlayerID;
            m_team = aTeam;
            m_kills = aKills;
            m_deaths = aDeaths;
            m_ping = aPing;
            m_displayName = aDisplayName;
        }

        IEnumerator UpdateVars()
        {
            while (true)
            {
                SetDirtyBit(syncVarDirtyBits);
                yield return new WaitForSeconds(GetNetworkSendInterval());
            }
        }

        IEnumerator UpdateVarsClient()
        {
            if (isServer) yield break;

            while (true)
            {
                CmdUpdateSyncVarsFromClient(m_score, m_playerID, m_team, m_kills, m_deaths, m_ping, m_displayName);
                yield return new WaitForSeconds(GetNetworkSendInterval());
            }
        }

        public void SetPlayerPlane(PlaneController playerPlane)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            m_leftBoundsTimer = 4;
            GameObject.Find("MapBounds").GetComponent<MapBoundsCheck>().m_playerPlane = playerPlane.gameObject;
            StartCoroutine("PulseMissionText");
            m_leftBounds = false;
            m_currentRotation = playerPlane.gameObject.transform.rotation.eulerAngles.z;
            m_isActive = true;
            m_playerPlane = playerPlane;
            GetComponent<WeaponController>().SetPlayerPlane(playerPlane);
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

        void Update()
        {
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
            if (m_playerPlane == null)
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
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            if (m_playerPlane == null)
            {
                return;
            }

            if (!m_isActive) return;

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
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdDestroyPlayerPlane(m_playerID, -1);
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdSpawnBombPickup(position, NetworkManager.singleton.client.connection.connectionId);
            for (int i = 0; i < bombs; i++)
            {

                GameObject.Find("GameManager").GetComponent<GameManager>().CmdSpawnBombPickup(position, NetworkManager.singleton.client.connection.connectionId);
            }
            //DestroyPlayerPlane();
        }


        public void DestroyPlayerPlane()
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            if (m_playerPlane == null)
            {
                return;
            }
            GameObject.Find("MapBounds").GetComponent<MapBoundsCheck>().m_playerPlane = null;
            m_leftBounds = false;
            m_isActive = false;
            //PhotonNetwork.Destroy(m_playerPlane.gameObject);
            NetworkManager.Destroy(m_playerPlane.gameObject);
            m_playerPlane = null;
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
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdDespawnBombPickup(aPickupID);
        }

        public void BombPickedUpCommand(int aPlayerID, int aPickupID)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdBombPickedUp(aPlayerID, aPickupID);
        }

        public void DestroyShipCommand(int shipID, string aJson)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdDestroyedShip(shipID, aJson);
        }

        public void HitShipTargetPointCommand(int aID, int aIndex, string aJson)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdHitShipTargetPoint(aID, aIndex, aJson);
        }

        public void DeleteBombCommand(string aJson, int aID)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdDeleteBomb(aJson, aID);
        }

        public void DeleteBulletCommand(string aJson)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdDeleteBullet(aJson);
        }

        
        public void BulletHitPlayerCommand(string aJson, Vector3 aHitPoint, int aID)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdBulletHitPlayer(aJson, aHitPoint, aID);
        }

        
        public void FireBulletCommand(string aJson)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdSpawnBullet(aJson);
        }

        public void FireFlareCommand(Vector3 aPosition, Vector3 aVelocity)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdSpawnFlare(aPosition, aVelocity, BombersNetworkManager.m_localPlayer.m_playerID);
        }

        public void SpawnBombCommand(string aJson)
        {
            if (!isLocalPlayer || !hasAuthority)
            {
                return;
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdSpawnBomb(aJson);
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
                GameObject.Find("GameManager").GetComponent<GameManager>().CmdDestroyPlayerPlane(m_playerID, aShooter);
                GameObject.Find("GameManager").GetComponent<GameManager>().CmdSpawnBombPickup(position, NetworkManager.singleton.client.connection.connectionId);
                for (int i = 0; i < bombs; i++)
                {

                    GameObject.Find("GameManager").GetComponent<GameManager>().CmdSpawnBombPickup(position, NetworkManager.singleton.client.connection.connectionId);
                }
            }
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
            m_leftBounds = true;
        }
    }
}