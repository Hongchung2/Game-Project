using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : BaseController
{
    Stat _stat;
    Rigidbody2D _rb;
    Vector3 _spawnPos;

    [SerializeField]
    float _scanRange = 5;

    [SerializeField]
    float _attackRange = 2;

    [SerializeField]
    float _moveRange = 20f;

    public override void Init()
    {
        WorldObjectType = Define.WorldObject.Monster;
        _stat = gameObject.GetComponent<Stat>();
        _rb = gameObject.GetOrAddComponent<Rigidbody2D>();
 

        _rb.gravityScale = 0;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        _spawnPos = transform.position;

        State = Define.State.Idle;
    }

    protected override void UpdateIdle()
    {
        if (_destPos == _spawnPos)
        {
            float distToHomeSqr = (transform.position - _spawnPos).sqrMagnitude;
            if (distToHomeSqr > 0.01f)
                return;
        }
        GameObject player = Managers.Game.GetPlayer();
        Debug.Log(player);

        if ( player == null)
        {
            return;
        }

        float distanceSqr = (player.transform.position - transform.position).sqrMagnitude;
        if (distanceSqr < _scanRange * _scanRange)
        {
            _lockTarget = player;
            Debug.Log("적 포착!");
            State = Define.State.Moving;
            return;

        }
    }

    protected override void UpdateMoving()
    {
        if (_lockTarget == null)
        {
            State = Define.State.Idle;
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
            State = Define.State.Skill;
        }

        // 실제 이동
        _rb.linearVelocity = dir * _stat.MoveSpeed;


        // 좌우 반전
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? -1f : 1f;
            transform.localScale = new Vector3(xTargetScale, 1f, 1f);
        }

        // 집 너무 멀리 가면 복귀
        float distFromHomeSqr = (transform.position - _spawnPos).sqrMagnitude;
        if (distFromHomeSqr > _moveRange * _moveRange)
        {
            _lockTarget = null;
            State = Define.State.Return;
        }
    }

    protected override void UpdateSkill()
    {
        if ( _lockTarget == null)
        {
            State = Define.State.Idle;
            return;
        }

        // 때리다가 멀어지면 다시 쫓아가기
        float distanceSqr = (_lockTarget.transform.position - transform.position).sqrMagnitude;
        if (distanceSqr > _attackRange * _attackRange)
        {
            State = Define.State.Moving;
            return;
        }

        // 때릴 때도 쳐다보기
        Vector3 dir = (_lockTarget.transform.position - transform.position).normalized;
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? -1f : 1f;
            transform.localScale = new Vector3(xTargetScale, 1f, 1f);
        }
    }

    protected override void UpdateReturn()
    {
        if (State == Define.State.Return)
        {
            Vector3 dir = (_spawnPos - transform.position).normalized;
            transform.position += dir * _stat.MoveSpeed * Time.deltaTime;

            if (Vector3.Distance(transform.position, _spawnPos) < 0.1f)
            {
                State = Define.State.Idle;
            }
        }
    }
    
    void OnHitEvent()
    {
        if (_lockTarget == null)
        {
            State = Define.State.Idle;
            return;
        }

        Stat targetStat = _lockTarget.GetComponent<Stat>();
        if (targetStat == null) return;

        targetStat.OnAttacked(_stat);

        if (targetStat.Hp > 0)
        {
            float distance = (_lockTarget.transform.position - transform.position).sqrMagnitude;
            if (distance <= _attackRange * _attackRange)
                State = Define.State.Skill;
            else
                State = Define.State.Moving;
        }
        else
        {
            _lockTarget = null;
            State = Define.State.Return;
        }
    }
}
