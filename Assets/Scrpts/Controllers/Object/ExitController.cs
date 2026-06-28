using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitController : MonoBehaviour, IInteractable
{
    public static ExitController Instance;

    private bool isUnlocked = false;
    private SpriteRenderer sr;

    public float fadeDuration = 1.5f;

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
        StartCoroutine(FadeIn());
        Debug.Log("출구가 열렸습니다!");
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

    public string GetInteractText()
    {
        return isUnlocked ? "F - 다음 스테이지로" : "";
    }

    public void OnInteract()
    {
        if (!isUnlocked) return;
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        yield return null;
        SceneManager.LoadScene("Stage2_SummerScene");
    }
}
