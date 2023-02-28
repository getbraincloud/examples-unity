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
            GameObject proj = Instantiate(Bullet, SpawnPoints[i].position, SpawnPoints[i].rotation);
            proj.layer = collisionLayer;
            GameManager.Instance.Projectiles.Add(proj);
        }
    }
}
