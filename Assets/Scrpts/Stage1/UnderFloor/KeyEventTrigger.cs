using System.Collections;
using UnityEngine;

public class KeyEventTrigger : MonoBehaviour
{
    public GameObject key; // 열쇠 오브젝트
    public GameObject goblin; // 도깨비 오브젝트
    public float goblinMoveSpeed = 5f; // 도깨비 이동 속도
    public Transform goblinExitPoint;  // 도깨비가 도망갈 위치
    public GameObject gameCanvas;
    public GameObject Decoration;
    

    private bool hasTriggered = false;
    void Start()
    {
        goblin.SetActive(false); // 도깨비 처음엔 숨김
        Decoration.SetActive(false); // 장애물들 처음에 숨김
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            Debug.Log("트리거 발동");
            hasTriggered = true;
            StartCoroutine(KeyEvent(other.gameObject));
        }
    }

    IEnumerator KeyEvent(GameObject player)
    {
        // UI 제거
        gameCanvas.SetActive(false);
        // 플레이어 이동 중지
        player.GetComponent<PlayerController>().enabled = false;
        // 열쇠 사라짐
        key.SetActive(false);
        // 도깨비 등장
        goblin.SetActive (true);

        yield return new WaitForSeconds(0.5f);

        // 도깨비 도망 + 카메라 따라가기
        yield return StartCoroutine(GoblinEscape(player));
        
        // 장애물들 나타남
        Decoration.SetActive(true);

        // 잠시 대기
        yield return new WaitForSeconds(1.0f);

        // 카메라 플레이어로 복귀
        yield return StartCoroutine(CameraReturnToPlayer(player));

        // UI 표기
        gameCanvas.SetActive(true);
        // 플레이어 이동 재개
        player.GetComponent<PlayerController>().enabled = true;
    }

    // 도깨비 도망 및 카메라 따라가기 코루틴
    IEnumerator GoblinEscape(GameObject player)
    {
        Camera playerCamera = player.GetComponentInChildren<Camera>();
        CameraFollow cameraFollow = playerCamera.GetComponent<CameraFollow>();

        // CameraFollow 잠깐 끄기
        if (cameraFollow != null) cameraFollow.enabled = false;

        while (Vector2.Distance(goblin.transform.position, goblinExitPoint.position) > 0.1f)
        {
            goblin.transform.position = Vector2.MoveTowards(
                goblin.transform.position,
                goblinExitPoint.position,
                goblinMoveSpeed * Time.deltaTime
            );

            // 카메라가 도깨비 따라가기
            if (playerCamera != null)
            {
                Vector3 targetPos = new Vector3(goblin.transform.position.x, goblin.transform.position.y, playerCamera.transform.position.z);
                playerCamera.transform.position = Vector3.MoveTowards(playerCamera.transform.position, targetPos, goblinMoveSpeed * Time.deltaTime);
            }

            yield return null;
        }

        // 도착 후 사라지는 애니메이션 재생
        Animator anim = goblin.GetComponent<Animator>();
        if (anim != null)
        {
            // 애니메이션 실행
            Debug.Log("애니메이션 실행");
            anim.SetTrigger("Disappear");
            // 애니메이션 길이만큼 대기
            yield return new WaitForSeconds(1f);
        }
        
        goblin.SetActive(false);
    }

    // 카메라 플레이어로 복귀 코루틴
    IEnumerator CameraReturnToPlayer(GameObject player)
    {
        Camera playerCamera = player.GetComponentInChildren<Camera>();
        CameraFollow cameraFollow = playerCamera?.GetComponent<CameraFollow>();
        if (playerCamera == null) yield break;

        float returnSpeed = 5f;
        while (Vector2.Distance(playerCamera.transform.position, player.transform.position) > 0.1f)
        {
            Vector3 targetPos = new Vector3(player.transform.position.x, player.transform.position.y, playerCamera.transform.position.z);
            playerCamera.transform.position = Vector3.MoveTowards(playerCamera.transform.position, targetPos, returnSpeed * Time.deltaTime);
            yield return null;
        }

        // CameraFollow 다시 켜기
        if (cameraFollow != null) cameraFollow.enabled = true;
    }
}
