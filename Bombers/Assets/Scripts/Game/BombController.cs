using UnityEngine;
using System.Collections.Generic;
using Gameframework;

namespace BrainCloudUNETExample.Game
{
    public class BombController : BaseNetworkBehavior
    {
        protected override void Start()
        {
            _classType = BombersNetworkManager.BOMB_CONTROLLER;
            if (!IsServer)
            {
                // take the collision off
                gameObject.layer = 0;
            }
            base.Start();
        }

        private BombInfo m_bombInfo = null;
        public BombInfo BombInfo { get { return m_bombInfo; } set { m_bombInfo = value; m_bombInfo.gameObject = this.gameObject; } }
        public bool m_isActive = true;

        void LateUpdate()
        {
            transform.rotation = Quaternion.LookRotation(GetComponent<Rigidbody>().velocity.normalized, transform.up);
        }

        void OnCollisionEnter(Collision aCollision)
        {
            m_isActive = false;

            if (IsServer)
            {
                if (aCollision.gameObject.layer == 4)
                {
                    BombersNetworkManager.LocalPlayer.DeleteBombCommand(this, 0);
                }
                else if (aCollision.gameObject.layer == 20) //it hit a rock
                {
                    BombersNetworkManager.LocalPlayer.DeleteBombCommand(this, 1);
                }
                else //it hit a ship
                {
                    BombersPlayerController shooterController = BombersPlayerController.GetPlayer(BombInfo.m_shooter);
                    if ((shooterController.m_team == 1 && aCollision.gameObject.layer == 16) || (shooterController.m_team == 2 && aCollision.gameObject.layer == 17))
                    {
                        BombersNetworkManager.LocalPlayer.DeleteBombCommand(this, 2);
                    }
                    else
                    {
                        ShipController controller = aCollision.transform.parent.parent.parent.gameObject.GetComponent<ShipController>();
                        List<ShipTarget> shipTargets = controller.GetTargets();
                        for (int i = 0; i < shipTargets.Count; i++)
                        {
                            if ((transform.position - shipTargets[i].m_position.position).magnitude <= m_bombRadius + shipTargets[i].m_radius)
                            {
                                if (!shipTargets[i].m_isAlive)
                                {
                                    continue;
                                }
                                else
                                {
                                    shipTargets[i].m_isAlive = false;
                                    BombersNetworkManager.LocalPlayer.HitShipTargetPointCommand(shipTargets[i].m_shipID, shipTargets[i].m_index, m_bombInfo.GetJson());
                                }

                                if (!aCollision.transform.parent.parent.parent.gameObject.GetComponent<ShipController>().IsAlive())
                                {
                                    BombersNetworkManager.LocalPlayer.DestroyShipCommand(controller.m_shipID, m_bombInfo.GetJson());
                                    break;
                                }
                            }
                        }
                        BombersNetworkManager.LocalPlayer.DeleteBombCommand(this, 1);
                    }
                }
            }
        }
        private float m_bombRadius = 15f;
    }

    public class BombInfo
    {
        public Vector3 m_startPosition;
        public Vector3 m_startDirection;
        public short m_shooter;
        public Vector3 m_startVelocity;
        public int m_bombID;
        public GameObject gameObject;
        public string m_bombInfoJson;

        public BombInfo(Vector3 aStartPos, Vector3 aStartDir, short aPlayer, Vector3 aSpeed, int aID = 0)
        {
            m_startPosition = aStartPos;
            m_startDirection = aStartDir;
            m_shooter = aPlayer;
            m_startVelocity = aSpeed;
            m_bombID = aID;
        }

        public BombInfo(Dictionary<string, object> info)
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

            m_shooter = System.Convert.ToInt16(info[BaseNetworkBehavior.SHOOTER_ID]);
            m_bombID = GConfigManager.ReadIntSafely(info, BaseNetworkBehavior.ID);
        }

        public static BombInfo GetBombInfo(string in_str)
        {
            Dictionary<string, object> info = BaseNetworkBehavior.DeserializeString(in_str, BombersNetworkManager.SPECIAL_INNER_JOIN, BombersNetworkManager.SPECIAL_INNER_SPLIT);
            return new BombInfo(info);
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

            info[BaseNetworkBehavior.SHOOTER_ID] = m_shooter;
            info[BaseNetworkBehavior.ID] = m_bombID;
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
            return ((BombInfo)obj).m_bombID == m_bombID;
        }

        public override int GetHashCode()
        {
            return m_bombID.GetHashCode();
        }
    }
}