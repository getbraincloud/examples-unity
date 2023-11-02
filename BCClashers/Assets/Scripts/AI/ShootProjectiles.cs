using UnityEngine;

public class ShootProjectiles : MonoBehaviour
{
    public GameObject Bullet;
    public Transform SpawnPoint;

    public void SpawnProjectile(int collisionLayer)
    {
        GameObject proj = Instantiate(Bullet, SpawnPoint.position, SpawnPoint.rotation);
        proj.layer = collisionLayer;
        GameManager.Instance.Projectiles.Add(proj);
    }
}
