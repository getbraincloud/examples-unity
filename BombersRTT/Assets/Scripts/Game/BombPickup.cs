using UnityEngine;
using Gameframework;

namespace BrainCloudUNETExample.Game
{
    public class BombPickup : MonoBehaviour
    {
        public int m_pickupID;
        private bool m_isActive = false;
        private float m_lifeTime = 5;


        void onCollision(Collider aOther)
        {
            if (!m_isActive) return;
            PlaneController pController = aOther.GetComponent<PlaneController>();
            if (pController != null)
            {
                pController.PlayerController.BombPickedUpCommand(pController.PlayerController.NetId, m_pickupID);
                m_isActive = false;
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter(Collider aOther)
        {
            onCollision(aOther);
        }

        void OnTriggerStay(Collider aOther)
        {
            onCollision(aOther);
        }

        public void Activate(int aBombID)
        {
            m_lifeTime = GConfigManager.GetFloatValue("BombPickupLifeTime");
            m_pickupID = aBombID;
            m_isActive = true;
            GetComponent<Rigidbody>().AddForce(GetRandomDirection() * 22, ForceMode.Impulse);
        }

        Vector3 GetRandomDirection()
        {
            Vector3 randomDirection = Vector3.up;
             float num = 0;
            if (((((4 * m_pickupID) + 1) % 9) + 1) == 0)
                num = 0.01f;
            else
            num = ((((4 * m_pickupID) + 1) % 9) + 1);
            randomDirection = Quaternion.Euler(new Vector3(0, 0, 360 / num)) * randomDirection;

            return randomDirection.normalized;
        }

        void FixedUpdate()
        {
            m_lifeTime -= Time.fixedDeltaTime;
            if (m_lifeTime <= 0 && m_isActive)
            {
                BombersNetworkManager.LocalPlayer.BombPickedUpCommand(-1, m_pickupID);
                m_isActive = false;
                return;
            }
            else if (m_lifeTime <= 0 && !m_isActive)
            {
                Destroy(gameObject);
            }
            Bounds mapBounds = GameObject.Find("MapBounds").GetComponent<Collider>().bounds;
            Vector3 position = transform.position;
            if (position.x < mapBounds.min.x + 25)
            {
                position.x = mapBounds.min.x + 25;
            }
            else if (position.x > mapBounds.max.x - 25)
            {
                position.x = mapBounds.max.x - 25;
            }

            if (position.y < mapBounds.min.y + 25)
            {
                position.y = mapBounds.min.y + 25;
            }
            else if (position.y > mapBounds.max.y - 25)
            {
                position.y = mapBounds.max.y - 25;
            }

            transform.position = position;

        }
    }
}
