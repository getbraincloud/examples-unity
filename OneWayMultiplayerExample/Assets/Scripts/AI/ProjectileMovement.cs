using System.Collections;
using UnityEngine;

public class ProjectileMovement : MonoBehaviour
{
    public float Speed;
    public int DamageAmount;
    public float LifeTimeDuration = 5;
    public GameObject ExplosionFX;
    private Rigidbody _rigidbody;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _rigidbody.velocity = transform.forward * Speed;
        Destroy(gameObject, LifeTimeDuration);
    }

    IEnumerator DelayToDestroy()
    {
        yield return new WaitForSeconds(LifeTimeDuration);

        GameManager.Instance.Projectiles.Remove(gameObject);
        Destroy(gameObject);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        var damageable = other.GetComponent<BaseHealthBehavior>();

        if (damageable != null)
        {
            damageable.Damage(DamageAmount);
            if (ExplosionFX)
            {
                Instantiate(ExplosionFX, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}
