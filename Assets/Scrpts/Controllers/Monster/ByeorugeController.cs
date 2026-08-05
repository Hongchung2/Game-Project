using System.Collections;
using UnityEngine;

// 벼루게(탱커형 몬스터). 평소엔 스폰 지점 주변을 배회하다, 플레이어를 발견하면
// 방패를 든 채로 다가가고(피해 감소+반사 적용), 사거리 안이면 집게 찍기(부채꼴 범위)를 한다.
public class ByeorugeController : BaseController
{
    private const int TELEMETRY_STAGE = 2; // 명세서 4.4 가중치 태깅용

    [Header("이동 (배회 + 추적)")]
    public float moveSpeed = 1.5f;
    public float sightRange = 6f;      // 이 범위 안에 플레이어가 들어오면 발견(추적 시작)
    public float wanderRadius = 2.5f;  // 스폰 지점 기준 배회 반경
    public float wanderPauseMin = 1f;
    public float wanderPauseMax = 2.5f;
    public LayerMask wallLayer = 1;    // 배회 목표가 벽 안이면 재추첨 (기본값 Default 레이어=Wall이 있는 레이어)

    [Header("집게 찍기")]
    public float detectRange = 3f;
    public float attackInterval = 4f;
    public float pincerTelegraph = 0.6f;   // 예고 동작
    public float pincerRange = 1.5f;
    public float pincerAngle = 90f;        // 전방 부채꼴 각도
    public const int PINCER_DAMAGE = 2;    // 명세서 1/5 × MaxHp(10) = 2 (스탯 밸런싱: 고정값으로 통일)

    [Header("먹물 방패")]
    public const int SHIELD_BURN_DAMAGE = 1; // 명세서 1/7 × MaxHp(10) ≈ 1 (도트 총합, 한 번에 적용)
    public float shieldBurnDelay = 1f;       // 방패 피격 후 이 시간 뒤에 도트 적용(잔류 연출용 대기)

    public bool IsShielding { get; private set; }
    public Vector2 FacingDir { get; private set; } = Vector2.down;

    private Transform _player;
    private Stat _playerStat;
    private Stat _selfStat; // 계측 전용: 후딜 중 반격당했는지 HP 비교로 확인하기 위함
    private bool _isDead = false;
    private Vector3 _spawnPos;
    private Vector3 _initialScale;

    public override void Init()
    {
        WorldObjectType = Define.WorldObject.Monster;

        _spawnPos = transform.position;
        _initialScale = transform.localScale;
        _selfStat = GetComponent<Stat>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _player = p.transform;
            _playerStat = p.GetComponent<Stat>();
        }

