using System.Collections;
using UnityEngine;

// 산수원의 주인 — 묵운산군, 1페이즈(표식 전투) 본체.
// (15,5) 상당 위치에 부유, 좌사/우사가 궤도 순환하며 4초마다 도깨비불 3발을 플레이어에게 발사.
// 피격 시 mark_weight로 산/구름 표식을 확률적으로 부여하고, 우사(산)/좌사(구름)가 발동한다.
public class MukunSangunController : BaseController
{
    private const int TELEMETRY_STAGE = 3; // 보스 자신
    private const string MONSTER_NAME = "묵운산군";

    [Header("판단 필요 항목 - 명세서 미기재, 임의값(플레이테스트로 재조정 예정)")]
    // 명세서 4-1: "플레이어 HP 10, 검 공격력 1, 공격 쿨 0.3초 기준 2~4분 전투"가 나오는 값으로 추정.
    public int baseHp = 90;
    public float projectileSpeed = 12f; // 먹등불 도깨비불 기본값 재사용
    public float orbitRadius = 2f;
    public float orbitSpeedDegPerSec = 60f;
    public float mountainHitRadius = 1f; // 파편 낙하 판정 반경

    [Header("공격 주기")]
    public float volleyInterval = 4f;
    public int boltCount = 3;
    public float boltGap = 0.15f;

    // 명세서 3.3/3.4: 볼트 명중 자체의 고정 데미지 - 표식 발동 데미지와는 별개 이벤트로 합산됨(대체 아님).
    private const int BOLT_HIT_DAMAGE = 1;

    [Header("산 표식 (파편 낙하) - 명세서 3.3/6.2 그대로")]
    public float mountainAirborneDuration = 1.5f;
    private const int MOUNTAIN_MARK_DAMAGE = 2;

    [Header("구름 표식 (먹구름 장막) - 명세서 3.3/7.2 그대로")]
    public float cloudVeilDuration = 4f;
    private const int CLOUD_MARK_DOT_TOTAL = 1;
    public float cloudMoveSpeedMultiplier = 0.75f; // 이동속도 25% 감소
    public float cloudTelegraphDuration = 0.3f; // 명세서 미기재, 임의값

    public GameObject dokkaebiFireBoltPrefab; // 비워두면 Resources/Prefabs/DokkaebiFire 자동 로드

    private const int UNMUGANGRIM_DAMAGE = 2;
    private const int PUNCH_DAMAGE = 2;
    private const int SLAM_DAMAGE = 3;

    [Header("2페이즈 전환 - 명세서 미기재, 임의값(보고 대상)")]
    [Range(0f, 1f)] public float phase2TransitionHpRatio = 0.5f; // "HP 50% 이하"
    public float descentDuration = 1f; // 운무강림 하강 연출 시간
    public float unmugangrimRadius = 7f; // slam과 동일

    [Header("2페이즈 패턴 - 명세서 3.3/3.5 그대로, 나머지는 임의값(보고 대상)")]
    public float phase2AttackInterval = 1.5f; // 패턴 사이 대기(판단 필요)
    public float punchChargeDuration = 1.5f;
    public float punchRange = 5f;
    public float punchHalfAngleDeg = 45f; // "정면 부채꼴" 각도, 명세서 미기재(판단 필요)
    public float slamChargeDuration = 2f;
    public float slamRadius = 7f;
    public float recoveryExposedDuration = 1.5f;  // phase2_recovery_exposed==true (정직하게 길게)
    public float recoveryHiddenDuration = 0.4f;   // phase2_recovery_exposed==false (짧게)

    [Header("2페이즈 최소 재접근 - 명세서 2.1절 그대로")]
    public float reapproachTriggerDistanceMargin = 3f; // 트리거 거리 = punchRange + 이 값
    public float reapproachTriggerDuration = 3f;        // 이 거리 밖에 이만큼 지속되면 발동
    public float reapproachSpeed = 5f;                   // 플레이어 MoveSpeed(10)의 절반

