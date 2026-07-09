using System.Collections;
using UnityEngine;

public class StatueController : MonoBehaviour
{
    [Header("석상 정보")]
    public string statueId; // "hak", "turtle", "tiger", "dragon"

    [Header("감지 설정")]
    public float contactRange = 1.0f;  // 플레이어가 석상에 닿았다고 판정하는 거리
    public float alignTolerance = 0.5f; // 미는 방향과 수직으로 허용되는 어긋남 (작을수록 정밀)
    public LayerMask playerLayer;      // 플레이어 레이어

    [Header("이동 설정")]
    public float moveTimePerCell = 0.5f; // 한 칸 미끄러지는 데 걸리는 시간
    public int maxSlideCells = 20;       // 최대 탐색 칸 수

    private bool _isSliding = false;
    private bool _isSealed = false;
    private bool _wasPushing = false;    // 직전 프레임에 밀고 있었는지 (한 번의 부딪힘 = 한 번의 슬라이드)

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Vector2 _originPos;          // 초기화 종을 위한 시작 위치
    private Color _originColor;          // 석상 고유 색 (리셋 시 복원용)

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _originPos = transform.position;
        _originColor = _sr.color;
    }

    private void Update()
    {
        if (_isSealed || _isSliding) return;

        // F키를 누르고 있지 않으면 무시
        if (!Input.GetKey(KeyCode.F))
        {
            _wasPushing = false;
            return;
        }

        // 석상에 닿은 플레이어 찾기
        Collider2D player = Physics2D.OverlapCircle(transform.position, contactRange, playerLayer);
        if (player == null)
        {
            _wasPushing = false;
            return;
        }

        // 플레이어가 누르는 방향 (방향키/WASD)
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector2 dir = GetCardinalDirection(input);

        // 플레이어 → 석상 벡터를 미는 방향 기준으로 분해
        Vector2 toStatue = (Vector2)transform.position - (Vector2)player.transform.position;
        float parallel = Vector2.Dot(toStatue, dir);                       // 미는 방향 성분 (앞에 있어야 함)
        float perp = Vector2.Dot(toStatue, new Vector2(-dir.y, dir.x));    // 수직 어긋남

        // 미는 방향 앞에 있고 + 같은 줄에 정렬돼 있어야만 발동
        bool pushing = dir != Vector2.zero
            && parallel > 0f
            && Mathf.Abs(perp) < alignTolerance;

        // "툭 부딪히는 순간"에만 한 번 발동 (계속 누르고 있어도 한 번만)
        if (pushing && !_wasPushing)
        {
            StartCoroutine(Slide(dir));
        }

        _wasPushing = pushing;
    }

    // 입력을 가장 가까운 상하좌우 방향으로 스냅
    private Vector2 GetCardinalDirection(Vector2 v)
    {
        if (v == Vector2.zero) return Vector2.zero;

        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x > 0 ? Vector2.right : Vector2.left;
        else
            return v.y > 0 ? Vector2.up : Vector2.down;
    }

    private IEnumerator Slide(Vector2 dir)
    {
        _isSliding = true;
        _wasPushing = false;

        int steps = CalculateSlideDistance(dir);

        for (int i = 0; i < steps; i++)
        {
            Vector2 startPos = _rb.position;
            Vector2 targetPos = startPos + dir;
            float elapsed = 0f;

            while (elapsed < moveTimePerCell)
            {
                elapsed += Time.fixedDeltaTime;
                _rb.MovePosition(Vector2.Lerp(startPos, targetPos, elapsed / moveTimePerCell));
                yield return new WaitForFixedUpdate();
            }
            _rb.MovePosition(targetPos);
            yield return new WaitForFixedUpdate();

            // 한 칸 이동할 때마다 발판 체크
            CheckPedestal();
            if (_isSealed) break;
        }

        _isSliding = false;
    }

    // 장애물(벽/기둥/다른 석상)에 닿기 직전까지 몇 칸 갈 수 있는지 계산
    private int CalculateSlideDistance(Vector2 dir)
    {
        int steps = 0;

        for (int i = 1; i <= maxSlideCells; i++)
        {
            Vector2 nextCell = (Vector2)transform.position + dir * i;
            Collider2D[] hits = Physics2D.OverlapBoxAll(nextCell, Vector2.one * 0.8f, 0f);

            bool blocked = false;
            foreach (var h in hits)
            {
                if (h.gameObject == gameObject) continue; // 자기 자신 무시
                if (h.isTrigger) continue;                // 발판/스위치/종(트리거)은 통과
                if (h.CompareTag("Player")) continue;     // 플레이어 무시
                // 남은 것: 벽, 기둥, 다른 석상 = 장애물
                blocked = true;
                break;
            }

            if (blocked) break;
            steps = i;
        }

        return steps;
    }

    private void CheckPedestal()
    {
        // 현재 위치에 발판이 있는지 체크
        Collider2D col = Physics2D.OverlapCircle(transform.position, 0.3f, LayerMask.GetMask("Pedestal"));
        if (col == null) return;

        SummerPedestal pedestal = col.GetComponent<SummerPedestal>();
        if (pedestal != null && pedestal.pedestalId == statueId)
        {
            Seal();
            pedestal.OnStatueSealed();
        }
    }

    private void Seal()
    {
        _isSealed = true;
        _sr.color = new Color(1f, 1f, 0f); // 노란색 테두리 임시 표현
        Debug.Log($"{statueId} 석상 봉인 완료!");
        SummerPuzzleManager.Instance.OnStatueSealed();
    }

    // 초기화 종이 울리면 시작 위치로 복귀
    public void ResetToOrigin()
    {
        StopAllCoroutines();
        _isSealed = false;
        _isSliding = false;
        _wasPushing = false;
        _rb.position = _originPos;
        transform.position = _originPos;
        _sr.color = _originColor;
    }
}
