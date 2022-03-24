using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseTroop : MonoBehaviour, IPrimaryAction, IDamageable<float>
{
    public GameObject DeathFX;
    public float StartingHealth = 100;
    public float DetectionRadius = 50;
    public LayerMask DetectionMask;
    protected GameObject _target;
    private float _health;
    private bool _isDead;
    public float Health { get => _health; set => _health = value; }

    protected float _delayBeforeDestroy = 2;
    
    // Start is called before the first frame update
    void Start()
    {
        _health = StartingHealth;
    }

    void FixedUpdate()
    {
        
    }
    
    public void PerformAction() { }
    
    public void Damage(float damageTaken)
    {
        if (_health <= 0) return;

        _health -= damageTaken;
        if (_health <= 0)
        {
            Dead();
        }
    }

    public void Dead()
    {
        if (_isDead) return;
        _isDead = true;
        StartCoroutine(DelayToDeath());
    }

    private IEnumerator DelayToDeath()
    {
        yield return new WaitForSeconds(_delayBeforeDestroy);
        Instantiate(DeathFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    protected void FindTarget()
    {
        Collider[] hitColliders = new Collider[10];
        int numOfColliders = Physics.OverlapSphereNonAlloc(transform.position, DetectionRadius, hitColliders);
        float shortestDistance = Mathf.Infinity;
        float distance = 0;
        for (int i = 0; i < numOfColliders; i++)
        {
            distance = Vector2.Distance(transform.position, hitColliders[i].transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                _target = hitColliders[i].gameObject;
            }
        }
    }
}
