using System.Collections;
using UnityEngine;

// 스테이지1 보스 "도깨비 장군" 본체/분신 공용 컨트롤러. 시간 제약상 단순한 패턴으로 설계:
// 계속 "일점돌파"(직선 돌진)를 반복하다가 암석(BossRockMarker)에 부딪히면 잠깐 무방비(헤롱헤롱)
// 상태가 되고, 그때만 실제로 피격됨(그 외엔 Defense를 사실상 무한으로 올려서 무적 처리).
// isRealBoss=false(페이즈2 분신)는 헤롱헤롱 중 맞으면 죽는 대신 좀비로 변신한다.
public class GoblinGeneralCharger : BaseMonsterController
{
    private const int TELEMETRY_STAGE = 3;
    // 카메라가 방 전체를 못 담아서(플레이어 추적 카메라) 장군이 화면 밖에서 돌진을 시작할 수 있음 -
    // 그래서 예고 시간을 넉넉히 2초로 두고, 그 사이 반투명 붉은 선으로 궤도를 미리 보여준다.
    private const float ATTACK_TELEGRAPH_DURATION = 2f;
    private const int CHARGE_DAMAGE = 35;
    private const float CHARGE_SPEED = 18f;
    private const float CHARGE_MAX_DISTANCE = 60f; // 보스방이 78x42로 커져서 비례 확대(기존 26x14 기준 20)
    private const float DAZE_DURATION = 2.5f;
    private const float RECOVER_DURATION = 1f;
    private const int INVULNERABLE_DEFENSE = 9999;
    private const float ROCK_CHECK_RADIUS = 0.6f;
    private const float CONTACT_DAMAGE_INTERVAL = 0.5f; // 페이즈2에서 계속 붙어있을 때 데미지 재적용 간격

    public bool isRealBoss = true;
    public GoblinGeneralBossDirector director;

    // 페이즈2에서는 조준 중에도 접촉 데미지가 들어감(암석이 거의 부서져서 헤롱헤롱만 노리기엔
    // 너무 쉬워지는 문제 보완). director가 페이즈2 시작 시 true로 설정.
    public bool isPhase2 = false;

    // 창 끝이 항상 플레이어를 향하게 - 아트가 정사각 캔버스 안에 대각선으로 그려져 있어서
    // "회전 0도일 때 실제로 창이 가리키는 각도"만큼 보정치가 필요함. 육안상 대략 127도 근처인데
    // 눈으로 보고 어긋나면 인스펙터에서 이 값만 조절하면 됨(코드 수정 불필요).
    public float weaponAngleOffset = 127f;

    public Stat BossStat => _stat;

    private bool _started;
    private bool _dazed;
    private bool _isCharging;
    private bool _hitPlayerThisCharge;
    private float _contactDamageCooldown;
    private Coroutine _chargeLoopCoroutine;
    private Transform _weaponTransform;

    // 페이즈2 연출(카메라가 분신 하나하나를 포커싱한 뒤 전투 시작) 동안은 director가 false로
    // 막아뒀다가 연출이 끝나면 true로 풀어줌 - 그 전까지는 소환되자마자 혼자 돌진 시작하면 안 됨.
    public bool autoStart = true;

    protected override void UpdateIdle()
    {
        if (_started || !autoStart) return;
        _started = true;

        _lockTarget = Managers.Game.GetPlayer();
        _weaponTransform = transform.Find("Weapon");
        _stat.Defense = INVULNERABLE_DEFENSE;
        RestartCharge();
    }

    protected override void UpdateMoving() { }
    protected override void UpdateSkill() { }
    protected override void UpdateReturn() { }

    protected override IEnumerator AttackRoutine()
    {
        // 이 보스는 추적-후-공격이 아니라 항상 직선 돌진이라 BaseMonsterController의 기본 공격
        // 트리거 경로를 안 쓴다. 추상 멤버라 형식상 구현만 해둠.
        yield break;
    }

    // director가 본체를 페이즈2 자리로 재배치한 뒤 호출 - 진행 중이던 돌진 루프를 새로 시작.
    public void RestartCharge()
    {
        if (_chargeLoopCoroutine != null) StopCoroutine(_chargeLoopCoroutine);
        _chargeLoopCoroutine = StartCoroutine(ChargeLoop());
    }

    // 페이즈2 연출 시작 시 director가 호출 - 안 그러면 기존 ChargeLoop이 연출 도중에도 계속
    // 돌면서 본체 위치를 제멋대로 옮겨버림(카메라 연출과 충돌).
    public void StopCharging()
    {
        if (_chargeLoopCoroutine != null)
        {
            StopCoroutine(_chargeLoopCoroutine);
            _chargeLoopCoroutine = null;
        }
    }

    private IEnumerator ChargeLoop()
    {
        while (true)
        {
            yield return StartCoroutine(ChargeOnce());

            if (isRealBoss) director?.CheckPhase2Trigger();

            if (!_dazed)
                yield return new WaitForSeconds(RECOVER_DURATION);
        }
    }

