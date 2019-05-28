using Gameframework;
using UnityEngine;

namespace BrainCloudUNETExample.Game
{
    public class OffScreenController : BaseBehaviour
    {
        [SerializeField]
        private Transform m_rotatePtr = null;
        [SerializeField]
        private Transform m_staticTransform = null;

        public void Init(BombersPlayerController in_target, BombersPlayerController in_parent)
        {
            initHelper(in_target.m_team, in_parent);
            m_target = in_target.m_playerPlane.transform;
        }

        public void Init(ShipController in_target, BombersPlayerController in_parent)
        {
            initHelper(in_target.m_team, in_parent);
            m_target = in_target.transform;
        }

        private void initHelper(int in_team, BombersPlayerController in_parent)
        {
            m_parentController = in_parent;
            m_reticle = gameObject.transform.Find("Pointer").gameObject;
        }

        void LateUpdate()
        {
            PlaneController planeTarget = m_target != null ? m_target.GetComponent<PlaneController>() : null;
            ShipController shipTarget = m_target != null ? m_target.GetComponent<ShipController>() : null;

            if (m_target == null
                || (planeTarget != null &&
                 (!planeTarget.PlayerController.m_planeActive ||
                  !planeTarget.PlayerController.WeaponController.HasBombs()))
                || (shipTarget != null &&
                    (shipTarget.GetActiveShipTarget() == null || !m_parentController.WeaponController.HasBombs()))
               )
            {
                Destroy(gameObject);
                return;
            }

            Vector3 targetPos = m_target.position;
            targetPos.z = transform.position.z; // make same linear plane as myself, so it doesn't rotate strangely
            m_rotatePtr.right = targetPos - transform.position;

            // Hide the reticle when the target is a ship and its distance is closer than LOW_TARGET_DISTANCE_SQR
            bool isWithinDistance = (targetPos - transform.position).sqrMagnitude < LOW_TARGET_DISTANCE_SQR;
            m_reticle.SetActive(!isWithinDistance);

            Vector3 eulerAngles = m_rotatePtr.eulerAngles;
            eulerAngles.z += 90.0f;
            m_rotatePtr.eulerAngles = eulerAngles;

            if (m_staticTransform)
                m_staticTransform.up = Vector3.up;
        }

        private GameObject m_reticle = null;
        private Transform m_target = null;
        private BombersPlayerController m_parentController;
        private const float LOW_TARGET_DISTANCE_SQR = 22500.0f;
    }
}