﻿using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Gameframework;

namespace BrainCloudUNETExample.Game
{
    public class ShipController : BaseBehaviour//NetworkBehaviour
    {
        public enum eShipType
        {
            SHIP_TYPE_NONE,
            SHIP_TYPE_CARRIER,
            SHIP_TYPE_BATTLESHIP,
            SHIP_TYPE_CRUISER,
            SHIP_TYPE_PATROLBOAT,
            SHIP_TYPE_DESTROYER,
            SHIP_TYPE_PATROL_BOAT,
        }

        public ShipTarget GetActiveShipTarget()
        {
            ShipTarget activeTarget = null;
            for (int i = 0; i < m_shipTargets.Count; ++i)
            {
                if (m_shipTargets[i].m_isAlive)
                {
                    activeTarget = m_shipTargets[i];
                    break;
                }
            }
            return activeTarget;
        }

        private List<ShipTarget> m_shipTargets;
        
        private GameObject m_shipPrefab;

        private eShipType m_shipType = eShipType.SHIP_TYPE_NONE;

        private float m_respawnTime;

        public int m_team;
        public bool m_isAlive = true;
        private Vector3 m_startPosition = new Vector3(-1, -1, -1);

        //[SyncVar]
        private float m_startAngle = -1;
        public int m_shipID;

        public bool ContainsShipTarget(ShipTarget aShipTarget)
        {
            return m_shipTargets.Contains(aShipTarget);
        }

        public ShipTarget GetShipTarget(ShipTarget aShipTarget)
        {

            return m_shipTargets[aShipTarget.m_index];
        }

        public eShipType GetShipType()
        {
            return m_shipType;
        }

        public void StartRespawn()
        {
            if (m_respawnTime != -1)
            {
                StartCoroutine("Respawn");
            }
        }

        IEnumerator Respawn()
        {
            float time = m_respawnTime;
            while (time > 0)
            {
                time -= Time.deltaTime;
                yield return YieldFactory.GetWaitForEndOfFrame();
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().CmdRespawnShip(this.m_shipID);
        }
        
        public List<ShipTarget> GetTargets()
        {
            return m_shipTargets;
        }

        public void SetShipType(eShipType aShipType, int aTeam, int aShipID)
        {
            RpcSetShipType1((int)aShipType, aTeam, aShipID);
        }

        public void SetShipType(eShipType aShipType, int aTeam, int aShipID, float aAngle, Vector3 aPosition, float aRespawnTime, Vector3[] aPath, float aPathSpeed)
        {
            RpcSetShipType2((int)aShipType, aTeam, aShipID, aAngle, aPosition, aRespawnTime, aPath, aPathSpeed);
        }

        //[ClientRpc]
        void RpcSetShipType1(int aShipType, int aTeam, int aShipID)
        {
            if (m_startPosition == new Vector3(-1, -1, -1))
            {
                m_startPosition = transform.position;
                m_startAngle = transform.eulerAngles.z;
                transform.rotation = Quaternion.Euler(0, 0, m_startAngle);
                m_respawnTime = -1;

                m_team = aTeam;
                GameObject.Find("GameManager").GetComponent<GameManager>().AddSpawnedShip(this);
                m_shipType = (eShipType)aShipType;
                m_shipTargets = new List<ShipTarget>();
                m_shipID = aShipID;
                int index = 1;
                GameObject graphic = null;
                string path = "";
                switch (m_shipType)
                {
                    case eShipType.SHIP_TYPE_CARRIER:
                        path = "Carrier0" + aTeam;
                        break;
                    case eShipType.SHIP_TYPE_BATTLESHIP:
                        path = "Battleship0" + aTeam;
                        break;
                    case eShipType.SHIP_TYPE_CRUISER:
                        path = "Cruiser0" + aTeam;
                        break;
                    case eShipType.SHIP_TYPE_PATROLBOAT:
                        path = "PatrolBoat0" + aTeam;
                        break;
                    case eShipType.SHIP_TYPE_DESTROYER:
                        path = "Destroyer0" + aTeam;
                        break;
                }


                m_shipPrefab = (GameObject)Resources.Load("Prefabs/Game/" + path);
                graphic = (GameObject)Instantiate(m_shipPrefab, transform.Find("ShipGraphic").position, transform.Find("ShipGraphic").rotation);
                graphic.transform.parent = transform.Find("ShipGraphic");

                if (aTeam == 1)
                {
                    graphic.transform.Find("Graphic").gameObject.layer = 16;
                }
                else
                {
                    graphic.transform.Find("Graphic").gameObject.layer = 17;
                }

                bool done = false;
                path = "";
                while (!done)
                {

                    path = "TargetPosition" + index;
                    Transform target = graphic.transform.Find(path);

                    if (target != null)
                    {
                        GameObject targetGraphic = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "TargetPosition"), target.position, target.rotation);
                        m_shipTargets.Add(new ShipTarget(targetGraphic, target, m_shipID, index - 1));
                    }
                    else
                    {
                        done = true;
                        break;
                    }

                    index++;
                }
            }
            else
            {
                m_team = aTeam;
                transform.position = m_startPosition;
                transform.rotation = Quaternion.Euler(0, 0, m_startAngle);

                GameObject.Find("GameManager").GetComponent<GameManager>().AddSpawnedShip(this);
                m_shipType = (eShipType)aShipType;
                m_shipTargets = new List<ShipTarget>();
                m_shipID = aShipID;
                int index = 1;
                GameObject graphic = transform.GetChild(0).GetChild(0).gameObject;
                m_shipTargets.Clear();
                bool done = false;
                string path = "";
                while (!done)
                {
                    path = "TargetPosition" + index;
                    Transform target = graphic.transform.Find(path);

                    if (target != null)
                    {
                        GameObject targetGraphic = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "TargetPosition"), target.position, target.rotation);
                        m_shipTargets.Add(new ShipTarget(targetGraphic, target, m_shipID, index - 1));
                    }
                    else
                    {
                        done = true;
                        break;
                    }

