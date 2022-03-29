using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum TroopStates {Idle, Rotate, Move, Attack}

public class BaseTroop : MonoBehaviour, IPrimaryAction, IDamageable<float>
{
    public LayerMask InvaderMask;
    public LayerMask DefenderMask;
    public GameObject DeathFX;
    public float StartingHealth = 100;
    public float DetectionRadius = 50;
    public float MoveSpeed = 10;
    public float RotationSpeed = 5;
    public float AcceptanceRangeToTarget = 2;

    public TroopStates CurrentState = TroopStates.Idle;
    
    private LayerMask _activeMask;
    protected GameObject _target;
    private float _health;
    private bool _isDead;
    private bool _rotationComplete;
    
    protected float _delayBeforeDestroy = 2;
    protected float _delaySearchTarget = 2;

    private float _distanceToTarget;
    
    private float _angle;
    private Quaternion _currQuat;
    private Rigidbody _rigidbodyComp;
    private Coroutine _targetSearchCoroutine;
    
    public float Health { get => _health; set => _health = value; }

    private void Awake()
    {
        _rigidbodyComp = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        _health = StartingHealth;
        StartCoroutine(DelayToSearchForTarget());
    }

    void FixedUpdate()
    {
        /*
         * Goals:
         * - Rotates to target direction
         * - Moves to target within x distance
         * - As long as we have _target, keep executing PerformAction()
         * - If there isn't _target, return and look for a target.(Might need to flag this so its not constantly looking)
         */

        if (!_target)
        {
            _rotationComplete = false;
            
            if (_targetSearchCoroutine == null)
            {
                FindTarget();
            }
            return;
        }
        
        _distanceToTarget = (_target.transform.position - transform.position).magnitude;
        
        RotateToTarget();
        
        //Move to Target
        if(_distanceToTarget > AcceptanceRangeToTarget && IsFacingObject())
        {
            CurrentState = TroopStates.Move;
            MoveTroop();
        }
        //Attack !!!!!!!!!!!!!
        else
        {
            _rigidbodyComp.velocity=Vector3.zero;
            CurrentState = TroopStates.Attack;
            PerformAction();
        }
    }

    private bool IsFacingObject()
    {
        Vector3 dirFromAtoB = (_target.transform.position - transform.position).normalized;
        float dotProduct = Vector3.Dot(dirFromAtoB, transform.forward);

        return dotProduct > 0.9f;
    }
    
    //give direction to move enemy towards
    private void MoveTroop()
    {
        _rigidbodyComp.AddForce(transform.forward * MoveSpeed);
        if(_rigidbodyComp.velocity.magnitude>MoveSpeed)
        {
            _rigidbodyComp.velocity = _rigidbodyComp.velocity.normalized * MoveSpeed;
        }
    }
    
    //give a positive direction if you want to face the target and 
    //a negative direction to look away
    private void RotateToTarget()
    {
        //rotate enemy
        Vector3 direction = (_target.transform.position - transform.position).normalized;
        _currQuat = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, _currQuat, RotationSpeed * Time.deltaTime);
    }
    
    
    //Try to make this a slottable action so this function never has to be overridden
    public virtual void PerformAction() { }

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
    
    private void FindTarget()
    {
        Collider[] hitColliders = new Collider[10];
        int numOfColliders = Physics.OverlapSphereNonAlloc(transform.position, DetectionRadius, hitColliders, _activeMask);
        float shortestDistance = Mathf.Infinity;
        
        float distance = 0;
        for (int i = 0; i < numOfColliders; i++)
        {
            distance = Vector3.Distance(transform.position, hitColliders[i].gameObject.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                _target = hitColliders[i].gameObject;
            }
        }
        
        //Start a coroutine if there isn't a target to look for it again in x seconds
        if (!_target)
        {
            _targetSearchCoroutine = StartCoroutine(DelayToSearchForTarget());
        }
    }

    IEnumerator DelayToSearchForTarget()
    {
        yield return new WaitForSeconds(_delaySearchTarget);
        FindTarget();
    }
}