    private IEnumerator ChargeOnce()
    {
        if (_lockTarget == null) _lockTarget = Managers.Game.GetPlayer();
        if (_lockTarget == null)
        {
            yield return new WaitForSeconds(RECOVER_DURATION);
            yield break;
        }

        // 360도 어느 방향이든 플레이어를 향해 직선으로 조준(가로/세로 제한 없음 - 정규화된 벡터라 이미 자유각도).
        Vector2 dir = ((Vector2)_lockTarget.transform.position - (Vector2)transform.position).normalized;
        FaceDirection(dir);

        Telemetry.AttackTelegraphStart("도깨비장군", ATTACK_TELEGRAPH_DURATION, TELEMETRY_STAGE);
        ShowTelegraphLine(dir);

        _hitPlayerThisCharge = false;

        yield return new WaitForSeconds(ATTACK_TELEGRAPH_DURATION);
        HideTelegraphLine();

        _isCharging = true;
        float traveled = 0f;

        while (traveled < CHARGE_MAX_DISTANCE)
        {
            float step = CHARGE_SPEED * Time.deltaTime;
            transform.position += (Vector3)(dir * step);
            traveled += step;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, ROCK_CHECK_RADIUS);
            BossRockMarker hitRock = null;
            foreach (var h in hits)
            {
                var marker = h.GetComponent<BossRockMarker>();
                if (marker != null) { hitRock = marker; break; }
            }
            if (hitRock != null)
            {
                _isCharging = false;
                // 한 번 부딪힌 암석은 부서져서 없어짐 - 같은 자리를 재사용 못 하게.
                StartCoroutine(GoblinGeneralBossDirector.SpawnSmokePoof(hitRock.transform.position));
                Destroy(hitRock.gameObject);

                yield return StartCoroutine(DazeSequence());
                yield break;
            }

            yield return null;
        }

