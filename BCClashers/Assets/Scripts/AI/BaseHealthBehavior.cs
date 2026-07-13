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
    private bool _isAStructure;

    private void Start()
    {
        _currentHealth = StartingHealth;
        _healthBar = GetComponentInChildren<HealthBar>();
        if (_healthBar)
        {
            _healthBar.SetMaxHealth(_currentHealth);
        }

        _isAStructure = gameObject.name.Contains("House");
    }

    public  void Damage(int damageTaken)
    {
        if (_currentHealth <= 0) return;

        _currentHealth -= damageTaken;

        if (_healthBar)
        {
            _healthBar.SetHealth(_currentHealth);
        }

        ShowDamageNumber(damageTaken);

        if (_currentHealth <= 0)
        {
            Dead();
        }
    }

    /// <summary>
    /// Pops a floating number off whatever just got hit. Every damage source (melee via TroopAI,
    /// projectiles via ProjectileMovement) funnels through Damage(), so hooking here covers all of them.
    /// </summary>
    private void ShowDamageNumber(int damageTaken)
    {
        //Hits on the enemy (their defenders + their structures) read cyan; hits you take read red.
        bool targetIsEnemy = _isAStructure || (this is TroopAI troop && troop.TeamID == 1);
        Color color = targetIsEnemy
            ? FloatingDamageNumber.EnemyHitColor
            : FloatingDamageNumber.SelfHitColor;

        //Spawn height comes from the target's bounds so the number sits above a tall tower
        //instead of inside it. Scale has a high floor on purpose: troops are short, so scaling
        //purely off their height made their numbers unreadably small next to the structures'.
        float height = 3f;
        float scale = 1.6f;
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            height = bounds.size.y * 0.85f + 1.5f;
            scale = Mathf.Clamp(bounds.size.y * 0.25f, 1.6f, 2.6f);
        }

        Vector3 spawnPos = transform.position + Vector3.up * height;
        FloatingDamageNumber.Spawn(spawnPos, damageTaken, color, scale);
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

        if (!GameManager.Instance.IsInPlaybackMode && NetworkManager.Instance != null)
        {
            NetworkManager.Instance?.RecordTargetDestroyed(EntityID, -1);
            if (_isAStructure)
            {
                NetworkManager.Instance.StructureKillCount++;
            }
        }
        
        Destroy(gameObject);
    }
}
