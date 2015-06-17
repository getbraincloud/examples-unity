using UnityEngine;
using System.Collections;
using BrainCloudPhotonExample.Connection;

namespace BrainCloudPhotonExample.Game.PlayerInput
{
    public class WeaponController : MonoBehaviour
    {
        //private float m_fireDelay = 0.3f;

        private GameObject m_playerPlane;

        public Transform m_bulletSpawnPoint;

        private float m_lastShot = 0.0f;
        private float m_lastFlare = 0.0f;

        private GameObject m_bullet1Prefab;
        private GameObject m_bullet2Prefab;

        private GameObject m_bombPrefab;

        private GameObject m_muzzleFlarePrefab;

        private GameObject m_bombDropPrefab;

        private int m_bombs = 0;

        //private GUISkin m_skin;
        private GameObject m_targetingReticule;
        private GameObject m_offscreenIndicator;

        private float m_bulletSpeed = 100f;
        private Vector3 m_bulletVelocity = Vector3.zero;

        void Start()
        {
            m_targetingReticule = (GameObject)Instantiate((GameObject)Resources.Load("TargetReticule"), Vector3.zero, Quaternion.identity);
            m_offscreenIndicator = (GameObject)Instantiate((GameObject)Resources.Load("OffscreenIndicator"), Vector3.zero, Quaternion.identity);
            m_bulletSpeed = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_bulletSpeed;
            if (m_bullet1Prefab == null)
            {
                m_bullet1Prefab = (GameObject)Resources.Load("Bullet01");
            }
            if (m_bullet2Prefab == null)
            {
                m_bullet2Prefab = (GameObject)Resources.Load("Bullet02");
            }

            if (m_bombPrefab == null)
            {
                m_bombPrefab = (GameObject)Resources.Load("Bomb");
            }

            if (m_muzzleFlarePrefab == null)
            {
                m_muzzleFlarePrefab = (GameObject)Resources.Load("MuzzleFlare");
            }

            if (m_bombDropPrefab == null)
            {
                m_bombDropPrefab = (GameObject)Resources.Load("BombDrop");
            }


            m_targetingReticule.transform.FindChild("WeakPointSprite").GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
            m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.r, m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.g, m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.b, 0);
        }

        public bool HasBombs()
        {
            return (m_bombs > 0);
        }

        public int GetBombs()
        {
            return m_bombs;
        }

