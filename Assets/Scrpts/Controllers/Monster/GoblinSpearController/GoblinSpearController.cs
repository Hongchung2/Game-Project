using UnityEngine;
using System.Collections;

public class GoblinSpearController : BaseMonsterController
{
    private const int TELEMETRY_STAGE = 1; // 명세서 4.4 가중치 태깅용

    float AttackCount = 0f;
    private float lastdistance;
    private bool _isWaiting = false;


    protected override void UpdateMoving()
    {
        Debug.Log($"isAttacking: {_isAttacking} lockTarget: {_lockTarget}");
        if (_lockTarget == null)
        {
            State = Define.State.Idle;
            _animator.SetBool("IsMoving", false);
            return;
        }
        if (_isAttacking) return;

        // detection 범위 안에 들어오면 공격 루틴 시작
        if (detection.playerDetected)
        {
            _rb.linearVelocity = Vector2.zero;
            if (_attackCoroutine == null)
            {
                _attackCoroutine = StartCoroutine(AttackRoutine());
            }
            return;
        }

        // 플레이어 방향으로 이동
        _destPos = _lockTarget.transform.position;
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

    protected override IEnumerator AttackRoutine()
    {Debug.Log("AttackRoutine 시작");
        _isAttacking = true;
            AttackCount = 0f;

            // 1초 누적 (나갔다 와도 유지)
            while (AttackCount < 1.0f)
            {
                
                if (_lockTarget == null)
                {
                    _stat.add_MoveSpeed = 0;
                    _isAttacking = false;
                    _attackCoroutine = null;
                    yield break;
                }

                // detection 밖으로 나가면 공격 루틴 종료하고 추적으로

                if (!detection.playerDetected)
                {
                    _animator.SetBool("IsMoving", false);
                    _rb.linearVelocity = Vector2.zero;
                }
                else
                {
                    //FollowPlayerSlowly();
                    _rb.linearVelocity = Vector2.zero;
                    _animator.SetBool("IsMoving", false);
                    AttackCount += Time.deltaTime;
                }
                

                lastdistance = Vector2.Distance(transform.position, _lockTarget.transform.position);
                yield return null;
            }

            // 공격
            _stat.add_MoveSpeed = 0;
            _rb.linearVelocity = Vector2.zero;
            _animator.SetBool("IsMoving", false);
            yield return new WaitForSeconds(1f);
            State = Define.State.Skill;

            Vector3 attackTargetPos = _lockTarget != null ? _lockTarget.transform.position : transform.position;

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
            // 2초 쿨타임 (이 동안 AttackCount 누적 안 됨)
            State = Define.State.Moving;
            _animator.SetBool("IsMoving", true);
            yield return new WaitForSeconds(2.0f);

            _isAttacking = false;
            _attackCoroutine = null;    
    }

    void FollowPlayerSlowly()
    {
        if (_lockTarget == null) return;

        _stat.add_MoveSpeed = -1.0f;

        float currentdistance = Vector2.Distance(transform.position, _lockTarget.transform.position);
        Vector3 dirToPlayer = (_lockTarget.transform.position - transform.position).normalized;

        _animator.SetBool("IsMoving", true);

        float xTargetScale = (_lockTarget.transform.position.x < transform.position.x) ? 1f : -1f;
        transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);


        if (currentdistance < detection.detectWidth / 2f)
        {
            _rb.linearVelocity = -dirToPlayer * _stat.Total_MoveSpeed;
        }
        else
        {
            _rb.linearVelocity = dirToPlayer * _stat.Total_MoveSpeed;
        }

    }
}

