using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum TroopStates {Idle, Move, Attack}
public enum EnemyTypes {Grunt, Soldier, Shooter}

public class TroopAI : BaseHealthBehavior
{
    public EnemyTypes EnemyType;
    public float RotationSpeed = 5;
    public float AcceptanceRangeToTarget = 2;
    public bool IsInPlaybackMode;
    public TroopStates CurrentState = TroopStates.Idle;
    
    /// <summary>
    /// 0 = invader(local player), 1 = defender(network player)
    /// </summary>
    public int TeamID;
    public LayerMask _activeMask;
    public LayerMask InvaderMask;
    public LayerMask DefenderMask;
    
    public int _hitBackForce = 5;
    
    //Checks every 10 frames for a new target
    private int _searchTargetInterval = 10;

    private bool _isDead;
    private bool _isAttacking;
    private bool _isSearching = false;
    private bool _isKnockedBack;
    public bool TargetIsHostile;
    
    private float _delayBeforeDestroy = 0.75f;
    private float _delayBeforeResume = 0.5f;
    private float _detectionRadius = 100;
    private float _distanceToTarget;

    private Animator _animator;
    

    private Vector3 _homeLocation;
    private Quaternion _currQuat;
    private Rigidbody _rigidbodyComp;
    private MeleeWeapon _meleeWeapon;
    
    private Coroutine _stunCoroutine;

    private ShootProjectiles _shootScript;
    private NavMeshAgent _navMeshAgent;
    
    private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
    private const string _nonTargetTag = "NonTarget";
    
    private GameObject _target;
    
    public GameObject Target { set => _target = value; }
    
    private void Awake()
    {
        _rigidbodyComp = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        
        _meleeWeapon = GetComponentInChildren<MeleeWeapon>();
        _healthBar = GetComponentInChildren<HealthBar>();
        _shootScript = GetComponent<ShootProjectiles>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
    }

    // Start is called before the first frame update
    void Start()
    {
        _currentHealth = StartingHealth;
        if (_healthBar)
        {
            _healthBar.SetMaxHealth(_currentHealth);    
        }

        _homeLocation = transform.position;
        if (!IsInPlaybackMode)
        {
            FindTarget();
        }
    }

    void FixedUpdate()
    {
        if (_isDead) return;

        if (!_target && !IsInPlaybackMode)
        {
            if (!_isSearching)
            {
                _isSearching = true;
                _animator.SetBool(IsAttacking, false);
                _isAttacking = false;
                FindTarget();
            }
            //return;
        }
        //Check every "x" frames, x = _searchTargetInterval
        if (Time.frameCount % _searchTargetInterval == 0 && !IsInPlaybackMode)
        {
            FindTarget();
        }

        if (_target != null)
        {
            if (!TargetIsHostile)
            {
                TargetIsHostile = true;
            }
            _distanceToTarget = (_target.transform.position - transform.position).magnitude;    
        }
        else
        {
            if (TargetIsHostile)
            {
                TargetIsHostile = false;
            }
            _distanceToTarget = (_homeLocation - transform.position).magnitude;
        }

        //Move to Target
        if(_distanceToTarget > AcceptanceRangeToTarget)
        {
            if (_isAttacking)
            {
                _animator.SetBool(IsAttacking, false);
                _isAttacking = false;
            }
            _navMeshAgent.isStopped = false;
            CurrentState = TroopStates.Move;
            MoveTroop();
        }
        //Attack !!!!!!!!!!!!!
        else if(_distanceToTarget < AcceptanceRangeToTarget && TargetIsHostile)
        {
            CurrentState = TroopStates.Attack;
            _navMeshAgent.isStopped = true;
            RotateToTarget();
            PerformAction();
        }
    }

    private bool IsFacingObject()
    {
        Vector3 dirFromAtoB = (_target.transform.position - transform.position).normalized;
        float dotProduct = Vector3.Dot(dirFromAtoB, transform.forward);
        return dotProduct > 0.9f;
    }
    
    //give direction to move troop towards
    private void MoveTroop()
    {
        if (_target != null)
        {
            _navMeshAgent.destination = _target.transform.position;    
        }
        else
        {
            _navMeshAgent.destination = _homeLocation;
        }
    }
    
    //give a positive direction if you want to face the target and 
    //a negative direction to look away
    private void RotateToTarget()
    {
        //rotate enemy
        Vector3 direction = Vector3.zero;
        if (_target != null)
        {
            direction = (_target.transform.position - transform.position).normalized;
        }
        else
        {
            direction  = (_homeLocation - transform.position).normalized;
        }
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
            case EnemyTypes.Soldier:
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
        _animator.SetBool(IsAttacking, true);
    }