                    index++;
                }
                ParticleSystem[] effects = transform.GetComponentsInChildren<ParticleSystem>();
                for (int i = 0; i < effects.Length; i++)
                {
                    Destroy(effects[i].gameObject);
                }
            }

        }

        //[ClientRpc]
        void RpcSetShipType2(int aShipType, int aTeam, int aShipID, float aAngle, Vector3 aPosition, float aRespawnTime, Vector3[] aPath, float aPathSpeed)
        {
            m_team = aTeam;
            m_startPosition = aPosition;
            m_startAngle = aAngle;
            m_respawnTime = aRespawnTime;

            transform.position = m_startPosition;
            transform.rotation = Quaternion.Euler(0, 0, m_startAngle);

            GameObject.Find("GameManager").GetComponent<GameManager>().AddSpawnedShip(this);
            m_shipType = (eShipType)aShipType;
            m_shipTargets = new List<ShipTarget>();
            m_shipID = aShipID;
            int index = 1;
            GameObject graphic = null;
            string path = "";
            switch (m_shipType)
            {
                case eShipType.SHIP_TYPE_CARRIER:
                    path = "Carrier0" + aTeam;
                    break;
                case eShipType.SHIP_TYPE_BATTLESHIP:
                    path = "Battleship0" + aTeam;
                    break;
                case eShipType.SHIP_TYPE_CRUISER:
                    path = "Cruiser0" + aTeam;
                    break;
                case eShipType.SHIP_TYPE_PATROLBOAT:
                    path = "PatrolBoat0" + aTeam;
                    break;
                case eShipType.SHIP_TYPE_DESTROYER:
                    path = "Destroyer0" + aTeam;
                    break;
            }


            m_shipPrefab = (GameObject)Resources.Load("Prefabs/Game/" + path);
            graphic = (GameObject)Instantiate(m_shipPrefab, transform.Find("ShipGraphic").position, transform.Find("ShipGraphic").rotation);
            graphic.transform.parent = transform.Find("ShipGraphic");

            if (aTeam == 1)
            {
                graphic.transform.Find("Graphic").gameObject.layer = 16;
            }
            else
            {
                graphic.transform.Find("Graphic").gameObject.layer = 17;
            }

            bool done = false;
            path = "";
            while (!done)
            {

                path = "TargetPosition" + index;
                Transform target = graphic.transform.Find(path);

                if (target != null)
                {
                    GameObject targetGraphic = (GameObject)Instantiate((GameObject)Resources.Load("Prefabs/Game/" + "TargetPosition"), target.position, target.rotation);
                    m_shipTargets.Add(new ShipTarget(targetGraphic, target, m_shipID, index - 1));
                }
                else
                {
                    done = true;
                    break;
                }

                index++;
            }
        }

        public bool IsAlive()
        {
            bool isAlive = false;

            for (int i = 0; i < m_shipTargets.Count; i++)
            {
                if (m_shipTargets[i].m_isAlive)
                {
                    isAlive = true;
                }
            }

            return isAlive && m_isAlive;
        }

        
    }

    public class ShipTarget
    {
        public int m_shipID;
        public int m_index;
        public Transform m_position;
        public float m_radius = 6;
        public bool m_isAlive = false;
        public GameObject m_targetGraphic;
        public GameObject gameObject;

        public ShipTarget(GameObject aGraphic, Transform aPosition, int aShipID, int aIndex)
        {
            m_position = aPosition;
            m_shipID = aShipID;
            m_index = aIndex;
            m_isAlive = true;
            m_targetGraphic = aGraphic;
            gameObject = aPosition.gameObject;
            m_targetGraphic.transform.parent = m_position;
            m_targetGraphic.transform.localScale = new Vector3(m_radius / 2, m_radius / 2, m_radius / 2);

        }

        public ShipTarget(int aShipID, int aIndex)
        {
            m_shipID = aShipID;
            m_index = aIndex;
        }

        public ShipTarget(int[] aShipTarget)
        {
            m_shipID = aShipTarget[0];
            m_index = aShipTarget[1];
        }

        public override bool Equals(object obj)
        {
            return ((ShipTarget)obj).m_shipID == m_shipID && ((ShipTarget)obj).m_index == m_index;
        }

        public override int GetHashCode()
        {
            return m_shipID.GetHashCode() ^ m_index.GetHashCode();
        }
    }
}