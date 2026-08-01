using UnityEngine;
using System.Collections;

public class GoblinSpearController : BaseMonsterController
{
    private const int TELEMETRY_STAGE = 1; // 명세서 4.4 가중치 태깅용

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
        float telegraphDuration = spear != null ? 1f / spear.thrustSpeed : 0f; // 실제 찌르기 모션 시간(코드값)에서 읽어옴
        Telemetry.AttackTelegraphStart("도깨비창병", telegraphDuration, TELEMETRY_STAGE);
        Vector2 dodgeCheckStartPos = _lockTarget != null ? (Vector2)_lockTarget.transform.position : Vector2.zero; // 계측 전용: 예고 시작 시점 위치(회피 방향 판정용)
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
                Telemetry.AttackHit("도깨비창병", telegraphDuration, TELEMETRY_STAGE);
            }
            else
            {
                Telemetry.AttackDodged("도깨비창병", telegraphDuration, Telemetry.ComputeDodgeDirectionFromPositions(dodgeCheckStartPos, _lockTarget.transform.position), TELEMETRY_STAGE);
            }
        }

        // 2초 쿨타임
        _stat.add_MoveSpeed = 0;
        State = Define.State.Moving;
        _animator.SetBool("IsMoving", true);
        int hpBeforeCooldown = _stat.Hp; // 후딜(쿨타임) 구간 중 반격당했는지 확인용
        yield return new WaitForSeconds(2.0f);
        if (_stat.Hp < hpBeforeCooldown) Telemetry.PlayerPunish("도깨비창병", TELEMETRY_STAGE);

        // 다음 공격 준비
        AttackCount = 0f;
        _isAttacking = false;
        _attackCoroutine = null;
    }

    // 계측 전용: 교전 중 0.5초마다 플레이어와의 거리 샘플링 (기존 로직과 무관, 순수 추가)
    private float _telemetrySampleTimer = 0f;
    private void LateUpdate()
    {
        if (_lockTarget == null || detection == null || !detection.playerDetected) return;

        _telemetrySampleTimer += Time.deltaTime;
        if (_telemetrySampleTimer < 0.5f) return;
        _telemetrySampleTimer = 0f;

        float distance = Vector2.Distance(transform.position, _lockTarget.transform.position);
        Telemetry.PlayerPositionSample("도깨비창병", distance, distance < _attackRange, TELEMETRY_STAGE);
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

