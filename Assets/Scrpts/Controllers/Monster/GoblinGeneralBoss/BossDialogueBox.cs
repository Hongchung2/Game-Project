using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 도깨비 장군 보스전 대사 연출. Stage1 인트로의 IntroDokkaebiLine.cs와 같은 방식(캐릭터 초상화 +
// 타이핑 출력 + 탭으로 스킵/다음 대사 진행)을 재사용하되, 대사 내용/초상화/완료 콜백을 파라미터로
// 받아서 여러 장면(입장 대사, 페이즈2 전환 대사)에 공용으로 쓸 수 있게 만듦.
public class BossDialogueBox : MonoBehaviour
{
    private string[] _lines;
    private Sprite _portrait;
    private Action _onComplete;

    private Text _text;
    private GameObject _arrow;
    private int _index;
    private bool _isTyping;

    public static void Show(string[] lines, Sprite portrait, Action onComplete = null)
    {
        GameObject go = new GameObject("BossDialogueBox");
        // AddComponent는 오브젝트가 활성 상태면 그 자리에서 즉시 Awake()를 실행해버려서
        // 그 다음 줄의 필드 대입보다 먼저 돌아버림(_lines가 비어있는 채로 Awake 실행 -> NRE).
        // 비활성 상태로 만들어두고 필드부터 채운 뒤 활성화해야 Awake가 그때 안전하게 실행됨.
        go.SetActive(false);
        var box = go.AddComponent<BossDialogueBox>();
        box._lines = lines;
        box._portrait = portrait;
        box._onComplete = onComplete;
        go.SetActive(true);
    }

    private void Awake()
    {
        Time.timeScale = 0f;
        BuildUI();
        StartCoroutine(TypeLine(_lines[0]));
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("BossDialogueCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject barGO = new GameObject("DialogueBar");
        barGO.transform.SetParent(canvasGO.transform, false);
        var barImg = barGO.AddComponent<Image>();
        barImg.color = new Color(0f, 0f, 0f, 0.75f);
        var barRt = barImg.rectTransform;
        barRt.anchorMin = new Vector2(0f, 0f);
        barRt.anchorMax = new Vector2(1f, 0f);
        barRt.pivot = new Vector2(0.5f, 0f);
        barRt.sizeDelta = new Vector2(0f, 200f);
        barRt.anchoredPosition = new Vector2(0f, 40f);

        if (_portrait != null)
        {
            GameObject portraitGO = new GameObject("Portrait");
            portraitGO.transform.SetParent(canvasGO.transform, false);
            var portraitImg = portraitGO.AddComponent<Image>();
            portraitImg.sprite = _portrait;
            portraitImg.preserveAspect = true;
            var portraitRt = portraitImg.rectTransform;
            portraitRt.anchorMin = new Vector2(1f, 0f);
            portraitRt.anchorMax = new Vector2(1f, 0f);
            portraitRt.pivot = new Vector2(1f, 0f);
            portraitRt.sizeDelta = new Vector2(380f, 380f);
            portraitRt.anchoredPosition = new Vector2(-60f, 200f);
        }

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(barGO.transform, false);
        _text = textGO.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = 32;
        _text.color = Color.white;
        _text.alignment = TextAnchor.MiddleLeft;
        _text.horizontalOverflow = HorizontalWrapMode.Wrap;
        var textRt = _text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(50f, 20f);
        textRt.offsetMax = new Vector2(-360f, -20f);

        _arrow = new GameObject("Arrow");
        _arrow.transform.SetParent(barGO.transform, false);
        var arrowText = _arrow.AddComponent<Text>();
        arrowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        arrowText.text = "▼";
        arrowText.fontSize = 28;
        arrowText.color = Color.white;
        arrowText.alignment = TextAnchor.MiddleCenter;
        var arrowRt = arrowText.rectTransform;
        arrowRt.anchorMin = new Vector2(1f, 0f);
        arrowRt.anchorMax = new Vector2(1f, 0f);
        arrowRt.pivot = new Vector2(1f, 0f);
        arrowRt.anchoredPosition = new Vector2(-30f, 20f);
        arrowRt.sizeDelta = new Vector2(40f, 40f);
        _arrow.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            OnTouch();
        }
    }

    private void OnTouch()
    {
        if (_isTyping)
        {
            StopAllCoroutines();
            _text.text = _lines[_index];
            _isTyping = false;
            _arrow.SetActive(true);
            return;
        }

        _index++;
        if (_index < _lines.Length)
        {
            _arrow.SetActive(false);
            StartCoroutine(TypeLine(_lines[_index]));
        }
        else
        {
            Time.timeScale = 1f;
            var callback = _onComplete;
            Destroy(gameObject);
            callback?.Invoke();
        }
    }

    private IEnumerator TypeLine(string line)
    {
        _text.text = "";
        _isTyping = true;

        foreach (char c in line)
        {
            _text.text += c;
            yield return new WaitForSecondsRealtime(0.04f);
        }

        _isTyping = false;
        _arrow.SetActive(true);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
