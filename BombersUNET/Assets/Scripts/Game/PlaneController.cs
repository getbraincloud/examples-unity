using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using BrainCloudUNETExample.Connection;
using BrainCloudUNETExample.Game.PlayerInput;

namespace BrainCloudUNETExample.Game
{
    public class PlaneController : NetworkBehaviour
    {
        public class PlaneVector
        {
            public Vector3 m_position { get; set; }
            public Vector3 m_direction { get; set; }

            public PlaneVector(Vector3 aPosition, Vector3 aDirection)
            {
                m_direction = aDirection;
                m_position = aPosition;
            }

            public float AngleTo(PlaneVector aVector)
            {
                float direction = 1;
                float angle = 0.0f;

                if ((m_direction.x * aVector.m_direction.y) - (m_direction.y * aVector.m_direction.x) < 0)
                {
                    direction = -1;
                }

                angle = Vector3.Angle(m_direction, aVector.m_direction);

                if (m_direction == aVector.m_direction) angle = 0;

                return angle * direction;
            }
        }

        [SyncVar]
        public int m_playerID;

        public Transform m_bulletSpawnPoint;

        public int m_health = 0;
        private GameObject m_gunCharge;

        private bool m_isBankingRight = false;
        private bool m_isBankingLeft = false;
        private float m_bankTime = 0;
        private Color m_whiteSmokeColor = new Color(236 / 255.0f, 236 / 255.0f, 236 / 255.0f, 168 / 255.0f);
        private Color m_blackSmokeColor = new Color(81 / 255.0f, 77 / 255.0f, 74 / 255.0f, 168 / 255.0f);

        [SerializeField]
        private float m_timeToFullBank = 1;

        [SerializeField]
        private AnimationCurve m_bankCurve = new AnimationCurve();

        private Transform m_planeBank;

        private float m_bankAngle = 0.0f;

        private PlaneVector[] m_lastPositions = new PlaneVector[6];

        private ParticleSystem m_leftContrail;
        private ParticleSystem m_rightContrail;
        private List<GameObject> m_planeDamage;

        public class TimePosition
        {
            public Vector3 m_position;
            public float m_time;
            public float m_rotation;

            public TimePosition(Vector3 aPosition, float aTime, float aRotation)
            {
                m_time = aTime;
                m_position = aPosition;
                m_rotation = aRotation;
            }
        }

        void Start()
        {
            m_lastPositions = new PlaneVector[1];
            m_planeDamage = new List<GameObject>() 
            { 
                null, null, null, null
            };
            Debug.Log(m_playerID);
            transform.FindChild("NameTag").gameObject.GetComponent<TextMesh>().text = BombersPlayerController.GetPlayer(m_playerID).m_displayName;
            if (BombersPlayerController.GetPlayer(m_playerID).isLocalPlayer) //isLocal
            {
                transform.FindChild("NameTag").gameObject.GetComponent<TextMesh>().text = "";
            }
            m_planeBank = transform.FindChild("PlaneBank");
            for (int i = 0; i < m_lastPositions.Length; i++)
            {
                m_lastPositions[i] = new PlaneVector(transform.position, transform.up);
            }

            string teamBomberPath = "";
            if (BombersPlayerController.GetPlayer(m_playerID).m_team == 1)
            {
                teamBomberPath = "Bomber01";
                gameObject.layer = 8;
                transform.FindChild("NameTag").gameObject.GetComponent<TextMesh>().color = Color.green;
            }
            else
            {
                teamBomberPath = "Bomber02";
                gameObject.layer = 9;
                transform.FindChild("NameTag").gameObject.GetComponent<TextMesh>().color = Color.red;
            }
            Transform graphicPivot = transform.FindChild("PlaneBank").FindChild("PlaneGraphic");
            GameObject graphic = (GameObject)Instantiate((GameObject)Resources.Load(teamBomberPath), graphicPivot.position, graphicPivot.rotation);
            graphic.transform.parent = graphicPivot;
            graphic.transform.localPosition = Vector3.zero;
            graphic.transform.localRotation = Quaternion.identity;

            m_bulletSpawnPoint = graphic.transform.FindChild("BulletSpawn");
            m_leftContrail = graphic.transform.FindChild("LeftSmokeTrail").GetComponent<ParticleSystem>();
            m_rightContrail = graphic.transform.FindChild("RightSmokeTrail").GetComponent<ParticleSystem>();

            m_gunCharge = transform.GetChild(0).GetChild(0).FindChild("GunCharge").gameObject;
            m_gunCharge.GetComponent<Animator>().speed = 1 / GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_multiShotDelay;
        }

