using System;
using UnityEngine;

// 텔레메트리 이벤트(Telemetry.cs)를 구독해서 5축 PlayerProfile을 실시간 누적하는 계층.
// 순수 static 클래스로 만들어서(ScrollCollection/WaterPuzzleState 등 이 프로젝트의 기존
// 크로스씬 상태 패턴과 동일), 씬 전환에도 값이 유지되고 별도 GameObject/DontDestroyOnLoad가
// 필요 없다. 정적 생성자에서 Telemetry의 C# 이벤트를 구독한다(Observer 패턴 — 이 클래스가
// Telemetry를 구독하는 방향으로만 의존성이 흐름, Telemetry는 이 클래스를 모른다).
public static class PlayerProfileAggregator
{
    private const string BUCKET_SHORT = "short";
    private const string BUCKET_MID = "mid";
    private const string BUCKET_LONG = "long";

    private class HitDodgeCount
    {
        public int Hits;
        public int Dodges;
        public int Total => Hits + Dodges;
        public float DodgeRate => Total > 0 ? (float)Dodges / Total : 0f;
    }

    private class RangeCount
    {
        public int MeleeCount;
        public int Total;
        public float MeleeRatio => Total > 0 ? (float)MeleeCount / Total : 0f;
    }

    private class LeftRightCount
    {
        public int Left;
        public int Right;
        public int Total => Left + Right;
        public float LeftRatio => Total > 0 ? (float)Left / Total : 0f;
    }

    // 인덱스 0 = 스테이지1, 1 = 스테이지2. stage 3(보스 자신)은 이 5축 집계에 쓰지 않는다(명세서 4.1 범위 밖).
    private static readonly HitDodgeCount[,] _reaction = NewReactionGrid();
    private static readonly RangeCount[] _range = { new RangeCount(), new RangeCount() };
    private static readonly LeftRightCount[] _dodgeBias = { new LeftRightCount(), new LeftRightCount() };
    private static readonly int[] _punishCountByStage = new int[2];
    private static readonly int[] _recoveryEntriesByStage = new int[2]; // = 그 스테이지의 hit+dodge 총합("후딜 진입 횟수")

    private static readonly HitDodgeCount _scarecrow = new HitDodgeCount(); // 허수아비 (항상 스테이지1)
    private static readonly HitDodgeCount _byeoruge = new HitDodgeCount();  // 벼루게 (항상 스테이지2)

    static PlayerProfileAggregator()
    {
        Telemetry.OnAttackHit += HandleAttackHit;
        Telemetry.OnAttackDodged += HandleAttackDodged;
        Telemetry.OnPlayerPunish += HandlePlayerPunish;
        Telemetry.OnPlayerPositionSample += HandlePlayerPositionSample;
    }

    // 첫 씬이 로드되기 전에 정적 생성자(=Telemetry 구독)가 반드시 실행되게 하는 훅.
    // 이게 없으면 보스방 진입 전까지 이 클래스의 static 멤버를 아무도 안 건드릴 수 있고,
    // 그러면 스테이지1 텔레메트리 이벤트가 구독자 없이 그냥 흘러가버릴 위험이 있다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitializeBeforeFirstScene()
    {
        // 이 메서드가 호출되는 것만으로 static 생성자가 이미 실행된 상태가 보장된다.
    }

    // 새 게임 시작 시 백지로 되돌리기 위한 리셋 (M2/M9에서 사용).
    public static void ResetAll()
    {
        for (int s = 0; s < 2; s++)
        {
            for (int b = 0; b < 3; b++)
            {
                _reaction[s, b].Hits = 0;
                _reaction[s, b].Dodges = 0;
            }
            _range[s].MeleeCount = 0;
            _range[s].Total = 0;
            _dodgeBias[s].Left = 0;
            _dodgeBias[s].Right = 0;
            _punishCountByStage[s] = 0;
            _recoveryEntriesByStage[s] = 0;
        }
        _scarecrow.Hits = 0; _scarecrow.Dodges = 0;
        _byeoruge.Hits = 0; _byeoruge.Dodges = 0;
    }

    public static PlayerProfile GetCurrentProfile()
    {
        var profile = new PlayerProfile();
        ComputeReaction(profile.reaction);
        ComputeRange(profile.range);
        ComputeTankerComparison(profile.tankerComparison);
        ComputePunish(profile.punish);
        ComputeDodgeBias(profile.dodgeBias);
        return profile;
    }

    // ===== 세이브/로드 (M2, BossDataPersistence가 사용) =====

    public static PlayerProfileRawState ExportRawState()
    {
        var state = new PlayerProfileRawState();
        for (int s = 0; s < 2; s++)
        {
            for (int b = 0; b < 3; b++)
            {
                int flat = s * 3 + b;
                state.reactionHits[flat] = _reaction[s, b].Hits;
                state.reactionDodges[flat] = _reaction[s, b].Dodges;
            }
            state.rangeMeleeCount[s] = _range[s].MeleeCount;
            state.rangeTotal[s] = _range[s].Total;
            state.dodgeBiasLeft[s] = _dodgeBias[s].Left;
            state.dodgeBiasRight[s] = _dodgeBias[s].Right;
            state.punishCountByStage[s] = _punishCountByStage[s];
            state.recoveryEntriesByStage[s] = _recoveryEntriesByStage[s];
        }
        state.scarecrowHits = _scarecrow.Hits;
        state.scarecrowDodges = _scarecrow.Dodges;
        state.byeorugeHits = _byeoruge.Hits;
        state.byeorugeDodges = _byeoruge.Dodges;
        return state;
    }

