using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseHealthBehavior : MonoBehaviour
{
    public GameObject DeathFX;
    public int StartingHealth = 100;
    public int EntityID;
    protected HealthBar _healthBar;
    protected int _currentHealth;
    protected float _delayBeforeDestruction = 1;

    public int Health { get => _currentHealth; set => _currentHealth = value; }
    private void Start()
    {
        _currentHealth = StartingHealth;
        _healthBar = GetComponentInChildren<HealthBar>();
        if (_healthBar)
        {
            _healthBar.SetMaxHealth(_currentHealth);
        }
    }

    public  void Damage(int damageTaken)
    {
        if (_currentHealth <= 0) return;

        _currentHealth -= damageTaken;

        if (_healthBar)
        {
            _healthBar.SetHealth(_currentHealth);
        }
        
        if (_currentHealth <= 0)
        {
            Dead();
        }
    }

    public virtual void Dead()
    {
        StartCoroutine(DelayToDestroy());
    }

    public virtual void LaunchObject(Vector3 direction)
    {
        //do nothing cause this is a structure
    }

    IEnumerator DelayToDestroy()
    {
        yield return new WaitForSeconds(_delayBeforeDestruction);
        if (DeathFX)
        {
            Instantiate(DeathFX, transform.position, Quaternion.identity);
        }

        if (!GameManager.Instance.IsInPlaybackMode)
        {
            BrainCloudManager.Instance.RecordTargetDestroyed(EntityID, -1);    
        }
        
        Destroy(gameObject);
    }
}
