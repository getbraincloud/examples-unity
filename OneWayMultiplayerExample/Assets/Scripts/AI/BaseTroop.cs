using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BaseTroop : MonoBehaviour, IPrimaryAction, IDamageable<float>
{
    public GameObject DeathFX;
    public float StartingHealth = 100;
    public float DetectionRadius = 50;
    public LayerMask InvaderMask;
    public LayerMask DefenderMask;
    private LayerMask _activeMask;
    protected GameObject _target;
    private float _health;
    private bool _isDead;
    
    public float Health { get => _health; set => _health = value; }

    protected float _delayBeforeDestroy = 2;
    
    // Start is called before the first frame update
    void Start()
    {
        _health = StartingHealth;
        FindTarget();
    }

    void FixedUpdate()
    {
        
    }
    
    public void PerformAction() { }

    //0 = invader(local player), 1 = defender(network player)
    public void AssignToTeam(int teamID)
    {
        //ToDo: Is troop manager really needed ?
        //TroopManager.Instance.ActiveTroopsList.Add(gameObject.GetInstanceID(), teamID);

        _activeMask = teamID == 0 ? InvaderMask : DefenderMask;
        //6 = Invader Layer, 7 = Defender Layer
        gameObject.layer = teamID == 0 ? 6 : 7;
    }
    
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
        if (DeathFX)
        {
            Instantiate(DeathFX, transform.position, Quaternion.identity);    
        }
        Destroy(gameObject);
    }
    
    //ToDo: Need to iterate on this function to include determining if a troop is on our team or not.
    //Dont do this with buildings bc they dont need a team ID but troops do. 
    protected void FindTarget()
    {
        Collider[] hitColliders = new Collider[10];
        int numOfColliders = Physics.OverlapSphereNonAlloc(transform.position, DetectionRadius, hitColliders, _activeMask);
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

        Debug.Log($"Target is: {_target}");
    }
}
