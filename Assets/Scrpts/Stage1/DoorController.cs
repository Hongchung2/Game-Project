using UnityEngine;
using System.Collections;
using Cinemachine;

public class DoorController : MonoBehaviour
{
    [Header("이동 목적지")]
    public Transform destination;  // 도착 지점 
    public PolygonCollider2D nextConfiner;  // 도착 공간의 Confiner

    [Header("카메라")]
    public CinemachineConfiner cinemachineConfiner; // 직접 할당

    [Header("페이드")]
    public CanvasGroup fadeCanvasGroup; // 검은 화면 CanvasGroup
    public float fadeDuration = 0.5f; // 페이딩 지속 시간

    private bool isTransitioning = false;

    [Header("잠금 설정")]
    public bool startLocked = false;
    private bool isLocked;

    void Start()
    {
        isLocked = startLocked;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isTransitioning && !isLocked)
        {
            StartCoroutine(Transition(other.gameObject));
        }
    }

    IEnumerator Transition(GameObject player)
    {
        isTransitioning = true;

        // 페이드 아웃
        yield return StartCoroutine(Fade(0f, 1f));

        // 플레이어 이동
        player.transform.position = destination.position;

        // 카메라 Confiner 교체
        cinemachineConfiner.m_BoundingShape2D = nextConfiner;
        cinemachineConfiner.InvalidatePathCache();

        // 페이드 인
        yield return StartCoroutine(Fade(1f, 0f));

        isTransitioning = false;
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        fadeCanvasGroup.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = to;
    }

    public void SetDoorLocked(bool locked)
    {
        isLocked = locked;
    }

}
