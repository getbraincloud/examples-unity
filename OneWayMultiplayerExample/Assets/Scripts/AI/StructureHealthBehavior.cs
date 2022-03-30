using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureHealthBehavior : MonoBehaviour, IDamageable<int>
{
    public GameObject DeathFX;
    public int StartingHealth = 100;

    private HealthBar _healthBar;
    private int _currentHealth;
    private float _delayBeforeDestruction = 1;
    
    void Awake()
    {
        _currentHealth = StartingHealth;
        _healthBar = GetComponentInChildren<HealthBar>();
        if (_healthBar)
        {
            _healthBar.SetMaxHealth(_currentHealth);
        }
    }

    public void Damage(int damageTaken)
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

    public void Dead()
    {
        StartCoroutine(DelayToDestroy());
    }

    IEnumerator DelayToDestroy()
    {
        yield return new WaitForSeconds(_delayBeforeDestruction);
        if (DeathFX)
        {
            Instantiate(DeathFX, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
