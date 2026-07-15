using System.Collections;
using UnityEngine;

public class KeyEventTrigger : MonoBehaviour
{
    public GameObject key; // 열쇠 오브젝트
    public GameObject goblin; // 도깨비 오브젝트
    public float goblinMoveSpeed = 5f; // 도깨비 이동 속도
    public Transform goblinExitPoint;  // 도깨비가 도망갈 위치
    public GameObject gameCanvas;

    private bool hasTriggered = false;
    void Start()
    {
        goblin.SetActive(false); // 도깨비 처음엔 숨김
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
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
        // 도깨비 도망
        yield return StartCoroutine(GoblinEscape());

        // UI 표기
        gameCanvas.SetActive(true);
        // 플레이어 이동 재개
        player.GetComponent<PlayerController>().enabled = true;
    }

    IEnumerator GoblinEscape()
    {
        while (Vector2.Distance(goblin.transform.position, goblinExitPoint.position) > 0.1f)
        {
            goblin.transform.position = Vector2.MoveTowards(
                goblin.transform.position,
                goblinExitPoint.position,
                goblinMoveSpeed * Time.deltaTime
            );
            yield return null;
        }
        goblin.SetActive(false);
    }
}
