using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum TroopStates {Idle, Rotate, Move, Attack}
public enum EnemyTypes {Grunt, Solder, Shooter}

public class BaseTroop : MonoBehaviour, IDamageable<int>
{
    public EnemyTypes EnemyType;
    
    public GameObject DeathFX;
    public int StartingHealth = 100;
    
    public float MoveSpeed = 10;
    public float RotationSpeed = 5;
    public float AcceptanceRangeToTarget = 2;
    public GameObject HomeWayPoint;
    
    public TroopStates CurrentState = TroopStates.Idle;
    
    public LayerMask _activeMask;
    
    public LayerMask InvaderMask ;
    public LayerMask DefenderMask ;
    private GameObject _target;
    private int _health;
    public int _hitBackForce = 10;
    //Checks every 10 frames for a new target
    private int _searchTargetInterval = 10;
    
    private bool _isDead;
    private bool _isAttacking;
    private bool _isSearching = false;
    private bool _isKnockedBack;
    private bool _targetIsHostile;
    
    private float _delayBeforeDestroy = 2;
    private float _delaySearchTarget = 2;
    private float _delayBeforeResume = 1;
    private float _detectionRadius = 100;
    private float _distanceToTarget;

    private Animator _animator;
    private string attackParameter = "isAttacking";

    private GameObject _homeLocationRef;
    private Quaternion _currQuat;
    private Rigidbody _rigidbodyComp;
    private MeleeWeapon _meleeWeapon;
    private HealthBar _healthBarRef;
    private Coroutine _targetSearchCoroutine;
    private Coroutine _stunCoroutine;

    private ShootProjectiles _shootScript;

    private const string _nonTargetTag = "NonTarget";
    
    public int Health { get => _health; set => _health = value; }

    private void Awake()
    {
        _rigidbodyComp = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _meleeWeapon = GetComponentInChildren<MeleeWeapon>();
        _healthBarRef = GetComponentInChildren<HealthBar>();
        _shootScript = GetComponent<ShootProjectiles>();
    }

    // Start is called before the first frame update
    void Start()
    {
        _health = StartingHealth;
        if (_healthBarRef)
        {
            _healthBarRef.SetMaxHealth(_health);    
        }
        _homeLocationRef = Instantiate(HomeWayPoint, transform.position, Quaternion.identity);
        FindTarget();
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

        if (_isDead) return;

        if (!_target)
        {
            if (!_isSearching)
            {
                _isSearching = true;
                _animator.SetBool(attackParameter, false);
                _isAttacking = false;
                FindTarget();
            }
            return;
        }
        //Check every "x" frames, x = _searchTargetInterval
        if (Time.frameCount % _searchTargetInterval == 0)
        {
            FindTarget();
        }
        
        _distanceToTarget = (_target.transform.position - transform.position).magnitude;
        
        RotateToTarget();
        
        //Move to Target
        if(_distanceToTarget > AcceptanceRangeToTarget &&
           IsFacingObject() &&
           !_isKnockedBack)
        {
            if (_isAttacking)
            {
                _animator.SetBool(attackParameter, false);
                _isAttacking = false;
            }
            
            CurrentState = TroopStates.Move;
            MoveTroop();
        }
        //Attack !!!!!!!!!!!!!
        else if(_distanceToTarget < AcceptanceRangeToTarget && _targetIsHostile)
        {
            _rigidbodyComp.velocity = Vector3.zero;
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
        var vel = _rigidbodyComp.velocity;
        vel.y = 0;
        _rigidbodyComp.velocity = vel;
        if(_rigidbodyComp.velocity.magnitude > MoveSpeed)
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
        //direction.x = 0;
        if (direction == Vector3.zero) return;
        
        _currQuat = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, _currQuat, RotationSpeed * Time.deltaTime);
    }
    
