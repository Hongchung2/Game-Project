// 명세서 5.5 폴백 규칙 알고리즘. 프록시 호출 실패/타임아웃/스키마검증실패/킬스위치 시
// BossDecisionClient가 이 규칙 엔진으로 대체한다. LLM 없이도 항상 유효한 BossDecision을 만든다.
public static class BossDecisionFallback
{
    private const float BASE_WEIGHT = 0.5f;
    private const float REACTION_LOW_BONUS = 0.15f;
    private const float RANGE_RANGED_BONUS = 0.15f;
    private const float WEIGHT_MIN = 0.3f;
    private const float WEIGHT_MAX = 0.7f;

    public static BossDecision Generate(PlayerProfile profile, DeathHistory deathHistory)
    {
        var decision = new BossDecision();

        float mountain = BASE_WEIGHT;
        float cloud = BASE_WEIGHT;
        if (profile.reaction.tier == "low") mountain += REACTION_LOW_BONUS;
        if (profile.range.tier == "ranged") cloud += RANGE_RANGED_BONUS;
        NormalizeAndClamp(ref mountain, ref cloud);
        decision.mark_weight.mountain = mountain;
        decision.mark_weight.cloud = cloud;

        bool habitual = profile.dodgeBias.tier != "none"
            && profile.dodgeBias.sample >= ProfileTierCalculator.DODGE_BIAS_MIN_SAMPLE;
        decision.mountain_target = habitual ? "habitual_position" : "current_position";

        if (profile.tankerComparison.weightedFavor == "허수아비")
        {
            decision.phase2_pattern_weight.punch = 0.65f;
            decision.phase2_pattern_weight.slam = 0.35f;
        }
        else if (profile.tankerComparison.weightedFavor == "벼루게")
        {
            decision.phase2_pattern_weight.punch = 0.35f;
            decision.phase2_pattern_weight.slam = 0.65f;
        }
        else // "데이터 부족"
        {
            decision.phase2_pattern_weight.punch = 0.5f;
            decision.phase2_pattern_weight.slam = 0.5f;
        }

        decision.phase2_recovery_exposed = profile.punish.tier != "high";

        // M7(FallbackDialoguePool) 연결: death_history 있으면 재도전 전용 풀 우선(명세서 5.5).
        bool hasDeathHistory = deathHistory != null
            && (deathHistory.recent.Count > 0 || deathHistory.summaryOlder.totalAttempts > 0);
        decision.boss_line_p1 = FallbackDialoguePool.GetLine(EmotionStage.Unease, hasDeathHistory);
        decision.boss_line_transition = FallbackDialoguePool.GetLine(EmotionStage.Chill, hasDeathHistory);

        decision.confidence = "low";

        return decision;
    }

    // 정규화(합이 1이 되도록) 후 0.3~0.7 clamp. 두 값 다 베이스가 0.5라 편향이 없으면 그대로 0.5/0.5 유지.
    private static void NormalizeAndClamp(ref float mountain, ref float cloud)
    {
        float sum = mountain + cloud;
        if (sum > 0f)
        {
            mountain = mountain / sum;
            cloud = cloud / sum;
        }

        mountain = Clamp(mountain);
        cloud = Clamp(cloud);
    }

    private static float Clamp(float value)
    {
        if (value < WEIGHT_MIN) return WEIGHT_MIN;
        if (value > WEIGHT_MAX) return WEIGHT_MAX;
        return value;
    }
}
