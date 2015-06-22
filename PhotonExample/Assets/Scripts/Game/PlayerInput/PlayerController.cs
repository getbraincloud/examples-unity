using UnityEngine;
using System.Collections;
using BrainCloudPhotonExample.Connection;
using UnityEngine.UI;

namespace BrainCloudPhotonExample.Game.PlayerInput
{
    public class PlayerController : MonoBehaviour
    {
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

        void Start()
        {
            m_turnSpeed = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_planeTurnSpeed;
            m_acceleration = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_planeAcceleration;
            m_baseHealth = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_basePlaneHealth;
            m_maxSpeedMultiplier = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_maxPlaneSpeedMultiplier;
        }

        public void SetPlayerPlane(PlaneController playerPlane)
        {
            m_leftBoundsTimer = 4;
            GameObject.Find("MapBounds").GetComponent<MapBoundsCheck>().m_playerPlane = playerPlane.gameObject;
            StartCoroutine("PulseMissionText");
            m_leftBounds = false;
            m_currentRotation = playerPlane.gameObject.transform.rotation.eulerAngles.z;
            m_isActive = true;
            m_playerPlane = playerPlane;
            GetComponent<WeaponController>().SetPlayerPlane(playerPlane);
            m_health = m_baseHealth;
        }

        IEnumerator PulseMissionText()
        {
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
                m_missionText.GetComponent<Text>().text = "Return to Mission Area " + Mathf.CeilToInt(m_leftBoundsTimer);
            }
            else
            {
                m_missionText.GetComponent<Text>().text = "";
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
            m_shakeIntensity = aIntensity;
            m_shakeDecay = aDecay;
        }

        public void SuicidePlayer()
        {
            m_leftBounds = false;
            m_health = 0;
            Vector3 position = m_playerPlane.transform.position;
            int bombs = GetComponent<WeaponController>().GetBombs();
            GameObject.Find("GameManager").GetComponent<GameManager>().DestroyPlayerPlane(PhotonNetwork.player);
            GameObject.Find("GameManager").GetComponent<GameManager>().SpawnBombPickup(position);
            for (int i = 0; i < bombs; i++)
            {

                GameObject.Find("GameManager").GetComponent<GameManager>().SpawnBombPickup(position);
            }
            DestroyPlayerPlane();
        }

        public void DestroyPlayerPlane()
        {
            if (m_playerPlane == null)
            {
                return;
            }
            GameObject.Find("MapBounds").GetComponent<MapBoundsCheck>().m_playerPlane = null;
            m_leftBounds = false;
            m_isActive = false;
            PhotonNetwork.Destroy(m_playerPlane.gameObject);
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

        public void TakeBulletDamage(PhotonPlayer aShooter)
        {
            if (m_health == 0) return;
            m_health--;
            m_playerPlane.m_health = m_health;
            if (m_health <= 0)
            {
                m_health = 0;
                Vector3 position = m_playerPlane.transform.position;
                int bombs = GetComponent<WeaponController>().GetBombs();
                GameObject.Find("GameManager").GetComponent<GameManager>().DestroyPlayerPlane(PhotonNetwork.player, aShooter);
                GameObject.Find("GameManager").GetComponent<GameManager>().SpawnBombPickup(position);
                for (int i = 0; i < bombs; i++)
                {

                    GameObject.Find("GameManager").GetComponent<GameManager>().SpawnBombPickup(position);
                }
            }
        }

        public void EnteredBounds()
        {
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