using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 긴 안내문(문제문 등)을 띄우는 팝업. ESC 또는 우측 상단 X로 닫을 수 있다.
// ScreenFader/CenterMessageUI와 같은 방식으로 빈 오브젝트에 컴포넌트만 붙이면 자동 생성된다.
public class QuestPopupUI : MonoBehaviour
{
    public static QuestPopupUI Instance;

    private GameObject _panel;
    private Text _text;

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

    private void Update()
    {
        if (_panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    private void CreateUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 970;

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(transform, false);

        Image bg = _panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        RectTransform bgRt = bg.rectTransform;
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(900, 600);

        // 텍스트
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(_panel.transform, false);
        _text = textObj.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = 26;
        _text.alignment = TextAnchor.UpperLeft;
        _text.color = Color.white;
        _text.horizontalOverflow = HorizontalWrapMode.Wrap;
        _text.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform textRt = _text.rectTransform;
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(40, 40);
        textRt.offsetMax = new Vector2(-40, -80);

        // 닫기 버튼 (우측 상단 X)
        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(_panel.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.6f, 0.15f, 0.15f, 1f);
        RectTransform btnRt = btnImg.rectTransform;
        btnRt.anchorMin = new Vector2(1, 1);
        btnRt.anchorMax = new Vector2(1, 1);
        btnRt.pivot = new Vector2(1, 1);
        btnRt.anchoredPosition = new Vector2(-10, -10);
        btnRt.sizeDelta = new Vector2(40, 40);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(Hide);

        GameObject btnTextObj = new GameObject("X");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        Text btnText = btnTextObj.AddComponent<Text>();
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 24;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.text = "X";
        RectTransform btnTextRt = btnText.rectTransform;
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.offsetMin = Vector2.zero;
        btnTextRt.offsetMax = Vector2.zero;

        _panel.SetActive(false);
    }

    private System.Action _onClosed;

    // onClosed: 팝업이 닫힐 때(ESC/X) 한 번 호출됨 (예: 문제문 닫으면 바로 퍼즐 열기)
    public void Show(string content, System.Action onClosed = null)
    {
        _text.text = content;
        _onClosed = onClosed;
        _panel.SetActive(true);
    }

    public void Hide()
    {
        _panel.SetActive(false);

        System.Action callback = _onClosed;
        _onClosed = null;
        callback?.Invoke();
    }

    public bool IsOpen => _panel.activeSelf;
}
