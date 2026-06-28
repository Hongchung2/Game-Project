using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DoorController : MonoBehaviour
{
    [SerializeField] private bool isLocked = true; // 잠김 여부
    [SerializeField] private Transform moveTarget; // 이동할 위치
    [SerializeField] private Image fadeImage; // 검은 화면 Image
    [SerializeField] private GameObject player;

    // 문 충돌 시
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (isLocked) return;

        StartCoroutine(MoveRoutine());
    } 

    // 문 충돌 시 코루틴 발생 (화면 꺼매짐)
    private IEnumerator MoveRoutine()
    {
        yield return StartCoroutine(FadeOut());
        player.transform.position = moveTarget.position;
        yield return StartCoroutine(FadeIn());
    }

    // 화면 꺼매지는 코루틴
    private IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, t);
            yield return null;
        }
    }
    // 화면 밝아지는 코루틴
    private IEnumerator FadeIn()
    {
        float t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, t);
            yield return null;
        }
    }

    public void TriggerDoor()
    {
        if (isLocked) return;
        StartCoroutine(MoveRoutine());
    }
    
    // 해당 지역 조건 만족시 문 열리도록
    public void UnlockDoor()
    {
        isLocked = false;
    }
}
