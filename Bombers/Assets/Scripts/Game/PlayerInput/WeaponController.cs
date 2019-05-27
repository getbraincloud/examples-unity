using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using BrainCloudUNETExample.Connection;
using Gameframework;

namespace BrainCloudUNETExample.Game
{
    public class WeaponController : BaseBehaviour
    {
        private PlaneController m_playerPlane;

        public Transform m_bulletSpawnPoint;

        private float m_lastShot = 0.0f;
        //private float m_lastFlare = 0.0f;

        private GameObject m_bullet1Prefab;
        private GameObject m_bullet2Prefab;

        private GameObject m_bombPrefab1;
        private GameObject m_bombPrefab2;

        public GameObject m_muzzleFlarePrefab;

        private GameObject m_bombDropPrefab;

        private int m_bombs = 0;

        private GameObject m_targetingReticule;

        public float m_bulletSpeed = 100f;
        private Vector3 m_bulletVelocity = Vector3.zero;

        private float m_aloneBombTimer = 0;

        void Start()
        {
            m_targetingReticule = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "TargetReticule"), Vector3.zero, Quaternion.identity);
            m_bulletSpeed = GConfigManager.GetFloatValue("BulletSpeed");
            if (m_bullet1Prefab == null)
            {
                m_bullet1Prefab = (GameObject)Resources.Load("Prefabs/Game/" + "Bullet01");
            }
            if (m_bullet2Prefab == null)
            {
                m_bullet2Prefab = (GameObject)Resources.Load("Prefabs/Game/" + "Bullet02");
            }

            if (m_bombPrefab1 == null)
            {
                m_bombPrefab1 = (GameObject)Resources.Load("Prefabs/Game/" + "Bomb01");
                m_bombPrefab2 = (GameObject)Resources.Load("Prefabs/Game/" + "Bomb02");
            }

            if (m_muzzleFlarePrefab == null)
            {
                m_muzzleFlarePrefab = (GameObject)Resources.Load("Prefabs/Game/" + "MuzzleFlare");
            }

            if (m_bombDropPrefab == null)
            {
                m_bombDropPrefab = (GameObject)Resources.Load("Prefabs/Game/" + "BombDrop");
            }

