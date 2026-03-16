using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections;

public class ScarecrowController : BaseController
{
    Stat _stat;
    Rigidbody2D _rb;
    Vector3 _spawnPos;
    Vector3 _initialScale;
    private SPUM_Prefabs _spum;
    public float StopTime = 1f;
    private bool _isAttacking = false; // 공격 루틴 중인지 체크

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
        if (_spum != null )
        {
            _spum.PopulateAnimationLists();
            _spum.OverrideControllerInit();

            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
        }
    }
    protected override void UpdateIdle()
    {
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
            Debug.Log("적 감지");
            State = Define.State.Moving;
            _spum.PlayAnimation(PlayerState.MOVE, 0);
            return;
        }
    }

    protected override void UpdateMoving()
    {
        if (_lockTarget == null || _isAttacking)
        {
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
            return;
        }

        // 방향 계산
        _destPos = _lockTarget.transform.position;
        Vector3 dir = (_destPos - transform.position).normalized;

        // 공격 사거리 체크
        float distSqr = (_destPos - transform.position).sqrMagnitude;
        if (distSqr <= _attackRange * _attackRange)
        {
            _rb.linearVelocity = Vector2.zero;
            Debug.Log($"사거리 진입 {distSqr}");

            if (!_isAttacking)
            {
                Debug.Log("공격");
                StartCoroutine(AttackRoutine());
            }
            return;
        }

        // 실제 이동
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

    protected override void UpdateReturn()
    {
        Return();
    }

    public void OnHitEvent()
    {
        if (_lockTarget == null)
        {
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
            return;
        }

        Stat targetStat = _lockTarget.GetComponent<Stat>();
        if (targetStat == null) return;

        targetStat.OnAttacked(_stat);

        if (targetStat.Hp > 0)
        {
            float distance = (_lockTarget.transform.position - transform.position).sqrMagnitude;
            if (distance <= _attackRange * _attackRange)
            {
                State = Define.State.Skill;
                _spum.PlayAnimation(PlayerState.ATTACK, 0);
            }
            else
            {
                State = Define.State.Moving;
                _spum.PlayAnimation(PlayerState.MOVE, 0);
            }
        }
        else
        {
            _lockTarget = null;
            State = Define.State.Return;
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
        State = Define.State.Idle;
        _spum.PlayAnimation(PlayerState.IDLE, 0);

        yield return new WaitForSeconds(StopTime);

        if (_lockTarget != null)
        {
            State = Define.State.Skill;
            _spum.PlayAnimation(PlayerState.ATTACK, 0);
            Debug.Log("공격 (코루틴)");
        }

        yield return new WaitForSeconds(0.5f);

        _isAttacking = false;
        State = Define.State.Moving;
        _spum.PlayAnimation(PlayerState.MOVE, 0);
    }


}
