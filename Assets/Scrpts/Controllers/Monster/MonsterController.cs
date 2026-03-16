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
    float _scanRange = 5f;

    [SerializeField]
    float _attackRange = 0.8f;

    [SerializeField]
    float _moveRange = 20f;

    Vector3 _initialScale;


    public override void Init()
    {

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
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
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

        Stat targetStat = _lockTarget.GetComponent<Stat>();
        if (targetStat != null && targetStat.Hp <= 0)
        {
            _lockTarget = null;
            State = Define.State.Return;
            return;
        }

        // 애니메이션 공격
        Animator anim = GetComponent<Animator>();
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Hit") == false)
        {
            anim.CrossFade("Hit", 0.1f);
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
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
        }

    }

    protected override void UpdateReturn()
    {
        // 방향과 거리 계산
        Vector3 dir = (_spawnPos - transform.position).normalized;
        float distToThomeSqr = (_spawnPos - transform.position).sqrMagnitude;

        // 도착 판정
        if (distToThomeSqr < 0.01f)
        {
            _rb.linearVelocity = Vector2.zero;
            transform.position = _spawnPos;
            State = Define.State.Idle;
            _lockTarget = null;
            return;
        }

        // Rigidbody로 이동 (이동 방식 통일)
        _rb.linearVelocity = dir * _stat.MoveSpeed;

        // 돌아갈 때도 방향 전환
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? -1f : 1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
        }
    }

    
    public void OnHitEvent()
    {
        if (_lockTarget == null)
        {
            State = Define.State.Idle;
            return;
        }

        Stat targetStat = _lockTarget.GetComponent<Stat>();
        if (targetStat == null) return;

        Debug.Log("여기까지?");
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
