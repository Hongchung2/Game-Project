#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// M6a 완료기준 중 Play Mode 없이도 검증 가능한 순수 로직(MarkSystem 확률분포,
// HabitualPositionTracker 격자 계산)만 배치모드에서 자동 확인한다.
// 실제 전투 흐름(볼트 발사/피격/데미지/HP연동)은 코루틴+MonoBehaviour 생명주기에
// 의존해서 Play Mode가 필요하므로 이 자체 테스트 범위 밖.
public static class M6aSelfTest
{
    private static bool _anyFailure;

    [MenuItem("Tools/Boss/Self-Test M6a Pure Logic")]
    public static void Run()
    {
        _anyFailure = false;

        Test_MarkWeightDistribution();
        Test_MountainTargetModes();
        Test_HabitualPositionGrid();
        Test_Phase2PatternWeightDistribution();

        if (_anyFailure)
            Debug.LogError("[M6aSelfTest] 일부 테스트 실패");
        else
            Debug.Log("[M6aSelfTest] 전부 통과");
    }

    private static void Check(string name, bool condition)
    {
        if (condition) Debug.Log($"[M6aSelfTest] PASS: {name}");
        else { Debug.LogError($"[M6aSelfTest] FAIL: {name}"); _anyFailure = true; }
    }

    // 완료기준 2: mark_weight를 0.7/0.3으로 바꿨을 때 실제 표식 분포가 달라지는지
    private static void Test_MarkWeightDistribution()
    {
        var decision70 = new BossDecision();
        decision70.mark_weight.mountain = 0.7f;
        decision70.mark_weight.cloud = 0.3f;
        MarkSystem.Configure(decision70);

        int mountainCount = 0;
        const int trials = 20000;
        for (int i = 0; i < trials; i++)
        {
            if (MarkSystem.RollMark() == MarkType.Mountain) mountainCount++;
        }
        float ratio = (float)mountainCount / trials;
        Check($"mountain 0.7 가중치 -> 실측 비율 {ratio:F3} (0.65~0.75 기대)", ratio >= 0.65f && ratio <= 0.75f);

        var decision30 = new BossDecision();
        decision30.mark_weight.mountain = 0.3f;
        decision30.mark_weight.cloud = 0.7f;
        MarkSystem.Configure(decision30);

        int mountainCount2 = 0;
        for (int i = 0; i < trials; i++)
        {
            if (MarkSystem.RollMark() == MarkType.Mountain) mountainCount2++;
        }
        float ratio2 = (float)mountainCount2 / trials;
        Check($"mountain 0.3 가중치 -> 실측 비율 {ratio2:F3} (0.25~0.35 기대)", ratio2 >= 0.25f && ratio2 <= 0.35f);
        Check("가중치를 반대로 바꾸면 분포도 반대로 바뀜", ratio > ratio2);
    }

    // 완료기준 3: mountain_target 두 모드가 각각 다른 지점을 조준하는지
    private static void Test_MountainTargetModes()
    {
        var currentPosDecision = new BossDecision { mountain_target = "current_position" };
        MarkSystem.Configure(currentPosDecision);
        Check("current_position 모드 설정 확인", MarkSystem.MountainTarget == "current_position");

        var habitualDecision = new BossDecision { mountain_target = "habitual_position" };
        MarkSystem.Configure(habitualDecision);
        Check("habitual_position 모드 설정 확인", MarkSystem.MountainTarget == "habitual_position");
    }

    private static void Test_HabitualPositionGrid()
    {
        HabitualPositionTracker.ResetAll();

        // (5,5) 근처에 많이 머물고, (25,25) 근처엔 적게 머무는 상황을 시뮬레이션.
        // Tick은 0.5초 간격 샘플링이라 deltaTime을 크게 줘서 매번 샘플링되게 함.
        for (int i = 0; i < 10; i++)
            HabitualPositionTracker.Tick(new Vector2(5f, 5f), 0.6f);

        for (int i = 0; i < 2; i++)
            HabitualPositionTracker.Tick(new Vector2(25f, 25f), 0.6f);

        Vector2 result = HabitualPositionTracker.GetMostVisitedWorldPosition(new Vector2(15f, 15f));
        bool nearFive = Vector2.Distance(result, new Vector2(5f, 5f)) < HabitualPositionTracker.CELL_SIZE;
        Check($"가장 많이 머문 (5,5) 근처가 habitual_position으로 반환됨 (실제: {result})", nearFive);

        HabitualPositionTracker.ResetAll();
        Vector2 fallback = new Vector2(1f, 2f);
        Vector2 emptyResult = HabitualPositionTracker.GetMostVisitedWorldPosition(fallback);
        Check("샘플이 없으면 fallback(현재 위치) 그대로 반환", emptyResult == fallback);
    }

    // M6b 완료기준 5: phase2_pattern_weight대로 돌주먹/양손강타 분포가 달라지는지
    private static void Test_Phase2PatternWeightDistribution()
    {
        const int trials = 20000;

        int punchCount = 0;
        for (int i = 0; i < trials; i++)
            if (MukunSangunController.RollUsePunch(0.65f, 0.35f)) punchCount++;
        float ratio = (float)punchCount / trials;
        Check($"punch 0.65 가중치 -> 실측 비율 {ratio:F3} (0.60~0.70 기대)", ratio >= 0.60f && ratio <= 0.70f);

        int punchCount2 = 0;
        for (int i = 0; i < trials; i++)
            if (MukunSangunController.RollUsePunch(0.35f, 0.65f)) punchCount2++;
        float ratio2 = (float)punchCount2 / trials;
        Check($"punch 0.35 가중치 -> 실측 비율 {ratio2:F3} (0.30~0.40 기대)", ratio2 >= 0.30f && ratio2 <= 0.40f);
        Check("가중치를 반대로 바꾸면 분포도 반대로 바뀜", ratio > ratio2);
    }
}
#endif
