using System.Collections;
using UnityEngine;

public class BaseHealthBehavior : MonoBehaviour
{
    public GameObject DeathFX;
    public int StartingHealth = 100;
    public int EntityID;
    protected HealthBar _healthBar;
    protected int _currentHealth;
    private float _delayBeforeDestruction = 1;

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
        //Dead events are recorded for playback so we skip destroying
        if (GameManager.Instance.IsInPlaybackMode) return;
        StartCoroutine(DelayToDestroy());
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
            NetworkManager.Instance?.RecordTargetDestroyed(EntityID, -1);    
        }
        
        Destroy(gameObject);
    }
}