    private void StartShootingAnimation()
    {
        _animator.SetBool(IsAttacking, true);
    }
    
    //This function is triggered through an Animation Event
    public void ShootTarget()
    {
        if (_shootScript && gameObject && _target)
        {
            _shootScript.SpawnProjectile(gameObject.layer, _target);
        }
    }

    /// <param name="in_teamID">0 = invader(local player), 1 = defender(network player)</param>
    public void AssignToTeam(int in_teamID)
    {
        TeamID = in_teamID;
        if (in_teamID == 0)
        {
            //Invaders
            _activeMask = DefenderMask;
            if (_meleeWeapon)
            {
                _meleeWeapon.gameObject.layer = 6; 
            }
            //6 = Invader Layer, 7 = Defender Layer
            gameObject.layer = 6;
            _healthBar.AssignTeamColor(Color.blue);
        }
        else
        {
            //Defenders
            _activeMask = InvaderMask;
            if (_meleeWeapon)
            {
                _meleeWeapon.gameObject.layer = 7; 
            }
            //6 = Invader Layer, 7 = Defender Layer
            gameObject.layer = 7;
            _healthBar.AssignTeamColor(Color.red);
        }
    }

    public void IncomingAttacker(TroopAI in_attacker)
    {
        _target = in_attacker.gameObject;
    }

    public override void Dead()
    {
        if (_isDead) return;
        _isDead = true;
        _navMeshAgent.isStopped = true;
        _navMeshAgent.speed = 0;
        _navMeshAgent.destination = transform.position;
        _animator.SetBool(IsAttacking, false);
        _rigidbodyComp.velocity = Vector3.zero;
        _isAttacking = false;
        _homeLocation = Vector3.zero;
        StartCoroutine(DelayToDeath());
    }
    
    private IEnumerator DelayToDeath()
    {
        yield return new WaitForSeconds(_delayBeforeDestroy/2);
        if (DeathFX)
        {
            Instantiate(DeathFX, transform.position, Quaternion.identity);    
        }
        if (!GameManager.Instance.IsInPlaybackMode)
        {
            BrainCloudManager.Instance.RecordTargetDestroyed(EntityID, TeamID);
        }
        //Check if troop is an invader or defender
        if (TeamID == 0)
        {
            //Invader
            GameManager.Instance.InvaderTroopCount--;
        }
        else
        {
            //Defender
            GameManager.Instance.DefenderTroopCount--;
        }
        yield return new WaitForSeconds(_delayBeforeDestroy/2);
        
        if (!GameManager.Instance.IsInPlaybackMode)
        {
            Destroy(gameObject);
        }
    }

    public override void LaunchObject(Vector3 direction)
    {
        if (_isKnockedBack)
        {
            StopCoroutine(_stunCoroutine);
        }

        if (_isAttacking)
        {
            _isAttacking = false;
            _animator.SetBool(IsAttacking, false);
        }
        
        _isKnockedBack = true;
        _navMeshAgent.isStopped = true;
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
        _navMeshAgent.isStopped = false;
    }

    private void FindTarget()
    {
        //Reset values
        GameObject previousTarget = _target;
        TargetIsHostile = false;
        _target = null;
        
        //Search for a target nearby
        Collider[] hitColliders = new Collider[10];
        int numOfColliders = Physics.OverlapSphereNonAlloc(transform.position, _detectionRadius, hitColliders, _activeMask);
        float shortestDistance = Mathf.Infinity;
        //Determine which collider is closest to our troop
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
                    TargetIsHostile = true;
                }   
            }
        }
        //Validate its not a repeated target being set, record target switch for playback stream along with the ID.
        if (_target != null && previousTarget != _target)
        {
            _isSearching = false;
            //Send target info as event for playback
            int targetID = -1;
            int targetTeamID = -1;
            if (_target.tag.Contains("Troop"))
            {
                TroopAI targetTroop = null;
                targetTroop = _target.GetComponent<TroopAI>();
                targetID = targetTroop.EntityID;
                targetTeamID = targetTroop.TeamID;
            }
            else
            {
                targetID = _target.GetComponent<BaseHealthBehavior>().EntityID;
                targetTeamID = -1;
            }

            if (BrainCloudManager.Instance)
            {
                BrainCloudManager.Instance.RecordTargetSwitch(this, targetID, targetTeamID);
            }
        }
    }
}
