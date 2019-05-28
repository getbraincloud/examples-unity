using UnityEngine;
using System.Collections;

namespace BrainCloudUNETExample
{
    public class ParticlesDestroyer : MonoBehaviour
    {

        [SerializeField]
        public float m_lifeTime;

        void FixedUpdate()
        {
            m_lifeTime -= Time.deltaTime;

            if (m_lifeTime <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
