using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 인트로 기획서 반영: 명령문(DialogScene) 다음 Stage1_Scene 진입 직후, 돗가비가 짧게 말을 거는 도입부 대사.
// GameProgress.IntroDialogueShown으로 딱 한 번만 재생(체크포인트가 Stage1_Scene 안쪽으로 다시 잡혀도 재생 안 됨).
public static class IntroDokkaebiLineLoader
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Stage1_Scene") return;
        if (GameProgress.IntroDialogueShown) return;
        IntroDokkaebiLine.CreateInstance();
    }
}

public class IntroDokkaebiLine : MonoBehaviour
{
    private static readonly string[] Lines =
    {
        "네 아비 녀석도 첫 임무 땐 벌벌 떨었지. 크크",
        "처음이라 긴장되지? 크크크",
        "내가 가면으로 변해서 너의 눈과 귀가 되어주니 걱정하지 말라고"
    };

    private Text _text;
    private GameObject _arrow;
    private int _index;
    private bool _isTyping;

    public static void CreateInstance()
    {
        GameObject go = new GameObject("IntroDokkaebiLine");
        go.AddComponent<IntroDokkaebiLine>();
    }

    private void Awake()
    {
        Time.timeScale = 0f;
        BuildUI();
        StartCoroutine(TypeLine(Lines[0]));
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("IntroDialogueCanvas");
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

        // 돗가비 초상화 - 대화창 오른쪽 위로 살짝 걸치게 배치(흔한 비주얼노벨식 대사 연출).
        Sprite[] portraitSprites = Resources.LoadAll<Sprite>("Art/돗가비1");
        if (portraitSprites != null && portraitSprites.Length > 0)
        {
            GameObject portraitGO = new GameObject("Portrait");
            portraitGO.transform.SetParent(canvasGO.transform, false);
            var portraitImg = portraitGO.AddComponent<Image>();
            portraitImg.sprite = portraitSprites[0];
            portraitImg.preserveAspect = true;
            var portraitRt = portraitImg.rectTransform;
            portraitRt.anchorMin = new Vector2(1f, 0f);
            portraitRt.anchorMax = new Vector2(1f, 0f);
            portraitRt.pivot = new Vector2(1f, 0f);
            portraitRt.sizeDelta = new Vector2(320f, 400f);
            portraitRt.anchoredPosition = new Vector2(-60f, 220f);
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
            _text.text = Lines[_index];
            _isTyping = false;
            _arrow.SetActive(true);
            return;
        }

        _index++;
        if (_index < Lines.Length)
        {
            _arrow.SetActive(false);
            StartCoroutine(TypeLine(Lines[_index]));
        }
        else
        {
            GameProgress.MarkIntroDialogueShown();
            Time.timeScale = 1f;
            Destroy(gameObject);
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
        // 도중에 파괴돼도(씬 전환 등) timeScale이 0으로 묶여있으면 안 되니 안전하게 복구.
        Time.timeScale = 1f;
    }
}
