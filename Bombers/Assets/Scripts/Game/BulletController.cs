using UnityEngine;
using System.Collections;
using BrainCloudPhotonExample.Connection;
using Photon.Pun;

namespace BrainCloudPhotonExample.Game
{
    public class BulletController : MonoBehaviour, IPunObservable
    {
        [SerializeField]
        private float m_lifeTime = 2.5f;

        private BulletInfo m_bulletInfo;

        public class BulletInfo : IPunObservable
        {
            public Vector3 m_startPosition;
            public Vector3 m_startDirection;
            public Photon.Realtime.Player m_shooter;
            public Vector3 m_startVelocity;
            public int m_bulletID;
            public bool m_isMaster = false;
            public GameObject gameObject;

            public BulletInfo(Vector3 aStartPos, Vector3 aStartDir, Photon.Realtime.Player aPlayer, Vector3 aSpeed, int aID = 0)
            {
                m_startPosition = aStartPos;
                m_startDirection = aStartDir;
                m_shooter = aPlayer;
                m_startVelocity = aSpeed;
                m_bulletID = aID;
            }

            public static byte[] SerializeBulletInfo(object aBulletInfo)
            {
                BulletInfo bulletInfo = (BulletInfo)aBulletInfo;
                byte[] bytes = new byte[sizeof(float) * 9 + sizeof(int) * 2];
                int index = 0;
                ExitGames.Client.Photon.Protocol.Serialize(bulletInfo.m_startPosition.x, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bulletInfo.m_startPosition.y, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bulletInfo.m_startPosition.z, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bulletInfo.m_startDirection.x, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bulletInfo.m_startDirection.y, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bulletInfo.m_startDirection.z, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bulletInfo.m_shooter.ActorNumber, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bulletInfo.m_startVelocity.x, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bulletInfo.m_startVelocity.y, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bulletInfo.m_startVelocity.z, bytes, ref index);

                ExitGames.Client.Photon.Protocol.Serialize(bulletInfo.m_bulletID, bytes, ref index);

                return bytes;
            }

            public static object DeserializeBulletInfo(byte[] bytes)
            {
                Vector3 startPos = Vector3.zero;
                Vector3 direction = Vector3.zero;
                Photon.Realtime.Player shooter = PhotonNetwork.LocalPlayer;
                int shooterID = 0;
                Vector3 speed = Vector3.zero;
                int id = 0;

                int index = 0;
                ExitGames.Client.Photon.Protocol.Deserialize(out startPos.x, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Deserialize(out startPos.y, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Deserialize(out startPos.z, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Deserialize(out direction.x, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Deserialize(out direction.y, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Deserialize(out direction.z, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Deserialize(out shooterID, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Deserialize(out speed.x, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Deserialize(out speed.y, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Deserialize(out speed.z, bytes, ref index);

                ExitGames.Client.Photon.Protocol.Deserialize(out id, bytes, ref index);

                shooter = shooter.Get(shooterID);

                BulletInfo bulletInfo = new BulletInfo(startPos, direction, shooter, speed, id);
                return bulletInfo;
            }

            public override bool Equals(object obj)
            {
                return ((BulletInfo)obj).m_bulletID == m_bulletID;
            }

            public override int GetHashCode()
            {
                return m_bulletID.GetHashCode();
            }

            public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
            {
                
            }
        }

        void OnCollisionEnter(Collision aCollision)
        {
            if (m_bulletInfo.m_isMaster)
            {
                if (aCollision.gameObject.GetComponent<PlaneController>() != null)
                    GameObject.Find("GameManager").GetComponent<GameManager>().BulletHitPlayer(m_bulletInfo, aCollision);
                else
                    GameObject.Find("GameManager").GetComponent<GameManager>().DeleteBullet(m_bulletInfo);
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
                GameObject.Find("GameManager").GetComponent<GameManager>().DeleteBullet(m_bulletInfo);
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
        }
    }
}