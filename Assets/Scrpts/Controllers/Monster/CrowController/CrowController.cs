using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using Goldmetal.UndeadSurvivor;
public class CrowController : BaseMonsterController
{
    private const float ATTACK_TELEGRAPH_DURATION = 1.45f; // Attack 애니메이션 발동 후 발사까지 대기시간(main에서 밸런스 조정된 값과 병합)
    private const int TELEMETRY_STAGE = 1; // 명세서 4.4 가중치 태깅용

    [SerializeField]
    float bulletSpeed = 5f;

    [SerializeField]
    private GameObject bulletPrefab;

    protected override void UpdateMoving()
    {
        if (_lockTarget == null)
        {
            State = Define.State.Idle;
            _animator.SetBool("IsMoving", false); 
            return;
        }

        if (_isAttacking) return;

        // Crow는 detection 범위에 들어오면 바로 공격
        if (detection.playerDetected)
        {
            _rb.linearVelocity = Vector2.zero;
            if (!_isAttacking && _attackCoroutine == null)
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

    public void Shoot()
    {
        if (_lockTarget == null) return;

        Vector2 dir = (_lockTarget.transform.position - transform.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        Bullet bulletComponent = bullet.GetOrAddComponent<Bullet>();
        bulletComponent.Init(_stat);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.linearVelocity = dir * bulletSpeed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(bullet, 5.0f);
        TelemetryBulletObserver.Attach(bullet, "까마귀", ATTACK_TELEGRAPH_DURATION, _lockTarget, TELEMETRY_STAGE);
    }
    
    Vector2 GetDiagonalDirection()
    {
        if (_lockTarget == null) return Vector2.zero;

        Vector2 dirToPlayer = (_lockTarget.transform.position - transform.position).normalized;
        
        float baseAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;

        float randomOffset = Random.Range(-17.5f, 17.5f);
        float finalAngle = (baseAngle + 180f) + randomOffset;

        float rad = finalAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
    protected override IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        while (_lockTarget != null)
        {
            float distanceSqr = (_lockTarget.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr > _scanRange * _scanRange)
            {
                _rb.linearVelocity = Vector2.zero;
                _isAttacking = false;
                _attackCoroutine = null;
                State = Define.State.Return;
                yield break;
            }

            Stat targetStat = _lockTarget.GetComponent<Stat>();
            if (targetStat != null && targetStat.Hp <= 0)
            {
                _lockTarget = null;
                break;
            }

            // 공격
            State = Define.State.Skill;
            _animator.SetTrigger("Attack");
            Telemetry.AttackTelegraphStart("까마귀", ATTACK_TELEGRAPH_DURATION, TELEMETRY_STAGE);
            yield return new WaitForSeconds(ATTACK_TELEGRAPH_DURATION);
            Shoot(); // playerDetected 조건 없이 발사

            // 1초 경직
            int hpBeforePunishWindow = _stat.Hp; // 경직(후딜) 구간 중 반격당했는지 확인용
            yield return new WaitForSeconds (1f);
            if (_stat.Hp < hpBeforePunishWindow) Telemetry.PlayerPunish("까마귀", TELEMETRY_STAGE);

            // 0.8초 대각선 이동
            State = Define.State.Moving;
            _animator.SetBool("IsMoving", true);
            Vector2 moveDir = GetDiagonalDirection();
            float moveTime = 0.8f;

            while (moveTime > 0)
            {
                float disFromHome = (transform.position - _spawnPos).sqrMagnitude;
                if (disFromHome > _moveRange * _moveRange)
                {
                    _rb.linearVelocity = Vector2.zero;
                    _isAttacking = false;
                    _attackCoroutine = null;
                    State = Define.State.Return;
                    yield break;
                }

                _rb.linearVelocity = moveDir * _stat.Total_MoveSpeed;

                Vector3 dir = (_lockTarget.transform.position - transform.position).normalized;
                if (dir.x != 0)
                {
                    float xTargetScale = (dir.x < 0) ? 1f : -1f;
                    transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
                }

                moveTime -= Time.deltaTime;
                yield return null;
            }

            // 1.2초 경직
            _rb.linearVelocity = Vector2.zero;
            State = Define.State.Idle;
            _animator.SetBool("IsMoving", false);
            yield return new WaitForSeconds(1.2f);
        }

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
        Telemetry.PlayerPositionSample("까마귀", distance, distance < _attackRange, TELEMETRY_STAGE);
    }
}
