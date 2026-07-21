using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public abstract class BaseMonsterController : BaseController
{
    protected Stat _stat;
    protected Rigidbody2D _rb;
    protected Vector3 _spawnPos;
    protected Vector3 _initialScale;

    protected Animator _animator;
    protected bool _isAttacking = false; 
    protected bool _isDeath = false;
    protected Coroutine _attackCoroutine;
    protected UI_NormalMonsterHPBar HPBar;


    [SerializeField]
    protected Detection detection;
    [SerializeField]
    protected float _attackRange = 3f;
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

        _animator = GetComponent<Animator>();
        State = Define.State.Idle;
        _animator.SetBool("IsMoving", false);
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
            _animator.SetBool("IsMoving", true);
            return;
        }
    }

    protected override void UpdateMoving()
    {
        if (_lockTarget == null)
        {
            State = Define.State.Idle;
            _animator.SetBool("IsMoving", false);
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
            return;
        }

        float distanceSqr = (_lockTarget.transform.position - transform.position).sqrMagnitude;
        if (distanceSqr > _scanRange * _scanRange)
        {
            _lockTarget = null;
            State = Define.State.Return;
            _animator.SetBool("IsMoving", false);
            return;
        }
    }

    protected override void UpdateSkill()
    {
        if (_isAttacking) return;   

        if (_lockTarget == null)
        {
            State = Define.State.Idle;
            _animator.SetBool("IsMoving", false);
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
            _animator.SetBool("IsMoving", true);
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
           _animator.SetBool("IsMoving", false);
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

        _animator.SetTrigger("Death");

        yield return new WaitForSeconds(2.0f);

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

    private void OnDrawGizmosSelected()
    {
        // 스캔 범위 (파랑)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _scanRange);

        // 이동 범위 (초록색)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_spawnPos == Vector3.zero ? transform.position : _spawnPos, _moveRange );
    }
}
