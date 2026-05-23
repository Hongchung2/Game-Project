using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class LoadManagers : MonoBehaviour
{
    public CanvasGroup fadeCanvas;
    [SerializeField] TextMeshProUGUI _messageText;

    void Update()
    {
        fadeCanvas.alpha = 1f;
        StartCoroutine(FadeIn());
    }
    IEnumerator FadeIn()
    {        
        float t = 1f;
        while ( t > 0f)
        {
            t -= Time.deltaTime;
            fadeCanvas.alpha = t;
            yield return null;
        }
        fadeCanvas.alpha = 0f;
    }

    public IEnumerator FadeAndLoad(string sceneName, string message = "")
    {
        if (!string.IsNullOrEmpty(message))
        {
            _messageText.gameObject.SetActive(true);
            _messageText.text = message;
            yield return new WaitForSeconds(1.5f);
        }
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = t;
            yield return null;
        }
        _messageText.gameObject.SetActive(false);
        SceneManager.LoadScene(sceneName);
    }
}
