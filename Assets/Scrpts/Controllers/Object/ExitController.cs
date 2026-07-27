using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitController : MonoBehaviour, IInteractable
{
    public static ExitController Instance;

    private bool isUnlocked = false;
    private bool _isLoading = false;
    private SpriteRenderer sr;

    public float fadeDuration = 1.5f;

    [Header("클리어 시 사라질 벽 (길을 막는 벽 오브젝트들)")]
    public GameObject[] gateWalls;

    private void Awake()
    {
        Instance = this;
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // 시작 시 완전 투명
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
        }
    }

    public void UnlockExit()
    {
        isUnlocked = true;
        StartCoroutine(FadeIn());   // 출구 바닥 타일 나타남
        StartCoroutine(OpenGate()); // 길 막던 벽 사라짐
        Debug.Log("출구가 열렸습니다!");
    }

    // 길을 막던 벽을 스르륵 사라지게 (Exit 페이드인의 반대)
    private IEnumerator OpenGate()
    {
        // 콜라이더는 즉시 꺼서 바로 지나갈 수 있게
        foreach (var wall in gateWalls)
        {
            if (wall == null) continue;
            Collider2D col = wall.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        // 스프라이트를 서서히 투명하게 (사라지는 연출)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - elapsed / fadeDuration);

            foreach (var wall in gateWalls)
            {
                if (wall == null) continue;
                SpriteRenderer wsr = wall.GetComponent<SpriteRenderer>();
                if (wsr != null)
                {
                    Color c = wsr.color;
                    c.a = alpha;
                    wsr.color = c;
                }
            }
            yield return null;
        }

        // 완전히 비활성화
        foreach (var wall in gateWalls)
        {
            if (wall != null) wall.SetActive(false);
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        Color c = sr.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            sr.color = c;
            yield return null;
        }

        c.a = 1f;
        sr.color = c;
    }

    // 잠금 해제된 상태에서 플레이어가 걸어 들어오면 자동으로 다음 씬으로
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isUnlocked || _isLoading) return;
        if (other.CompareTag("Player"))
            GoNext();
    }

    // F키 상호작용도 유지 (감지되면 F로도 넘어감)
    public string GetInteractText()
    {
        return isUnlocked ? "F - 다음 스테이지로" : "";
    }

    public void OnInteract()
    {
        if (!isUnlocked) return;
        GoNext();
    }

    private void GoNext()
    {
        if (_isLoading) return;
        _isLoading = true;
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        Debug.Log("다음 스테이지로 이동!");

        // 검은 페이드 후 전환 (ScreenFader 있으면)
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, 0.5f);

        SceneManager.LoadScene("Stage2_SummerScene");
    }
}
