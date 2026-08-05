using System;

public enum EmotionStage { Complacency, Unease, Awareness, Chill } // 방심, 위화감, 자각, 소름
                                                                     // 선언 순서 = 진행 순서(후퇴 금지 검사에 이 순서를 그대로 씀)

// 명세서 6장: 방심→위화감→자각→소름 상태머신. 보스방은 한 번에 하나만 진행되므로
// Telemetry/PlayerProfileAggregator와 같은 프로젝트 관례를 따라 static으로 둠(다른 시스템이
// 별도 참조 없이 바로 CurrentStage를 조회/구독할 수 있게).
public static class EmotionCurveStateMachine
{
    public static EmotionStage CurrentStage { get; private set; } = EmotionStage.Complacency;

    // (이전 단계, 새 단계) 순서로 발행. 채널1/채널2/실루엣/대사 시스템이 구독.
    public static event Action<EmotionStage, EmotionStage> OnStageChanged;

    private const float HP_SAFETY_VALVE_THRESHOLD = 0.6f; // 이하이면서 아직 위화감이면 강제 승급

    private static int _markThreshold;   // N
    private static float _timeThreshold; // T
    private static int _markHitCount;
    private static float _uneaseElapsed;
    private static bool _uneaseTimerRunning;

    // 보스방 진입/재도전 시 호출해서 이번 시도의 시작 단계와 문턱(N,T)을 정한다.
    public static void Begin(int attempt)
    {
        (_markThreshold, _timeThreshold) = GetThresholds(attempt);
        _markHitCount = 0;
        _uneaseElapsed = 0f;
        _uneaseTimerRunning = false;

        CurrentStage = attempt <= 1 ? EmotionStage.Complacency : EmotionStage.Unease;
        if (CurrentStage == EmotionStage.Unease) _uneaseTimerRunning = true;
    }

    // 보스 컨트롤러(M6a)의 Update에서 매 프레임 호출. 위화감 단계에서만 시간을 센다.
    public static void Tick(float deltaTime)
    {
        if (!_uneaseTimerRunning || CurrentStage != EmotionStage.Unease) return;

        _uneaseElapsed += deltaTime;
        if (_uneaseElapsed >= _timeThreshold)
            AdvanceTo(EmotionStage.Awareness);
    }

    // 산/구름 표식이 플레이어에게 적중할 때마다 호출(M6a의 MarkSystem에서).
    public static void NotifyMarkHit()
    {
        if (CurrentStage == EmotionStage.Complacency)
        {
            // 첫 표식 적중 = 위화감 진입 조건이자, 그 적중 자체도 N회 누적에 포함되는 첫 타.
            AdvanceTo(EmotionStage.Unease);
            _uneaseTimerRunning = true;
            _markHitCount = 1;
            CheckMarkThreshold();
            return;
        }

        if (CurrentStage != EmotionStage.Unease) return; // 자각/소름 단계는 더 셀 필요 없음

        _markHitCount++;
        CheckMarkThreshold();
    }

    private static void CheckMarkThreshold()
    {
        if (CurrentStage == EmotionStage.Unease && _markHitCount >= _markThreshold)
            AdvanceTo(EmotionStage.Awareness);
    }

    // HP 60% 안전판. 보스 컨트롤러가 플레이어 피격마다 호출.
    public static void NotifyHpPercent(float hpPercent01)
    {
        if (CurrentStage == EmotionStage.Unease && hpPercent01 <= HP_SAFETY_VALVE_THRESHOLD)
            AdvanceTo(EmotionStage.Awareness);
    }

    // 운무강림(페이즈 전환) 이벤트 - 소름 진입은 오직 이 경로로만.
    //
    // 버그 수정(플레이테스트에서 발견): 표식에 한 번도 안 맞고 근접 공격만으로 보스 HP를
    // 50% 밑으로 떨어뜨리면 Complacency에서 곧장 Chill로 건너뛰어버려서 위화감/자각 단계가
    // 통째로 스킵됐음(테두리 F1/F2, 실루엣, 첫 대사가 전부 발동 안 함) - 명세서 6.1
    // "자각 건너뛰고 소름부터 시작은 불가능"과 정면으로 어긋남. 그래서 소름으로 가기 전에
    // 중간 단계를 반드시 거치도록(각 단계 OnStageChanged도 정상 발행되도록) 강제함.
    public static void NotifyCloudDescent()
    {
        if (CurrentStage < EmotionStage.Unease) AdvanceTo(EmotionStage.Unease);
        if (CurrentStage < EmotionStage.Awareness) AdvanceTo(EmotionStage.Awareness);
        AdvanceTo(EmotionStage.Chill);
    }

    private static void AdvanceTo(EmotionStage newStage)
    {
        if (newStage <= CurrentStage) return; // 절대 후퇴 없음(enum 선언 순서 = 진행 순서)

        EmotionStage previous = CurrentStage;
        CurrentStage = newStage;
        _uneaseTimerRunning = CurrentStage == EmotionStage.Unease;
        OnStageChanged?.Invoke(previous, CurrentStage);
    }

    // 명세서 6.2 문턱표 그대로.
    public static (int markThreshold, float timeThreshold) GetThresholds(int attempt)
    {
        if (attempt <= 2) return (3, 60f);
        if (attempt == 3) return (2, 45f);
        if (attempt == 4) return (1, 30f);
        return (1, 25f); // attempt 5+ 하한선 고정
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // M9 디버그 도구 전용 - 정상적인 후퇴 금지 규칙을 우회해서 강제로 단계를 지정한다.
    // 릴리즈 빌드에서는 컴파일에서 아예 빠짐.
    public static void DebugForceStage(EmotionStage stage)
    {
        EmotionStage previous = CurrentStage;
        CurrentStage = stage;
        _uneaseTimerRunning = stage == EmotionStage.Unease;
        OnStageChanged?.Invoke(previous, stage);
    }
#endif
}
