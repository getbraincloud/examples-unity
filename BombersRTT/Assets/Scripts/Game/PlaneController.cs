using UnityEngine;
using System.Collections.Generic;
using Gameframework;

namespace BrainCloudUNETExample.Game
{
    public class PlaneController : BaseNetworkBehavior
    {
        //[SyncVar]
        public string PlayerId
        {
            get { return m_playerId; }
            set
            {
                m_playerId = value;
                IsServerBot = m_playerId.Contains(GameManager.SERVER_BOT);
            }
        }
        private string m_playerId = "";

        // wrapper to the base behavior one
        public short NetId
        {
            get { return _netId; }
            set
            {
                _netId = value;
                _hasAuthority = (IsServer && IsServerBot) || (BombersNetworkManager.LocalPlayer != null && _netId == BombersNetworkManager.LocalPlayer._netId);
            }
        }

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

        public BombersPlayerController PlayerController { get; private set; }
        public GameObject SmartsComponent { get; private set; }
        override protected void Start()
        {
            m_velocityMaxMagnitude = GConfigManager.GetFloatValue("MaxAcceleration");
            _classType = BombersNetworkManager.PLANE_CONTROLLER;
            m_lastPositions = new PlaneVector[1];
            m_planeDamage = new List<GameObject>()
            {
                null, null, null, null
            };
            TextMesh textMesh = transform.FindDeepChild("NameTag").gameObject.GetComponent<TextMesh>();
            PlayerController = BombersPlayerController.GetPlayer(PlayerId);

            // setup the member info
            if (PlayerController.MemberInfo == null)
                PlayerController.MemberInfo = BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(PlayerController.ProfileId);

            textMesh.text = PlayerController.m_displayName;
            if (PlayerController.IsLocalPlayer)
            {
                textMesh.text = "";
            }
            m_planeBank = transform.FindDeepChild("PlaneBank");
            for (int i = 0; i < m_lastPositions.Length; i++)
            {
                m_lastPositions[i] = new PlaneVector(transform.position, transform.up);
            }

            string teamBomberPath = "";
            bool bHasGoldWings = false;
            if (PlayerController.MemberInfo.ExtraData.ContainsKey(GBomberRTTConfigManager.JSON_GOLD_WINGS))
                bHasGoldWings = (bool)PlayerController.MemberInfo.ExtraData[GBomberRTTConfigManager.JSON_GOLD_WINGS];

            if (PlayerController.m_team == 1)
            {
                teamBomberPath = bHasGoldWings ? "Bomber01Golden" : "Bomber01";
                gameObject.layer = 8;
                textMesh.color = Color.green;
            }
            else
            {
                teamBomberPath = bHasGoldWings ? "Bomber02Golden" : "Bomber02";
                gameObject.layer = 9;
                textMesh.color = Color.red;
            }

            SmartsComponent = PlayerController.transform.FindDeepChild("smartsComponent").gameObject;
            SmartsComponent.SetActive(true);
            SmartsComponent.layer = PlayerController.m_team == 1 ? 21 : 22; // debug collisions

            Transform graphicPivot = transform.FindDeepChild("PlaneGraphic");
            GameObject graphic = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + teamBomberPath), graphicPivot.position, graphicPivot.rotation);
            graphic.transform.parent = graphicPivot;
            graphic.transform.localPosition = Vector3.zero;
            graphic.transform.localRotation = Quaternion.identity;

            m_bulletSpawnPoint = graphic.transform.FindDeepChild("BulletSpawn");
            m_leftContrail = graphic.transform.FindDeepChild("LeftSmokeTrail").GetComponent<ParticleSystem>();
            m_rightContrail = graphic.transform.FindDeepChild("RightSmokeTrail").GetComponent<ParticleSystem>();

            m_gunCharge = transform.FindDeepChild("GunCharge").gameObject;
            m_gunCharge.GetComponent<Animator>().speed = 1 / GConfigManager.GetFloatValue("MultishotDelay");
            if (!PlayerController.IsLocalPlayer)
                m_gunCharge.transform.Find("ChargeReady").transform.GetComponent<AudioSource>().enabled = false;

