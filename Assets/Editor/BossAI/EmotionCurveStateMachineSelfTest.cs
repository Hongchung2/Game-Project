#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// M5 완료기준 1~5를 배치모드에서 자동으로 확인하는 자체 검증 도구.
// EmotionCurveStateMachine은 UnityEngine 의존성이 없는 순수 C# static 클래스라
// Play Mode 없이도 실제 로직을 그대로 실행해서 검증할 수 있다.
public static class EmotionCurveStateMachineSelfTest
{
    private static bool _anyFailure;

    [MenuItem("Tools/Boss/Self-Test Emotion Curve State Machine")]
    public static void Run()
    {
        _anyFailure = false;

        Test_Attempt1_MarkCountPath();
        Test_Attempt1_TimePath();
        Test_Attempt2_SkipsComplacency();
        Test_ThresholdsPerAttempt();
        Test_HpSafetyValve();
        Test_NoRegression();
        Test_EventFires();

        if (_anyFailure)
            Debug.LogError("[EmotionSelfTest] 일부 테스트 실패 — 위 로그의 FAIL 항목 확인");
        else
            Debug.Log("[EmotionSelfTest] 전부 통과");
    }

    private static void Check(string name, bool condition)
    {
        if (condition)
        {
            Debug.Log($"[EmotionSelfTest] PASS: {name}");
        }
        else
        {
            Debug.LogError($"[EmotionSelfTest] FAIL: {name}");
            _anyFailure = true;
        }
    }

    // 완료기준 1: 표식 누적 N회로 자각 전이 (attempt 1~2는 N=3)
    private static void Test_Attempt1_MarkCountPath()
    {
        EmotionCurveStateMachine.Begin(1);
        Check("attempt1 시작은 Complacency", EmotionCurveStateMachine.CurrentStage == EmotionStage.Complacency);

        EmotionCurveStateMachine.NotifyMarkHit(); // 1번째 = 첫 표식 적중 -> Unease 진입 + 카운트 1
        Check("첫 표식 적중으로 Complacency->Unease", EmotionCurveStateMachine.CurrentStage == EmotionStage.Unease);

        EmotionCurveStateMachine.NotifyMarkHit(); // 2
        Check("2번째 표식으로는 아직 Unease(N=3 미달)", EmotionCurveStateMachine.CurrentStage == EmotionStage.Unease);

        EmotionCurveStateMachine.NotifyMarkHit(); // 3
        Check("3번째 표식(N=3)으로 Unease->Awareness", EmotionCurveStateMachine.CurrentStage == EmotionStage.Awareness);
    }

    // 완료기준 1: 경과 T초로 자각 전이 (표식 부족해도 시간으로 넘어감)
    private static void Test_Attempt1_TimePath()
    {
        EmotionCurveStateMachine.Begin(1);
        EmotionCurveStateMachine.NotifyMarkHit(); // Unease 진입, 카운트 1 (N=3에는 부족)

        EmotionCurveStateMachine.Tick(59f);
        Check("59초 경과로는 아직 Unease(T=60 미달)", EmotionCurveStateMachine.CurrentStage == EmotionStage.Unease);

        EmotionCurveStateMachine.Tick(1.5f); // 총 60.5초
        Check("60초 경과(T=60)로 Unease->Awareness", EmotionCurveStateMachine.CurrentStage == EmotionStage.Awareness);
    }

    // 완료기준 3: 재도전(attempt 2+)은 방심 생략하고 위화감부터 시작
    private static void Test_Attempt2_SkipsComplacency()
    {
        EmotionCurveStateMachine.Begin(2);
        Check("attempt2는 Complacency 생략, Unease부터 시작", EmotionCurveStateMachine.CurrentStage == EmotionStage.Unease);
    }

