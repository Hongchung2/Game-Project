using UnityEngine;

public enum MarkType { Mountain, Cloud } // 산 표식 / 구름 표식

// BossDecision.mark_weight/mountain_target을 보관하고, 도깨비불 피격마다 표식을 확률적으로 뽑는다.
// 보스방 전투 1회당 하나만 존재하므로 이 프로젝트의 static 상태 관례(Telemetry 등)를 그대로 따름.
public static class MarkSystem
{
    private static float _mountainWeight = 0.5f;
    private static float _cloudWeight = 0.5f;
    private static string _mountainTarget = "current_position";

    public static void Configure(BossDecision decision)
    {
        if (decision == null || decision.mark_weight == null) return;
        _mountainWeight = decision.mark_weight.mountain;
        _cloudWeight = decision.mark_weight.cloud;
        _mountainTarget = string.IsNullOrEmpty(decision.mountain_target) ? "current_position" : decision.mountain_target;
    }

    public static string MountainTarget => _mountainTarget;

    public static MarkType RollMark()
    {
        float sum = _mountainWeight + _cloudWeight;
        if (sum <= 0f) return Random.value < 0.5f ? MarkType.Mountain : MarkType.Cloud;

        float r = Random.value * sum;
        return r < _mountainWeight ? MarkType.Mountain : MarkType.Cloud;
    }
}
