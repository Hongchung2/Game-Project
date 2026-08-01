using System.Collections;
using UnityEngine;

// 먹등불(원거리형 몬스터). 평소엔 스폰 지점 주변을 배회하다, 플레이어를 발견하면
// 사거리 안까지 다가가서 도깨비불을 4연발로 쏜다.
// '어둠 잠식'(화면 흐림 연출)은 셰이더 작업이 필요해 이번 버전에서는 제외.
public class MeokdeungbulController : BaseController
{
    private const float ATTACK_TELEGRAPH_DURATION = 0f; // 코드상 발사 전 별도 대기 없음(실측값 그대로 보고)
    private const int TELEMETRY_STAGE = 2; // 명세서 4.4 가중치 태깅용

    [Header("이동 (배회 + 추적)")]
    public float moveSpeed = 1.2f;
    public float sightRange = 10f;     // 이 범위 안에 플레이어가 들어오면 발견(추적 시작)
    public float wanderRadius = 2.5f;  // 스폰 지점 기준 배회 반경
    public float wanderPauseMin = 1f;
    public float wanderPauseMax = 2.5f;
    public LayerMask wallLayer = 1;    // 배회 목표가 벽 안이면 재추첨 (기본값 Default 레이어=Wall이 있는 레이어)

    [Header("먹등불 - 원거리 공격")]
    public float detectRange = 8f;      // 공격 사거리(길다)
    public float attackInterval = 3f;
    public GameObject projectilePrefab;
    public int burstCount = 4;          // 직선 방향으로 4회 연속 발사
    public float burstGap = 0.15f;      // 발사 간격

    private Transform _player;
    private bool _isDead = false;
    private Vector3 _spawnPos;
    private Vector3 _initialScale;
    private Stat _selfStat; // 계측 전용: 후딜 중 반격당했는지 HP 비교로 확인하기 위함

    public override void Init()
    {
        WorldObjectType = Define.WorldObject.Monster;

        _spawnPos = transform.position;
        _initialScale = transform.localScale;
        _selfStat = GetComponent<Stat>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        StartCoroutine(AttackLoop());
    }

    private IEnumerator AttackLoop()
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

            yield return StartCoroutine(FireBurst());
            int hpBeforeCooldown = _selfStat != null ? _selfStat.Hp : 0; // 후딜(쿨다운) 구간 중 반격당했는지 확인용
            yield return new WaitForSeconds(attackInterval);
            if (_selfStat != null && _selfStat.Hp < hpBeforeCooldown) Telemetry.PlayerPunish("먹등불", TELEMETRY_STAGE);
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

        Telemetry.PlayerPositionSample("먹등불", dist, dist <= detectRange, TELEMETRY_STAGE);
    }

    // 사거리 밖 ~ 발견 범위 안: 사거리 안으로 들어올 때까지 다가간다
    private IEnumerator ChasePlayer()
    {
        while (!_isDead && _player != null)
        {
            float dist = Vector2.Distance(transform.position, _player.position);
            if (dist <= detectRange || dist > sightRange) break;

            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            transform.position = Vector2.MoveTowards(transform.position, _player.position, moveSpeed * Time.deltaTime);
            FlipTowards(dir.x);
            yield return null;
        }
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
                yield break; // 발견하면 즉시 AttackLoop로 복귀해서 추적 전환

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

    private IEnumerator FireBurst()
    {
        if (projectilePrefab == null || _player == null) yield break;

        Telemetry.AttackTelegraphStart("먹등불", ATTACK_TELEGRAPH_DURATION, TELEMETRY_STAGE);

        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;

        for (int i = 0; i < burstCount; i++)
        {
            GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            DokkaebiFireProjectile p = proj.GetComponent<DokkaebiFireProjectile>();
            if (p != null) p.Init(dir);
            TelemetryDokkaebiFireObserver.Attach(proj, "먹등불", ATTACK_TELEGRAPH_DURATION, _player.gameObject, TELEMETRY_STAGE);

            yield return new WaitForSeconds(burstGap);
        }
    }

    protected override void OnDie()
    {
        if (_isDead) return;
        _isDead = true;
        gameObject.SetActive(false);
    }
}
