using UnityEngine;
using System.Collections;

public class ScarecrowController : BaseMonsterController
{
    private const int TELEMETRY_STAGE = 1; // 명세서 4.4 가중치 태깅용

    private Vector2 _telemetryDodgeStartPos; // 계측 전용: 예고 시작 시점 플레이어 위치(회피 방향 판정용)

    public override void OnHitEvent()
    {
        if (_lockTarget == null) return;


            Stat targetStat = _lockTarget.GetComponent<Stat>();
            float distToPlayer = (_lockTarget.transform.position - transform.position).sqrMagnitude;
            if (targetStat != null && distToPlayer < _attackRange * _attackRange)
            {
                targetStat.OnAttacked(_stat);
                Telemetry.AttackHit("허수아비", StopTime, TELEMETRY_STAGE);
            }
            else
            {
                Telemetry.AttackDodged("허수아비", StopTime, Telemetry.ComputeDodgeDirectionFromPositions(_telemetryDodgeStartPos, _lockTarget.transform.position), TELEMETRY_STAGE);
            }

    }

    protected override IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        State = Define.State.Idle;
        _animator.SetBool("IsMoving", false);

        Telemetry.AttackTelegraphStart("허수아비", StopTime, TELEMETRY_STAGE);
        _telemetryDodgeStartPos = _lockTarget != null ? (Vector2)_lockTarget.transform.position : Vector2.zero;
        yield return new WaitForSeconds(StopTime);

        if (_lockTarget != null)
        {
            State = Define.State.Skill;
            _animator.SetTrigger("Attack");
        }

        int hpBeforeRecovery = _stat.Hp; // 후딜(허리 절반 피기) 구간 중 반격당했는지 확인용
        yield return new WaitForSeconds(StopTime);
        if (_stat.Hp < hpBeforeRecovery) Telemetry.PlayerPunish("허수아비", TELEMETRY_STAGE);
        State = Define.State.Moving;
        _animator.SetBool("IsMoving", true);

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
        Telemetry.PlayerPositionSample("허수아비", distance, distance < _attackRange, TELEMETRY_STAGE);
    }
}
