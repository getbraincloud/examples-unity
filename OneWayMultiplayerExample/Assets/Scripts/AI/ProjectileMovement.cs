using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileMovement : MonoBehaviour
{
    public float Speed;
    public int DamageAmount;
    public GameObject ExplosionFX;
    private Rigidbody _rigidbody;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _rigidbody.velocity = transform.forward * Speed;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        var damageable = other.GetComponent<IDamageable<int>>();

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
