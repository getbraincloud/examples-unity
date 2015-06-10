using UnityEngine;
using System.Collections;
using BrainCloudPhotonExample.Game.PlayerInput;

namespace BrainCloudPhotonExample.Game
{
    public class MapBoundsCheck : MonoBehaviour
    {

        void OnTriggerEnter(Collider other)
        {
            if (transform.parent.GetComponent<PhotonView>().isMine)
                GameObject.Find("PlayerController").GetComponent<PlayerController>().EnteredBounds();
        }

        void OnTriggerExit(Collider other)
        {
            if (transform.parent.GetComponent<PhotonView>().isMine)
                GameObject.Find("PlayerController").GetComponent<PlayerController>().LeftBounds();
        }

    }
}