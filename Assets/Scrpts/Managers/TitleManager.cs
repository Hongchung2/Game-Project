using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// 인트로 기획서 반영: 오프닝 영상(오프닝씬.mp4) 재생 후 타이틀 메뉴(게임시작/이어하기/나가기) 노출.
// 씬에 이미 있는 로고("돗가비")와 Fade 오브젝트, Canvas, OpeningVideo(VideoPlayer)는 그대로 쓰고,
// 버튼들만 코드로 생성한다(GameOverUI 등 이 세션에서 만든 다른 UI들과 같은 방식).
public class TitleManager : MonoBehaviour
{
    public CanvasGroup fadeCanvas;

    private VideoPlayer _openingVideo;
    private GameObject _logo;
    private GameObject _menuRoot;
    private bool _isTransitioning = false;
    private bool _menuShown = false;

    private void Start()
    {
        // 예전 "화면을 터치하세요" 안내문은 이제 안 맞는 문구라 꺼둔다(이름으로 찾아서 비활성화 -
        // 씬 파일을 직접 편집하지 않고 처리).
        GameObject oldPrompt = GameObject.Find("화면을 터치하세요");
        if (oldPrompt != null) oldPrompt.SetActive(false);

        _logo = GameObject.Find("돗가비");

        GameObject videoGO = GameObject.Find("OpeningVideo");
        _openingVideo = videoGO != null ? videoGO.GetComponent<VideoPlayer>() : null;

        BuildMenu();

        if (_openingVideo != null)
        {
            if (_logo != null) _logo.SetActive(false);
            _menuRoot.SetActive(false);
            _openingVideo.loopPointReached += _ => ShowMenu();
            _openingVideo.Play();
        }
        else
        {
            ShowMenu();
        }
    }

    private void Update()
    {
        // 영상 재생 중 터치/클릭하면 바로 스킵(기획서의 "터치하면 넘어간다" 관례를 오프닝 영상에도 적용).
        if (_openingVideo != null && _openingVideo.isPlaying && !_menuShown)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                _openingVideo.Stop();
                ShowMenu();
            }
        }
    }

    private void ShowMenu()
    {
        if (_menuShown) return;
        _menuShown = true;
        if (_logo != null) _logo.SetActive(true);
        _menuRoot.SetActive(true);
    }

    private void BuildMenu()
    {
        Canvas existingCanvas = FindAnyObjectByType<Canvas>();
        Transform parent = existingCanvas != null ? existingCanvas.transform : transform;

        _menuRoot = new GameObject("MenuRoot");
        _menuRoot.transform.SetParent(parent, false);
        var menuRect = _menuRoot.AddComponent<RectTransform>();
        menuRect.anchorMin = Vector2.zero;
        menuRect.anchorMax = Vector2.one;
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;

        const float startY = -100f;
        const float gap = 80f;

        CreateButton(_menuRoot.transform, "게임시작", new Vector2(0f, startY), OnGameStart);

        Button continueBtn = CreateButton(_menuRoot.transform, "이어하기", new Vector2(0f, startY - gap), OnContinue);
        if (!GameProgress.HasSave())
        {
            continueBtn.interactable = false;
            ColorBlock colors = continueBtn.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.3f);
            continueBtn.colors = colors;
        }

        CreateButton(_menuRoot.transform, "나가기", new Vector2(0f, startY - gap * 2), OnQuit);
    }

    private Button CreateButton(Transform parent, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGO = new GameObject($"Button_{label}");
        btnGO.transform.SetParent(parent, false);

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(240f, 60f);

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

        return btn;
    }

    // "게임시작"은 항상 새 게임. 기존 세이브가 있으면 지워진다는 걸 먼저 확인받는다.
    private void OnGameStart()
    {
        if (_isTransitioning) return;

        if (GameProgress.HasSave())
        {
            ShowConfirm(
                "기존 세이브가 삭제되고 새로 시작합니다.\n계속하시겠습니까?",
                StartNewGame);
        }
        else
        {
            StartNewGame();
        }
    }

    private void StartNewGame()
    {
        GameProgress.NewGame();
        StartCoroutine(FadeAndLoad("DialogScene"));
    }

    private void OnContinue()
    {
        if (_isTransitioning || !GameProgress.HasSave()) return;

        GameProgress.Load();
        string target = GameProgress.CheckpointScene;

        // 체크포인트가 가리키는 씬이 비어있거나(예전 버전 세이브) 더 이상 빌드에 없으면(개발 중 씬 정리 등)
        // 그 씬을 못 찾아 멈추는 대신 새 게임으로 안전하게 폴백한다.
        if (string.IsNullOrEmpty(target) || !Application.CanStreamedLevelBeLoaded(target))
        {
            StartNewGame();
            return;
        }

        StartCoroutine(FadeAndLoad(target));
    }

    private void OnQuit()
    {
        Application.Quit();
    }

    // "게임시작" 눌렀는데 이미 세이브가 있을 때 띄우는 확인창 - 예/아니오만 있는 단순 모달.
    private void ShowConfirm(string message, UnityEngine.Events.UnityAction onConfirm)
    {
        Canvas existingCanvas = FindAnyObjectByType<Canvas>();
        Transform parent = existingCanvas != null ? existingCanvas.transform : transform;

        GameObject overlay = new GameObject("ConfirmOverlay");
        overlay.transform.SetParent(parent, false);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.7f);
        var overlayRt = overlayImg.rectTransform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(overlay.transform, false);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        var panelRt = panelImg.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(600f, 260f);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(panel.transform, false);
        var text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = message;
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var textRt = text.rectTransform;
        textRt.anchorMin = new Vector2(0f, 0.4f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(20f, 0f);
        textRt.offsetMax = new Vector2(-20f, -20f);

        CreateButton(panel.transform, "예", new Vector2(-90f, -70f), () =>
        {
            Destroy(overlay);
            onConfirm();
        });
        CreateButton(panel.transform, "아니오", new Vector2(90f, -70f), () => Destroy(overlay));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        _isTransitioning = true;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            if (fadeCanvas != null) fadeCanvas.alpha = t;
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }
}
