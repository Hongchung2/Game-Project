using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class InGameDialogManager : MonoBehaviour
{
    [SerializeField] GameObject _dialogPanel;
    [SerializeField] CanvasGroup _panelCanvasGroup;
    [SerializeField] GameObject _comunity;
    [SerializeField] CanvasGroup _comunityCanvasGroup;
    [SerializeField] TextMeshProUGUI _dialogText;
    [SerializeField] GameObject _arrowIcon;
    [SerializeField] CanvasGroup _arrowIconCanvasGroup;

    bool _isTyping = false;
    bool _isDialogActive = false;
    int _index = 0;
    string[] _dialogues;

    void Start()
    {
        string[] openingDialogues =
        {
            "나는 \"코쵸우 시노부\"",
            "잘생긴 나의 왕자님, \"홍승환\"님의 여자친구이다!",
            "못생긴 국재환이랑 차원이 다르다 이말이여!"
        };

        StartDialogue(openingDialogues);
    }
    public void StartDialogue(string[] dialogues)
    {
        _dialogues = dialogues;
        _index = 0;
        StartCoroutine(ShowDialogue());
    }

    IEnumerator ShowDialogue()
    {
        // 게임 멈추기
        Time.timeScale = 0f;
        _isDialogActive = true;

        _arrowIcon.SetActive(false);

        // 패널 나타내기
        _dialogPanel.SetActive(true);
        _panelCanvasGroup.alpha = 0f;
        _comunityCanvasGroup.alpha = 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 2f;
            _panelCanvasGroup.alpha = t;
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 2f;
            _comunityCanvasGroup.alpha = t;
            yield return null;
        }

        
        _typingCoroutine = StartCoroutine(TypeText(_dialogues[0]));
    }
    

    void Update()
    {
        if (!_isDialogActive) return;

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            OnTouch();
        }
    }

    void OnTouch()
    {
        if (_isTyping)
        {
            StopCoroutine(_typingCoroutine);
            _dialogText.text = _dialogues[_index];
            _isTyping = false;
            return;
        }

        _index++;
        if (_index < _dialogues.Length)
        {
            _typingCoroutine = StartCoroutine(TypeText(_dialogues[_index]));
            
        }

        else
        {
            StartCoroutine(EndDialogue());
        }
    }

    Coroutine _typingCoroutine;

    IEnumerator TypeText(string text)
    {
        _dialogText.text = "";
        _isTyping = true;

        foreach (char c in text)
        {
            _dialogText.text += c;
            yield return new WaitForSecondsRealtime(0.05f); // TimeScale이 0이라 Realtime 사용해야함
        }

        if (_index == _dialogues.Length-1)
        {
            StartCoroutine(ShowArrow());
        }
        _isTyping = false;
    }

    IEnumerator ShowArrow()
    {
        _arrowIcon.SetActive(true);
        _arrowIconCanvasGroup.alpha = 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 2f;
            _arrowIconCanvasGroup.alpha = t;
            yield return null;
        }
    }

    IEnumerator EndDialogue()
    {
        _dialogPanel.SetActive(false);
        _isDialogActive = false;
        Time.timeScale = 1f;
        yield return null;
    }
}
