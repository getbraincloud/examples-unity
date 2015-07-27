using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using BrainCloudUNETExample.Game.PlayerInput;

namespace BrainCloudUNETExample.Game
{
    public class BombController : MonoBehaviour
    {
        public class BombInfo
        {
            public Vector3 m_startPosition;
            public Vector3 m_startDirection;
            public int m_shooter;
            public Vector3 m_startVelocity;
            public int m_bombID;
            public bool m_isMaster = false;
            public GameObject gameObject;
            public string m_bombInfoJson;

            public BombInfo(Vector3 aStartPos, Vector3 aStartDir, int aPlayer, Vector3 aSpeed, int aID = 0)
            {
                m_startPosition = aStartPos;
                m_startDirection = aStartDir;
                m_shooter = aPlayer;
                m_startVelocity = aSpeed;
                m_bombID = aID;
            }

            public static BombInfo GetBombInfo(string aJson)
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

                return new BombInfo(startPos, direction, shooterID, speed, id);
            }

            public string GetJson()
            {
                string info = "{\"startPos\" : {\"x\" : \""+m_startPosition.x+"\", \"y\" : \""+m_startPosition.y+"\", \"z\" : \""+m_startPosition.z+"\"}, \"direction\" : {\"x\" : \""+m_startDirection.x+"\", \"y\" : \""+m_startDirection.y+"\", \"z\" : \""+m_startDirection.z+"\"}, \"speed\" : {\"x\" : \""+m_startVelocity.x+"\", \"y\" : \""+m_startVelocity.y+"\", \"z\" : \""+m_startVelocity.z+"\"}, \"shooterID\" : \""+m_shooter+"\", \"id\" : \""+m_bombID+"\"}";
                return info;
            }
            
            //public static byte[] SerializeBombInfo(object aBombInfo)
            //{
            //    BombInfo bombInfo = (BombInfo)aBombInfo;
            //    byte[] bytes = new byte[sizeof(float) * 9 + sizeof(int) * 2];
            //    int index = 0;
            //    ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startPosition.x, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startPosition.y, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startPosition.z, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startDirection.x, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startDirection.y, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startDirection.z, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_shooter.ID, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startVelocity.x, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startVelocity.y, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_startVelocity.z, bytes, ref index);

            //    ExitGames.Client.Photon.Protocol.Serialize(bombInfo.m_bombID, bytes, ref index);
            //    return bytes;
            //}

            //public static object DeserializeBombInfo(byte[] bytes)
            //{
            //    Vector3 startPos = Vector3.zero;
            //    Vector3 direction = Vector3.zero;
            //    PhotonPlayer shooter = PhotonNetwork.player;
            //    int shooterID = 0;
            //    Vector3 speed = Vector3.zero;
            //    int id = 0;

            //    int index = 0;
            //    ExitGames.Client.Photon.Protocol.Deserialize(out startPos.x, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Deserialize(out startPos.y, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Deserialize(out startPos.z, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Deserialize(out direction.x, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Deserialize(out direction.y, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Deserialize(out direction.z, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Deserialize(out shooterID, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Deserialize(out speed.x, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Deserialize(out speed.y, bytes, ref index);
            //    ExitGames.Client.Photon.Protocol.Deserialize(out speed.z, bytes, ref index);

            //    ExitGames.Client.Photon.Protocol.Deserialize(out id, bytes, ref index);

            //    shooter = shooter.Get(shooterID);

            //    BombInfo bombInfo = new BombInfo(startPos, direction, shooter, speed, id);
            //    return bombInfo;
            //}

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
                    BombersNetworkManager.m_localPlayer.DeleteBombCommand(m_bombInfo.GetJson(), 0);
                }
                else if (aCollision.gameObject.layer == 20) //it hit a rock
                {
                    BombersNetworkManager.m_localPlayer.DeleteBombCommand(m_bombInfo.GetJson(), 1);
                }
                else //it hit a ship
                {
                    if ((BombersPlayerController.GetPlayer(m_bombInfo.m_shooter).m_team == 1 && aCollision.gameObject.layer == 16) || (BombersPlayerController.GetPlayer(m_bombInfo.m_shooter).m_team == 2 && aCollision.gameObject.layer == 17))
                    {
                        BombersNetworkManager.m_localPlayer.DeleteBombCommand(m_bombInfo.GetJson(), 2);
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
                                    BombersNetworkManager.m_localPlayer.HitShipTargetPointCommand(shipTargets[i].m_shipID, shipTargets[i].m_index, m_bombInfo.GetJson());
                                }

                                if (!aCollision.transform.parent.parent.parent.gameObject.GetComponent<ShipController>().IsAlive())
                                {
                                    BombersNetworkManager.m_localPlayer.DestroyShipCommand(aCollision.transform.parent.parent.parent.gameObject.GetComponent<ShipController>().m_shipID, m_bombInfo.GetJson());
                                    break;
                                }
                            }
                        }
                        BombersNetworkManager.m_localPlayer.DeleteBombCommand(m_bombInfo.GetJson(), 1);
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
                else if ((BombersPlayerController.GetPlayer(m_bombInfo.m_shooter).m_team == 1 && aCollision.gameObject.layer == 16) || (BombersPlayerController.GetPlayer(m_bombInfo.m_shooter).m_team == 2 && aCollision.gameObject.layer == 17))
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

            if (BombersPlayerController.GetPlayer(m_bombInfo.m_shooter).m_team == 1)
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