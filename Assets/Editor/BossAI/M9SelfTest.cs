#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// M9 완료기준 검증: 프리셋이 실제로 의도한 티어를 만드는지, 강제 전환/주입 훅들이
// 릴리즈 빌드 조건부 컴파일(#if UNITY_EDITOR || DEVELOPMENT_BUILD) 안에서 정상 동작하는지.
public static class M9SelfTest
{
    private static bool _anyFailure;

    [MenuItem("Tools/Boss/Self-Test M9 DevTools")]
    public static void Run()
    {
        _anyFailure = false;

        Test_PresetSlowRangedLeft();
        Test_PresetFastMeleeNone();
        Test_EmotionForceStageBypassesRegression();
        Test_DecisionInjectionSkipsNetwork();

        PlayerProfileAggregator.ResetAll();
        EmotionCurveStateMachine.Begin(1);
        BossDecisionClient.ForceFallbackForDebug = false;
        BossDecisionClient.DebugInjectedDecision = null;

        if (_anyFailure) Debug.LogError("[M9SelfTest] 일부 테스트 실패");
        else Debug.Log("[M9SelfTest] 전부 통과");
    }

    private static void Check(string name, bool condition)
    {
        if (condition) Debug.Log($"[M9SelfTest] PASS: {name}");
        else { Debug.LogError($"[M9SelfTest] FAIL: {name}"); _anyFailure = true; }
    }

    private static PlayerProfileRawState BuildPreset(bool reactionHigh, bool meleeHeavy, bool leftBias)
    {
        var method = typeof(BossDevTools).GetMethod("BuildPreset", BindingFlags.NonPublic | BindingFlags.Static);
        return (PlayerProfileRawState)method.Invoke(null, new object[] { reactionHigh, meleeHeavy, leftBias });
    }

    private static void Test_PresetSlowRangedLeft()
    {
        PlayerProfileAggregator.ImportRawState(BuildPreset(false, false, true));
        var profile = PlayerProfileAggregator.GetCurrentProfile();

        Check("프리셋(느림/원거리/좌편향) - reaction.tier == low", profile.reaction.tier == "low");
        Check("프리셋(느림/원거리/좌편향) - range.tier == ranged", profile.range.tier == "ranged");
        Check("프리셋(느림/원거리/좌편향) - dodgeBias.tier == left", profile.dodgeBias.tier == "left");
    }

    private static void Test_PresetFastMeleeNone()
    {
        PlayerProfileAggregator.ImportRawState(BuildPreset(true, true, false));
        var profile = PlayerProfileAggregator.GetCurrentProfile();

        Check("프리셋(빠름/근접/편향없음) - reaction.tier == high", profile.reaction.tier == "high");
        Check("프리셋(빠름/근접/편향없음) - range.tier == melee", profile.range.tier == "melee");
        Check("프리셋(빠름/근접/편향없음) - dodgeBias.tier == none", profile.dodgeBias.tier == "none");
    }

    private static void Test_EmotionForceStageBypassesRegression()
    {
        EmotionCurveStateMachine.Begin(1);
        EmotionCurveStateMachine.DebugForceStage(EmotionStage.Chill);
        Check("강제 전환으로 방심->소름 즉시 이동", EmotionCurveStateMachine.CurrentStage == EmotionStage.Chill);

        EmotionCurveStateMachine.DebugForceStage(EmotionStage.Complacency);
        Check("강제 전환은 후퇴 금지 규칙을 우회함(소름->방심)", EmotionCurveStateMachine.CurrentStage == EmotionStage.Complacency);
    }

    private static void Test_DecisionInjectionSkipsNetwork()
    {
        var injected = new BossDecision { confidence = "high" };
        BossDecisionClient.DebugInjectedDecision = injected;

        BossDecision received = null;
        var routine = BossDecisionClient.RequestDecision(new PlayerProfile(), new DeathHistory(), d => received = d);
        while (routine.MoveNext()) { } // ProxyBaseUrl 네트워크 호출 없이 즉시 완료되어야 함(에디터 코루틴 수동 pump)

        Check("DebugInjectedDecision이 그대로 반환됨(네트워크 스킵)", received == injected);

        BossDecisionClient.DebugInjectedDecision = null;
        BossDecisionClient.ForceFallbackForDebug = true;

        BossDecision fallbackReceived = null;
        var routine2 = BossDecisionClient.RequestDecision(new PlayerProfile(), new DeathHistory(), d => fallbackReceived = d);
        while (routine2.MoveNext()) { }

        Check("ForceFallbackForDebug=true면 폴백 규칙으로 즉시 대체됨", fallbackReceived != null && fallbackReceived.confidence == "low");
    }
}
#endif
