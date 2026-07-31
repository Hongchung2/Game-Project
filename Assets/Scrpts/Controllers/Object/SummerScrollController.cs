using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 여름 하(夏) 족자. 퍼즐 클리어 후 맵 중앙에 등장 → F키로 습득 → 검은 페이드 → 가을방 전환.
public class SummerScrollController : MonoBehaviour, IInteractable
{
    [Header("씬 전환")]
    public string nextSceneName = ""; // 가을방 씬 이름 (아직 없으면 비워둠)
    public float fadeDuration = 0.5f;

    private bool _collected = false;

    public string GetInteractText()
    {
        return "F - 하(夏) 족자 습득";
    }

    public void OnInteract()
    {
        if (_collected) return;
        _collected = true;
        StartCoroutine(CollectSequence());
    }

    private IEnumerator CollectSequence()
    {
        // 우상단 족자 수집 UI 채우기
        ScrollCollection.Collect("summer");
        Debug.Log("하(夏) 족자 습득!");

        // 습득 연출: 족자 숨기기
        // 주의: gameObject.SetActive(false)를 쓰면 코루틴이 즉시 멈춰서
        // 아래 페이드/씬전환이 실행되지 않음. 그래서 렌더러/콜라이더만 끈다.
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 0.5초 검은 화면 페이드아웃
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, fadeDuration);
        else
            yield return new WaitForSeconds(fadeDuration);

        // 가을방으로 전환 (씬이 준비돼 있을 때만)
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("가을방 씬이 아직 없습니다. nextSceneName을 설정하면 전환됩니다.");
            // 다음 씬이 없으면 화면을 다시 밝혀서 테스트를 이어갈 수 있게 함
            if (ScreenFader.Instance != null)
                yield return ScreenFader.Instance.FadeIn(fadeDuration);
        }
    }
}