    [Header("회피 + 1페이즈 배회 - 명세서 미기재, 시나리오 A 피드백 반영(보고 대상)")]
    // 사용자 피드백: "칼 빼들고 다가가는데 안 피하는 보스가 어딨냐" - 1/2페이즈 모두
    // 플레이어가 검 사거리(2) 안쪽으로 붙으면 짧게 물러남. 2페이즈는 패턴 실행 중(예고~후딜)이
    // 아닌 대기 구간에서만 발동(2.1절 재접근과 동일한 게이팅 - Phase2IdleAndReapproach 안에서만 체크).
    public float meleeDodgeTriggerRange = 3f;
    public float dodgeDistance = 4f;
    public float dodgeDuration = 0.35f;
    public float dodgeCooldown = 1.5f;

    [Header("1페이즈 배회 - 명세서 미기재, 시나리오 A 피드백 반영(보고 대상)")]
    // "부유" 상태도 완전히 고정이면 안 된다는 피드백 - 회피 트리거가 없을 때도 주기적으로
    // 기준 위치 근처를 천천히 떠다니게 함(궤도 순환하는 좌사/우사와는 별개로 보스 본체 자체가 움직임).
    public float phase1RepositionInterval = 5f;
    public float phase1RepositionRadius = 3f;
    public float phase1DriftSpeed = 2f;

    private const float ARENA_MIN = 2f; // 30x30 아레나 안쪽으로 벽에 안 붙게(BossRoomSceneBuilder ROOM_SIZE=30과 맞춤)
    private const float ARENA_MAX = 28f;

    private Vector2 _phase1BasePosition;
    private bool _isDodging;
    private float _dodgeCooldownTimer;

    private Transform _player;
    private Stat _playerStat;
    private Stat _selfStat;
    private SasaOrbitController _leftSasa;
    private SasaOrbitController _rightSasa;
    private bool _isDead;
    private bool _decisionReady;
    private bool _phase2Triggered;
    private BossDecision _decision;
    private static Sprite _circleSpriteCache;

    public override void Init()
    {
        WorldObjectType = Define.WorldObject.Monster;
        _selfStat = GetComponent<Stat>();
        if (_selfStat != null)
        {
            _selfStat.base_MaxHp = baseHp;
            _selfStat.InitStats();
        }

        // BossRoomSceneBuilder가 SpriteRenderer는 붙였지만 Sprite는 비워둔 채였음(플레이테스트에서
        // "보스가 안 보임"으로 발견) - 씬 빌드 시점엔 없던 스프라이트를 여기서 채워줌.
        var selfSr = GetComponent<SpriteRenderer>();
        if (selfSr != null && selfSr.sprite == null)
            selfSr.sprite = GetOrCreateCircleSprite();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerStat = playerObj.GetComponent<Stat>();
        }

        Transform phase1Position = FindMarker("BossPhase1Position");
        if (phase1Position != null) transform.position = phase1Position.position;
        _phase1BasePosition = transform.position;

        if (dokkaebiFireBoltPrefab == null)
            dokkaebiFireBoltPrefab = Resources.Load<GameObject>("Prefabs/DokkaebiFire");

        _leftSasa = CreateSasa("좌사", FindMarker("LeftOrbitCenter"), SasaSide.Left);
        _rightSasa = CreateSasa("우사", FindMarker("RightOrbitCenter"), SasaSide.Right);

        int attempt = ComputeAttemptNumber();
        EmotionCurveStateMachine.Begin(attempt);

        // M8: 채널1(테두리)/채널2(화선지)/채널링 실루엣 초기화 - 전투 로직과 독립적인 순수 표출 계층.
        BorderInkFrame.CreateInstance().SetInitialFrameForAttempt(attempt);
        InkCanvas.CreateInstance();
        ChannelingSilhouetteOverlay.Initialize();

