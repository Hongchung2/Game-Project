using System.Collections;
using UnityEngine;

public class GoblinInteract : MonoBehaviour
{
    public GameObject goblin;
    public GameObject speechBubble; // 말풍선 오브젝트
    public GameObject Canvas; // UI
    public float displayTime = 3f; // 표시 시간

    private bool hasTriggered = false;
    void Start()
    {
        speechBubble.SetActive(false);  // 말풍선 처음에는 숨김 처리
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(ShowBubble(other.gameObject));
        }
    }

    IEnumerator ShowBubble(GameObject player)
    {
        // 플레이어 이동 중지
        player.GetComponent<PlayerController>().enabled = false;

        // UI 숨기기
        Canvas.SetActive(false);

        // 말풍선 표시
        speechBubble.SetActive(true);
        
        yield return new WaitForSeconds(displayTime);

        // 말풍선 닫기
        speechBubble.SetActive(false);
        
        // 디버그
        GoblinDisappear goblinDisappear = goblin.GetComponent<GoblinDisappear>();
        StartCoroutine(goblinDisappear.DisappearAndSplit(this));
        // 도깨비 분열
        yield return new WaitForSeconds(goblinDisappear.disappearTime + 0.3f);

        Debug.Log("분열 대기 끝");
        // UI 숨김 해제
        Canvas.SetActive(true);
        Debug.Log("Canvas 활성화");
        // 플레이어 이동 재개
        player.GetComponent<PlayerController>().enabled = true;
        Debug.Log("플레이어 이동 재개");
    }
}
