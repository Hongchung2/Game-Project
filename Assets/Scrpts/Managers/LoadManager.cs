using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class LoadManagers : MonoBehaviour
{
    public CanvasGroup fadeCanvas;
    [SerializeField] TextMeshProUGUI _messageText;

    void Start()
    {
        fadeCanvas.alpha = 1f;
        StartCoroutine(FadeIn());
    }
    IEnumerator FadeIn()
    {        
        float t = 1f;
        while ( t > 0f)
        {
            t -= Time.unscaledDeltaTime;
            if (fadeCanvas != null)
            {
                fadeCanvas.alpha = t;
            }
            yield return null;
        }
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
        }
    }

    public IEnumerator FadeAndLoad(string sceneName, string message = "")
    {
        fadeCanvas.gameObject.SetActive(true);
        fadeCanvas.alpha = 0f;
        if (!string.IsNullOrEmpty(message) && _messageText != null)
        {
            _messageText.gameObject.SetActive(true);
            _messageText.text = message;
            yield return new WaitForSecondsRealtime(2.5f);
        }
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 0.5f;
            fadeCanvas.alpha = t;
            yield return null;
        }
        if (_messageText != null)
        {
        _messageText.gameObject.SetActive(false);
        }

        yield return new WaitForSecondsRealtime(1.5f);
        SceneManager.LoadScene(sceneName);
    }

    public void StartFadeAndLoad(string sceneName, string message = "")
    {
        StartCoroutine(FadeAndLoad(sceneName, message));
    }
}
