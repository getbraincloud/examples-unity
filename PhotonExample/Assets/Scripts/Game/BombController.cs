using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BrainCloudPhotonExample.Game
{
    public class BombController : MonoBehaviour
    {

        public class BombInfo
        {
            public Vector3 m_startPosition;
            public Vector3 m_startDirection;
            public PhotonPlayer m_shooter;
            public Vector3 m_startVelocity;
            public int m_bombID;
            public bool m_isMaster = false;
            public GameObject gameObject;

            public BombInfo(Vector3 aStartPos, Vector3 aStartDir, PhotonPlayer aPlayer, Vector3 aSpeed, int aID = 0)
            {
                m_startPosition = aStartPos;
                m_startDirection = aStartDir;
                m_shooter = aPlayer;
                m_startVelocity = aSpeed;
                m_bombID = aID;
            }

            public static byte[] SerializeBombInfo(object aBombInfo)
            {
                BombInfo bombInfo = (BombInfo)aBombInfo;
                byte[] bytes = new byte[sizeof(float) * 9 + sizeof(int) * 2];
                int index = 0;
                ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startPosition.x, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startPosition.y, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startPosition.z, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startDirection.x, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startDirection.y, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startDirection.z, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_shooter.ID, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startVelocity.x, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startVelocity.y, bytes, ref index);
                ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startVelocity.z, bytes, ref index);

                ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_bombID, bytes, ref index);

                return bytes;
            }

            public static object DeserializeBombInfo(byte[] bytes)
            {
                Vector3 startPos = Vector3.zero;
                Vector3 direction = Vector3.zero;
                PhotonPlayer shooter = PhotonNetwork.player;
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

                BombInfo bombInfo = new BombInfo(startPos, direction, shooter, speed, id);
                return bombInfo;
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

        void OnCollisionEnter(Collision aCollision)
        {
            if (!m_isActive) return;

            m_isActive = false;

            if (m_bombInfo.m_isMaster)
            {
                if (aCollision.gameObject.layer == 4)
                {
                    GameObject.Find("GameManager").GetComponent<GameManager>().DeleteBomb(m_bombInfo, 0);
                }
                else if (aCollision.gameObject.layer == 20) //it hit a rock
                {
                    GameObject.Find("GameManager").GetComponent<GameManager>().DeleteBomb(m_bombInfo, 1);
                }
                else //it hit a ship
                {
                    if (((int)m_bombInfo.m_shooter.customProperties["Team"] == 1 && aCollision.gameObject.layer == 16) || ((int)m_bombInfo.m_shooter.customProperties["Team"] == 2 && aCollision.gameObject.layer == 17))
                    {
                        GameObject.Find("GameManager").GetComponent<GameManager>().DeleteBomb(m_bombInfo, 2);
                    }
                    else
                    {
                        List<ShipController.ShipTarget> shipTargets = aCollision.transform.parent.parent.parent.gameObject.GetComponent<ShipController>().GetTargets();
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
                                    GameObject.Find("GameManager").GetComponent<GameManager>().HitShipTargetPoint(shipTargets[i], m_bombInfo);
                                }

                                if (!aCollision.transform.parent.parent.parent.gameObject.GetComponent<ShipController>().IsAlive())
                                {
                                    GameObject.Find("GameManager").GetComponent<GameManager>().DestroyedShip(aCollision.transform.parent.parent.parent.gameObject.GetComponent<ShipController>(), m_bombInfo);
                                    break;
                                }
                            }
                        }
                        GameObject.Find("GameManager").GetComponent<GameManager>().DeleteBomb(m_bombInfo, 1);
                    }
                }
            }
            else
            {
                if (aCollision.gameObject.layer == 4)
                {
                    GameObject explosion = (GameObject)Instantiate((GameObject)Resources.Load("BombWaterExplosion"), transform.position, Quaternion.identity);
                    explosion.GetComponent<AudioSource>().Play();
                }
                else if (((int)m_bombInfo.m_shooter.customProperties["Team"] == 1 && aCollision.gameObject.layer == 16) || ((int)m_bombInfo.m_shooter.customProperties["Team"] == 2 && aCollision.gameObject.layer == 17))
                {
                    GameObject explosion = (GameObject)Instantiate((GameObject)Resources.Load("BombDud"), transform.position, Quaternion.identity);
                    explosion.GetComponent<AudioSource>().Play();
                }
                else
                {
                    GameObject explosion = (GameObject)Instantiate((GameObject)Resources.Load("BombExplosion"), transform.position, Quaternion.identity);
                    explosion.GetComponent<AudioSource>().Play();
                }
            }
            transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().enabled = false;
        }

        private BombInfo m_bombInfo;
        private float m_bombRadius = 15f;
        public bool m_isActive = true;

        public void SetBombInfo(BombInfo aBombInfo)
        {
            m_bombInfo = aBombInfo;
            m_bombInfo.gameObject = this.gameObject;

            string teamBombPath = "";

            if ((int)m_bombInfo.m_shooter.customProperties["Team"] == 1)
            {
                teamBombPath = "Bomb01";
            }
            else
            {
                teamBombPath = "Bomb02";
            }

            GameObject graphic = (GameObject)Instantiate((GameObject)Resources.Load(teamBombPath), transform.position, transform.rotation);
            graphic.transform.parent = transform;
        }

        public BombInfo GetBombInfo()
        {
            return m_bombInfo;
        }

        void LateUpdate()
        {
            transform.rotation = Quaternion.LookRotation(GetComponent<Rigidbody>().velocity.normalized, transform.up);
        }
    }
}