			m_targetingReticule.transform.Find("TargetSprite").GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        }

        public bool HasBombs()
        {
            return (m_bombs > 0);
        }

        public int GetBombs()
        {
            return m_bombs;
        }

        void Update()
        {
            if (m_playerPlane != null)
                m_aloneBombTimer += Time.deltaTime;
        }

        void LateUpdate()
        {
            bool isAlone = true;
            BombersPlayerController tempController;
            BombersPlayerController[] playerList;
            List<BombersPlayerController> playerListList = new List<BombersPlayerController>();
            foreach (LobbyMemberInfo member in BombersNetworkManager.LobbyInfo.Members)
            {
                tempController = member.PlayerController;
                playerListList.Add(member.PlayerController);
            }

            int count = 0;
            while (count < playerListList.Count)
            {
                if (playerListList[count] != null && 
                        playerListList[count].m_team == 0)
                {
                    playerListList.RemoveAt(count);
                }
                else
                {
                    count++;
                }
            }
            playerList = playerListList.ToArray();
            {
                if (playerList.Length > 1)
                    isAlone = false;
            }
            if (isAlone && m_bombs == 0 && m_aloneBombTimer >= 3)
            {
                m_aloneBombTimer = 0;
                AddBomb();
            }

            if (m_bombs > 0 && m_playerPlane != null)
            {
				m_targetingReticule.transform.Find("TargetSprite").GetComponent<SpriteRenderer>().color = 
                                                            Color.Lerp(m_targetingReticule.transform.Find("TargetSprite").GetComponent<SpriteRenderer>().color, new Color(1, 1, 1, m_playerPlane.IsLocalPlayer ? 0.3f : 0.0f), 4 * Time.deltaTime);

                m_targetingReticule.GetComponent<MeshRenderer>().enabled = m_playerPlane.IsLocalPlayer;
                Vector3 position = m_playerPlane.transform.position;
                Vector3 planeVelocity = m_playerPlane.GetComponent<Rigidbody>().velocity;
                Vector3 velocity = planeVelocity;
                count = 1;
                Vector3 lastPos = m_playerPlane.transform.position;
                int layerMask = (1 << 16) | (1 << 17) | (1 << 4) | (1 << 20);
                bool hitFound = false;
                while (!hitFound)
                {
                    velocity += Physics.gravity * 0.01f;
                    velocity = velocity * 1 / (1 + 0.01f * 0.8f);
                    position += velocity * 0.01f;

                    if (position.z > 10 * count)
                    {
                        count++;
                        RaycastHit hit = new RaycastHit();
                        if (Physics.SphereCast(lastPos, 10.4f, position - lastPos, out hit, (position - lastPos).magnitude, layerMask))
                        {
                            hitFound = true;

                            position = lastPos + ((position - lastPos).normalized * ((hit.point.z - lastPos.z) / (position.z - lastPos.z)));
                            position.z += 5;
                            position.z -= 0.5f;
                        }

                        lastPos = position;
                    }
                    else if (position.y > 3000.0f || position.y < -3000.0f || position.z >= 121.5f)
                    {
                        hitFound = true;
                        position.z = 121.5f;
                    }
                }

                m_targetingReticule.transform.position = position;
                TextMesh bombCounter = m_targetingReticule.transform.Find("BombCounter").GetComponent<TextMesh>();
                int maxBombs = GConfigManager.GetIntValue("MaxBombCapacity");
                if (m_bombs == 0 || m_bombs == 1 || !m_playerPlane.IsLocalPlayer)
                {
                    bombCounter.text = "";
                }
                else if (m_bombs < maxBombs)
                {
                    bombCounter.color = new Color(1, 1, 1, 0.8f);
                    bombCounter.text = m_bombs.ToString();
                }
                else
                {
                    bombCounter.color = new Color(1, 0.4f, 0.4f, 0.8f);
                    bombCounter.text = m_bombs.ToString();
                }
            }
            else
            {
                m_targetingReticule.GetComponent<MeshRenderer>().enabled = false;
				m_targetingReticule.transform.Find("TargetSprite").GetComponent<SpriteRenderer>().color = Color.Lerp(m_targetingReticule.transform.Find("TargetSprite").GetComponent<SpriteRenderer>().color, new Color(1, 1, 1, 0), 4 * Time.deltaTime);
                m_targetingReticule.transform.Find("BombCounter").GetComponent<TextMesh>().text = "";
            }
        }

        public void AddBomb()
        {
            if (m_bombs < GConfigManager.GetIntValue("MaxBombCapacity"))
            {
                if (m_playerPlane != null)
                    GetComponent<AudioSource>().Play();
                m_bombs++;
            }
        }

        public void SetPlayerPlane(PlaneController aPlane)
        {
            m_bombs = 0;
            m_playerPlane = aPlane;
            m_bulletSpawnPoint = aPlane.m_bulletSpawnPoint;
        }

        public void DestroyPlayerPlane()
        {
            m_bombs = 0;
            m_playerPlane = null;
            m_bulletSpawnPoint = null;
        }

        public void DropBomb()
        {
            if (m_bombs > 0)
            {
                GetComponent<BombersPlayerController>().SpawnBombCommand(new BombInfo(m_playerPlane.transform.position, m_playerPlane.transform.up, GetComponent<BombersPlayerController>().NetId, m_playerPlane.GetComponent<Rigidbody>().velocity).GetJson());
            }
        }

        IEnumerator FireMultiShot()
        {
            for (int i = 0; i < GConfigManager.GetIntValue("MultishotAmount"); i++)
            {
                if (m_playerPlane == null) break;
                m_lastShot = Time.time;
                m_bulletSpawnPoint = m_playerPlane.m_bulletSpawnPoint;
                m_bulletVelocity = m_bulletSpawnPoint.forward.normalized;
                m_bulletVelocity *= m_bulletSpeed;
                m_bulletVelocity += m_playerPlane.GetComponent<Rigidbody>().velocity;
                GetComponent<BombersPlayerController>().FireBulletCommand(new BulletInfo(m_bulletSpawnPoint.position, m_bulletSpawnPoint.forward.normalized, GetComponent<BombersPlayerController>().NetId, m_bulletVelocity).GetJson());
                yield return new WaitForSeconds(GConfigManager.GetFloatValue("MultishotBurstDelay"));
            }
        }

        public void FireWeapon(bool aIsAccelerating)
        {
            float fireDelay = GConfigManager.GetFloatValue("FireRateDelay");

            if (aIsAccelerating)
                fireDelay = GConfigManager.GetFloatValue("FastModeFireRateDelay");

            if ((Time.time - m_lastShot) > GConfigManager.GetFloatValue("MultishotDelay"))
            {
                StartCoroutine("FireMultiShot");
            }
            else if ((Time.time - m_lastShot) > fireDelay)
            {
                m_lastShot = Time.time;
                m_bulletSpawnPoint = m_playerPlane.m_bulletSpawnPoint;
                m_bulletVelocity = m_bulletSpawnPoint.forward.normalized;
                m_bulletVelocity *= m_bulletSpeed;
                m_bulletVelocity += m_playerPlane.GetComponent<Rigidbody>().velocity;
                GetComponent<BombersPlayerController>().FireBulletCommand(new BulletInfo(m_bulletSpawnPoint.position, m_bulletSpawnPoint.forward.normalized, GetComponent<BombersPlayerController>().NetId, m_bulletVelocity).GetJson());
            }
        }
        /*
        public void FireFlare(Vector3 aPosition, Vector3 aVelocity)
        {
            float flareDelay = BrainCloudStats.Instance.m_flareCooldown;
            if ((Time.time - m_lastFlare) > flareDelay)
            {
                m_lastFlare = Time.time;
                GetComponent<BombersPlayerController>().FireFlareCommand(aPosition, aVelocity);
            }
        }
        */

        public GameObject SpawnBomb(BombInfo aBombInfo)
        {
            m_bombs--;
            GameObject player = null;
            PlaneController playerPlaneController = null;
            PlaneController tempPlaneController = null;
            foreach (LobbyMemberInfo member in BombersNetworkManager.LobbyInfo.Members)
            {
                tempPlaneController = member.PlayerController.m_playerPlane;
                if (tempPlaneController.NetId == aBombInfo.m_shooter)
                {
                    player = member.PlayerController.gameObject;
                    playerPlaneController = tempPlaneController;
                    break;
                }
            }
            if (playerPlaneController != null)
            {
                GameObject flare = (GameObject)Instantiate(m_bombDropPrefab, player.transform.position, playerPlaneController.m_bulletSpawnPoint.rotation);
                flare.transform.parent = player.transform;
                if (aBombInfo.m_shooter == GetComponent<BombersPlayerController>().NetId)
                {
                    flare.GetComponent<AudioSource>().spatialBlend = 0;
                }
                flare.GetComponent<AudioSource>().Play();
            }

            GameObject bomb = (GameObject)Instantiate((BombersPlayerController.GetPlayer(aBombInfo.m_shooter).m_team == 1) ? m_bombPrefab1 : m_bombPrefab2, aBombInfo.m_startPosition, Quaternion.LookRotation(aBombInfo.m_startDirection, -Vector3.forward));
            bomb.GetComponent<Rigidbody>().velocity = aBombInfo.m_startVelocity;
            bomb.GetComponent<BombController>().BombInfo = aBombInfo;
            return bomb;
        }

        public GameObject SpawnBullet(BulletInfo aBulletInfo)
        {
            GameObject player = null;
            PlaneController playerPlaneController = null;
            PlaneController tempPlaneController = null;
            BCLobbyInfo info = BombersNetworkManager.LobbyInfo;
            foreach (LobbyMemberInfo member in info.Members)
            {
                tempPlaneController = member.PlayerController.m_playerPlane;
                if (tempPlaneController.NetId == aBulletInfo.m_shooter)
                {
                    player = member.PlayerController.gameObject;
                    playerPlaneController = tempPlaneController;
                    break;
                }
            }

            if (playerPlaneController != null && playerPlaneController.PlayerController.m_planeActive)
            {
                playerPlaneController.ResetGunCharge();
                GameObject flare = (GameObject)Instantiate(m_muzzleFlarePrefab, aBulletInfo.m_startPosition, playerPlaneController.m_bulletSpawnPoint.rotation);
                flare.transform.parent = player.transform;
                flare.GetComponent<AudioSource>().pitch = 1 + Random.Range(-2.0f, 3.0f) * 0.2f;
                if (aBulletInfo.m_shooter == playerPlaneController.NetId)
                {
                    flare.GetComponent<AudioSource>().spatialBlend = 0;
                }
            }
            int team = player.GetComponent<BombersPlayerController>().m_team;
            GameObject bullet = (GameObject)Instantiate((team == 1) ? m_bullet1Prefab : m_bullet2Prefab, aBulletInfo.m_startPosition, Quaternion.LookRotation(aBulletInfo.m_startDirection, -Vector3.forward));
            bullet.GetComponent<Rigidbody>().velocity = aBulletInfo.m_startVelocity;
            bullet.GetComponent<BulletController>().SetBulletInfo(aBulletInfo);

            return bullet;
        }
    }

}