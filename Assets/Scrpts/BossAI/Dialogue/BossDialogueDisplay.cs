using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 명세서 M7: LLM 대사(boss_line_p1/boss_line_transition) 또는 폴백 대사를 보스 머리 위
// 말풍선으로 표시. EmotionCurveStateMachine.OnStageChanged를 구독해서 트리거 시점을
// 스스로 판단한다(보스 컨트롤러가 매번 직접 호출할 필요 없음 - M5 상태머신에 대한 순수 구독).
public class BossDialogueDisplay : MonoBehaviour
{
    // 명세서 0장 원칙(AI라는 사실 절대 노출 금지)을 위한 금칙어 필터.
    private static readonly string[] BannedWords = { "AI", "데이터", "분석", "알고리즘", "시스템" };
    private const int MAX_LENGTH = 40;

    public static BossDialogueDisplay Instance { get; private set; }

    public float showDuration = 2.5f;
    public float fadeDuration = 0.4f;

    private CanvasGroup _canvasGroup;
    private Text _text;
    private readonly Queue<string> _pendingLines = new Queue<string>();
    private Coroutine _queueRoutine;
    private bool _p1LineShown;
    private BossDecision _decision;
    private DeathHistory _deathHistory;

    // followTarget: 말풍선이 계속 따라다닐 보스 Transform (보스 머리 위에 뜨는 말풍선용).
    public static BossDialogueDisplay CreateInstance(Transform followTarget)
    {
        if (Instance != null) return Instance;

        GameObject canvasGO = new GameObject("BossDialogueBubble");
        canvasGO.transform.SetParent(followTarget, false);
        canvasGO.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        canvasGO.transform.localScale = Vector3.one * 0.01f; // 월드 스페이스 캔버스 - 픽셀 단위를 월드 크기로 축소

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(460f, 160f);

        var group = canvasGO.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // 말풍선 배경 - 정식 아트 나오기 전까지 반투명 검정 사각형으로 대체(placeholder).
        GameObject bgGO = new GameObject("BubbleBackground");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);
        var bgRect = bg.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject textGO = new GameObject("BossLineText");
        textGO.transform.SetParent(canvasGO.transform, false);
        var text = textGO.AddComponent<Text>();
        // 프로젝트의 다른 한글 UI와 동일한 레거시 폰트 사용 - TextMeshPro 기본 폰트(LiberationSans SDF)는
        // 한글 글리프가 없어서 한글 대사가 그대로 안 보이는 문제가 있었음(플레이테스트에서 발견).
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 32;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        var rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20f, 10f);
        rect.offsetMax = new Vector2(-20f, -10f);

        var display = canvasGO.AddComponent<BossDialogueDisplay>();
        display._canvasGroup = group;
        display._text = text;
        Instance = display;
        return display;
    }

    // 보스 컨트롤러가 BossDecision을 받은 직후 호출.
    public void Configure(BossDecision decision, DeathHistory deathHistory)
    {
        _decision = decision;
        _deathHistory = deathHistory;
    }

    private void OnEnable()
    {
        EmotionCurveStateMachine.OnStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        EmotionCurveStateMachine.OnStageChanged -= HandleStageChanged;
    }

    private void HandleStageChanged(EmotionStage from, EmotionStage to)
    {
        // boss_line_p1: Unease 또는 Awareness로 "처음" 전이될 때 1회만(재도전은 Unease부터
        // 시작이라 전이 이벤트가 안 뜨므로 자연스럽게 Awareness 전이가 그 1회가 됨).
        if (!_p1LineShown && (to == EmotionStage.Unease || to == EmotionStage.Awareness))
        {
            _p1LineShown = true;
            ShowLine(ResolveLine(_decision != null ? _decision.boss_line_p1 : null, to));
        }
        else if (to == EmotionStage.Chill)
        {
            ShowLine(ResolveLine(_decision != null ? _decision.boss_line_transition : null, to));
        }
    }

    private bool HasDeathHistory()
    {
        return _deathHistory != null && (_deathHistory.recent.Count > 0 || _deathHistory.summaryOlder.totalAttempts > 0);
    }

    private string ResolveLine(string candidate, EmotionStage stageForFallback)
    {
        string sanitized = Sanitize(candidate);
        if (sanitized != null) return sanitized;
        return FallbackDialoguePool.GetLine(stageForFallback, HasDeathHistory());
    }

    // 금칙어 포함 또는 40자 초과면 null(호출부가 폴백 대사로 대체) - 자르는 대신 통째로 교체하는 쪽을 택함
    // (문장 중간을 잘라내면 "AI 노출 금지" 원칙에 오히려 더 어색하게 걸릴 위험이 있다고 판단).
    public static string Sanitize(string line)
    {
        if (string.IsNullOrEmpty(line)) return null;

        foreach (var banned in BannedWords)
        {
            if (line.Contains(banned)) return null;
        }

        if (line.Length > MAX_LENGTH) return null;

        return line;
    }

    // 감정 단계가 같은 프레임 안에서 연달아 여러 번 전이될 수 있음(예: 표식 한 번도 안 맞고
    // 근접만으로 페이즈 전환 - EmotionCurveStateMachine.NotifyCloudDescent가 위화감→자각→소름을
    // 한 번에 강제로 훑고 지나감). 이때 대사가 즉시 덮어써지면서 이전 줄이 0초 노출로 사라지던
    // 버그를 큐로 순서대로 재생하게 고침(플레이테스트에서 "대사 하나도 안 뜬다"로 발견).
    public void ShowLine(string line)
    {
        _pendingLines.Enqueue(line);
        if (_queueRoutine == null) _queueRoutine = StartCoroutine(DrainQueue());
    }

    private IEnumerator DrainQueue()
    {
        while (_pendingLines.Count > 0)
        {
            _text.text = _pendingLines.Dequeue();

            yield return Fade(0f, 1f, fadeDuration);
            yield return new WaitForSeconds(showDuration);
            yield return Fade(1f, 0f, fadeDuration);
        }
        _queueRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _canvasGroup.alpha = to;
    }
}