    //Try to make this a slottable action so this function never has to be overridden
    private void PerformAction()
    {
        switch (EnemyType)
        {
            case EnemyTypes.Grunt:
            case EnemyTypes.Solder:
                MeleeTarget();
                break;
            case EnemyTypes.Shooter:
                StartShootingAnimation();
                break;
        }
    }

    private void MeleeTarget()
    {
        if (_isAttacking) return;
        _isAttacking = true;
        _animator.SetBool(attackParameter, true);
    }

    private void StartShootingAnimation()
    {
        _animator.SetBool(attackParameter, true);
    }
    
    //This function is triggered through an Animation Event
    public void ShootTarget()
    {
        /*if (_isAttacking) return;
        _isAttacking = true;*/
        if (_shootScript)
        {
            _shootScript.SpawnProjectile(gameObject.layer, _target);
        }
    }

    //0 = invader(local player), 1 = defender(network player)
    public void AssignToTeam(int teamID)
    {
        //ToDo: Is troop manager really needed ?
        //TroopManager.Instance.ActiveTroopsList.Add(gameObject.GetInstanceID(), teamID);

        _activeMask = teamID == 0 ? DefenderMask : InvaderMask;

        if (_meleeWeapon)
        {
            _meleeWeapon.gameObject.layer = teamID == 0 ? 6 : 7; 
        }
        
        //6 = Invader Layer, 7 = Defender Layer
        gameObject.layer = teamID == 0 ? 6 : 7;
    }
    
    public void Damage(int damageTaken)
    {
        if (_health <= 0) return;

        _health -= damageTaken;

        if (_healthBarRef)
        {
            _healthBarRef.SetHealth(_health);
        }
        
        if (_health <= 0)
        {
            Dead();
        }
    }

    public void IncomingAttacker(BaseTroop in_attacker)
    {
        if (_target == in_attacker.gameObject) return;
        
        _target = in_attacker.gameObject;
    }

    public void Dead()
    {
        if (_isDead) return;
        _isDead = true;
        StartCoroutine(DelayToDeath());
    }

    public void LaunchObject(Vector3 direction)
    {
        if (_isKnockedBack)
        {
            StopCoroutine(_stunCoroutine);
        }

        if (_isAttacking)
        {
            _isAttacking = false;
            _animator.SetBool(attackParameter, false);
        }
        
        _isKnockedBack = true;
        _rigidbodyComp.AddForce(direction * _hitBackForce);
        _stunCoroutine = StartCoroutine(DelayToResumeMovement());
    }
    
    //meant for stun effect
    IEnumerator DelayToResumeMovement()
    {
        yield return new WaitForSeconds(_delayBeforeResume);
        _rigidbodyComp.velocity = Vector3.zero;
        _stunCoroutine = null;
        _isKnockedBack = false;
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
        _targetSearchCoroutine = null;
        _target = null;
        Collider[] hitColliders = new Collider[10];
        int numOfColliders = Physics.OverlapSphereNonAlloc(transform.position, _detectionRadius, hitColliders, _activeMask);
        float shortestDistance = Mathf.Infinity;
        
        float distance = 0;
        for (int i = 0; i < numOfColliders; i++)
        {
            if (!hitColliders[i].tag.Contains(_nonTargetTag))
            {
                distance = Vector3.Distance(transform.position, hitColliders[i].gameObject.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    _target = hitColliders[i].gameObject;
                    _targetIsHostile = true;
                }   
            }
        }
        
        //Start a coroutine if there isn't a target to look for it again in x seconds
        if (!_target)
        {
            _rigidbodyComp.velocity = Vector3.zero;
            _target = _homeLocationRef;
            _targetIsHostile = false;
            _targetSearchCoroutine = StartCoroutine(DelayToSearchForTarget());
        }
        else
        {
            _isSearching = false;
        }
    }

    IEnumerator DelayToSearchForTarget()
    {
        yield return new WaitForSeconds(_delaySearchTarget);
        FindTarget();
    }
}
