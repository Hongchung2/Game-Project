using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class DialogManagers : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _dialogText;
    [SerializeField] GameObject _ArrowIcon;
    [SerializeField] CanvasGroup _arrowCanvasGroup;
    public CanvasGroup fadeCanvas;

    string[] _dialoge =
    {
        "때는 조선시대...",
        "국재환이라는 못생긴 놈이 살고 있었다.",
        "이 아이는 정말 끔찍하게 생겨 모든 여자한테 맞고 살았다."
    };

    int _index = 0;
    bool _isTyping = false;
    bool _isFading = false;
    void Start()
    {
        _ArrowIcon.SetActive(false);
        StartCoroutine(TypeText(_dialoge[0]));
    }

    void Update()
    {
        if (_isFading) return;
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            OnTouch();
        }
    }

    void OnTouch()
    {
        if (_isTyping)
        {
            StopAllCoroutines();
            _dialogText.text = _dialoge[_index];
            _isTyping = false;
            StartCoroutine(ShowArrow());
            return;
        }

        _index++;
        if (_index < _dialoge.Length)
        {
            _ArrowIcon.SetActive(false);
            StartCoroutine(TypeText(_dialoge[_index]));
        }
        else
        {
            StartCoroutine(FadeAndLoad());
        }
    }

    IEnumerator TypeText(string text)
    {
        _dialogText.text = "";
        _isTyping = true;

        foreach (char c in text)
        {
            _dialogText.text += c;
            yield return new WaitForSeconds(0.05f);
        }

        _isTyping = false;
        StartCoroutine(ShowArrow());
    }

    IEnumerator FadeAndLoad()
    {
        _isFading = true;
        _ArrowIcon.SetActive(false);
        _dialogText.gameObject.SetActive(false);

        fadeCanvas.alpha = 1f;
        yield return new WaitForSeconds(1f);

        float t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            fadeCanvas.alpha = t;
            yield return null;
        }
        SceneManager.LoadScene("GameScene");
    }

    IEnumerator ShowArrow()
    {
        _ArrowIcon.SetActive(true);
        _arrowCanvasGroup.alpha = 0f;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            _arrowCanvasGroup.alpha = t;
            yield return null;
        }

        _arrowCanvasGroup.alpha = 1f;
    }
}