    // 구조가 깨진(길이 불일치/누락) 상태가 들어오면 저장을 건너뛰고 백지 유지 (손상 세이브 대비).
    public static void ImportRawState(PlayerProfileRawState state)
    {
        ResetAll();
        if (state == null || !state.IsStructurallyValid()) return;

        for (int s = 0; s < 2; s++)
        {
            for (int b = 0; b < 3; b++)
            {
                int flat = s * 3 + b;
                _reaction[s, b].Hits = state.reactionHits[flat];
                _reaction[s, b].Dodges = state.reactionDodges[flat];
            }
            _range[s].MeleeCount = state.rangeMeleeCount[s];
            _range[s].Total = state.rangeTotal[s];
            _dodgeBias[s].Left = state.dodgeBiasLeft[s];
            _dodgeBias[s].Right = state.dodgeBiasRight[s];
            _punishCountByStage[s] = state.punishCountByStage[s];
            _recoveryEntriesByStage[s] = state.recoveryEntriesByStage[s];
        }
        _scarecrow.Hits = state.scarecrowHits;
        _scarecrow.Dodges = state.scarecrowDodges;
        _byeoruge.Hits = state.byeorugeHits;
        _byeoruge.Dodges = state.byeorugeDodges;
    }

    // ===== 이벤트 핸들러 =====

    private static void HandleAttackHit(string monsterType, float telegraphDuration, int stage)
    {
        int idx = StageIndex(stage);
        if (idx < 0) return;

        if (ProfileTierCalculator.IsReactionEligible(telegraphDuration))
            _reaction[idx, BucketIndex(telegraphDuration)].Hits++;
        _recoveryEntriesByStage[idx]++;

        if (monsterType == "허수아비") _scarecrow.Hits++;
        else if (monsterType == "벼루게") _byeoruge.Hits++;
    }

    private static void HandleAttackDodged(string monsterType, float telegraphDuration, string dodgeDirection, int stage)
    {
        int idx = StageIndex(stage);
        if (idx < 0) return;

        if (ProfileTierCalculator.IsReactionEligible(telegraphDuration))
            _reaction[idx, BucketIndex(telegraphDuration)].Dodges++;
        _recoveryEntriesByStage[idx]++;

        if (monsterType == "허수아비") _scarecrow.Dodges++;
        else if (monsterType == "벼루게") _byeoruge.Dodges++;

        // "none"은 회피편향 분모에서 제외(명세서 4.2)
        if (dodgeDirection == "left") _dodgeBias[idx].Left++;
        else if (dodgeDirection == "right") _dodgeBias[idx].Right++;
    }

    private static void HandlePlayerPunish(string monsterType, int stage)
    {
        int idx = StageIndex(stage);
        if (idx < 0) return;
        _punishCountByStage[idx]++;
    }

    private static void HandlePlayerPositionSample(string monsterType, float distance, bool inMeleeRange, int stage)
    {
        int idx = StageIndex(stage);
        if (idx < 0) return;
        _range[idx].Total++;
        if (inMeleeRange) _range[idx].MeleeCount++;
    }

    // ===== 축별 계산 =====

    private static void ComputeReaction(ReactionAxis axis)
    {
        float avgS1 = AverageNonEmptyBucketRate(0, out int sampleS1, out bool minPerBucketOkS1);
        float avgS2 = AverageNonEmptyBucketRate(1, out int sampleS2, out bool minPerBucketOkS2);

        int totalSample = sampleS1 + sampleS2;
        bool enoughSample = totalSample >= ProfileTierCalculator.REACTION_MIN_SAMPLE_TOTAL || (minPerBucketOkS1 && minPerBucketOkS2);

        axis.shortSuccessRate = BucketRateAcrossStages(BucketIndex(0.1f)); // 대표값(0.1s=short 버킷)
        axis.midSuccessRate = BucketRateAcrossStages(BucketIndex(0.4f));
        axis.longSuccessRate = BucketRateAcrossStages(BucketIndex(0.6f));
        axis.sample = totalSample;

        float weightedAvg = ProfileTierCalculator.WeightedAverage(avgS1, sampleS1, avgS2, sampleS2);
        axis.tier = enoughSample ? ProfileTierCalculator.ReactionTier(weightedAvg) : "mid";
    }

    private static float AverageNonEmptyBucketRate(int stageIdx, out int totalSample, out bool everyNonEmptyBucketMeetsMin)
    {
        float sum = 0f;
        int nonEmptyBuckets = 0;
        totalSample = 0;
        everyNonEmptyBucketMeetsMin = true;

        for (int b = 0; b < 3; b++)
        {
            var c = _reaction[stageIdx, b];
            if (c.Total <= 0) continue;

            sum += c.DodgeRate;
            nonEmptyBuckets++;
            totalSample += c.Total;
            if (c.Total < ProfileTierCalculator.REACTION_MIN_SAMPLE_PER_BUCKET) everyNonEmptyBucketMeetsMin = false;
        }

        return nonEmptyBuckets > 0 ? sum / nonEmptyBuckets : 0f;
    }