    // 완료기준 4: 시도 횟수에 따라 N/T가 실제로 줄어드는지
    private static void Test_ThresholdsPerAttempt()
    {
        var t1 = EmotionCurveStateMachine.GetThresholds(1);
        var t2 = EmotionCurveStateMachine.GetThresholds(2);
        var t3 = EmotionCurveStateMachine.GetThresholds(3);
        var t4 = EmotionCurveStateMachine.GetThresholds(4);
        var t5 = EmotionCurveStateMachine.GetThresholds(5);
        var t6 = EmotionCurveStateMachine.GetThresholds(6);

        Check("attempt1==attempt2 문턱(N=3,T=60)", t1 == (3, 60f) && t2 == (3, 60f));
        Check("attempt3 문턱(N=2,T=45)", t3 == (2, 45f));
        Check("attempt4 문턱(N=1,T=30)", t4 == (1, 30f));
        Check("attempt5 문턱(N=1,T=25, 하한선)", t5 == (1, 25f));
        Check("attempt6도 하한선 그대로(N=1,T=25)", t6 == (1, 25f));
    }

    // 완료기준 2: HP 60% 안전판 - 표식/시간 조건 미달이어도 강제 승급
    private static void Test_HpSafetyValve()
    {
        EmotionCurveStateMachine.Begin(1);
        EmotionCurveStateMachine.NotifyMarkHit(); // Unease 진입, 카운트 1(N=3 미달), 시간도 0(T=60 미달)

        EmotionCurveStateMachine.NotifyHpPercent(0.61f);
        Check("HP 61%에서는 아직 안전판 미발동", EmotionCurveStateMachine.CurrentStage == EmotionStage.Unease);

        EmotionCurveStateMachine.NotifyHpPercent(0.6f);
        Check("HP 60%(경계값)에서 안전판 발동, Unease->Awareness", EmotionCurveStateMachine.CurrentStage == EmotionStage.Awareness);
    }

    // 완료기준 5: 어떤 경우에도 단계가 후퇴하지 않음 (자각 도달 후 재적중/재HP체크로 되돌아가지 않는지)
    private static void Test_NoRegression()
    {
        EmotionCurveStateMachine.Begin(4); // N=1,T=30 - 표식 1회면 바로 자각
        EmotionCurveStateMachine.NotifyMarkHit();
        Check("attempt4에서 표식 1회로 바로 Awareness", EmotionCurveStateMachine.CurrentStage == EmotionStage.Awareness);

        EmotionCurveStateMachine.NotifyMarkHit(); // 자각 상태에서 더 맞아도 그대로
        EmotionCurveStateMachine.NotifyHpPercent(0.1f); // 안전판도 자각→위화감 되돌리기용이 아님
        Check("Awareness 상태에서 추가 적중/HP체크로도 후퇴 없음", EmotionCurveStateMachine.CurrentStage == EmotionStage.Awareness);

        EmotionCurveStateMachine.NotifyCloudDescent();
        Check("운무강림으로 Awareness->Chill", EmotionCurveStateMachine.CurrentStage == EmotionStage.Chill);

        EmotionCurveStateMachine.NotifyMarkHit();
        EmotionCurveStateMachine.NotifyHpPercent(0.1f);
        Check("Chill 도달 후에는 어떤 이벤트로도 후퇴 없음", EmotionCurveStateMachine.CurrentStage == EmotionStage.Chill);
    }

    // 요구사항: 단계 전이 시 OnStageChanged 이벤트가 실제로 발행되는지
    private static void Test_EventFires()
    {
        EmotionCurveStateMachine.Begin(4); // N=1이라 표식 1회로 바로 전이

        EmotionStage capturedFrom = default;
        EmotionStage capturedTo = default;
        bool fired = false;

        void Handler(EmotionStage from, EmotionStage to)
        {
            fired = true;
            capturedFrom = from;
            capturedTo = to;
        }

        EmotionCurveStateMachine.OnStageChanged += Handler;
        EmotionCurveStateMachine.NotifyMarkHit();
        EmotionCurveStateMachine.OnStageChanged -= Handler;

        Check("OnStageChanged 이벤트 발행됨", fired);
        Check("이벤트 인자(from=Unease,to=Awareness) 정확함", capturedFrom == EmotionStage.Unease && capturedTo == EmotionStage.Awareness);
    }
}
#endif