        StartCoroutine(RequestDecisionAndBegin());
        StartCoroutine(Phase1MovementLoop());
    }

    private IEnumerator RequestDecisionAndBegin()
    {
        var history = DeathHistoryTracker.Current;
        BossDecision decision;

        // 작업지시서 #09: 인터미션 영상 재생 중에 이미 요청을 시작해뒀으면(BossDecisionPrefetch)
        // 그 결과를 그대로 씀 - 보통 영상이 API 응답보다 훨씬 기니까 여기서 기다릴 일이 거의 없음.
        // 프리페치가 없으면(보스방 씬을 곧장 Play해서 테스트하는 경우 등) 기존처럼 그 자리에서 요청.
        if (BossDecisionPrefetch.HasPendingOrReady)
        {
            bool wasReadyAlready = BossDecisionPrefetch.IsReady;
            yield return new WaitUntil(() => BossDecisionPrefetch.IsReady);
            decision = BossDecisionPrefetch.Consume();
            Debug.Log(wasReadyAlready
                ? "[MukunSangunController] 보스방 입장 - 인터미션 중 프리페치된 판단을 즉시 사용"
                : "[MukunSangunController] 보스방 입장 - 프리페치가 아직 안 끝나서 잠깐 대기 후 사용");
        }
        else
        {
            Debug.Log("[MukunSangunController] 프리페치 없음(보스방 직접 Play) - 지금부터 요청");
            var profile = PlayerProfileAggregator.GetCurrentProfile();
            BossDecision fetched = null;
            yield return BossDecisionClient.RequestDecision(profile, history, d => fetched = d);
            decision = fetched;
        }

        // BossDecisionClient는 항상 non-null을 콜백하지만(내부적으로 이미 폴백 처리됨), 방어적으로 한 번 더 체크.
        _decision = decision ?? new BossDecision();
        MarkSystem.Configure(_decision);
        _decisionReady = true;

        BossDialogueDisplay.CreateInstance(transform).Configure(_decision, history);

        StartCoroutine(VolleyLoop());
    }

    private int ComputeAttemptNumber()
    {
        var h = DeathHistoryTracker.Current;
        return h.summaryOlder.totalAttempts + h.recent.Count + 1;
    }

    private static Transform FindMarker(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.transform : null;
    }

    private SasaOrbitController CreateSasa(string name, Transform center, SasaSide side)
    {
        GameObject go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateCircleSprite();
        sr.color = side == SasaSide.Left ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.55f, 0.45f, 0.3f); // 임의 placeholder 색
        sr.sortingOrder = 3;
        go.transform.localScale = Vector3.one * 0.8f;
        if (center != null) go.transform.position = center.position;

        var orbit = go.AddComponent<SasaOrbitController>();
        orbit.orbitRadius = orbitRadius;
        orbit.orbitSpeedDegPerSec = orbitSpeedDegPerSec;
        orbit.Init(center, side);
        return orbit;
    }

    private IEnumerator VolleyLoop()
    {
        while (!_isDead && !_phase2Triggered)
        {
            yield return new WaitForSeconds(volleyInterval);
            if (_isDead || _phase2Triggered) yield break;
            yield return FireVolley();
        }
    }

    private IEnumerator FireVolley()
    {
        if (_isDead || _phase2Triggered || _player == null || dokkaebiFireBoltPrefab == null) yield break;

        // 먹등불 실측과 동일하게 무예고(0초) 채널링 - 명세서 4.6 참고.
        Telemetry.AttackTelegraphStart(MONSTER_NAME, 0f, TELEMETRY_STAGE);
        ChannelingSignal.Raise(ChanneledMonster.Meokdeungbul, 0f);

        for (int i = 0; i < boltCount; i++)
        {
            if (_isDead || _player == null) yield break;

            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            GameObject boltObj = Instantiate(dokkaebiFireBoltPrefab, transform.position, Quaternion.identity);

            // 먹등불 자체 데미지 로직 제거(보스는 표식 부여만 함, 기존 파일은 무수정).
            var existingScript = boltObj.GetComponent<DokkaebiFireProjectile>();
            if (existingScript != null) Destroy(existingScript);

            var bolt = boltObj.AddComponent<BossDokkaebiFireBolt>();
            bolt.speed = projectileSpeed;
            bolt.Init(dir, OnDokkaebiFireHitPlayer);

            TelemetryDokkaebiFireObserver.Attach(boltObj, MONSTER_NAME, 0f, _player.gameObject, TELEMETRY_STAGE);

            yield return new WaitForSeconds(boltGap);
        }
    }

    private void OnDokkaebiFireHitPlayer()
    {
        if (_isDead || _phase2Triggered) return; // 2페이즈 전환 중 날아가던 볼트는 표식 부여 없이 무시

        // 명세서 3.4: 볼트 명중 자체가 이미 하나의 데미지 이벤트(표식 발동과 합산, 대체 아님).
        // 명중/회피 텔레메트리는 TelemetryDokkaebiFireObserver가 이미 담당하므로 여기선 데미지만 적용.
        DealFixedDamageToPlayer(BOLT_HIT_DAMAGE);
        NotifyHpPercentToEmotionCurve();

        MarkType mark = MarkSystem.RollMark();
        if (mark == MarkType.Mountain)
            StartCoroutine(MountainMarkSequence());
        else
            StartCoroutine(CloudMarkSequence());
    }

    // 우사 발동 - 파편 낙하. 낙하지점은 mountain_target에 따라 결정(habitual/current).
    private IEnumerator MountainMarkSequence()
    {
        if (_player == null || _playerStat == null) yield break;

        _rightSasa?.PlayActivateCue();

        Vector2 fallback = _player.position;
        Vector2 targetPos = MarkSystem.MountainTarget == "habitual_position"
            ? HabitualPositionTracker.GetMostVisitedWorldPosition(fallback)
            : fallback;

        // 공정성 요구사항: 실루엣 없이도 회피 가능하도록 바닥에 명확한 예고 표시.
        GameObject telegraphMarker = CreateGroundTelegraphMarker(targetPos, Color.red, mountainHitRadius);

        Telemetry.AttackTelegraphStart(MONSTER_NAME, mountainAirborneDuration, TELEMETRY_STAGE);
        ChannelingSignal.Raise(ChanneledMonster.Mukryeong, mountainAirborneDuration);
        Vector2 startPlayerPos = _player.position;

        yield return new WaitForSeconds(mountainAirborneDuration);

        if (telegraphMarker != null) Destroy(telegraphMarker);
        if (_player == null || _playerStat == null) yield break;

        float dist = Vector2.Distance(targetPos, _player.position);
        if (dist <= mountainHitRadius)
        {
            DealFixedDamageToPlayer(MOUNTAIN_MARK_DAMAGE);
            Telemetry.AttackHit(MONSTER_NAME, mountainAirborneDuration, TELEMETRY_STAGE);
            EmotionCurveStateMachine.NotifyMarkHit();
            NotifyHpPercentToEmotionCurve();
        }
        else
        {
            string dodgeDir = Telemetry.ComputeDodgeDirectionFromPositions(startPlayerPos, _player.position);
            Telemetry.AttackDodged(MONSTER_NAME, mountainAirborneDuration, dodgeDir, TELEMETRY_STAGE);
        }
    }

    // 좌사 발동 - 먹구름 장막. 표식 자체가 이미 적중을 의미하므로(볼트에 맞아야 뽑히는 표식)
    // 회피 판정 없이 바로 적용. 도트 총합이 1이라 여러 틱으로 쪼개는 대신 장막 시작 시 1회로
    // 적용하고, 이동속도 감소만 4초간 지속시킴(명세서 7.2 "도트 총합 1" 그대로, 분배 방식은 임의 판단).
    private IEnumerator CloudMarkSequence()
    {
        if (_player == null || _playerStat == null) yield break;

        _leftSasa?.PlayActivateCue();

        Telemetry.AttackTelegraphStart(MONSTER_NAME, cloudTelegraphDuration, TELEMETRY_STAGE);
        ChannelingSignal.Raise(ChanneledMonster.Meokdeungbul, cloudTelegraphDuration);
        yield return new WaitForSeconds(cloudTelegraphDuration);

        if (_player == null || _playerStat == null) yield break;

        DealFixedDamageToPlayer(CLOUD_MARK_DOT_TOTAL);
        Telemetry.AttackHit(MONSTER_NAME, cloudTelegraphDuration, TELEMETRY_STAGE);
        EmotionCurveStateMachine.NotifyMarkHit();
        NotifyHpPercentToEmotionCurve();

        float originalSpeed = _playerStat.MoveSpeed;
        _playerStat.MoveSpeed = originalSpeed * cloudMoveSpeedMultiplier;

        yield return new WaitForSeconds(cloudVeilDuration);

        if (_playerStat != null) _playerStat.MoveSpeed = originalSpeed;
    }

    private void DealFixedDamageToPlayer(int amount)
    {
        if (_playerStat == null || _selfStat == null) return;
        _selfStat.Attack = amount; // 명세서 3.3의 고정 정수값을 그대로 전달(Stat.OnAttacked 관례 재사용)
        _playerStat.OnAttacked(_selfStat);
    }

    private void NotifyHpPercentToEmotionCurve()
    {
        if (_playerStat == null || _playerStat.MaxHp <= 0) return;
        float pct = (float)_playerStat.Hp / _playerStat.MaxHp;
        EmotionCurveStateMachine.NotifyHpPercent(pct);
    }

    private float _telemetrySampleTimer;
    private void LateUpdate()
    {
        if (_isDead) return;

        EmotionCurveStateMachine.Tick(Time.deltaTime);

        // 명세서: "HP 50% 이하" 페이즈 전환 조건 - 여기서 말하는 HP는 보스 자신의 체력
        // (M5의 플레이어 HP 60% 안전판과는 별개 트리거).
        if (!_phase2Triggered && _decisionReady && _selfStat != null && _selfStat.MaxHp > 0
            && (float)_selfStat.Hp / _selfStat.MaxHp <= phase2TransitionHpRatio)
        {
            _phase2Triggered = true;
            StartCoroutine(UnmugangrimSequence());
        }

        if (_player == null) return;
        HabitualPositionTracker.Tick(_player.position, Time.deltaTime);

        _telemetrySampleTimer += Time.deltaTime;
        if (_telemetrySampleTimer < 0.5f) return;
        _telemetrySampleTimer = 0f;

        float dist = Vector2.Distance(transform.position, _player.position);
        Telemetry.PlayerPositionSample(MONSTER_NAME, dist, false, TELEMETRY_STAGE);
    }

    // 운무강림: 좌사/우사 정리 -> 착지지점으로 하강(예고 표시 동반) -> 착지 충격파 -> 감정곡선 Chill 전이 -> 2페이즈 루프 시작.
    private IEnumerator UnmugangrimSequence()
    {
        if (_leftSasa != null) Destroy(_leftSasa.gameObject);
        if (_rightSasa != null) Destroy(_rightSasa.gameObject);

        Transform landing = FindMarker("Phase2LandingPoint");
        Vector2 landingPos = landing != null ? (Vector2)landing.position : (Vector2)transform.position;

        GameObject telegraphMarker = CreateGroundTelegraphMarker(landingPos, Color.black, unmugangrimRadius);
        Telemetry.AttackTelegraphStart(MONSTER_NAME, descentDuration, TELEMETRY_STAGE);

        Vector3 startPos = transform.position;
        float elapsed = 0f;
        while (elapsed < descentDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, landingPos, Mathf.Clamp01(elapsed / descentDuration));
            yield return null;
        }
        transform.position = landingPos;

        if (telegraphMarker != null) Destroy(telegraphMarker);

        if (_player != null && _playerStat != null)
        {
            float dist = Vector2.Distance(landingPos, _player.position);
            if (dist <= unmugangrimRadius)
            {
                DealFixedDamageToPlayer(UNMUGANGRIM_DAMAGE);
                Telemetry.AttackHit(MONSTER_NAME, descentDuration, TELEMETRY_STAGE);
                NotifyHpPercentToEmotionCurve();
            }
            else
            {
                Telemetry.AttackDodged(MONSTER_NAME, descentDuration, "none", TELEMETRY_STAGE);
            }
        }

        EmotionCurveStateMachine.NotifyCloudDescent(); // 소름(2페이즈) 전이는 오직 이 이벤트로만

        StartCoroutine(Phase2Loop());
    }

    private IEnumerator Phase2Loop()
    {
        while (!_isDead)
        {
            yield return Phase2IdleAndReapproach();
            if (_isDead) yield break;
            yield return ExecutePhase2Pattern();
        }
    }

    // 명세서 2.1절 "2페이즈 최소 재접근 행동" - 패턴 사이 대기 구간에서만 발동(예고~후딜 중엔
    // 이 함수 자체가 실행되지 않으므로 자동으로 배제됨). 플레이어가 punchRange+3 밖에 3초 이상
    // 머물면 절반 속도로 직선 드리프트, punchRange 안으로 들어오면 즉시 멈추고 패턴 루프로 복귀.
    // 시나리오 A 피드백 반영: 같은 대기 구간에서, 플레이어가 검 사거리 안까지 붙으면 재접근보다
    // 우선해서 짧게 회피(패턴 실행 중이 아닐 때만 - 이 함수 자체가 그 구간에서만 도니까 자동 보장).
    private IEnumerator Phase2IdleAndReapproach()
    {
        float triggerDistance = punchRange + reapproachTriggerDistanceMargin;
        float elapsed = 0f;
        float farTimer = 0f;
        bool reapproaching = false;

        while (elapsed < phase2AttackInterval || reapproaching)
        {
            if (_isDead) yield break;

            if (_dodgeCooldownTimer > 0f) _dodgeCooldownTimer -= Time.deltaTime;

            if (_player != null)
            {
                float dist = Vector2.Distance(transform.position, _player.position);

                if (!_isDodging && _dodgeCooldownTimer <= 0f && dist < meleeDodgeTriggerRange)
                {
                    reapproaching = false;
                    farTimer = 0f;
                    yield return DodgeAwayFromPlayer();
                    elapsed = 0f; // 회피했으면 그만큼 대기 시간도 다시 셈
                    continue;
                }

                if (!reapproaching)
                {
                    farTimer = dist > triggerDistance ? farTimer + Time.deltaTime : 0f;
                    if (farTimer >= reapproachTriggerDuration) reapproaching = true;
                }
                else if (dist <= punchRange)
                {
                    reapproaching = false;
                    farTimer = 0f;
                }
                else
                {
                    Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
                    transform.position += (Vector3)(dir * reapproachSpeed * Time.deltaTime);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // 시나리오 A 피드백: 1페이즈("부유")도 완전히 고정이면 안 됨 - 검 사거리 안으로 붙으면
    // 피하고, 평소엔 기준 위치 근처를 천천히 배회. 2페이즈 진입(_phase2Triggered)하면 자동 종료.
    private IEnumerator Phase1MovementLoop()
    {
        float repositionTimer = 0f;

        while (!_isDead && !_phase2Triggered)
        {
            if (_dodgeCooldownTimer > 0f) _dodgeCooldownTimer -= Time.deltaTime;

            if (!_isDodging && _dodgeCooldownTimer <= 0f && _player != null &&
                Vector2.Distance(transform.position, _player.position) < meleeDodgeTriggerRange)
            {
                yield return DodgeAwayFromPlayer();
                repositionTimer = 0f;
                continue;
            }

            repositionTimer += Time.deltaTime;
            if (!_isDodging && repositionTimer >= phase1RepositionInterval)
            {
                repositionTimer = 0f;
                yield return DriftToNewSpot(_phase1BasePosition, phase1RepositionRadius, phase1DriftSpeed);
            }

            yield return null;
        }
    }

    // 짧고 재빠르게 플레이어 반대 방향으로 물러남 - 회피 동작 전용(느긋한 재접근/배회와는 속도감이 달라야 함).
    private IEnumerator DodgeAwayFromPlayer()
    {
        _isDodging = true;

        Vector2 awayDir = (Vector2)transform.position - (Vector2)_player.position;
        if (awayDir == Vector2.zero) awayDir = Random.insideUnitCircle.normalized;
        else awayDir.Normalize();

        Vector2 target = ClampToArena((Vector2)transform.position + awayDir * dodgeDistance);
        yield return MoveTo(target, dodgeDuration);

        _dodgeCooldownTimer = dodgeCooldown;
        _isDodging = false;
    }

    private IEnumerator DriftToNewSpot(Vector2 center, float radius, float speed)
    {
        Vector2 target = ClampToArena(center + Random.insideUnitCircle * radius);
        float dist = Vector2.Distance(transform.position, target);
        float duration = dist / Mathf.Max(speed, 0.01f);
        yield return MoveTo(target, duration);
    }

    private IEnumerator MoveTo(Vector2 target, float duration)
    {
        Vector2 start = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (_isDead) yield break;
            elapsed += Time.deltaTime;
            Vector2 pos = Vector2.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
            yield return null;
        }
        transform.position = new Vector3(target.x, target.y, transform.position.z);
    }

    private static Vector2 ClampToArena(Vector2 pos)
    {
        return new Vector2(Mathf.Clamp(pos.x, ARENA_MIN, ARENA_MAX), Mathf.Clamp(pos.y, ARENA_MIN, ARENA_MAX));
    }

    private IEnumerator ExecutePhase2Pattern()
    {
        float punchWeight = _decision?.phase2_pattern_weight?.punch ?? 0.5f;
        float slamWeight = _decision?.phase2_pattern_weight?.slam ?? 0.5f;
        bool usePunch = RollUsePunch(punchWeight, slamWeight);

        yield return usePunch ? PunchSequence() : SlamSequence();
    }

    // 순수 로직만 분리(자체 테스트에서 Play Mode 없이 확률분포 검증 가능하도록).
    public static bool RollUsePunch(float punchWeight, float slamWeight)
    {
        float sum = punchWeight + slamWeight;
        if (sum <= 0f) return Random.value < 0.5f;
        return Random.value * sum < punchWeight;
    }

    // 돌주먹 내려찍기 - 정면 부채꼴(반경 5, 명세서 미기재 각도는 임의로 좌우 45도 = 총 90도).
    private IEnumerator PunchSequence()
    {
        if (_player == null || _playerStat == null) yield break;

        Vector2 facingDir = ((Vector2)_player.position - (Vector2)transform.position);
        facingDir = facingDir == Vector2.zero ? Vector2.right : facingDir.normalized;

        GameObject telegraphMarker = CreateGroundTelegraphMarker(transform.position, Color.yellow, punchRange);
        Telemetry.AttackTelegraphStart(MONSTER_NAME, punchChargeDuration, TELEMETRY_STAGE);
        ChannelingSignal.Raise(ChanneledMonster.Heosuabi, punchChargeDuration);
        Vector2 startPlayerPos = _player.position;

        yield return new WaitForSeconds(punchChargeDuration);

        if (telegraphMarker != null) Destroy(telegraphMarker);
        if (_player == null || _playerStat == null) yield break;

        Vector2 toPlayer = (Vector2)_player.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;
        float angle = Vector2.Angle(facingDir, toPlayer);

        if (dist <= punchRange && angle <= punchHalfAngleDeg)
        {
            DealFixedDamageToPlayer(PUNCH_DAMAGE);
            Telemetry.AttackHit(MONSTER_NAME, punchChargeDuration, TELEMETRY_STAGE);
        }
        else
        {
            string dodgeDir = Telemetry.ComputeDodgeDirectionFromPositions(startPlayerPos, _player.position);
            Telemetry.AttackDodged(MONSTER_NAME, punchChargeDuration, dodgeDir, TELEMETRY_STAGE);
        }

        yield return Phase2Recovery();
    }

    // 양손 지면강타 - 원형 반경 7 (보스 자신 중심).
    private IEnumerator SlamSequence()
    {
        if (_player == null || _playerStat == null) yield break;

        GameObject telegraphMarker = CreateGroundTelegraphMarker(transform.position, Color.magenta, slamRadius);
        Telemetry.AttackTelegraphStart(MONSTER_NAME, slamChargeDuration, TELEMETRY_STAGE);
        ChannelingSignal.Raise(ChanneledMonster.Byeoruge, slamChargeDuration);
        Vector2 startPlayerPos = _player.position;

        yield return new WaitForSeconds(slamChargeDuration);

        if (telegraphMarker != null) Destroy(telegraphMarker);
        if (_player == null || _playerStat == null) yield break;

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= slamRadius)
        {
            DealFixedDamageToPlayer(SLAM_DAMAGE);
            Telemetry.AttackHit(MONSTER_NAME, slamChargeDuration, TELEMETRY_STAGE);
        }
        else
        {
            string dodgeDir = Telemetry.ComputeDodgeDirectionFromPositions(startPlayerPos, _player.position);
            Telemetry.AttackDodged(MONSTER_NAME, slamChargeDuration, dodgeDir, TELEMETRY_STAGE);
        }

        yield return Phase2Recovery();
    }

    // phase2_recovery_exposed: true면 후딜을 정직하게 길게(반격 노출), false면 짧게.
    private IEnumerator Phase2Recovery()
    {
        bool exposed = _decision?.phase2_recovery_exposed ?? true;
        int hpBeforeRecovery = _selfStat != null ? _selfStat.Hp : 0;

        yield return new WaitForSeconds(exposed ? recoveryExposedDuration : recoveryHiddenDuration);

        if (_selfStat != null && _selfStat.Hp < hpBeforeRecovery)
            Telemetry.PlayerPunish(MONSTER_NAME, TELEMETRY_STAGE);
    }

    protected override void OnDie()
    {
        _isDead = true;

        // 연출 없이도 최소한 "죽은 채로 계속 얻어맞는" 상태는 피하도록 콜라이더를 꺼서
        // 더 이상 타겟팅되지 않게 함(플레이테스트 중 발견 - 원샷킬 후 보스가 그대로 남아있었음).
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (_leftSasa != null) Destroy(_leftSasa.gameObject);
        if (_rightSasa != null) Destroy(_rightSasa.gameObject);

        // 작업지시서 #09: 엔딩 트리거를 Grandfather.cs(즉시 엔딩)에서 여기로 옮김 - 보스 격파가
        // 진짜 게임 클리어 시점.
        GameEndingTrigger.Trigger(this);
    }

    // 바닥 예고 표시용 원형 스프라이트를 런타임에 생성(전용 아트 없음 - 최소 placeholder).
    private static Sprite GetOrCreateCircleSprite()
    {
        if (_circleSpriteCache != null) return _circleSpriteCache;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= size / 2f ? Color.white : new Color(0f, 0f, 0f, 0f));
            }
        }
        tex.Apply();

        _circleSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _circleSpriteCache;
    }

    private GameObject CreateGroundTelegraphMarker(Vector2 pos, Color color, float radius)
    {
        GameObject go = new GameObject("MountainMarkTelegraph");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateCircleSprite();
        sr.color = new Color(color.r, color.g, color.b, 0.5f);
        sr.sortingOrder = 2; // Wall(1)보다 위 - Order in Layer 체크리스트

        go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        return go;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // ===== M9 디버그 도구 전용 (릴리즈 빌드에서는 컴파일에서 아예 빠짐) =====

    public void DebugInjectDecision(BossDecision decision)
    {
        _decision = decision;
        MarkSystem.Configure(_decision);
        BossDialogueDisplay.CreateInstance(transform).Configure(_decision, DeathHistoryTracker.Current);
    }

    public void DebugSetAttempt(int attempt)
    {
        EmotionCurveStateMachine.Begin(attempt);
        BorderInkFrame.Instance?.SetInitialFrameForAttempt(attempt);
    }

    public void DebugForcePhase2()
    {
        if (_phase2Triggered || _isDead) return;
        _phase2Triggered = true;
        StartCoroutine(UnmugangrimSequence());
    }

    // 완전한 원상복구는 아니고, 반복 테스트를 위해 좌사/우사를 되살리고 볼트 루프를 재시작.
    public void DebugRevertToPhase1()
    {
        if (!_phase2Triggered) return;
        _phase2Triggered = false;

        _leftSasa = CreateSasa("좌사", FindMarker("LeftOrbitCenter"), SasaSide.Left);
        _rightSasa = CreateSasa("우사", FindMarker("RightOrbitCenter"), SasaSide.Right);

        Transform phase1Position = FindMarker("BossPhase1Position");
        if (phase1Position != null) transform.position = phase1Position.position;
        _phase1BasePosition = transform.position;

        StartCoroutine(VolleyLoop());
        StartCoroutine(Phase1MovementLoop());
    }
#endif
}
