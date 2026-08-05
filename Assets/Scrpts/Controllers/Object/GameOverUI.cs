using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 사망 시 뜨는 게임오버 화면. ScreenFader/CenterMessageUI처럼 씬마다 미리 배치해둘 필요 없이,
// 죽는 순간(PlayerController.DeadAction) 코드로 직접 생성한다(BossDialogueDisplay와 같은 방식).
// "다시 도전"은 GameProgress의 마지막 체크포인트 씬을 다시 불러온다 - 체크포인트는 방/구간
// 진입 시점에만 갱신되므로(GameProgress.SetCheckpoint 호출부 참고), 사망한 바로 그 씬을
// 처음부터 다시 시작하는 것과 사실상 같다(몬스터/퍼즐 상태도 자연히 초기화됨).
public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    public static GameOverUI CreateInstance()
    {
        if (Instance != null) return Instance;

        GameObject canvasGO = new GameObject("GameOverUI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 990;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var instance = canvasGO.AddComponent<GameOverUI>();

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var title = titleGO.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.text = "GAME OVER";
        title.fontSize = 60;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0f, 100f);
        titleRt.sizeDelta = new Vector2(800f, 150f);

        instance.BuildButton(canvasGO.transform, "다시 도전", new Vector2(-130f, -80f), instance.Retry);
        instance.BuildButton(canvasGO.transform, "타이틀로", new Vector2(130f, -80f), instance.GoToTitle);

        Instance = instance;
        canvasGO.SetActive(false);
        return instance;
    }

    private void BuildButton(Transform parent, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGO = new GameObject($"Button_{label}");
        btnGO.transform.SetParent(parent, false);

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(220f, 70f);

        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        var text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    private void Retry()
    {
        string target = GameProgress.CheckpointScene;
        if (string.IsNullOrEmpty(target))
            target = SceneManager.GetActiveScene().name;

        SceneManager.LoadScene(target);
    }

    private void GoToTitle()
    {
        SceneManager.LoadScene("GameTitle");
    }
}
