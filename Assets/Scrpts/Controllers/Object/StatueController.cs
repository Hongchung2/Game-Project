using System.Collections;
using UnityEngine;

public class StatueController : MonoBehaviour
{
    [Header("석상 정보")]
    public string statueId; // "hak", "turtle", "tiger", "dragon"

    [Header("감지 설정")]
    public float playerDetectRadius = 1.2f;
    public LayerMask playerLayer;

    private bool _isPlayerNear = false;
    private bool _isSliding = false;
    private bool _isSealed = false;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_isSealed || _isSliding) return;

        // 플레이어가 근처에 있는지 매 프레임 체크
        Collider2D col = Physics2D.OverlapCircle(transform.position, playerDetectRadius, playerLayer);
        _isPlayerNear = col != null;

        if (!_isPlayerNear) return;

        // F키를 누른 채로 방향키 입력 감지
        if (Input.GetKey(KeyCode.F))
        {
            Vector2 dir = Vector2.zero;

            if (Input.GetKeyDown(KeyCode.UpArrow))    dir = Vector2.up;
            if (Input.GetKeyDown(KeyCode.DownArrow))  dir = Vector2.down;
            if (Input.GetKeyDown(KeyCode.LeftArrow))  dir = Vector2.left;
            if (Input.GetKeyDown(KeyCode.RightArrow)) dir = Vector2.right;

            if (dir != Vector2.zero)
                StartCoroutine(Slide(dir));
        }
    }

    private IEnumerator Slide(Vector2 dir)
    {
        _isSliding = true;

        // Raycast로 이동 가능한 거리 계산
        float slideDistance = CalculateSlideDistance(dir);

        if (slideDistance <= 0)
        {
            _isSliding = false;
            yield break;
        }

        // 1칸씩 이동 (0.5초/칸)
        int steps = Mathf.RoundToInt(slideDistance);
        for (int i = 0; i < steps; i++)
        {
            Vector2 targetPos = _rb.position + dir;
            float elapsed = 0f;
            Vector2 startPos = _rb.position;

            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                _rb.MovePosition(Vector2.Lerp(startPos, targetPos, elapsed / 0.5f));
                yield return null;
            }

            _rb.MovePosition(targetPos);

            // 매 칸 이동 후 발판 체크
            CheckPedestal();
            if (_isSealed) break;
        }

        _isSliding = false;
    }

    private float CalculateSlideDistance(Vector2 dir)
    {
        // 석상 크기(0.5f)를 고려한 Raycast
        RaycastHit2D hit = Physics2D.BoxCast(
            transform.position,
            Vector2.one * 0.9f,
            0f,
            dir,
            20f
        );

        if (hit.collider == null) return 0f;

        // 충돌 지점까지 거리에서 0.5 빼서 딱 앞 칸에 멈추게
        float dist = hit.distance;
        return Mathf.Floor(dist);
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

    public void ResetToOrigin(Vector2 originPos)
    {
        _isSealed = false;
        _isSliding = false;
        _rb.MovePosition(originPos);
        _sr.color = Color.white;
    }
}
