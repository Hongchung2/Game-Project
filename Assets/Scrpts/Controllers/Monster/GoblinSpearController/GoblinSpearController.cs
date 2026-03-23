using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections;

public class GoblinSpearController : BaseController
{
    Stat _stat;
    Rigidbody2D _rb;
    Vector3 _spawnPos;
    Vector3 _initialScale;
    private SPUM_Prefabs _spum;
    public float StopTime = 1f;
    private bool _isAttacking = false; // 공격 루틴 중인지 체크
    private bool _isDeath = false;
    private Coroutine _attackCoroutine;
    float AttackCount = 0f;
    private bool _isWaiting = false;



    [SerializeField]
    float _scanRange = 5f;

    [SerializeField]
    float _attackRange = 0.8f;

    [SerializeField]
    float _moveRange = 20f;

    [SerializeField]
    SpriteRenderer _spriteRenderer;



    public override void Init()
    {
        _initialScale = transform.localScale;

        WorldObjectType = Define.WorldObject.Monster;

        _stat = gameObject.GetComponent<Stat>();

        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();



        _rb = gameObject.GetOrAddComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        _spawnPos = transform.position;

        _spum = GetComponent<SPUM_Prefabs>();
        if (_spum != null)
        {
            _spum.PopulateAnimationLists();
            _spum.OverrideControllerInit();

            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
        }
    }
    protected override void UpdateIdle()
    {

        if (_isAttacking) return;

        if (_destPos == _spawnPos)
        {
            float disToHomeSqr = (transform.position - _spawnPos).sqrMagnitude;
            if (disToHomeSqr > 0.01f)
                return;
        }
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

        // 방향 계산
        _destPos = _lockTarget.transform.position;


        // 공격 사거리 체크
        float distance = Vector2.Distance(transform.position, _destPos);
        if (distance <= _attackRange)
        {
            _rb.linearVelocity = Vector2.zero;
            if (!_isAttacking && _attackCoroutine == null)
            {
                _attackCoroutine = StartCoroutine(AttackRoutine());
            }
            return;
        }

        // 실제 이동
        Vector3 dir = (_destPos - transform.position).normalized;
        _rb.linearVelocity = dir * _stat.MoveSpeed;
        Debug.Log("이동");
        // 좌우 반전 (이동시)
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? 1f : -1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
        }

        // 복귀
        float disFromHomeSqr = (transform.position - _spawnPos).sqrMagnitude;
        if (disFromHomeSqr > _moveRange * _moveRange)
        {
            _lockTarget = null;
            State = Define.State.Return;
        }
    }

    protected override void UpdateSkill()
    {
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

        // 때리다가 멀어지면 쫓아가기
        float distanceSqr = (_lockTarget.transform.position - transform.position).sqrMagnitude;
        if (distanceSqr > _attackRange * _attackRange)
        {
            State = Define.State.Moving;
            _spum.PlayAnimation(PlayerState.MOVE, 0);
            return;
        }

        // 때릴 때 플레이어 쳐다보기
        Vector3 dir = (_lockTarget.transform.position - transform.position).normalized;
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? 1f : -1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
        }
    }

    protected override void UpdateDie()
    {
        if (_isDeath) return;
        StartCoroutine(DeadAction());
    }

    protected override void UpdateReturn()
    {
        Return();
    }

    public override void OnHitEvent()
    {
        Debug.Log("onhitevent 정상적으로 실행");
        if (_lockTarget == null) return;

        float distance = Vector3.Distance(transform.position, _lockTarget.transform.position);

        if (distance <= _attackRange)
        {
            Stat targetStat = _lockTarget.GetComponent<Stat>();
            if (targetStat != null)
            {
                targetStat.OnAttacked(_stat);
                Debug.Log($"현재 Hp : {targetStat.Hp}");
            }
        }
        else
        {
            Debug.Log("회피 성공");
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

    IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _isWaiting = true;

        while (AttackCount <= 1.0f)
        {
            FollowPlayerSlowly();
            AttackCount += Time.deltaTime;
            yield return null;
        }

        State = Define.State.Skill;
        _spum.PlayAnimation(PlayerState.ATTACK, 0);
        AttackCount = 0f;

        

        _isAttacking = false;
        _attackCoroutine = null;
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
    void FollowPlayerSlowly()
    {

    }

}