using UnityEngine;
using UnityEngine.UI;

// 화면 우측 상단에 족자 수집 현황(춘하추동 4칸)을 표시.
// 빈 오브젝트에 이 컴포넌트만 붙이면 캔버스/슬롯을 코드가 자동 생성한다.
// 모은 족자는 밝게, 아직 안 모은 족자는 어둡게 표시된다.
public class ScrollCollectionUI : MonoBehaviour
{
    public static ScrollCollectionUI Instance;

    [Header("배치")]
    public Vector2 slotSize = new Vector2(60f, 80f);
    public float spacing = 10f;
    public float margin = 20f;

    [Header("아트 (선택) - 비우면 색 박스로 표시)")]
    public Sprite[] scrollSprites; // 순서: 춘,하,추,동. 아트 나오면 여기 연결

    // 계절별 placeholder 색 (춘=초록, 하=주황, 추=갈색, 동=하늘)
    private readonly Color[] _seasonColors =
    {
        new Color(0.40f, 0.80f, 0.35f),
        new Color(0.95f, 0.55f, 0.15f),
        new Color(0.70f, 0.40f, 0.18f),
        new Color(0.45f, 0.75f, 0.95f),
    };

    private Image[] _slots;

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

    private void Start()
    {
        Refresh(); // 씬 진입 시 지금까지 모은 상태 반영
    }

    private void CreateUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // 페이드(999)보다는 아래

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        int count = ScrollCollection.Seasons.Length;
        _slots = new Image[count];

        for (int i = 0; i < count; i++)
        {
            GameObject slot = new GameObject($"Scroll_{ScrollCollection.Seasons[i]}");
            slot.transform.SetParent(transform, false);

            Image img = slot.AddComponent<Image>();
            img.raycastTarget = false;
            if (scrollSprites != null && i < scrollSprites.Length && scrollSprites[i] != null)
                img.sprite = scrollSprites[i];

            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = slotSize;
            // 오른쪽 끝에서부터: i=0(춘) 가장 왼쪽, 마지막(동) 가장 오른쪽
            float x = -(margin + (count - 1 - i) * (slotSize.x + spacing));
            rt.anchoredPosition = new Vector2(x, -margin);

            _slots[i] = img;
        }
    }

    // 수집 상태에 맞춰 각 슬롯 밝기 갱신
    public void Refresh()
    {
        if (_slots == null) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            string season = ScrollCollection.Seasons[i];
            // 작업지시서 #09: 기둥에 끼워 넣어서 "사용됨" 처리되면 다시 흐리게(모은 적 없는 것과 같은 표시).
            bool has = ScrollCollection.IsCollected(season) && !ScrollCollection.IsUsed(season);
            Color baseColor = _seasonColors[i];

            if (has)
                _slots[i].color = baseColor;                                   // 모음: 밝게
            else
                _slots[i].color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.25f); // 안 모음/사용함: 흐리게
        }
    }
}
