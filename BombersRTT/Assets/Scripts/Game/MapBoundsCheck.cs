using UnityEngine;

namespace BrainCloudUNETExample.Game
{
    public class MapBoundsCheck : MonoBehaviour
    {
        void Update()
        {
            BombersPlayerController controller = null;
            for (int i = 0; i < BombersNetworkManager.LobbyInfo.Members.Count; ++i)
            {
                controller = BombersNetworkManager.LobbyInfo.Members[i].PlayerController;
                if (controller != null && controller.m_planeActive && controller.m_playerPlane != null && 
                    (controller.IsLocalPlayer || (controller.m_playerPlane.IsServerBot && controller.IsServer)))
                {
                    if (!GetComponent<Collider>().bounds.Contains(controller.m_playerPlane.transform.position))
                    {
                        controller.LeftBounds();
                    }
                    else
                    {
                        controller.EnteredBounds();
                    }
                }
            }
        }
    }
}
