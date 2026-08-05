using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 메인화면 개편: "아무 데나 클릭하면 시작" → 새 게임/이어하기/조작법/종료 메뉴.
// 씬에 이미 있는 로고("돗가비")와 Fade 오브젝트, Canvas는 그대로 쓰고, 버튼들만 코드로 생성한다
// (GameOverUI 등 이 세션에서 만든 다른 UI들과 같은 방식 - 씬 파일을 직접 손대지 않아도 됨).
public class TitleManager : MonoBehaviour
{
    public CanvasGroup fadeCanvas;

    private GameObject _controlsPanel;
    private bool _isTransitioning = false;

    private void Start()
    {
        // 예전 "화면을 터치하세요" 안내문은 이제 안 맞는 문구라 꺼둔다(이름으로 찾아서 비활성화 -
        // 씬 파일을 직접 편집하지 않고 처리).
        GameObject oldPrompt = GameObject.Find("화면을 터치하세요");
        if (oldPrompt != null) oldPrompt.SetActive(false);

        BuildMenu();
    }

    private void BuildMenu()
    {
        Canvas existingCanvas = FindAnyObjectByType<Canvas>();
        Transform parent = existingCanvas != null ? existingCanvas.transform : transform;

        const float startY = -100f;
        const float gap = 80f;

        CreateButton(parent, "새 게임", new Vector2(0f, startY), OnNewGame);

        Button continueBtn = CreateButton(parent, "이어하기", new Vector2(0f, startY - gap), OnContinue);
        if (!GameProgress.HasSave())
        {
            continueBtn.interactable = false;
            ColorBlock colors = continueBtn.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.3f);
            continueBtn.colors = colors;
        }

        CreateButton(parent, "조작법", new Vector2(0f, startY - gap * 2), ToggleControls);
        CreateButton(parent, "종료", new Vector2(0f, startY - gap * 3), OnQuit);

        BuildControlsPanel(parent);
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

    private void BuildControlsPanel(Transform parent)
    {
        GameObject panel = new GameObject("ControlsPanel");
        panel.transform.SetParent(parent, false);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(600f, 320f);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(panel.transform, false);
        var text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "이동 : 방향키 / WASD\n상호작용 : F\n공격 : Space\n\n(아무 곳이나 클릭하면 닫힙니다)";
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(20f, 20f);
        textRt.offsetMax = new Vector2(-20f, -20f);

        Button closeBtn = panel.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => panel.SetActive(false));

        panel.SetActive(false);
        _controlsPanel = panel;
    }

    private void ToggleControls()
    {
        if (_controlsPanel != null) _controlsPanel.SetActive(!_controlsPanel.activeSelf);
    }

    private void OnNewGame()
    {
        if (_isTransitioning) return;
        GameProgress.NewGame();
        StartCoroutine(FadeAndLoad("DialogScene"));
    }

    private void OnContinue()
    {
        if (_isTransitioning || !GameProgress.HasSave()) return;
        GameProgress.Load();
        string target = string.IsNullOrEmpty(GameProgress.CheckpointScene) ? "DialogScene" : GameProgress.CheckpointScene;
        StartCoroutine(FadeAndLoad(target));
    }

    private void OnQuit()
    {
        Application.Quit();
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
