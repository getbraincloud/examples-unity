using UnityEngine;
using System.Collections;

namespace BrainCloudPhotonExample
{
    public class ParticlesDestroyer : MonoBehaviour
    {

        [SerializeField]
        private float m_lifeTime;

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
