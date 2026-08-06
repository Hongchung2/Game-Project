using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 도깨비 장군 보스전 총괄. 본체 GoblinGeneralCharger가 체력 1/3 이하로 내려가는 시점을 감지해서
// 호출하는 페이즈2 연출(분신 7체 추가 소환 + 본체 재배치)과, 본체가 죽었을 때의 클리어 처리를 담당.
// 스테이지1의 DisguisedGoblin과 같은 방식(흰 스프라이트 스케일업+페이드)의 "펑" 연출을 재사용.
public class GoblinGeneralBossDirector : MonoBehaviour
{
    public GoblinGeneralCharger bossCharger;
    public GameObject clonePrefab;
    public Sprite[] zombieRunSprites; // Enemy 0.png의 Run 0~3
    public Sprite zombieHitSprite;
    public Sprite zombieDeadSprite;
    public Sprite bossPortraitSprite; // 도깨비 장군 상반신 초상화(대사 연출용)
    public Sprite escapePortalSprite; // 본체 처치 후 나타나는 탈출구(족자) 스프라이트
    public float zombieScale = 2f; // 좀비로 변신할 때 적용할 스케일(Enemy 0.png는 18px/18PPU라 스케일값=월드 세로길이(유닛)와 거의 같음, 플레이어 세로길이 약 4.5유닛 기준)
    public Transform[] phase2SpawnPoints; // 정확히 8개: [0]은 본체가 재배치될 자리, [1..7]은 분신
    public string nextSceneName = "Stage2_SpringScene";
    public float fadeDuration = 0.5f;
    public float roomWidth = 78f;
    public float roomHeight = 42f;

    private const float FOCUS_ORTHO_SIZE = 5f;
    private const float FOCUS_MOVE_TIME = 0.3f;
    private const float FOCUS_HOLD_TIME = 0.5f;
    private const float WIDE_MOVE_TIME = 0.7f;
    private const float WIDE_HOLD_TIME = 1.8f;
    private const float RETURN_MOVE_TIME = 0.4f;

    // 탈출 족자 - 보스 죽은 자리가 아니라 방 안 무작위 위치에 생성하고, 제한시간 안에 못 찾으면 사망.
    private const float ESCAPE_TIME_LIMIT = 45f;
    private const float ESCAPE_MIN_DISTANCE_FROM_PLAYER = 20f; // 너무 가까우면 찾는 의미가 없어서 최소 거리 강제
    private const float ESCAPE_WALL_MARGIN = 6f;
    private const int ESCAPE_POSITION_ATTEMPTS = 30;

    private static readonly string[] IntroLines =
    {
        "여기 왜 들어왔느냐, 오지랖이 너를 염라대왕한테 이끄는구나.",
        "나는 한때 조선 제일의 창이었다..",
        "어디 내 창 앞에서도 너의 그 오지랖이 고개를 드는지 보자꾸나.",
    };

    private static readonly string[] Phase2Lines =
    {
        "그분께는 가지 못한다. 여기서 죽어다오.",
        "너를 절대로 그분께 보낼 수 없다.",
        "그분이야말로 만물을 꿰뚫는.... 진정한 도깨비들의 주인이시다!!!!",
    };

    private bool _phase2Triggered;
    private bool _bossDefeated;
    private bool _escaped;
    public bool Phase2Triggered => _phase2Triggered;
    private readonly List<ZombieChaser> _zombies = new List<ZombieChaser>();
    // 아직 좀비로 변하지 않은(=한 번도 안 맞은) 분신들 - 본체가 쓰러지면 같이 사라져야 한다.
    private readonly List<GoblinGeneralCharger> _clones = new List<GoblinGeneralCharger>();

    private void Awake()
    {
        // 입장 대사가 끝나기 전엔 절대 못 움직이게 - Awake는 모든 오브젝트의 첫 Update보다
        // 먼저 실행되니 bossCharger.Awake/Start 순서와 무관하게 안전함.
        if (bossCharger != null) bossCharger.autoStart = false;
    }