        _isCharging = false;
        if (!_hitPlayerThisCharge)
            Telemetry.AttackDodged("도깨비장군", ATTACK_TELEGRAPH_DURATION, "none", TELEMETRY_STAGE);
    }

    // 페이즈1: 실제 돌진 중(_isCharging)에만 위험. 페이즈2: 헤롱헤롱(_dazed)만 아니면 조준/정지/
    // 돌진 전부 위험 - 암석이 거의 다 부서져서 헤롱헤롱 기회 자체가 희귀해지는 문제 보완.
    private bool IsDangerous()
    {
        if (_dazed) return false;
        return isPhase2 || _isCharging;
    }

    // "창이 닿는 것처럼 보이는데 데미지가 안 들어간다"는 피드백으로 거리 판정 대신 실제 몸통
    // 콜라이더 접촉(트리거)으로 교체. Enter뿐 아니라 Stay도 받아서 - 조준/정지 중에 플레이어가
    // 이미 붙어있던 경우(새로 "닿는" 이벤트가 안 생기는 경우)도 놓치지 않게 한다.
    private void OnTriggerEnter2D(Collider2D other) => TryDealContactDamage(other);
    private void OnTriggerStay2D(Collider2D other) => TryDealContactDamage(other);

    private void TryDealContactDamage(Collider2D other)
    {
        if (!IsDangerous()) return;
        if (_contactDamageCooldown > 0f) return;
        if (!other.CompareTag("Player")) return;

        Stat playerStat = other.GetComponent<Stat>();
        if (playerStat == null) return;

        _stat.Attack = CHARGE_DAMAGE;
        playerStat.OnAttacked(_stat);
        Telemetry.AttackHit("도깨비장군", ATTACK_TELEGRAPH_DURATION, TELEMETRY_STAGE);
        _hitPlayerThisCharge = true;
        _contactDamageCooldown = CONTACT_DAMAGE_INTERVAL;
    }

    private IEnumerator DazeSequence()
    {
        _dazed = true;
        _stat.Defense = 0;
        // 지금이 "때릴 기회"라는 걸 눈에 띄게 - 회전 흔들림만으로는 잘 안 보인다는 피드백 반영.
        if (_spriteRenderer != null) _spriteRenderer.color = new Color(1f, 0.85f, 0.3f);

        int hpBefore = _stat.Hp;
        float wobbleTimer = 0f;
        while (wobbleTimer < DAZE_DURATION && _stat.Hp > 0)
        {
            wobbleTimer += Time.deltaTime;
            float angle = Mathf.Sin(wobbleTimer * 12f) * 12f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        transform.rotation = Quaternion.identity;
        if (_spriteRenderer != null) _spriteRenderer.color = Color.white;

        if (_stat.Hp < hpBefore && _stat.Hp > 0) Telemetry.PlayerPunish("도깨비장군", TELEMETRY_STAGE);

        _dazed = false;
        if (_stat.Hp > 0) _stat.Defense = INVULNERABLE_DEFENSE;
    }

    private LineRenderer _telegraphLine;

    // 카메라가 장군을 못 담고 있어도 붉은 궤도선은 화면에 걸칠 수 있게 방 전체 길이만큼 길게 그린다.
    private void ShowTelegraphLine(Vector2 dir)
    {
        if (_telegraphLine == null)
        {
            GameObject lineGO = new GameObject("TelegraphLine");
            lineGO.transform.SetParent(transform, false);
            _telegraphLine = lineGO.AddComponent<LineRenderer>();
            _telegraphLine.material = new Material(Shader.Find("Sprites/Default"));
            _telegraphLine.startColor = new Color(1f, 0f, 0f, 0.4f);
            _telegraphLine.endColor = new Color(1f, 0f, 0f, 0.05f);
            _telegraphLine.startWidth = 0.3f;
            _telegraphLine.endWidth = 0.3f;
            _telegraphLine.positionCount = 2;
            _telegraphLine.sortingOrder = 10;
            _telegraphLine.useWorldSpace = true;
        }

        _telegraphLine.SetPosition(0, transform.position);
        _telegraphLine.SetPosition(1, transform.position + (Vector3)(dir * CHARGE_MAX_DISTANCE));
        _telegraphLine.enabled = true;
    }

    private void HideTelegraphLine()
    {
        if (_telegraphLine != null) _telegraphLine.enabled = false;
    }

    private void FaceDirection(Vector2 dir)
    {
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? 1f : -1f;
            transform.localScale = new Vector3(xTargetScale * Mathf.Abs(_initialScale.x), _initialScale.y, _initialScale.z);
        }
    }

    // 계측 전용: 교전 중 0.5초마다 플레이어와의 거리 샘플링.
    private float _telemetrySampleTimer;
    private void LateUpdate()
    {
        AimWeaponAtPlayer();

        if (_contactDamageCooldown > 0f) _contactDamageCooldown -= Time.deltaTime;

        if (_lockTarget == null) return;

        _telemetrySampleTimer += Time.deltaTime;
        if (_telemetrySampleTimer < 0.5f) return;
        _telemetrySampleTimer = 0f;

        float distance = Vector2.Distance(transform.position, _lockTarget.transform.position);
        Telemetry.PlayerPositionSample("도깨비장군", distance, _isCharging, TELEMETRY_STAGE);
    }

    // 부모(본체)가 좌우 반전(localScale.x 부호 변경)되면 자식 회전도 같이 뒤틀리기 때문에,
    // 로컬 회전이 아니라 월드 회전을 직접 지정해서 부모 반전과 무관하게 항상 플레이어를 향하게 한다.
    private void AimWeaponAtPlayer()
    {
        if (_weaponTransform == null || _lockTarget == null || _dazed) return;

        Vector2 dir = (Vector2)_lockTarget.transform.position - (Vector2)_weaponTransform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _weaponTransform.rotation = Quaternion.Euler(0f, 0f, angle - weaponAngleOffset);
    }

    protected override void OnDie()
    {
        // 헤롱헤롱 중 여러 대를 연속으로 맞아서 페이즈2 문턱(1/3)을 건너뛰고 한 번에 0 밑으로
        // 떨어진 경우 - CheckPhase2Trigger는 돌진이 한 번 끝나야 호출되는데 그 전에 죽어버려서
        // 페이즈2를 영영 못 보는 버그였음. 아직 페이즈2 전이면 죽지 않고 대신 페이즈2를 강제로 연다.
        if (isRealBoss && director != null && !director.Phase2Triggered)
        {
            // Hp를 1로 두면 페이즈2가 시작되자마자 한 대에 죽어버려서, 정상적으로 문턱을 밟고
            // 넘어온 경우와 난이도가 완전히 달라짐 - 어느 경로로 오든 같은 체력에서 시작하게 맞춘다.
            _stat.Hp = Mathf.Max(1, _stat.Total_MaxHp / 3);
            State = Define.State.Idle;
            director.CheckPhase2Trigger();
            return;
        }

        StopAllCoroutines();

        if (isRealBoss)
        {
            director?.OnRealBossDefeated();
            base.OnDie();
        }
        else
        {
            StartCoroutine(TransformIntoZombie());
        }
    }

    private IEnumerator TransformIntoZombie()
    {
        transform.rotation = Quaternion.identity;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        yield return GoblinGeneralBossDirector.SpawnSmokePoof(transform.position);

        if (_spriteRenderer != null && director != null && director.zombieRunSprites != null && director.zombieRunSprites.Length > 0)
        {
            _spriteRenderer.sprite = director.zombieRunSprites[0];
        }

        if (director != null)
        {
            float sign = transform.localScale.x < 0 ? -1f : 1f;
            transform.localScale = new Vector3(director.zombieScale * sign, director.zombieScale, director.zombieScale);
        }

        Transform weapon = transform.Find("Weapon");
        if (weapon != null) weapon.gameObject.SetActive(false);

        if (col != null) col.enabled = true;

        var chaser = gameObject.AddComponent<ZombieChaser>();
        chaser.Init(_lockTarget, director);

        Destroy(this);
    }
}
