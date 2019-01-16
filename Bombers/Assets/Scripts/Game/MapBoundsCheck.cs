using UnityEngine;
using System.Collections;
using BrainCloudPhotonExample.Game.PlayerInput;
using Photon.Pun;

namespace BrainCloudPhotonExample.Game
{
    public class MapBoundsCheck : MonoBehaviour, IPunObservable
    {
        public GameObject m_playerPlane;

        void Update()
        {
            if (m_playerPlane != null)
            {
                if (!GetComponent<Collider>().bounds.Contains(m_playerPlane.transform.position))
                {
                    GameObject.Find("PlayerController").GetComponent<PlayerController>().LeftBounds();
                }
                else
                {
                    GameObject.Find("PlayerController").GetComponent<PlayerController>().EnteredBounds();
                }
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            
        }
    }
}