    private void Start()
    {
        BossDialogueBox.Show(IntroLines, bossPortraitSprite, () =>
        {
            if (bossCharger != null) bossCharger.autoStart = true;
        });
    }

    public void RegisterZombie(ZombieChaser zombie)
    {
        _zombies.Add(zombie);
    }

    public void CheckPhase2Trigger()
    {
        if (_phase2Triggered || bossCharger == null) return;

        Stat stat = bossCharger.BossStat;
        if (stat == null) return;

        if (stat.Hp <= stat.Total_MaxHp / 3)
        {
            _phase2Triggered = true;
            StartCoroutine(Phase2Sequence());
        }
    }

    private IEnumerator Phase2Sequence()
    {
        if (phase2SpawnPoints == null || phase2SpawnPoints.Length < 8)
        {
            Debug.LogWarning("[GoblinGeneralBossDirector] phase2SpawnPoints가 8개 미만이라 페이즈2 소환을 건너뜀");
            yield break;
        }

        bossCharger.autoStart = false; // 연출 끝날 때까지 아무도 돌진 시작 안 함
        bossCharger.StopCharging(); // 안 그러면 기존 ChargeLoop이 연출 도중에도 계속 돌면서 위치를 제멋대로 옮김

        // 산수원의 주인이 배후라는 암시를 주는 대사 - 연출(카메라 이동 등) 시작 전에 먼저 보여줌.
        bool dialogueDone = false;
        BossDialogueBox.Show(Phase2Lines, bossPortraitSprite, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        Camera cam = Camera.main;
        CameraFollow camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        float camOriginalSize = 4f;
        bool camControlled = cam != null;

        if (camControlled)
        {
            camOriginalSize = cam.orthographicSize;
            if (camFollow != null) camFollow.enabled = false; // 연출 동안은 직접 조종하니 추적을 잠깐 꺼둠
        }

        // 1) 본체 재배치 - 첫 "소환"으로 취급해서 카메라가 먼저 포커싱.
        if (camControlled)
            yield return LerpCamera(cam, phase2SpawnPoints[0].position, FOCUS_ORTHO_SIZE, FOCUS_MOVE_TIME);

        yield return SpawnSmokePoof(bossCharger.transform.position);
        bossCharger.transform.position = phase2SpawnPoints[0].position;
        bossCharger.isPhase2 = true;

        if (camControlled) yield return new WaitForSeconds(FOCUS_HOLD_TIME);

        // 2) 분신 7체 - 카메라가 하나씩 포커싱하며 순서대로 소환.
        var clones = _clones;
        clones.Clear();
        for (int i = 1; i < 8; i++)
        {
            Vector3 pos = phase2SpawnPoints[i].position;

            if (camControlled)
                yield return LerpCamera(cam, pos, FOCUS_ORTHO_SIZE, FOCUS_MOVE_TIME);

            yield return SpawnSmokePoof(pos);

            if (clonePrefab != null)
            {
                GameObject cloneGO = Instantiate(clonePrefab, pos, Quaternion.identity);
                GoblinGeneralCharger clone = cloneGO.GetComponent<GoblinGeneralCharger>();
                if (clone != null)
                {
                    clone.isRealBoss = false;
                    clone.director = this;
                    clone.autoStart = false;
                    clone.isPhase2 = true;
                    clones.Add(clone);
                }
            }

            if (camControlled) yield return new WaitForSeconds(FOCUS_HOLD_TIME);
        }

        // 3) 맵 전체를 와이드하게 보여줘서 8체가 흩어진 모습을 한눈에.
        if (camControlled)
        {
            Vector3 roomCenter = new Vector3(roomWidth / 2f, roomHeight / 2f, cam.transform.position.z);
            float wideSize = roomHeight / 2f + 2f;
            yield return LerpCamera(cam, roomCenter, wideSize, WIDE_MOVE_TIME);
            yield return new WaitForSeconds(WIDE_HOLD_TIME);
        }

        // 4) 전투 돌입 - 플레이어 위치로 부드럽게 복귀한 뒤 다시 추적 모드로 전환하고 전원 기동 시작.
        if (camControlled)
        {
            Vector3 playerPos = bossCharger.transform.position; // 폴백(플레이어 참조를 못 구했을 때)
            if (camFollow != null && camFollow.target != null) playerPos = camFollow.target.position;
            playerPos.z = cam.transform.position.z;

            yield return LerpCamera(cam, playerPos, camOriginalSize, RETURN_MOVE_TIME);
            if (camFollow != null) camFollow.enabled = true;
        }

        bossCharger.autoStart = true;
        bossCharger.RestartCharge();
        foreach (var clone in clones)
        {
            clone.autoStart = true;
            clone.RestartCharge();
        }
    }

    // 카메라를 월드 좌표 기준으로 목표 위치/줌으로 부드럽게 이동(분리된 상태에서 사용).
    private static IEnumerator LerpCamera(Camera cam, Vector3 targetPos, float targetSize, float duration)
    {
        Vector3 startPos = cam.transform.position;
        float startSize = cam.orthographicSize;
        targetPos.z = startPos.z;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, p);
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, p);
            yield return null;
        }

        cam.transform.position = targetPos;
        cam.orthographicSize = targetSize;
    }

    // DisguisedGoblin.SmokeEffect()와 동일한 기법(흰 스프라이트 스케일업 + 페이드) - 별도 파티클/애니메이션 자산 없이
    // 순수 코드로 "펑" 연출을 재현.
    public static IEnumerator SpawnSmokePoof(Vector3 position)
    {
        GameObject smoke = new GameObject("SmokePoof");
        smoke.transform.position = position;
        var sr = smoke.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWhiteCircleSprite();
        sr.sortingOrder = 20;
        smoke.transform.localScale = Vector3.one * 0.1f;

        float t = 0f;
        const float duration = 0.3f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            smoke.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 2f, p);
            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, p);
            sr.color = c;
            yield return null;
        }

        Destroy(smoke);
    }

    private static Sprite _whiteCircleCache;
    private static Sprite CreateWhiteCircleSprite()
    {
        if (_whiteCircleCache != null) return _whiteCircleCache;

        const int size = 32;
        var tex = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= size / 2f ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        tex.Apply();

        _whiteCircleCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _whiteCircleCache;
    }

    // 본체(isRealBoss=true)가 죽었을 때 GoblinGeneralCharger.OnDie()에서 호출.
    public void OnRealBossDefeated()
    {
        if (_bossDefeated) return;
        _bossDefeated = true;
        StartCoroutine(ClearSequence());
    }

    private static readonly string[] DeathLines =
    {
        "가라.. 이 공간은 애초에 나의 공간.",
        "이제 내가 무너지니 이곳도 무너진다.",
        "어서.. 어서 가라..",
    };

    private IEnumerator ClearSequence()
    {
        // 본체가 쓰러지면 그때까지 안 죽고 남아있던 좀비들도 같이 죽는 연출(Hit -> Dead 애니메이션 후 소멸).
        foreach (var zombie in _zombies)
        {
            if (zombie != null) zombie.Die();
        }

        // 아직 한 번도 안 맞아서 좀비가 되지 않은 분신들도 본체와 함께 사라진다 - 안 그러면 본체가
        // 죽은 뒤 탈출로를 찾는 내내 분신 7체가 계속 돌진해와서 사실상 탈출이 불가능해짐.
        foreach (var clone in _clones)
        {
            if (clone == null) continue;
            clone.StopCharging();
            StartCoroutine(SpawnSmokePoof(clone.transform.position));
            Destroy(clone.gameObject);
        }
        _clones.Clear();

        bool dialogueDone = false;
        BossDialogueBox.Show(DeathLines, bossPortraitSprite, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        // 공간이 무너진다 - 화면이 계속 흔들리고, 플레이어가 직접 탈출구(족자)를 찾아 들어가야 함
        // (자동 전환이 아니라 "탈출로를 찾으세요"에 맞춰 플레이어 행동을 기다림). 제한시간 안에
        // 못 찾으면 공간에 삼켜져서 사망 처리.
        Camera cam = Camera.main;
        CameraFollow camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        if (camFollow != null) camFollow.StartShake(0.4f);

        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("이제 이 공간은 무너집니다. 탈출로를 찾으세요!", 5f);

        _escaped = false;
        SpawnEscapePortal(camFollow);
        StartCoroutine(EscapeTimeoutWatch(camFollow));
    }

    // 제한시간 안에 탈출 족자를 못 찾으면 그대로 공간에 삼켜져서 사망(플레이어 Hp를 0으로 만들어
    // 기존 사망 처리 경로 그대로 태움 - PlayerController.UpdateIdle/Moving이 매 프레임 Hp<=0 체크).
    private IEnumerator EscapeTimeoutWatch(CameraFollow camFollow)
    {
        float elapsed = 0f;
        while (elapsed < ESCAPE_TIME_LIMIT)
        {
            if (_escaped) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (_escaped) yield break;

        if (camFollow != null) camFollow.StopShake();
        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("결국 공간에 삼켜지고 말았다...", 3f);

        GameObject playerObj = PlayerLocator.Find();
        Stat playerStat = playerObj != null ? playerObj.GetComponent<Stat>() : null;
        if (playerStat != null) playerStat.Hp = 0;
    }

    // 보스 죽은 자리가 아니라 방 안 무작위 위치(플레이어와 최소 거리 확보)에 족자(탈출구)를 띄운다.
    // 밟으면 EscapePortalReachedTrigger가 망설임 텀 + 재촉 대사 후 씬 전환까지 처리.
    private void SpawnEscapePortal(CameraFollow camFollow)
    {
        Vector3 pos = FindEscapePortalPosition();

        GameObject portal = new GameObject("EscapePortal");
        portal.transform.position = pos;
        portal.transform.localScale = Vector3.one * 2.5f;

        var sr = portal.AddComponent<SpriteRenderer>();
        sr.sprite = escapePortalSprite;
        sr.sortingOrder = 15;

        var col = portal.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var trigger = portal.AddComponent<EscapePortalReachedTrigger>();
        trigger.nextSceneName = nextSceneName;
        trigger.fadeDuration = fadeDuration;
        trigger.onEscaped = () =>
        {
            _escaped = true;
            if (camFollow != null) camFollow.StopShake();
        };

        StartCoroutine(PulsePortal(sr));
    }

    private Vector3 FindEscapePortalPosition()
    {
        Vector3 playerPos = bossCharger != null ? bossCharger.transform.position : transform.position;
        GameObject playerObj = PlayerLocator.Find();
        if (playerObj != null) playerPos = playerObj.transform.position;

        for (int i = 0; i < ESCAPE_POSITION_ATTEMPTS; i++)
        {
            float x = Random.Range(ESCAPE_WALL_MARGIN, roomWidth - ESCAPE_WALL_MARGIN);
            float y = Random.Range(ESCAPE_WALL_MARGIN, roomHeight - ESCAPE_WALL_MARGIN);
            Vector3 candidate = new Vector3(x, y, 0f);
            if (Vector2.Distance(candidate, playerPos) >= ESCAPE_MIN_DISTANCE_FROM_PLAYER)
                return candidate;
        }

        return new Vector3(roomWidth / 2f, roomHeight / 2f, 0f); // 폴백: 적당한 자리를 못 찾으면 방 한가운데
    }

    // 눈에 확 띄게 은은히 색이 맥동 - 별도 파티클 없이 순수 코드로.
    private IEnumerator PulsePortal(SpriteRenderer sr)
    {
        while (sr != null)
        {
            float t = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
            sr.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.5f), t);
            yield return null;
        }
    }
}
