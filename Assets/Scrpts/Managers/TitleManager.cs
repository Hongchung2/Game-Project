using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleManager : MonoBehaviour
{   
    public CanvasGroup fadeCanvas;

    void Update()
    {   
        // 모바일 터치 및 마우스 클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(FadeAndLoad());
        }
    }

    IEnumerator FadeAndLoad()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = t;
            yield return null;
        }

        SceneManager.LoadScene("DialogScene");
    }
}
