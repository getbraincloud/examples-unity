using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using LitJson;
using BrainCloudUNETExample.Connection;

namespace BrainCloudUNETExample.Game
{
    public class BulletController : MonoBehaviour
    {
        [SerializeField]
        private float m_lifeTime = 2.5f;

        private BulletInfo m_bulletInfo;

        public class BulletInfo
        {
            public Vector3 m_startPosition;
            public Vector3 m_startDirection;
            public int m_shooter;
            public Vector3 m_startVelocity;
            public int m_bulletID;
            public bool m_isMaster = false;
            public GameObject gameObject;

            public BulletInfo(Vector3 aStartPos, Vector3 aStartDir, int aPlayer, Vector3 aSpeed, int aID = 0)
            {
                m_startPosition = aStartPos;
                m_startDirection = aStartDir;
                m_shooter = aPlayer;
                m_startVelocity = aSpeed;
                m_bulletID = aID;
            }

            public static BulletInfo GetBulletInfo(string aJson)
            {
                JsonData bombInfo = JsonMapper.ToObject(aJson);

                Vector3 startPos = Vector3.zero;
                Vector3 direction = Vector3.zero;
                int shooterID = 0;
                Vector3 speed = Vector3.zero;
                int id = 0;

                startPos.x = float.Parse(bombInfo["startPos"]["x"].ToString());
                startPos.y = float.Parse(bombInfo["startPos"]["y"].ToString());
                startPos.z = float.Parse(bombInfo["startPos"]["z"].ToString());
                direction.x = float.Parse(bombInfo["direction"]["x"].ToString());
                direction.y = float.Parse(bombInfo["direction"]["y"].ToString());
                direction.z = float.Parse(bombInfo["direction"]["z"].ToString());
                speed.x = float.Parse(bombInfo["speed"]["x"].ToString());
                speed.y = float.Parse(bombInfo["speed"]["y"].ToString());
                speed.z = float.Parse(bombInfo["speed"]["z"].ToString());
                shooterID = int.Parse(bombInfo["shooterID"].ToString());
                id = int.Parse(bombInfo["id"].ToString());

                return new BulletInfo(startPos, direction, shooterID, speed, id);
            }

            public string GetJson()
            {
                string info = "{\"startPos\" : {\"x\" : \"" + m_startPosition.x + "\", \"y\" : \"" + m_startPosition.y + "\", \"z\" : \"" + m_startPosition.z + "\"}, \"direction\" : {\"x\" : \"" + m_startDirection.x + "\", \"y\" : \"" + m_startDirection.y + "\", \"z\" : \"" + m_startDirection.z + "\"}, \"speed\" : {\"x\" : \"" + m_startVelocity.x + "\", \"y\" : \"" + m_startVelocity.y + "\", \"z\" : \"" + m_startVelocity.z + "\"}, \"shooterID\" : \"" + m_shooter + "\", \"id\" : \"" + m_bulletID + "\"}";
                return info;
            }

            public override bool Equals(object obj)
            {
                return ((BulletInfo)obj).m_bulletID == m_bulletID;
            }

            public override int GetHashCode()
            {
                return m_bulletID.GetHashCode();
            }
        }

        void OnCollisionEnter(Collision aCollision)
        {
            if (m_bulletInfo.m_isMaster)
            {
                if (aCollision.gameObject.GetComponent<PlaneController>() != null)
                {
                    m_bulletInfo.gameObject.transform.parent = aCollision.gameObject.transform;
                    Vector3 relativeHitPoint = m_bulletInfo.gameObject.transform.localPosition;
                    m_bulletInfo.gameObject.transform.parent = null;
                    BombersNetworkManager.m_localPlayer.BulletHitPlayerCommand(m_bulletInfo.GetJson(), relativeHitPoint, aCollision.gameObject.GetComponent<PlaneController>().m_playerID);
                }
                else
                {
                   
                }
                BombersNetworkManager.m_localPlayer.DeleteBulletCommand(m_bulletInfo.GetJson());
            }
        }

        void Start()
        {
            m_lifeTime = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_bulletLifeTime;
        }

        public void SetBulletInfo(BulletInfo aBulletInfo)
        {
            m_bulletInfo = aBulletInfo;
            m_bulletInfo.gameObject = this.gameObject;
        }

        public BulletInfo GetBulletInfo()
        {
            return m_bulletInfo;
        }

        void Update()
        {
            m_lifeTime -= Time.deltaTime;
            if (m_lifeTime <= 0)
            {
                if (m_bulletInfo.m_isMaster)
                {
                    BombersNetworkManager.m_localPlayer.DeleteBulletCommand(m_bulletInfo.GetJson());
                }
                
            }
            
        }
    }
}