    private static float BucketRateAcrossStages(int bucketIdx)
    {
        var s1 = _reaction[0, bucketIdx];
        var s2 = _reaction[1, bucketIdx];
        return ProfileTierCalculator.WeightedAverage(s1.DodgeRate, s1.Total, s2.DodgeRate, s2.Total);
    }

    private static void ComputeRange(RangeAxis axis)
    {
        int totalSample = _range[0].Total + _range[1].Total;
        bool enoughSample = totalSample >= ProfileTierCalculator.RANGE_MIN_SAMPLE_TICKS;

        float weightedRatio = ProfileTierCalculator.WeightedAverage(
            _range[0].MeleeRatio, _range[0].Total, _range[1].MeleeRatio, _range[1].Total);

        axis.meleeRatio = weightedRatio;
        axis.sample = totalSample;
        axis.tier = enoughSample ? ProfileTierCalculator.RangeTier(weightedRatio) : "mid";
    }

    private static void ComputeTankerComparison(TankerComparisonAxis axis)
    {
        axis.scarecrow.rate = _scarecrow.Total > 0 ? (float)_scarecrow.Hits / _scarecrow.Total : 0f; // 피격률
        axis.scarecrow.sample = _scarecrow.Total;

        axis.byeoruge.rate = _byeoruge.Total > 0 ? (float)_byeoruge.Hits / _byeoruge.Total : 0f;
        axis.byeoruge.sample = _byeoruge.Total;

        bool enoughSample = _scarecrow.Total >= ProfileTierCalculator.TANKER_MIN_SAMPLE_PER_MONSTER
                          && _byeoruge.Total >= ProfileTierCalculator.TANKER_MIN_SAMPLE_PER_MONSTER;

        if (!enoughSample)
        {
            axis.weightedFavor = "데이터 부족";
            return;
        }

        // 명세서 4.4: 허수아비=스테이지1 가중치 1.0, 벼루게=스테이지2 가중치 1.8을 각자의 피격률에 곱해서 비교
        float weightedScarecrow = axis.scarecrow.rate * 1.0f;
        float weightedByeoruge = axis.byeoruge.rate * 1.8f;
        axis.weightedFavor = weightedByeoruge >= weightedScarecrow ? "벼루게" : "허수아비";
    }

    private static void ComputePunish(PunishAxis axis)
    {
        int totalRecovery = _recoveryEntriesByStage[0] + _recoveryEntriesByStage[1];
        bool enoughSample = totalRecovery >= ProfileTierCalculator.PUNISH_MIN_SAMPLE;

        float rateS1 = _recoveryEntriesByStage[0] > 0 ? (float)_punishCountByStage[0] / _recoveryEntriesByStage[0] : 0f;
        float rateS2 = _recoveryEntriesByStage[1] > 0 ? (float)_punishCountByStage[1] / _recoveryEntriesByStage[1] : 0f;
        float weightedRate = ProfileTierCalculator.WeightedAverage(rateS1, _recoveryEntriesByStage[0], rateS2, _recoveryEntriesByStage[1]);

        axis.rate = weightedRate;
        axis.sample = totalRecovery;
        axis.tier = enoughSample ? ProfileTierCalculator.PunishTier(weightedRate) : "mid";
    }

    private static void ComputeDodgeBias(DodgeBiasAxis axis)
    {
        int totalEligible = _dodgeBias[0].Total + _dodgeBias[1].Total; // "none" 제외한 좌/우 표본만
        bool enoughSample = totalEligible >= ProfileTierCalculator.DODGE_BIAS_MIN_SAMPLE;

        float weightedLeftRatio = ProfileTierCalculator.WeightedAverage(
            _dodgeBias[0].LeftRatio, _dodgeBias[0].Total, _dodgeBias[1].LeftRatio, _dodgeBias[1].Total);

        axis.leftRatio = weightedLeftRatio;
        axis.sample = totalEligible;
        axis.tier = enoughSample ? ProfileTierCalculator.DodgeBiasTier(weightedLeftRatio) : "none";
    }

    // ===== 유틸 =====

    private static int StageIndex(int stage)
    {
        if (stage == 1) return 0;
        if (stage == 2) return 1;
        return -1; // 스테이지3(보스 자신) 등은 이 5축 집계 대상이 아님
    }

    private static int BucketIndex(float telegraphDuration)
    {
        if (telegraphDuration < 0.3f) return 0; // short
        if (telegraphDuration <= 0.5f) return 1; // mid
        return 2; // long
    }

    private static HitDodgeCount[,] NewReactionGrid()
    {
        var grid = new HitDodgeCount[2, 3];
        for (int s = 0; s < 2; s++)
            for (int b = 0; b < 3; b++)
                grid[s, b] = new HitDodgeCount();
        return grid;
    }
}
