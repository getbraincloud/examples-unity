using System.Collections;
using UnityEngine;

public class ProjectileMovement : MonoBehaviour
{
    public float Speed;
    public int DamageAmount;
    public float LifeTimeDuration = 5;
    public GameObject ExplosionFX;
    
    private Rigidbody _rigidbody;
    private int _teamID;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _teamID = gameObject.layer == 6 ? 0 : 1;
    }

    private void OnEnable()
    {
        _rigidbody.velocity = transform.forward * Speed;
        StartCoroutine(DelayToDestroy());
    }

    private IEnumerator DelayToDestroy()
    {
        yield return new WaitForSeconds(LifeTimeDuration);

        GameManager.Instance.Projectiles.Remove(gameObject);
        Destroy(gameObject);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        var troop = other.GetComponent<TroopAI>();
        if (troop != null)
        {
            if (troop.TeamID != _teamID)
            {
                troop.Damage(DamageAmount);
                DestroyProjectile();
            }
            return;    
        }

        var damageable = other.GetComponent<BaseHealthBehavior>();
        if (damageable != null)
        {
            damageable.Damage(DamageAmount);
            DestroyProjectile();
        }
    }

    private void DestroyProjectile()
    {
        if (ExplosionFX)
        {
            Instantiate(ExplosionFX, transform.position, Quaternion.identity);
        }
            
        GameManager.Instance.Projectiles.Remove(gameObject);
        Destroy(gameObject);
    }
}