        StartCoroutine(BehaviorLoop());
    }

    private IEnumerator BehaviorLoop()
    {
        while (!_isDead)
        {
            if (_player == null)
            {
                yield return null;
                continue;
            }

            float dist = Vector2.Distance(transform.position, _player.position);

            if (dist > sightRange)
            {
                yield return StartCoroutine(Wander());
                continue;
            }

            if (dist > detectRange)
            {
                yield return StartCoroutine(ChasePlayer());
                continue;
            }

            FacingDir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            yield return StartCoroutine(PincerAttack());
            int hpBeforeCooldown = _selfStat != null ? _selfStat.Hp : 0; // 후딜(쿨다운) 구간 중 반격당했는지 확인용
            yield return new WaitForSeconds(attackInterval);
            if (_selfStat != null && _selfStat.Hp < hpBeforeCooldown) Telemetry.PlayerPunish("벼루게", TELEMETRY_STAGE);
        }
    }

    // 계측 전용: 교전 중 0.5초마다 플레이어와의 거리 샘플링 (기존 로직과 무관, 순수 추가)
    private float _telemetrySampleTimer = 0f;
    private void LateUpdate()
    {
        if (_player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist > sightRange) return;

        _telemetrySampleTimer += Time.deltaTime;
        if (_telemetrySampleTimer < 0.5f) return;
        _telemetrySampleTimer = 0f;

        Telemetry.PlayerPositionSample("벼루게", dist, dist <= pincerRange, TELEMETRY_STAGE);
    }

    // 사거리 밖 ~ 발견 범위 안: 방패를 든 채로 다가간다
    private IEnumerator ChasePlayer()
    {
        IsShielding = true;

        while (!_isDead && _player != null)
        {
            float dist = Vector2.Distance(transform.position, _player.position);
            if (dist <= detectRange || dist > sightRange) break;

            FacingDir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            transform.position = Vector2.MoveTowards(transform.position, _player.position, moveSpeed * Time.deltaTime);
            FlipTowards(FacingDir.x);
            yield return null;
        }

        IsShielding = false;
    }

    // 발견 범위 밖: 스폰 지점 주변을 천천히 배회
    private IEnumerator Wander()
    {
        Vector2 target = PickWanderPoint();
        float timeout = 3f;
        float elapsed = 0f;

        while (!_isDead && elapsed < timeout)
        {
            if (_player != null && Vector2.Distance(transform.position, _player.position) <= sightRange)
                yield break; // 발견하면 즉시 BehaviorLoop로 복귀해서 추적 전환

            if (Vector2.Distance(transform.position, target) < 0.1f)
                break;

            Vector2 dir = (target - (Vector2)transform.position).normalized;
            transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * 0.6f * Time.deltaTime);
            FlipTowards(dir.x);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(Random.Range(wanderPauseMin, wanderPauseMax));
    }

    private Vector2 PickWanderPoint()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 candidate = (Vector2)_spawnPos + Random.insideUnitCircle * wanderRadius;
            if (!Physics2D.OverlapCircle(candidate, 0.3f, wallLayer))
                return candidate;
        }
        return _spawnPos;
    }

    private void FlipTowards(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        float sign = dirX < 0 ? -1f : 1f;
        transform.localScale = new Vector3(sign * Mathf.Abs(_initialScale.x), _initialScale.y, _initialScale.z);
    }

    private IEnumerator PincerAttack()
    {
        Telemetry.AttackTelegraphStart("벼루게", pincerTelegraph, TELEMETRY_STAGE);
        Vector2 dodgeCheckStartPos = _player != null ? (Vector2)_player.position : Vector2.zero; // 계측 전용: 예고 시작 시점 위치(회피 방향 판정용)
        yield return new WaitForSeconds(pincerTelegraph);
        if (_isDead || _player == null || _playerStat == null) yield break;

        Vector2 toPlayer = (Vector2)_player.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;
        float angle = Vector2.Angle(FacingDir, toPlayer);

        if (dist <= pincerRange && angle <= pincerAngle * 0.5f)
        {
            if (_selfStat != null)
            {
                _selfStat.Attack = PINCER_DAMAGE;
                _playerStat.OnAttacked(_selfStat);
            }
            Telemetry.AttackHit("벼루게", pincerTelegraph, TELEMETRY_STAGE);
        }
        else
        {
            Telemetry.AttackDodged("벼루게", pincerTelegraph, Telemetry.ComputeDodgeDirectionFromPositions(dodgeCheckStartPos, _player.position), TELEMETRY_STAGE);
        }
    }

    // 방패 중 피격당하면 공격자(플레이어)에게 먹물 도트 데미지를 튀긴다
    public void SplashInkOnAttacker(Stat attacker)
    {
        StartCoroutine(SplashDot(attacker));
    }

    private IEnumerator SplashDot(Stat attacker)
    {
        yield return new WaitForSeconds(shieldBurnDelay);
        if (attacker == null || _selfStat == null) yield break;

        _selfStat.Attack = SHIELD_BURN_DAMAGE;
        attacker.OnAttacked(_selfStat);
    }

    protected override void OnDie()
    {
        if (_isDead) return;
        _isDead = true;
        gameObject.SetActive(false);
    }
}
