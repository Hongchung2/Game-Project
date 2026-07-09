using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public abstract class BaseMonsterController : BaseController
{
    protected Stat _stat;
    protected Rigidbody2D _rb;
    protected Vector3 _spawnPos;
    protected Vector3 _initialScale;

    protected SPUM_Prefabs _spum;
    protected bool _isAttacking = false; 
    protected bool _isDeath = false;
    protected Coroutine _attackCoroutine;
    protected UI_NormalMonsterHPBar HPBar;


    [SerializeField]
    protected Detection detection;
    [SerializeField]
    protected float _scanRange = 5f;
    [SerializeField]
    protected float _moveRange = 20f;
    [SerializeField]
    protected SpriteRenderer _spriteRenderer;
    [SerializeField]
    private float stopDistance = 0.8f;
    public float StopTime = 1f;
    protected abstract IEnumerator AttackRoutine();
    [SerializeField]
    EnemyManager _enemyManager;



    public override void Init()
    {
        _initialScale = transform.localScale;

        WorldObjectType = Define.WorldObject.Monster;

        _stat = gameObject.GetComponent<Stat>();

        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (detection == null) detection = GetComponent<Detection>();



        _rb = gameObject.GetOrAddComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        _spawnPos = transform.position;


        _spum = GetComponent<SPUM_Prefabs>();
        if (_spum != null )
        {
            _spum.PopulateAnimationLists();
            _spum.OverrideControllerInit();

            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    protected virtual void InitHPBar()
    {
         if (HPBar == null)
        {
            HPBar = Managers.UI.MakeWorldSpaceUI<UI_NormalMonsterHPBar>(transform);
            HPBar.transform.localPosition = new Vector3(0, 1.0f, 0);
            HPBar.SetTarget(_stat);
        }
    }
    protected override void UpdateIdle()
    {

        if (_isAttacking) return;

        GameObject player = Managers.Game.GetPlayer();

        if (player == null)
        {
            return;
        }
        
        float distanceSqr = (player.transform.position - transform.position).sqrMagnitude;
        if (distanceSqr < _scanRange * _scanRange)
        {
            _lockTarget = player;

            State = Define.State.Moving;
            _spum.PlayAnimation(PlayerState.MOVE, 0);
            return;
        }
    }

    protected override void UpdateMoving()
    {
        if (_lockTarget == null)
        {
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
            return;
        }
        if (_isAttacking) return;

        _destPos = _lockTarget.transform.position;

        float distToPlayer = (transform.position - _lockTarget.transform.position).sqrMagnitude;
        if (distToPlayer < stopDistance * stopDistance || detection.playerDetected)
        {
            _rb.linearVelocity = Vector2.zero;
            if (!_isAttacking && _attackCoroutine == null)
            {
                _attackCoroutine = StartCoroutine(AttackRoutine());
            }
            return;
        }

        Vector3 dir = (_destPos - transform.position).normalized;
        _rb.linearVelocity = dir * _stat.MoveSpeed;
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? 1f : -1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
        }

        float disFromHomeSqr = (transform.position - _spawnPos).sqrMagnitude;
        if (disFromHomeSqr > _moveRange * _moveRange)
        {
            _lockTarget = null;
            State = Define.State.Return;
        }
    }

    protected override void UpdateSkill()
    {
        if (_isAttacking) return;   

        if (_lockTarget == null)
        {
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
            return;
        }

        Stat targetStat = _lockTarget.GetComponent<Stat>();
        if (targetStat != null && targetStat.Hp <= 0)
        {
            _lockTarget = null;
            State = Define.State.Return;
            return;
        }

        if (!detection.playerDetected)
        {
            State = Define.State.Moving;
            _spum.PlayAnimation(PlayerState.MOVE, 0);
            return;
        }

        Vector3 dir = (_lockTarget.transform.position - transform.position).normalized;
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? 1f : -1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
        }
    }

    protected override void OnDie()
    {
        if (_isDeath) return;
        if (_enemyManager != null)
        {
            _enemyManager.OnEnemyDied();
        }
        StartCoroutine(DeadAction());
    }

    protected override void UpdateReturn()
    {
        Return();
    }

    public override void OnHitEvent()
    {
        if (_lockTarget == null) return;

        if (detection.playerDetected)
        {
            Stat targetStat = _lockTarget.GetComponent<Stat>();
            if (targetStat != null)
            {
                targetStat.OnAttacked(_stat);
                
            }
        }
    }

    public void Return()
    {
        Vector3 dir = (_spawnPos - transform.position).normalized;
        float disToHomeSqr = (_spawnPos - transform.position).sqrMagnitude;

        if (disToHomeSqr < 0.01f)
        {
            _rb.linearVelocity = Vector2.zero;
            transform.position = _spawnPos;
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
            _lockTarget = null;
            return;
        }

        _rb.linearVelocity = dir * _stat.MoveSpeed;
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? 1f : -1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
        }
    }
    
    IEnumerator DeadAction()
    {
        if (_lockTarget != null) _lockTarget = null;

        _isDeath = true;

        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }

        _rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        _spum.PlayAnimation(PlayerState.DEATH, 0);

        yield return new WaitForSeconds(3.0f);

        gameObject.SetActive(false);

        _isDeath = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }
}
