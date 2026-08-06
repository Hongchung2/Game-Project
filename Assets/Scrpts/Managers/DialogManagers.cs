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

    // 인트로 기획서의 "조정의 명" 명령문. 조정의 명씬.png를 배경으로 한 자씩 출력된다.
    string[] _dialoge =
    {
        "흥덕현에 있는 오래된 저택에서 정체를 알 수 없는 이변이 이어지고 있다.\n" +
        "사람들은 도깨비가 머물며 온갖 사물을 빌려 모습을 감춘다고 전한다.\n" +
        "이에 조정은 그대를 수색관으로 임명하니, 즉시 저택으로 향하여\n" +
        "사건의 진상을 밝히고 백성의 근심을 거둘지어다.\n" +
        "명을 받들어 즉시 출발하라."
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
        GameProgress.SetCheckpoint("Stage1_Scene");
        SceneManager.LoadScene("Stage1_Scene");
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
