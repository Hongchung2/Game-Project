using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Detection : MonoBehaviour
{
    [Header("감지 설정")]

    [SerializeField]
    private float detectWidth = 6.0f;   // 박스 너비

    [SerializeField]
    private float detectHeight = 2.0f;  // 박스 높이

    [SerializeField]
    private float offsetX = 1.0f;       // X축 오프셋 (앞쪽으로 얼마나 치우칠지)
     
    [SerializeField]
    private float offsetY = 0.5f;       // Y축 오프셋 (위쪽으로 얼마나 치우칠지)

    [SerializeField]
    private LayerMask playerLayer;

    [Header("디버그용")]

    [SerializeField]
    private Color gizmoColor = new Color(1, 0, 0, 1.0f);

    public bool playerDetected { get; private set; } //플레이어 감지 여부
    public bool playerBack { get; private set; } //플레이어가 뒤에 있는지 여부 + 몬스터가 바라보는 방향은 왼쪽이 기본 방향
    public Transform Player { get; private set; } //감지된 플레이어의 Transform 정보 넘기기



    void Update()
    {
        DetectPlayer();
    }

    private void DetectPlayer() //플레이어 감지 로직
    {
        // 1. 방향 확인 (작성하신 기준: scale.x > 0이면 왼쪽)
        bool isFacingLeft = transform.localScale.x > 0;

        // 2. 오프셋 방향 계산 (왼쪽 보면 -1, 오른쪽 보면 +1)
        // offsetX가 양수일 때 '앞쪽'으로 이동하게 함
        float xDirection = isFacingLeft ? -1f : 1f;

        // 3. 중심정 계산 (X축 오프셋 적용)
        Vector2 centerOffset = new Vector2(offsetX * xDirection, offsetY);
        Vector2 center = (Vector2)transform.position + centerOffset;

        Vector2 boxsize = new Vector2(detectWidth, detectHeight);

        // 4. 박스 감지 실행
        Collider2D hit = Physics2D.OverlapBox(center, boxsize, 0f, playerLayer);
        playerDetected = (hit != null);

        if (playerDetected)
        {
            Player = hit.transform;

            // --- 방향 판별 로직 ---
            bool isPlayerOnRight = Player.position.x > transform.position.x;

            // 방향 판별 (서로 반대 방향이면 '뒤'
            if ((isFacingLeft && isPlayerOnRight) || (!isFacingLeft && !isPlayerOnRight)) 
            {
                playerBack = true;
            }
            else
            {
                playerBack = false;
            }
        }
        else
        {
            playerBack = false;
            Player = null;
        }
    }

    // 에디터에서 범위 확인 (실제 로직과 동일하게 계산)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;

        // 로직과 똑같은 계산을 해서 기즈모를 그려야 정확함
        bool isFacingLeft = transform.localScale.x > 0;
        float xDirection = isFacingLeft ? -1f : 1f;

        Vector2 centerOffset = new Vector2(offsetX * xDirection, offsetY);
        Vector2 center = (Vector2)transform.position + centerOffset;

        Gizmos.DrawWireCube(center, new Vector3(detectWidth, detectHeight, 1));
    }
}
