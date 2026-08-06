using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// ESC로 여는 일시정지 메뉴 (이어하기 / 다시하기 / 타이틀).
// GameOverUI와 같은 방식으로 씬에 미리 배치하지 않고 코드로 생성하며, 게임플레이 씬에 들어갈
// 때마다 자동으로 붙는다(타이틀/대화 씬 등 조작이 없는 씬은 제외).
public class PauseMenuUI : MonoBehaviour
{
    // 조작할 게 없어서 일시정지가 의미 없는 씬들.
    private static readonly string[] ExcludedScenes =
    {
        "GameTitle",
        "DialogScene",
        "Stage2_IntermissionScene",
    };

    public static PauseMenuUI Instance { get; private set; }

    private GameObject _panel;
    private bool _isPaused;
    // 대사창/퍼즐 UI가 이미 timeScale을 0으로 만들어둔 상태에서 일시정지를 열 수 있으므로,
    // 1로 되돌리는 게 아니라 "열기 직전의 값"으로 복원해야 대사가 도중에 재생되지 않는다.
    private float _prevTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreateForActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryCreateForActiveScene();

    private static void TryCreateForActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        foreach (var excluded in ExcludedScenes)
        {
            if (sceneName == excluded) return;
        }
        CreateInstance();
    }

    public static PauseMenuUI CreateInstance()
    {
        if (Instance != null) return Instance;

        GameObject canvasGO = new GameObject("PauseMenuUI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 게임오버(990)보다 위, 페이드(999)보다 아래.
        canvas.sortingOrder = 995;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        var instance = canvasGO.AddComponent<PauseMenuUI>();
        instance.BuildUI(canvasGO.transform);

        Instance = instance;
        return instance;
    }

    private void BuildUI(Transform root)
    {
        _panel = new GameObject("Panel");
        _panel.transform.SetParent(root, false);
        var bg = _panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(_panel.transform, false);
        var title = titleGO.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.text = "일시정지";
        title.fontSize = 60;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0f, 180f);
        titleRt.sizeDelta = new Vector2(800f, 150f);

        BuildButton(_panel.transform, "이어하기", new Vector2(0f, 50f), Resume);
        BuildButton(_panel.transform, "다시하기", new Vector2(0f, -40f), Restart);
        BuildButton(_panel.transform, "타이틀", new Vector2(0f, -130f), GoToTitle);

        _panel.SetActive(false);
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
        rt.sizeDelta = new Vector2(280f, 75f);

        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        var text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (_panel != null) _panel.SetActive(true);
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        Time.timeScale = _prevTimeScale;
        if (_panel != null) _panel.SetActive(false);
    }

    private void Restart()
    {
        // 체크포인트는 방/구간 진입 시점에만 갱신되므로 사실상 "지금 이 씬을 처음부터"와 같다
        // (GameOverUI.Retry와 동일한 정책 - 두 경로의 동작을 일부러 맞춰둠).
        string target = GameProgress.CheckpointScene;
        if (string.IsNullOrEmpty(target)) target = SceneManager.GetActiveScene().name;

        LeavePause();
        SceneManager.LoadScene(target);
    }

    private void GoToTitle()
    {
        // 지금까지 쌓인 묵운산군 학습 데이터를 흘리지 않게 나가기 전에 저장.
        BossDataPersistence.Save(BossDataPersistence.CurrentSlot);
        GameProgress.Save(GameProgress.CurrentSlot);

        LeavePause();
        SceneManager.LoadScene("GameTitle");
    }

    // 씬을 떠날 땐 직전 값이 아니라 무조건 1로 되돌린다 - 대사 중 일시정지했다가 나가면
    // timeScale이 0인 채로 다음 씬이 시작돼 아무것도 안 움직이게 된다.
    private void LeavePause()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (_panel != null) _panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }
}
