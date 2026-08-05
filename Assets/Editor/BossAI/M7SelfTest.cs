#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// M7 완료기준 중 Play Mode 없이도 검증 가능한 순수 로직(Sanitize 필터, 폴백 풀 선택)만 확인.
// 실제 화면 표시(페이드 인/아웃, Canvas)는 MonoBehaviour 생명주기가 필요해서 Play Mode 확인 필요.
public static class M7SelfTest
{
    private static bool _anyFailure;

    [MenuItem("Tools/Boss/Self-Test M7 Dialogue")]
    public static void Run()
    {
        _anyFailure = false;

        Test_ValidLinePassesThrough();
        Test_BannedWordsReplaced();
        Test_TooLongReplaced();
        Test_FallbackPoolBySrage();
        Test_RetryPoolOverridesStage();
        Test_FallbackGeneratorProducesCleanLines();

        if (_anyFailure) Debug.LogError("[M7SelfTest] 일부 테스트 실패");
        else Debug.Log("[M7SelfTest] 전부 통과");
    }

    private static void Check(string name, bool condition)
    {
        if (condition) Debug.Log($"[M7SelfTest] PASS: {name}");
        else { Debug.LogError($"[M7SelfTest] FAIL: {name}"); _anyFailure = true; }
    }

    // 완료기준 1: 정상 대사(금칙어 없음, 40자 이내)는 그대로 통과
    private static void Test_ValidLinePassesThrough()
    {
        string result = BossDialogueDisplay.Sanitize("또 그 자리군.");
        Check("정상 대사는 그대로 통과", result == "또 그 자리군.");
    }

    // 완료기준 2,3: 금칙어 포함 mock 응답 -> null(호출부에서 폴백으로 교체)
    private static void Test_BannedWordsReplaced()
    {
        Check("'AI' 포함 -> null(폴백 대상)", BossDialogueDisplay.Sanitize("이건 AI가 아니야") == null);
        Check("'데이터' 포함 -> null(폴백 대상)", BossDialogueDisplay.Sanitize("네 데이터를 안다") == null);
        Check("'시스템' 포함 -> null(폴백 대상)", BossDialogueDisplay.Sanitize("시스템이 알려줬지") == null);
    }

    // 완료기준 4: 40자 초과 mock 응답 -> null(호출부에서 폴백으로 교체)
    private static void Test_TooLongReplaced()
    {
        string exact40 = new string('가', 40);
        string over40 = new string('가', 41);
        Check("정확히 40자는 통과", BossDialogueDisplay.Sanitize(exact40) == exact40);
        Check("41자는 null(폴백 대상)", BossDialogueDisplay.Sanitize(over40) == null);
    }

    private static void Test_FallbackPoolBySrage()
    {
        string complacency = FallbackDialoguePool.GetLine(EmotionStage.Complacency, false);
        string unease = FallbackDialoguePool.GetLine(EmotionStage.Unease, false);
        string awareness = FallbackDialoguePool.GetLine(EmotionStage.Awareness, false);
        string chill = FallbackDialoguePool.GetLine(EmotionStage.Chill, false);

        Check("Complacency 풀에서 대사 반환", !string.IsNullOrEmpty(complacency));
        Check("Unease 풀에서 대사 반환", !string.IsNullOrEmpty(unease));
        Check("Awareness 풀에서 대사 반환", !string.IsNullOrEmpty(awareness));
        Check("Chill 풀에서 대사 반환", !string.IsNullOrEmpty(chill));
    }

    // death_history 있으면 감정단계 무관하게 재도전 전용 풀 사용(명세서 5.5)
    private static void Test_RetryPoolOverridesStage()
    {
        const string retry1 = "저번엔 오른쪽이었지.";
        const string retry2 = "또 같은 자리인가.";
        const string retry3 = "달라진 게 없군.";

        bool allFromRetryPool = true;
        for (int i = 0; i < 30; i++)
        {
            string line = FallbackDialoguePool.GetLine(EmotionStage.Awareness, true);
            if (line != retry1 && line != retry2 && line != retry3)
            {
                allFromRetryPool = false;
                break;
            }
        }
        Check("hasDeathHistory=true면 재도전 풀에서만 선택됨", allFromRetryPool);
    }

    // BossDecisionFallback(M4)이 만드는 대사가 자체 필터를 통과하는지(회귀 확인)
    private static void Test_FallbackGeneratorProducesCleanLines()
    {
        var profile = new PlayerProfile();
        var deathHistory = new DeathHistory();

        BossDecision decision = BossDecisionFallback.Generate(profile, deathHistory);

        Check("BossDecisionFallback.boss_line_p1이 Sanitize 통과", BossDialogueDisplay.Sanitize(decision.boss_line_p1) != null);
        Check("BossDecisionFallback.boss_line_transition이 Sanitize 통과", BossDialogueDisplay.Sanitize(decision.boss_line_transition) != null);
    }
}
#endif