            transform.localPosition = Vector3.zero;
            m_rigidBody = GetComponent<Rigidbody>();

            _syncTransformInformation = true;
            // no delay by default
            m_syncTransformationDelay = 0.0f;
            transform.position = PlayerController.transform.position;

            if (IsServer)
            {
                SendStart(_classType, PlayerId, _fileName, transform);
            }

            base.Start();
        }

        public void ResetGunCharge()
        {
            if (m_gunCharge != null)
            {
                m_gunCharge.SetActive(false);
                m_gunCharge.SetActive(true);
            }
        }

        private float m_syncTransformationDelay = 0.0f;
        private const float SYNC_TRANSFORM_DELAY = 0.5f;
        void Update()
        {
            // update contrails based of alive or dead
            if (m_leftContrail.gameObject != null) m_leftContrail.gameObject.SetActive(PlayerController.m_planeActive);
            if (m_rightContrail.gameObject != null) m_rightContrail.gameObject.SetActive(PlayerController.m_planeActive);

            ParticleSystem.MainModule rtContrail = m_rightContrail.main;
            ParticleSystem.MainModule ltContrail = m_leftContrail.main;
            SmartsComponent.transform.position = transform.position;

            switch (m_health)
            {
                case 1:
                    ltContrail.startColor = m_blackSmokeColor;
                    rtContrail.startColor = m_blackSmokeColor;
                    if (m_planeDamage[3] == null) m_planeDamage[3] = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "LowHPEffect"), transform.FindDeepChild("LowHPDummy4").position, transform.FindDeepChild("LowHPDummy4").rotation);
                    m_planeDamage[3].transform.parent = transform.FindDeepChild("LowHPDummy4");
                    if (m_planeDamage[2] == null) m_planeDamage[2] = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "LowHPEffect"), transform.FindDeepChild("LowHPDummy3").position, transform.FindDeepChild("LowHPDummy3").rotation);
                    m_planeDamage[2].transform.parent = transform.FindDeepChild("LowHPDummy3");
                    if (m_planeDamage[1] == null) m_planeDamage[1] = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "LowHPEffect"), transform.FindDeepChild("LowHPDummy2").position, transform.FindDeepChild("LowHPDummy2").rotation);
                    m_planeDamage[1].transform.parent = transform.FindDeepChild("LowHPDummy2");
                    if (m_planeDamage[0] == null) m_planeDamage[0] = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "LowHPEffect"), transform.FindDeepChild("LowHPDummy1").position, transform.FindDeepChild("LowHPDummy1").rotation);
                    m_planeDamage[0].transform.parent = transform.FindDeepChild("LowHPDummy1");
                    break;
                case 2:
                    ltContrail.startColor = m_blackSmokeColor;
                    rtContrail.startColor = m_whiteSmokeColor;
                    if (m_planeDamage[3] != null) Destroy(m_planeDamage[3]);
                    if (m_planeDamage[2] == null) m_planeDamage[2] = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "LowHPEffect"), transform.FindDeepChild("LowHPDummy3").position, transform.FindDeepChild("LowHPDummy3").rotation);
                    m_planeDamage[2].transform.parent = transform.FindDeepChild("LowHPDummy3");
                    if (m_planeDamage[1] == null) m_planeDamage[1] = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "LowHPEffect"), transform.FindDeepChild("LowHPDummy2").position, transform.FindDeepChild("LowHPDummy2").rotation);
                    m_planeDamage[1].transform.parent = transform.FindDeepChild("LowHPDummy2");
                    if (m_planeDamage[0] == null) m_planeDamage[0] = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "LowHPEffect"), transform.FindDeepChild("LowHPDummy1").position, transform.FindDeepChild("LowHPDummy1").rotation);
                    m_planeDamage[0].transform.parent = transform.FindDeepChild("LowHPDummy1");
                    break;
                case 3:
                    ltContrail.startColor = m_whiteSmokeColor;
                    rtContrail.startColor = m_whiteSmokeColor;
                    if (m_planeDamage[3] != null) Destroy(m_planeDamage[3]);
                    if (m_planeDamage[2] != null) Destroy(m_planeDamage[2]);
                    if (m_planeDamage[1] == null) m_planeDamage[1] = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "LowHPEffect"), transform.FindDeepChild("LowHPDummy2").position, transform.FindDeepChild("LowHPDummy2").rotation);
                    m_planeDamage[1].transform.parent = transform.FindDeepChild("LowHPDummy2");
                    if (m_planeDamage[0] == null) m_planeDamage[0] = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "LowHPEffect"), transform.FindDeepChild("LowHPDummy1").position, transform.FindDeepChild("LowHPDummy1").rotation);
                    m_planeDamage[0].transform.parent = transform.FindDeepChild("LowHPDummy1");
                    break;
                case 4:
                    ltContrail.startColor = m_whiteSmokeColor;
                    rtContrail.startColor = m_whiteSmokeColor;
                    if (m_planeDamage[3] != null) Destroy(m_planeDamage[3]);
                    if (m_planeDamage[2] != null) Destroy(m_planeDamage[2]);
                    if (m_planeDamage[1] != null) Destroy(m_planeDamage[1]);
                    if (m_planeDamage[0] == null) m_planeDamage[0] = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "LowHPEffect"), transform.FindDeepChild("LowHPDummy1").position, transform.FindDeepChild("LowHPDummy1").rotation);
                    m_planeDamage[0].transform.parent = transform.FindDeepChild("LowHPDummy1");
                    break;
                case 5:
                    if (m_planeDamage[3] != null) Destroy(m_planeDamage[3]);
                    if (m_planeDamage[2] != null) Destroy(m_planeDamage[2]);
                    if (m_planeDamage[1] != null) Destroy(m_planeDamage[1]);
                    if (m_planeDamage[0] != null) Destroy(m_planeDamage[0]);
                    ltContrail.startColor = m_whiteSmokeColor;
                    rtContrail.startColor = m_whiteSmokeColor;
                    break;
            }

            if (!IsLocalPlayer && (!IsServerBot || (IsServerBot && !IsServer)))
            {
                if (PlayerController.m_planeActive)
                {
                    if (m_syncTransformationDelay < SYNC_TRANSFORM_DELAY)
                        m_syncTransformationDelay += Time.deltaTime;

                    if (m_syncTransformationDelay >= SYNC_TRANSFORM_DELAY)
                        syncTransformsWithNetworkData();
                }
                else
                {
                    m_syncTransformationDelay = 0.0f;
                    transform.position = PlayerController.transform.position;
                }
                Vector3 local = transform.localPosition;
                local.z = 0.0f;
                transform.localPosition = local;
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
            if (!IsLocalPlayer && (IsServerBot && !IsServer))
            {
                return;
            }

            Vector3 direction = m_rigidBody.velocity;

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
        private Rigidbody m_rigidBody = null;
        private float m_velocityMaxMagnitude = 30.0f;
        public void Accelerate(float aAcceleration, float aSpeedMultiplier)
        {
            m_rigidBody.AddForce(transform.up * aAcceleration * aSpeedMultiplier);
            if (m_rigidBody.velocity.magnitude > m_velocityMaxMagnitude * aSpeedMultiplier)
            {
                m_rigidBody.velocity = m_rigidBody.velocity.normalized * m_velocityMaxMagnitude * aSpeedMultiplier;
            }
        }

        void LateUpdate()
        {
            if (!IsLocalPlayer && (IsServerBot && !IsServer))
            {
                transform.Find("NameTag").position = (transform.position - new Vector3(0, 7.4f, 10));
                transform.Find("NameTag").eulerAngles = new Vector3(0, 0, 0);
                return;
            }

            for (int i = 0; i < m_lastPositions.Length - 1; i++)
            {
                m_lastPositions[i] = m_lastPositions[i + 1];
            }

            Vector3 direction = m_rigidBody.velocity;

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

            transform.Find("NameTag").position = (transform.position - new Vector3(0, 7.4f, 10));
            transform.Find("NameTag").eulerAngles = new Vector3(0, 0, 0);
        }

        public void SetRotation(float aRotation)
        {
            Vector3 eulerAngles = transform.localEulerAngles;
            eulerAngles.z = aRotation;
            transform.localEulerAngles = eulerAngles;
        }
    }


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
}
