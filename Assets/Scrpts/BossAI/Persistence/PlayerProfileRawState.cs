using System;

// PlayerProfileAggregator의 내부 누적 카운터(원본, 파생 전) 스냅샷.
// PlayerProfile(명세서 4.1 파생값)만 저장하면 스테이지별 세부 표본이 사라져서
// 다음 세션에서 가중평균(명세서 4.4)이 처음부터 다시 시작돼버린다 — 그래서
// 정확한 세션 간 이어붙이기를 위해 raw 카운터 자체를 별도로 저장/복원한다.
// index 0=스테이지1, 1=스테이지2. reaction 계열은 [스테이지*3 + 버킷(0=short,1=mid,2=long)]으로 평탄화.
[Serializable]
public class PlayerProfileRawState
{
    public int[] reactionHits = new int[6];
    public int[] reactionDodges = new int[6];
    public int[] rangeMeleeCount = new int[2];
    public int[] rangeTotal = new int[2];
    public int[] dodgeBiasLeft = new int[2];
    public int[] dodgeBiasRight = new int[2];
    public int[] punishCountByStage = new int[2];
    public int[] recoveryEntriesByStage = new int[2];
    public int scarecrowHits;
    public int scarecrowDodges;
    public int byeorugeHits;
    public int byeorugeDodges;

    public bool IsStructurallyValid()
    {
        return reactionHits != null && reactionHits.Length == 6
            && reactionDodges != null && reactionDodges.Length == 6
            && rangeMeleeCount != null && rangeMeleeCount.Length == 2
            && rangeTotal != null && rangeTotal.Length == 2
            && dodgeBiasLeft != null && dodgeBiasLeft.Length == 2
            && dodgeBiasRight != null && dodgeBiasRight.Length == 2
            && punishCountByStage != null && punishCountByStage.Length == 2
            && recoveryEntriesByStage != null && recoveryEntriesByStage.Length == 2;
    }
}
