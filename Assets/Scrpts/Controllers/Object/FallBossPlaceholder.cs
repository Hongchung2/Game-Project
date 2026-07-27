using System.Collections;
using UnityEngine;

// 가을방 몬스터방의 목령(보스).
// 패턴: 점프 후 플레이어 위치로 내려찍기 (코어 키퍼 '혐오스러운 덩어리 글리치' 참고).
// 애니메이션은 실제 아트(스프라이트시트를 잘라 배열로 연결)로 재생한다.
public class FallBossPlaceholder : BaseController
{
    [Header("점프 + 내려찍기 패턴")]
    public float attackInterval = 3f;      // 공격 시도 주기
    public float jumpUpDuration = 0.4f;    // 뛰어오르는 시간
    public float airborneHold = 0.8f;      // 공중에 떠서 낙하지점 조준하는 시간(플레이어가 피할 틈)
    public float slamRadius = 2f;          // 내려찍기 피해 반경
    public float jumpHeight = 1.5f;        // 점프 연출용 높이(시각적으로 위로 뜸)
    public Color airborneColor = new Color(1f, 1f, 1f, 0.5f); // 공중에 뜬 동안(무적) 반투명 표시

    [Header("보스방 제한 범위 (이 범위 밖으로 내려찍기 불가 - 미로로 못 나감)")]
    public Vector2 roomMin = new Vector2(20.5f, -9.5f);
    public Vector2 roomMax = new Vector2(39.5f, 9.5f);

    [Header("애니메이션 - 스프라이트시트를 잘라서 순서대로 연결")]
    public Sprite[] idleFrames;   // 평소 떠있는 idle 루프
    public float idleFrameTime = 0.15f;
    public Sprite[] jumpFrames;   // 점프~착지까지 이어지는 한 세트 (마지막 프레임 = 착지 순간)
    public Sprite[] deathFrames;  // 고통 표정 + 서서히 사라지는 얼룩
    public float deathFrameTime = 0.15f;

    private Stat _stat;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private bool _isDead = false;
    private bool _isAttacking = false;

    private Transform _player;
    private Stat _playerStat;

    public override void Init()
    {
        WorldObjectType = Define.WorldObject.Monster;
        _stat = GetComponent<Stat>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerStat = playerObj.GetComponent<Stat>();
        }

        StartCoroutine(IdleLoop());
        StartCoroutine(AttackLoop());
    }

    private IEnumerator IdleLoop()
    {
        if (idleFrames == null || idleFrames.Length == 0) yield break;

        int i = 0;
        while (!_isDead)
        {
            if (!_isAttacking && _sr != null)
            {
                _sr.sprite = idleFrames[i % idleFrames.Length];
                i++;
            }
            yield return new WaitForSeconds(idleFrameTime);
        }
    }

    private IEnumerator AttackLoop()
    {
        while (!_isDead)
        {
            yield return new WaitForSeconds(attackInterval);
            if (_isDead) yield break;
            yield return JumpSlamAttack();
        }
    }

    private IEnumerator JumpSlamAttack()
    {
        if (_player == null) yield break;

        _isAttacking = true;
        Vector3 startPos = transform.position;

        // 공격 시작 시점에 목표 지점을 미리 정한다 (보스방 범위로 제한) -
        // 점프하는 내내 이 지점을 향해 서서히 다가가고, 이후 플레이어가 움직여서 피할 수 있게 한다.
        Vector3 targetPos = _player.position;
        targetPos.x = Mathf.Clamp(targetPos.x, roomMin.x, roomMax.x);
        targetPos.y = Mathf.Clamp(targetPos.y, roomMin.y, roomMax.y);
        targetPos.z = startPos.z;

        if (_sr != null) _sr.color = airborneColor;

        // 1) 뛰어오름 + 공중 이동 - 시작지점→목표지점으로 서서히 이동하며 포물선(위로 떴다 내려옴)을 그린다
        float totalRise = jumpUpDuration + airborneHold;
        int frameCount = (jumpFrames != null) ? jumpFrames.Length : 0;
        float elapsed = 0f;

        while (elapsed < totalRise)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / totalRise);

            Vector3 horizontal = Vector3.Lerp(startPos, targetPos, p);
            float arc = Mathf.Sin(p * Mathf.PI) * jumpHeight; // 0 -> 최고점 -> 0으로 자연스러운 포물선
            transform.position = horizontal + new Vector3(0, arc, 0);

            if (frameCount > 0 && _sr != null)
            {
                int frameIdx = Mathf.Clamp(Mathf.FloorToInt(p * frameCount), 0, frameCount - 1);
                _sr.sprite = jumpFrames[frameIdx];
            }
            yield return null;
        }

        // 2) 착지 - 정확히 목표 지점에, 착지 프레임 표시, 범위 내 플레이어 피해
        transform.position = targetPos;
        if (_sr != null)
        {
            _sr.color = _originalColor;
            if (frameCount > 0) _sr.sprite = jumpFrames[frameCount - 1];
        }

        if (_playerStat != null)
        {
            float dist = Vector2.Distance(targetPos, _player.position);
            if (dist <= slamRadius)
                _playerStat.OnAttacked(_stat);
        }

        yield return new WaitForSeconds(0.2f); // 착지 후 잠깐 정지

        _isAttacking = false;
    }

    protected override void OnDie()
    {
        if (_isDead) return;
        _isDead = true;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (deathFrames != null && _sr != null)
        {
            foreach (var frame in deathFrames)
            {
                _sr.sprite = frame;
                yield return new WaitForSeconds(deathFrameTime);
            }
        }

        BronzeBellState.HasBell = true;

        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("청동 방울을 획득했습니다!");

        Debug.Log("목령 처치! 청동방울 획득.");

        gameObject.SetActive(false);
    }
}
