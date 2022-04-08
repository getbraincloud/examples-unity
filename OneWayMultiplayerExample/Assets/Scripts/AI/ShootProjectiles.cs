using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootProjectiles : MonoBehaviour
{
    public GameObject Bullet;
    public Transform[] SpawnPoints;
    private bool _canShoot = true;
    
    public void SpawnProjectile(int collisionLayer, GameObject target)
    {
        if (!_canShoot) return;

        for (int i = 0; i < SpawnPoints.Length; i++)
        {
            var direction = (SpawnPoints[i].position - target.transform.position).normalized;
            SpawnPoints[i].rotation = Quaternion.LookRotation(-direction);
            
            GameObject proj = Instantiate(Bullet, SpawnPoints[i].position, SpawnPoints[i].rotation);
            proj.layer = collisionLayer;
        }

        //StartCoroutine(DelayToShootAgain());
    }
}
