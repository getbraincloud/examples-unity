using UnityEngine;
using System.Collections;

public class DestroyAfterSeconds : MonoBehaviour {

    public float m_lifeTime = 0;

	void Update () 
    {
        m_lifeTime -= Time.deltaTime;
        if (m_lifeTime <= 0)
        {
            Destroy(gameObject);
        }
	}
}
