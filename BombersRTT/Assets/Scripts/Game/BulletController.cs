using UnityEngine;
using Gameframework;
using System.Collections.Generic;

namespace BrainCloudUNETExample.Game
{
    public class BulletController : BaseNetworkBehavior
    {
        [SerializeField]
        private float m_lifeTime = 2.5f;
        private float m_lifeTimeOffset = 0.0f;

        public void setLifeTimeOffset(float in_offset)
        {
            m_lifeTimeOffset = in_offset;
            m_lifeTime += m_lifeTimeOffset;
        }

        private BulletInfo m_bulletInfo;
        void OnCollisionEnter(Collision aCollision)
        {
            if (IsServer)
            {
                PlaneController planeController = aCollision.gameObject.GetComponent<PlaneController>();
                if (planeController != null)
                {
                    m_bulletInfo.gameObject.transform.parent = aCollision.gameObject.transform;
                    Vector3 relativeHitPoint = m_bulletInfo.gameObject.transform.localPosition;
                    m_bulletInfo.gameObject.transform.parent = null;
                    m_bulletInfo.m_hitId = planeController.NetId;
                    planeController.transform.parent.GetComponent<BombersPlayerController>().BulletHitPlayerCommand(m_bulletInfo, relativeHitPoint );
                }
                BombersNetworkManager.LocalPlayer.DeleteBulletCommand(m_bulletInfo);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        protected override void Start()
        {
            _classType = BombersNetworkManager.BULLET_CONTROLLER;
            m_lifeTime = GConfigManager.GetFloatValue("BulletLifeTime") + m_lifeTimeOffset;
            base.Start();
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
                if (IsServer)
                {
                    BombersNetworkManager.LocalPlayer.DeleteBulletCommand(m_bulletInfo);
                }
                else
                {
                    // Hide it on the client, but don't destroy it. So it is hidden at the same time as on the server
                    gameObject.SetActive(false);
                }
            }
        }
    }

    #region BulletInfo
    public class BulletInfo
    {
        public Vector3 m_startPosition;
        public Vector3 m_startDirection;
        public short m_shooter;
        public short m_hitId;
        public Vector3 m_startVelocity;
        public int m_bulletID;
        public GameObject gameObject;
        public double m_lastPing;

        public BulletInfo(Vector3 aStartPos, Vector3 aStartDir, short aPlayer, Vector3 aSpeed, int aID = 0, double lastPing = 0.0)
        {
            m_startPosition = aStartPos;
            m_startDirection = aStartDir;
            m_shooter = aPlayer;
            m_startVelocity = aSpeed;
            m_bulletID = aID;
            m_lastPing = lastPing;
        }

        public BulletInfo(Dictionary<string, object> info)
        {
            m_startPosition = Vector3.zero;
            m_startDirection = Vector3.zero;
            m_startVelocity = Vector3.zero;

            m_startPosition.x = BaseNetworkBehavior.ConvertToFloat(info, BaseNetworkBehavior.POSITION_X);
            m_startPosition.y = BaseNetworkBehavior.ConvertToFloat(info, BaseNetworkBehavior.POSITION_Y);
            m_startPosition.z = BaseNetworkBehavior.ConvertToFloat(info, BaseNetworkBehavior.POSITION_Z);

            m_startDirection.x = BaseNetworkBehavior.ConvertToFloat(info, BaseNetworkBehavior.DIRECTION_X);
            m_startDirection.y = BaseNetworkBehavior.ConvertToFloat(info, BaseNetworkBehavior.DIRECTION_Y);

            m_startVelocity.x = BaseNetworkBehavior.ConvertToFloat(info, BaseNetworkBehavior.SPEED_X);
            m_startVelocity.y = BaseNetworkBehavior.ConvertToFloat(info, BaseNetworkBehavior.SPEED_Y);

            m_bulletID = GConfigManager.ReadIntSafely(info, BaseNetworkBehavior.ID);

            m_shooter = System.Convert.ToInt16(info[BaseNetworkBehavior.SHOOTER_ID]);
            m_hitId = System.Convert.ToInt16(info[BaseNetworkBehavior.HIT_ID]);
            m_lastPing = BaseNetworkBehavior.ConvertToFloat(info, BaseNetworkBehavior.LAST_PING);
        }

        public static BulletInfo GetBulletInfo(string in_str)
        {
            Dictionary<string, object> info = BaseNetworkBehavior.DeserializeString(in_str, BombersNetworkManager.SPECIAL_INNER_JOIN, BombersNetworkManager.SPECIAL_INNER_SPLIT);
            return new BulletInfo(info);
        }

        public Dictionary<string, object> GetDict()
        {
            Dictionary<string, object> info = new Dictionary<string, object>();
            info[BaseNetworkBehavior.POSITION_X] = BaseNetworkBehavior.ConvertToShort(m_startPosition.x);
            info[BaseNetworkBehavior.POSITION_Y] = BaseNetworkBehavior.ConvertToShort(m_startPosition.y);
            info[BaseNetworkBehavior.POSITION_Z] = BaseNetworkBehavior.ConvertToShort(m_startPosition.z);

            if (m_startDirection.x != 0) info[BaseNetworkBehavior.DIRECTION_X] = BaseNetworkBehavior.ConvertToShort(m_startDirection.x);
            if (m_startDirection.y != 0) info[BaseNetworkBehavior.DIRECTION_Y] = BaseNetworkBehavior.ConvertToShort(m_startDirection.y);

            if (m_startVelocity.x != 0) info[BaseNetworkBehavior.SPEED_X] = BaseNetworkBehavior.ConvertToShort(m_startVelocity.x);
            if (m_startVelocity.y != 0) info[BaseNetworkBehavior.SPEED_Y] = BaseNetworkBehavior.ConvertToShort(m_startVelocity.y);
            if (m_startVelocity.z != 0) info[BaseNetworkBehavior.SPEED_Z] = BaseNetworkBehavior.ConvertToShort(m_startVelocity.z);

            info[BaseNetworkBehavior.SHOOTER_ID] = m_shooter;
            info[BaseNetworkBehavior.HIT_ID] = m_hitId;
            info[BaseNetworkBehavior.ID] = (int)m_bulletID;
            info[BaseNetworkBehavior.LAST_PING] = BaseNetworkBehavior.ConvertToShort(GCore.Wrapper.Client.RelayService.LastPing * 0.0001f);
            return info;
        }

        public string GetJson()
        {
            Dictionary<string, object> info = GetDict();
            string infoStr = BaseNetworkBehavior.SerializeDict(info, BombersNetworkManager.SPECIAL_INNER_JOIN, BombersNetworkManager.SPECIAL_INNER_SPLIT);
            return infoStr;
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
    #endregion
}