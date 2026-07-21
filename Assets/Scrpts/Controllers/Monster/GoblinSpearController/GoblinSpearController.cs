using UnityEngine;
using System.Collections;

public class GoblinSpearController : BaseMonsterController
{
    float AttackCount = 0f;
    private float lastdistance;
    private bool _isWaiting = false;

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
        else
        {
            
        }
    }

    protected override IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        AttackCount = 0f;

        // 1초 누적 (나갔다 와도 유지)
        while (AttackCount < 1.0f)
        {
            if (detection.playerDetected)
            {
                FollowPlayerSlowly();
                AttackCount += Time.deltaTime;
            }
            else
            {
                _animator.SetBool("IsMoving", false);
                _rb.linearVelocity = Vector2.zero;
            }

            // 스캔 범위 벗어나면 공격 취소
            if (_lockTarget == null)
            {
                _stat.add_MoveSpeed = 0;
                _isAttacking = false;
                _attackCoroutine = null;
                yield break;
            }

            lastdistance = Vector2.Distance(transform.position, _lockTarget.transform.position);
            yield return null;
        }

        // 공격
        Vector3 attackTargetPos = _lockTarget.transform.position; // 플레이어 위치 저장
        State = Define.State.Skill;
        //_animator.SetTrigger("Attack");

        SpearController spear = GetComponentInChildren<SpearController>(true);
        if (spear != null)
        {
            yield return StartCoroutine(spear.Thrust());
        }

        // 데미지
        if (_lockTarget != null)
        {
            float dist = Vector2.Distance(transform.position, _lockTarget.transform.position);
            if (dist <= detection.detectWidth / 2f)
            {
                Stat targetStat = _lockTarget.GetComponent<Stat>();
                if (targetStat != null)
                {
                    targetStat.OnAttacked(_stat);
                }
            }
        }

        // 2초 쿨타임
        _stat.add_MoveSpeed = 0;
        State = Define.State.Moving;
        _animator.SetBool("IsMoving", true);
        yield return new WaitForSeconds(2.0f);

        // 다음 공격 준비
        AttackCount = 0f;
        _isAttacking = false;
        _attackCoroutine = null;
    }

    void FollowPlayerSlowly()
    {
        if (_lockTarget == null) return;

        _stat.add_MoveSpeed = -1.0f;

        float currentdistance = Vector2.Distance(transform.position, _lockTarget.transform.position);
        Vector3 dirToPlayer = (_lockTarget.transform.position - transform.position).normalized;
        
        if (detection.playerDetected)
        {
            _animator.SetBool("IsMoving", true);

            float xTargetScale = (_lockTarget.transform.position.x < transform.position.x) ? 1f : -1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
           
            if (currentdistance > lastdistance + 0.05f)
            {
                _rb.linearVelocity = dirToPlayer * _stat.Total_MoveSpeed;
            }

           else if (currentdistance <= lastdistance - 0.05f)
            {
                _rb.linearVelocity = -dirToPlayer * _stat.Total_MoveSpeed;
            }
            
            else
            {
                _rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
             _rb.linearVelocity = Vector2.zero;
        }
    }
}