        public void ResetGunCharge()
        {
            m_gunCharge.SetActive(false);
            m_gunCharge.SetActive(true);
        }

        void Update()
        {
            m_health = GetComponent<BombersPlayerController>().m_health;
            switch (m_health)
            {
                case 1:
                    m_rightContrail.startColor = m_blackSmokeColor;
                    m_leftContrail.startColor = m_blackSmokeColor;
                    if (m_planeDamage[3] == null) m_planeDamage[3] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy4").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy4").rotation);
                    m_planeDamage[3].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy4");
                    if (m_planeDamage[2] == null) m_planeDamage[2] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy3").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy3").rotation);
                    m_planeDamage[2].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy3");
                    if (m_planeDamage[1] == null) m_planeDamage[1] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2").rotation);
                    m_planeDamage[1].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2");
                    if (m_planeDamage[0] == null) m_planeDamage[0] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").rotation);
                    m_planeDamage[0].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1");
                    break;
                case 2:
                    m_leftContrail.startColor = m_blackSmokeColor;
                    m_rightContrail.startColor = m_whiteSmokeColor;
                    if (m_planeDamage[3] != null) Destroy(m_planeDamage[3]);
                    if (m_planeDamage[2] == null) m_planeDamage[2] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy3").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy3").rotation);
                    m_planeDamage[2].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy3");
                    if (m_planeDamage[1] == null) m_planeDamage[1] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2").rotation);
                    m_planeDamage[1].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2");
                    if (m_planeDamage[0] == null) m_planeDamage[0] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").rotation);
                    m_planeDamage[0].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1");
                    break;
                case 3:
                    m_leftContrail.startColor = m_whiteSmokeColor;
                    m_rightContrail.startColor = m_whiteSmokeColor;
                    if (m_planeDamage[3] != null) Destroy(m_planeDamage[3]);
                    if (m_planeDamage[2] != null) Destroy(m_planeDamage[2]);
                    if (m_planeDamage[1] == null) m_planeDamage[1] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2").rotation);
                    m_planeDamage[1].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2");
                    if (m_planeDamage[0] == null) m_planeDamage[0] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").rotation);
                    m_planeDamage[0].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1");
                    break;
                case 4:
                    m_leftContrail.startColor = m_whiteSmokeColor;
                    m_rightContrail.startColor = m_whiteSmokeColor;
                    if (m_planeDamage[3] != null) Destroy(m_planeDamage[3]);
                    if (m_planeDamage[2] != null) Destroy(m_planeDamage[2]);
                    if (m_planeDamage[1] != null) Destroy(m_planeDamage[1]);
                    if (m_planeDamage[0] == null) m_planeDamage[0] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").rotation);
                    m_planeDamage[0].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1");
                    break;
                case 5:
                    if (m_planeDamage[3] != null) Destroy(m_planeDamage[3]);
                    if (m_planeDamage[2] != null) Destroy(m_planeDamage[2]);
                    if (m_planeDamage[1] != null) Destroy(m_planeDamage[1]);
                    if (m_planeDamage[0] != null) Destroy(m_planeDamage[0]);
                    m_leftContrail.startColor = m_whiteSmokeColor;
                    m_rightContrail.startColor = m_whiteSmokeColor;
                    break;
            }

            if (!isLocalPlayer)
            {
                return;
            }

            if (m_isBankingLeft)
            {
                m_bankTime += Time.deltaTime;
                if (m_bankTime > m_timeToFullBank)
                {
                    m_bankTime = m_timeToFullBank;
                }
            }
            else if (m_isBankingRight)
            {
                m_bankTime -= Time.deltaTime;
                if (m_bankTime < -m_timeToFullBank)
                {
                    m_bankTime = -m_timeToFullBank;
                }
            }
            else
            {
                if (m_bankTime > 0)
                {
                    m_bankTime -= Time.deltaTime;
                    if (m_bankTime < 0)
                    {
                        m_bankTime = 0;
                    }
                }
                else if (m_bankTime < 0)
                {
                    m_bankTime += Time.deltaTime;
                    if (m_bankTime > 0)
                    {
                        m_bankTime = 0;
                    }
                }
            }
        }

        void FixedUpdate()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            Vector3 direction = GetComponent<Rigidbody>().velocity;

            if (direction == Vector3.zero)
            {
                direction = transform.up;
            }
            else
            {
                direction.Normalize();
            }

            direction = transform.up;
            float angle = m_lastPositions[0].AngleTo(new PlaneVector(transform.position, direction)) * 5;
            if (angle > 90)
            {
                angle = 90;
            }
            else if (angle < -90)
            {
                angle = -90;
            }

            if (angle < 0)
            {
                m_isBankingRight = true;
                m_isBankingLeft = false;
            }
            else if (angle > 0)
            {
                m_isBankingRight = false;
                m_isBankingLeft = true;
            }
            else
            {
                m_isBankingRight = false;
                m_isBankingLeft = false;
            }

            float targetAngle = 0;


            if (m_bankTime < 0)
            {
                targetAngle = -90 * m_bankCurve.Evaluate(m_bankTime / -m_timeToFullBank);
            }
            else
            {
                targetAngle = 90 * m_bankCurve.Evaluate(m_bankTime / m_timeToFullBank);
            }

            m_bankAngle = targetAngle;

            Vector3 eulerAngles = m_planeBank.localEulerAngles;
            eulerAngles.y = m_bankAngle;
            m_planeBank.localEulerAngles = eulerAngles;
            m_lastPositions[0] = new PlaneVector(transform.position, direction);

        }

        public void Accelerate(float aAcceleration, float aSpeedMultiplier)
        {
            GetComponent<Rigidbody>().AddForce(transform.up * aAcceleration * aSpeedMultiplier);
            if (GetComponent<Rigidbody>().velocity.magnitude > 30 * aSpeedMultiplier)
            {
                GetComponent<Rigidbody>().velocity = GetComponent<Rigidbody>().velocity.normalized * 30 * aSpeedMultiplier;
            }
        }

        void LateUpdate()
        {
            if (!isLocalPlayer)
            {
                transform.FindChild("NameTag").position = (transform.position - new Vector3(0, 7.4f, 10));
                transform.FindChild("NameTag").eulerAngles = new Vector3(0, 0, 0);
                return;
            }

            for (int i = 0; i < m_lastPositions.Length - 1; i++)
            {
                m_lastPositions[i] = m_lastPositions[i + 1];
            }

            Vector3 direction = GetComponent<Rigidbody>().velocity;

            if (direction == Vector3.zero)
            {
                direction = transform.up;
            }
            else
            {
                direction.Normalize();
            }

            direction = transform.up;

            m_lastPositions[m_lastPositions.Length - 1] = new PlaneVector(transform.position, direction);

            transform.FindChild("NameTag").position = (transform.position - new Vector3(0, 7.4f, 10));
            transform.FindChild("NameTag").eulerAngles = new Vector3(0, 0, 0);
        }

        public void SetRotation(float aRotation)
        {
            Vector3 eulerAngles = transform.localEulerAngles;
            eulerAngles.z = aRotation;
            transform.localEulerAngles = eulerAngles;
        }
    }
}