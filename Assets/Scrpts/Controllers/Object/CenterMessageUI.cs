using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 화면 중앙에 잠깐 뜨는 안내 텍스트 (예: "청동 방울을 획득했습니다!").
// 빈 오브젝트에 이 컴포넌트만 붙이면 캔버스/텍스트를 자동 생성한다. (ScreenFader와 같은 방식)
public class CenterMessageUI : MonoBehaviour
{
    public static CenterMessageUI Instance;

    private Text _text;
    private Coroutine _current;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateUI();
    }

    private void CreateUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 페이드(999)보다도 위 - 검은 화면 위에 엔딩 텍스트를 띄우기 위함

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(transform, false);

        _text = textObj.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = 48;
        _text.alignment = TextAnchor.MiddleCenter;
        _text.color = Color.white;
        _text.text = "";

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        RectTransform rt = _text.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 150f); // 화면 중앙보다 살짝 위
        rt.sizeDelta = new Vector2(1000, 100);
    }

    public void Show(string message, float duration = 2f)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(ShowRoutine(message, duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        _text.text = message;
        yield return new WaitForSeconds(duration);
        _text.text = "";
        _current = null;
    }
}
