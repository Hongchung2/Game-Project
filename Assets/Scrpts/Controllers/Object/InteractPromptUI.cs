using UnityEngine;
using UnityEngine.UI;

// 상호작용 가능한 대상 머리 위에 뜨는 F키 아이콘.
// 정식 아이콘 그림이 나오기 전까지는 원형+텍스트로 표시한다.
// ScreenFader와 같은 방식: 빈 오브젝트에 컴포넌트만 붙이면 캔버스/아이콘이 자동 생성된다.
public class InteractPromptUI : MonoBehaviour
{
    public static InteractPromptUI Instance;

    [Header("표시 위치")]
    public Vector3 worldOffset = new Vector3(0, 1f, 0); // 대상 머리 위로 띄우는 높이

    private RectTransform _iconRoot;
    private Camera _cam;
    private Transform _followTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _cam = Camera.main;
        CreateUI();
        Hide();
    }

    private void CreateUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject iconObj = new GameObject("FIcon");
        iconObj.transform.SetParent(transform, false);

        Image bg = iconObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        _iconRoot = bg.rectTransform;
        _iconRoot.sizeDelta = new Vector2(40, 40);

        GameObject textObj = new GameObject("FText");
        textObj.transform.SetParent(iconObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "F";
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }

    public void Show(Transform target)
    {
        _followTarget = target;
        _iconRoot.gameObject.SetActive(true);
    }

    public void Hide()
    {
        _followTarget = null;
        if (_iconRoot != null) _iconRoot.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_followTarget == null) return;
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Vector3 screenPos = _cam.WorldToScreenPoint(_followTarget.position + worldOffset);
        _iconRoot.position = screenPos;
    }
}
