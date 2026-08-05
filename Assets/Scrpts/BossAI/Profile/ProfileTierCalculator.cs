// 명세서 4장 기준 raw 값 → tier 변환, 표본 임계값 판정.
public static class ProfileTierCalculator
{
    // 4.3 표본 최소 임계값
    public const int REACTION_MIN_SAMPLE_TOTAL = 8;
    public const int REACTION_MIN_SAMPLE_PER_BUCKET = 3;
    public const int RANGE_MIN_SAMPLE_TICKS = 20;
    public const int TANKER_MIN_SAMPLE_PER_MONSTER = 2;
    public const int PUNISH_MIN_SAMPLE = 3;
    public const int DODGE_BIAS_MIN_SAMPLE = 5;

    public static string ReactionTier(float avgSuccessRate)
    {
        if (avgSuccessRate < 0.4f) return "low";
        if (avgSuccessRate <= 0.75f) return "mid";
        return "high";
    }

    public static string RangeTier(float meleeRatio)
    {
        if (meleeRatio > 0.6f) return "melee";
        if (meleeRatio >= 0.3f) return "mid";
        return "ranged";
    }

    public static string PunishTier(float rate)
    {
        if (rate < 0.3f) return "low";
        if (rate <= 0.6f) return "mid";
        return "high";
    }

    public static string DodgeBiasTier(float leftRatio)
    {
        if (leftRatio >= 0.65f) return "left";
        if (leftRatio <= 0.35f) return "right";
        return "none";
    }

    // 예고시간 0초는 사실상 회피 불가능한 즉발 공격(예: 먹등불 실측 버그, 명세서 4.6 참고)이라
    // 반응속도 판정에 넣으면 "반응이 느리다"는 왜곡된 신호가 됨 — 반응속도 축에서만 제외한다.
    // 다른 축(펀치백/탱커비교/회피편향 등)은 telegraphDuration과 무관하므로 영향 없음.
    public static bool IsReactionEligible(float telegraphDuration) => telegraphDuration > 0f;

    // 4.4 스테이지1·2 가중평균. sampleWeight는 각 스테이지의 raw 표본 수(가중치 적용 전).
    public static float WeightedAverage(float s1Value, int s1Sample, float s2Value, int s2Sample, float s1Weight = 1.0f, float s2Weight = 1.8f)
    {
        float denom = s1Weight * s1Sample + s2Weight * s2Sample;
        if (denom <= 0f) return 0f;
        return (s1Value * s1Weight * s1Sample + s2Value * s2Weight * s2Sample) / denom;
    }
}
