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

        Vector3 CalculateBezierPoint(float aTime, Vector3 aStartPoint, Vector3 aStartHandle, Vector3 aEndHandle, Vector3 aEndPoint)
        {
            float u = 1.0f - aTime;
            float tt = aTime * aTime;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * aTime;

            Vector3 p = uuu * aStartPoint;
            p += 3 * uu * aTime * aStartHandle;
            p += 3 * u * tt * aEndHandle;
            p += ttt * aEndPoint;

            return p;
        }

        Vector3 CalculateBezierPoint(float aTime, Vector3 aStartPoint, Vector3 aHandle, Vector3 aEndPoint)
        {
            return CalculateBezierPoint(aTime, aStartPoint, aHandle, aHandle, aEndPoint);
        }

        [SyncVar]
        public int m_playerID;

        public Transform m_bulletSpawnPoint;

        private Vector3 m_photonPosition = Vector3.zero;
        private float m_photonRotation = 0;
        //private System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
        public int m_health = 0;
        private GameObject m_gunCharge;

        //public void OnPhotonSerializeView(int aStream, int aInfo)
        //{
        //    if (aStream.isWriting)
        //    {
        //        aStream.SendNext(transform.position);
        //        aStream.SendNext(transform.rotation.eulerAngles.z);
        //        aStream.SendNext(m_health);
        //    }
        //    else
        //    {

        //        m_photonPosition = (Vector3)aStream.ReceiveNext();
        //        m_photonRotation = (float)aStream.ReceiveNext();
        //        m_health = (int)aStream.ReceiveNext();

        //        stopWatch.Stop();
        //        if (stopWatch.ElapsedMilliseconds > (1000 / PhotonNetwork.sendRate))
        //        {
        //            m_photonReleasedPositions.Add(new TimePosition(m_photonPosition, (float)stopWatch.ElapsedMilliseconds, m_photonRotation));
        //            if (m_once && m_photonReleasedPositions.Count >= 4)
        //            {
        //                m_once = false;
        //                StartCoroutine("ReleasePositions");
        //            }
        //            stopWatch.Reset();
        //        }
        //        stopWatch.Start();
        //    }
        //}

        //private bool m_once = true;

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

        private List<TimePosition> m_photonReleasedPositions = new List<TimePosition>();
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

        IEnumerator ReleasePositions()
        {
            bool loop = true;
            m_bezierTime = 0.5f;
            while (loop)
            {
                if (m_photonReleasedPositions.Count != 0)
                {
                    for (int i = 0; i < m_photonPositions.Length - 1; i++)
                    {
                        m_photonPositions[i] = m_photonPositions[i + 1];
                        m_photonRotations[i] = m_photonRotations[i + 1];
                    }

                    Vector3 velocity = (m_photonReleasedPositions[0].m_position - m_photonPositions[m_photonPositions.Length - 1]);
                    velocity *= (((float)(1000 / 1)) / m_photonReleasedPositions[0].m_time);
                    Vector3 calculatedPosition = m_photonPositions[m_photonPositions.Length - 1] + velocity;

                    if (!float.IsNaN(calculatedPosition.x) && !float.IsNaN(calculatedPosition.y) && !float.IsNaN(calculatedPosition.z))
                    {
                        if ((m_photonPosition - calculatedPosition).magnitude > 10.0f)
                        {
                            m_photonPositions[m_photonPositions.Length - 1] = m_photonPosition;
                            m_photonRotations[m_photonRotations.Length - 1] = m_photonRotation;
                        }
                        else
                        {
                            m_photonPositions[m_photonPositions.Length - 1] = calculatedPosition;
                            m_photonRotations[m_photonRotations.Length - 1] = m_photonReleasedPositions[0].m_rotation;
                        }
                    }

                    bool found = false;
                    float properBezierTime = 0.0f;

                    Vector3 lastBezierPoint = m_lastBezierPoint;
                    Vector3 calculatedPosition2 = CalculateBezierPoint(properBezierTime, lastBezierPoint, m_photonPositions[1], m_photonPositions[2], m_photonPositions[3]);

                    Vector3 currentPosition = transform.position;
                    float lastMagnitude = 100;
                    while (!found)
                    {
                        calculatedPosition2 = CalculateBezierPoint(properBezierTime, lastBezierPoint, m_photonPositions[1], m_photonPositions[2], m_photonPositions[3]);

                        if ((currentPosition - calculatedPosition2).magnitude < lastMagnitude)
                        {
                            lastMagnitude = (currentPosition - calculatedPosition2).magnitude;
                        }
                        else if ((currentPosition - calculatedPosition2).magnitude > lastMagnitude)
                        {
                            found = true;
                            break;
                        }

                        properBezierTime += 0.01f;
                        if (properBezierTime > 1)
                        {
                            properBezierTime = 0.75f;
                            found = true;
                        }
                    }

                    m_bezierTime = properBezierTime;

                    m_photonReleasedPositions.RemoveAt(0);
                }
                else
                {
                    for (int i = 0; i < m_photonPositions.Length - 1; i++)
                    {
                        m_photonPositions[i] = m_photonPositions[i + 1];
                        m_photonRotations[i] = m_photonRotations[i + 1];
                    }
                    m_photonPositions[m_photonPositions.Length - 1] = m_photonPosition;
                    m_photonRotations[m_photonRotations.Length - 1] = m_photonRotation;

                    bool found = false;
                    float properBezierTime = 0.0f;

                    Vector3 lastBezierPoint = m_lastBezierPoint;

                    Vector3 calculatedPosition2 = CalculateBezierPoint(properBezierTime, lastBezierPoint, m_photonPositions[1], m_photonPositions[2], m_photonPositions[3]);
                    Vector3 currentPosition = transform.position;
                    float lastMagnitude = 100;
                    while (!found)
                    {
                        calculatedPosition2 = CalculateBezierPoint(properBezierTime, lastBezierPoint, m_photonPositions[1], m_photonPositions[2], m_photonPositions[3]);

                        if ((currentPosition - calculatedPosition2).magnitude < lastMagnitude)
                        {
                            lastMagnitude = (currentPosition - calculatedPosition2).magnitude;
                        }
                        else if ((currentPosition - calculatedPosition2).magnitude > lastMagnitude)
                        {
                            found = true;
                            break;
                        }

                        properBezierTime += 0.01f;
                        if (properBezierTime > 1)
                        {
                            properBezierTime = 0.75f;
                            found = true;
                        }
                    }

                    m_bezierTime = properBezierTime;
                }
                yield return new WaitForSeconds(((float)(1000 / 1)) / 1000);
            }
        }

        void Start()
        {
            m_bezierTime = 0.0f;
            m_planeDamage = new List<GameObject>() 
            { 
                null, null, null, null
            };
            transform.FindChild("NameTag").gameObject.GetComponent<TextMesh>().text = BombersPlayerController.GetPlayer(m_playerID).m_displayName;
            if (BombersPlayerController.GetPlayer(m_playerID).isLocalPlayer) //isLocal
            {
                transform.FindChild("NameTag").gameObject.GetComponent<TextMesh>().text = "";
            }
            m_planeBank = transform.FindChild("PlaneBank");
            m_lastBezierPoint = transform.position;
            for (int i = 0; i < m_lastPositions.Length; i++)
            {
                m_lastPositions[i] = new PlaneVector(transform.position, transform.up);
            }


            for (int i = 0; i < m_photonPositions.Length; i++)
            {
                m_photonPositions[i] = transform.position;
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
                    if (m_planeDamage[2] == null) m_planeDamage[2] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy3").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy3").rotation);
                    m_planeDamage[2].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy3");
                    if (m_planeDamage[1] == null) m_planeDamage[1] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2").rotation);
                    m_planeDamage[1].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2");
                    if (m_planeDamage[0] == null) m_planeDamage[0] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").rotation);
                    m_planeDamage[0].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1");
                    break;
                case 3:
                    if (m_planeDamage[1] == null) m_planeDamage[1] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2").rotation);
                    m_planeDamage[1].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy2");
                    if (m_planeDamage[0] == null) m_planeDamage[0] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").rotation);
                    m_planeDamage[0].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1");
                    break;
                case 4:
                    if (m_planeDamage[0] == null) m_planeDamage[0] = (GameObject)Instantiate((GameObject)Resources.Load("LowHPEffect"), transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").position, transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1").rotation);
                    m_planeDamage[0].transform.parent = transform.GetChild(0).GetChild(0).FindChild("LowHPDummy1");
                    break;
                case 5:
                    m_leftContrail.startColor = m_whiteSmokeColor;
                    m_rightContrail.startColor = m_whiteSmokeColor;
                    break;
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

        private Vector3[] m_photonPositions = new Vector3[4];
        private float[] m_photonRotations = new float[4];
        [SerializeField]
        private float m_bezierSpeed = 1000.0f;
        private float m_bezierTime = 0;
        private Vector3 m_lastBezierPoint;

        void FixedUpdate()
        {
            //if (false)//!isLocalClient
            //{
            //    m_bezierTime += Time.deltaTime * m_bezierSpeed;
            //    if (m_bezierTime > 1) m_bezierTime = 1;

            //    m_lastBezierPoint = CalculateBezierPoint(m_bezierTime, m_lastBezierPoint, m_photonPositions[1], m_photonPositions[2], m_photonPositions[3]);
            //    GetComponent<Rigidbody>().MovePosition(Vector3.Lerp(transform.position, m_lastBezierPoint, 11 * Time.smoothDeltaTime));
            //    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0, 0, m_photonRotations[0])), Time.smoothDeltaTime * 20);
            //}

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