        void LateUpdate()
        {
            bool isAlone = true;

            if (PhotonNetwork.room != null && PhotonNetwork.room.playerCount > 1)
            {
                PhotonPlayer[] players = PhotonNetwork.playerList;

                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].customProperties["Team"] == null || (int)players[i].customProperties["Team"] == 3 || (int)players[i].customProperties["Team"] == 0 || players[i] == PhotonNetwork.player || (int)players[i].customProperties["Team"] == (int)PhotonNetwork.player.customProperties["Team"])
                    {
                        continue;
                    }
                    else
                    {
                        isAlone = false;
                    }
                }
            }
            if (isAlone && m_bombs < GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_maxBombCapacity)
            {
                AddBomb();
            }

            if (m_bombs > 0 && m_playerPlane != null)
            {
                m_targetingReticule.transform.FindChild("WeakPointSprite").GetComponent<SpriteRenderer>().color = Color.Lerp(m_targetingReticule.transform.FindChild("WeakPointSprite").GetComponent<SpriteRenderer>().color, new Color(1, 1, 1, 0.3f), 4 * Time.deltaTime);

                m_targetingReticule.GetComponent<MeshRenderer>().enabled = true;
                Vector3 position = m_playerPlane.transform.position;
                Vector3 planeVelocity = m_playerPlane.GetComponent<Rigidbody>().velocity;
                Vector3 velocity = planeVelocity;
                int count = 1;
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
                    else if (position.z >= 121.5f)
                    {
                        hitFound = true;
                        position.z = 121.5f;
                    }

                }

                m_targetingReticule.transform.position = position;
                TextMesh bombCounter = m_targetingReticule.transform.FindChild("BombCounter").GetComponent<TextMesh>();
                int maxBombs = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_maxBombCapacity;
                if (m_bombs == 0 || m_bombs == 1)
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

                m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.r, m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.g, m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.b, 1);
                //m_offscreenIndicator.transform.position = Camera.main.transform.position;
                //Vector3 indicatorPosition = new Vector3(m_offscreenIndicator.transform.position.x, m_offscreenIndicator.transform.position.y, m_offscreenIndicator.transform.position.z + 10);
                //float height = 2 * 10 * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
                //float width = height * Camera.main.aspect;
                //Bounds bounds = new Bounds(new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 22), new Vector3(width, height, 0));
                GameObject ship = GameObject.Find("GameManager").GetComponent<GameManager>().GetClosestEnemyShip(m_playerPlane.transform.position, (int)PhotonNetwork.player.customProperties["Team"]);

                if (ship != null)
                {
                    Plane[] frustrum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
                    if (!GeometryUtility.TestPlanesAABB(frustrum, ship.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Collider>().bounds))
                    {
                        m_offscreenIndicator.transform.position = ship.transform.position;
                        position = m_offscreenIndicator.transform.position;
                        Vector3 point = Camera.main.WorldToScreenPoint(position);
                        if (point.x > Screen.width - 10) point.x = Screen.width - 10;
                        if (point.x < 0 + 10) point.x = 0 + 10;
                        if (point.y > Screen.height - 10) point.y = Screen.height - 10;
                        if (point.y < 0 + 10) point.y = 0 + 10;
                        point.z = 10;
                        point = Camera.main.ScreenToWorldPoint(point);
                        m_offscreenIndicator.transform.position = point;
                        point -= Camera.main.transform.position;
                        m_offscreenIndicator.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg - 90);
                    }
                    else
                    {
                        m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.r, m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.g, m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.b, 0);
                    }
                }
                else
                {
                    m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.r, m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.g, m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.b, 0);
                }

            }
            else
            {
                m_targetingReticule.GetComponent<MeshRenderer>().enabled = false;
                m_targetingReticule.transform.FindChild("WeakPointSprite").GetComponent<SpriteRenderer>().color = Color.Lerp(m_targetingReticule.transform.FindChild("WeakPointSprite").GetComponent<SpriteRenderer>().color, new Color(1, 1, 1, 0), 4 * Time.deltaTime);
                m_targetingReticule.transform.FindChild("BombCounter").GetComponent<TextMesh>().text = "";
                m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.r, m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.g, m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color.b, 0);
            }
        }

        public void AddBomb()
        {
            if (m_bombs < GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_maxBombCapacity)
            {
                m_bombs++;
            }
        }

        public void SetPlayerPlane(PlaneController aPlane)
        {
            m_bombs = 0;
            m_playerPlane = aPlane.gameObject;
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
                m_bombs--;
                GameObject.Find("GameManager").GetComponent<GameManager>().SpawnBomb(new BombController.BombInfo(m_playerPlane.transform.position, m_playerPlane.transform.up, PhotonNetwork.player, m_playerPlane.GetComponent<Rigidbody>().velocity));
            }
        }

        IEnumerator FireMultiShot()
        {
            for (int i = 0; i < GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_multiShotAmount; i++)
            {
                if (m_playerPlane == null) break;
                m_lastShot = Time.time;
                m_bulletSpawnPoint = m_playerPlane.GetComponent<PlaneController>().m_bulletSpawnPoint;
                m_bulletVelocity = m_bulletSpawnPoint.forward.normalized;
                m_bulletVelocity *= m_bulletSpeed;
                m_bulletVelocity += m_playerPlane.GetComponent<Rigidbody>().velocity;
                GameObject.Find("GameManager").GetComponent<GameManager>().SpawnBullet(new BulletController.BulletInfo(m_bulletSpawnPoint.position, m_bulletSpawnPoint.forward.normalized, PhotonNetwork.player, m_bulletVelocity));
                yield return new WaitForSeconds(GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_multiShotBurstDelay);
            }
        }

        public void FireWeapon(bool aIsAccelerating)
        {
            float fireDelay = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_fireRateDelay;

            if (aIsAccelerating)
                fireDelay = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_fastModeFireRateDelay;

            if ((Time.time - m_lastShot) > GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_multiShotDelay)
            {
                StartCoroutine("FireMultiShot");
            }
            else if ((Time.time - m_lastShot) > fireDelay)
            {
                m_lastShot = Time.time;
                m_bulletSpawnPoint = m_playerPlane.GetComponent<PlaneController>().m_bulletSpawnPoint;
                m_bulletVelocity = m_bulletSpawnPoint.forward.normalized;
                m_bulletVelocity *= m_bulletSpeed;
                m_bulletVelocity += m_playerPlane.GetComponent<Rigidbody>().velocity;
                GameObject.Find("GameManager").GetComponent<GameManager>().SpawnBullet(new BulletController.BulletInfo(m_bulletSpawnPoint.position, m_bulletSpawnPoint.forward.normalized, PhotonNetwork.player, m_bulletVelocity));
            }
        }

        public void FireFlare(Vector3 aPosition, Vector3 aVelocity)
        {
            float flareDelay = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_flareCooldown;
            if ((Time.time - m_lastFlare) > flareDelay)
            {
                m_lastFlare = Time.time;
                GameObject.Find("GameManager").GetComponent<GameManager>().SpawnFlare(aPosition, aVelocity);
            }
        }

        public GameObject SpawnBomb(BombController.BombInfo aBombInfo)
        {
            GameObject player = null;

            GameObject[] planes = GameObject.FindGameObjectsWithTag("Plane");
            for (int i = 0; i < planes.Length; i++)
            {
                if (planes[i].GetComponent<PhotonView>().owner == aBombInfo.m_shooter)
                {
                    player = planes[i];
                    break;
                }
            }

            if (player != null)
            {
                GameObject flare = (GameObject)Instantiate(m_bombDropPrefab, player.transform.position, player.GetComponent<PlaneController>().m_bulletSpawnPoint.rotation);
                flare.transform.parent = player.transform;
                if (aBombInfo.m_shooter == PhotonNetwork.player)
                {
                    flare.GetComponent<AudioSource>().spatialBlend = 0;
                }
                flare.GetComponent<AudioSource>().Play();
            }

            GameObject bomb = (GameObject)Instantiate(m_bombPrefab, aBombInfo.m_startPosition, Quaternion.LookRotation(aBombInfo.m_startDirection, -Vector3.forward));
            bomb.GetComponent<Rigidbody>().velocity = aBombInfo.m_startVelocity;
            bomb.GetComponent<BombController>().SetBombInfo(aBombInfo);
            return bomb;
        }

        public GameObject SpawnBullet(BulletController.BulletInfo aBulletInfo)
        {
            GameObject player = null;
            GameObject[] planes = GameObject.FindGameObjectsWithTag("Plane");
            for (int i = 0; i < planes.Length; i++)
            {
                if (planes[i].GetComponent<PhotonView>().owner == aBulletInfo.m_shooter)
                {
                    player = planes[i];
                    break;
                }
            }


            if (player != null)
            {
                player.GetComponent<PlaneController>().ResetGunCharge();
                GameObject flare = (GameObject)Instantiate(m_muzzleFlarePrefab, player.GetComponent<PlaneController>().m_bulletSpawnPoint.position, player.GetComponent<PlaneController>().m_bulletSpawnPoint.rotation);
                flare.transform.parent = player.transform;
                flare.GetComponent<AudioSource>().pitch = 1 + Random.Range(-2.0f, 3.0f) * 0.2f;
                if (aBulletInfo.m_shooter == PhotonNetwork.player)
                {
                    flare.GetComponent<AudioSource>().spatialBlend = 0;
                }
                flare.GetComponent<AudioSource>().Play();
            }

            GameObject bullet = (GameObject)Instantiate(((int)aBulletInfo.m_shooter.customProperties["Team"] == 1) ? m_bullet1Prefab : m_bullet2Prefab, aBulletInfo.m_startPosition, Quaternion.LookRotation(aBulletInfo.m_startDirection, -Vector3.forward));
            bullet.GetComponent<Rigidbody>().velocity = aBulletInfo.m_startVelocity;
            bullet.GetComponent<BulletController>().SetBulletInfo(aBulletInfo);

            return bullet;
        }
    }

}